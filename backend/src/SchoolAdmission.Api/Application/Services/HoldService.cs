using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Npgsql;
using SchoolAdmission.Api.Application.Options;
using SchoolAdmission.Api.Contracts.Responses;
using SchoolAdmission.Api.Domain.Entities;
using SchoolAdmission.Api.Domain.Enums;
using SchoolAdmission.Api.Domain.Exceptions;
using SchoolAdmission.Api.Infrastructure.Persistence;

namespace SchoolAdmission.Api.Application.Services;

public class HoldService : IHoldService
{
    private readonly AppDbContext _context;
    private readonly IOptions<HoldOptions> _holdOptions;
    private readonly ILogger<HoldService> _logger;

    public HoldService(
        AppDbContext context,
        IOptions<HoldOptions> holdOptions,
        ILogger<HoldService> logger)
    {
        _context = context;
        _holdOptions = holdOptions;
        _logger = logger;
    }

    public async Task<HoldResponse> ApplyHoldAsync(Guid classId, string studentId, CancellationToken cancellationToken = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var conn = (NpgsqlConnection)_context.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken);
        }

        var dbTx = tx.GetDbTransaction() as NpgsqlTransaction;

        // 1. Lock class row and read DB clock
        int totalSeats;
        DateTimeOffset now;
        await using (var lockCmd = conn.CreateCommand())
        {
            lockCmd.Transaction = dbTx;
            lockCmd.CommandText = "SELECT id, total_seats, clock_timestamp() AS now FROM classes WHERE id = @id FOR UPDATE;";
            lockCmd.Parameters.Add(new NpgsqlParameter("id", classId));
            await using var reader = await lockCmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw ProblemException.NotFound("CLASS_NOT_FOUND", $"Class with ID '{classId}' was not found.");
            }
            totalSeats = reader.GetInt32(1);
            now = reader.GetFieldValue<DateTimeOffset>(2);
        }

        // 2. Mark this student's stale HELD rows in this class as EXPIRED
        await using (var cleanupCmd = conn.CreateCommand())
        {
            cleanupCmd.Transaction = dbTx;
            cleanupCmd.CommandText = @"UPDATE holds 
                                      SET status = 'EXPIRED', updated_at = @now 
                                      WHERE class_id = @classId 
                                        AND student_id = @studentId 
                                        AND status = 'HELD' 
                                        AND expires_at <= @now;";
            cleanupCmd.Parameters.Add(new NpgsqlParameter("now", now));
            cleanupCmd.Parameters.Add(new NpgsqlParameter("classId", classId));
            cleanupCmd.Parameters.Add(new NpgsqlParameter("studentId", studentId));
            await cleanupCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // 3. Check if student already has any active HELD or CONFIRMED hold across the system (one application per student)
        await using (var checkCmd = conn.CreateCommand())
        {
            checkCmd.Transaction = dbTx;
            checkCmd.CommandText = @"SELECT class_id, status 
                                     FROM holds 
                                     WHERE student_id = @studentId 
                                       AND (status = 'CONFIRMED' OR (status = 'HELD' AND expires_at > @now))
                                     LIMIT 1;";
            checkCmd.Parameters.Add(new NpgsqlParameter("studentId", studentId));
            checkCmd.Parameters.Add(new NpgsqlParameter("now", now));
            await using var checkReader = await checkCmd.ExecuteReaderAsync(cancellationToken);
            if (await checkReader.ReadAsync(cancellationToken))
            {
                var existingClassId = checkReader.GetGuid(0);
                var existingStatus = checkReader.GetString(1);
                if (existingStatus == "CONFIRMED")
                {
                    throw ProblemException.Conflict("ALREADY_CONFIRMED", 
                        existingClassId == classId 
                            ? "You have already confirmed admission for this class."
                            : "This student birth registration already has a confirmed admission in another class. One application allowed per student.");
                }
                if (existingStatus == "HELD")
                {
                    throw ProblemException.Conflict("ALREADY_HELD", 
                        existingClassId == classId
                            ? "You already have an active hold for this class."
                            : "This student birth registration already has an active seat application in progress. Complete or cancel it first.");
                }
            }
        }

        // 4. Count active held + confirmed seats in this class
        await using (var countCmd = conn.CreateCommand())
        {
            countCmd.Transaction = dbTx;
            countCmd.CommandText = @"SELECT 
                COUNT(*) FILTER (WHERE status = 'HELD' AND expires_at > @now) AS active_held,
                COUNT(*) FILTER (WHERE status = 'CONFIRMED') AS confirmed
                FROM holds 
                WHERE class_id = @classId;";
            countCmd.Parameters.Add(new NpgsqlParameter("classId", classId));
            countCmd.Parameters.Add(new NpgsqlParameter("now", now));
            await using var countReader = await countCmd.ExecuteReaderAsync(cancellationToken);
            if (await countReader.ReadAsync(cancellationToken))
            {
                var activeHeld = countReader.GetInt64(0);
                var confirmed = countReader.GetInt64(1);
                if (activeHeld + confirmed >= totalSeats)
                {
                    throw ProblemException.Conflict("SEATS_FULL", "No seats are available in this class.");
                }
            }
        }

        // 5. Insert new HELD hold
        var holdDurationSeconds = _holdOptions.Value.DurationSeconds;
        var expiresAt = now.AddSeconds(holdDurationSeconds);
        var holdId = Guid.CreateVersion7();

        await using (var insertCmd = conn.CreateCommand())
        {
            insertCmd.Transaction = dbTx;
            insertCmd.CommandText = @"INSERT INTO holds (id, class_id, student_id, status, created_at, expires_at, updated_at) 
                                     VALUES (@id, @classId, @studentId, 'HELD', @now, @expiresAt, @now);";
            insertCmd.Parameters.Add(new NpgsqlParameter("id", holdId));
            insertCmd.Parameters.Add(new NpgsqlParameter("classId", classId));
            insertCmd.Parameters.Add(new NpgsqlParameter("studentId", studentId));
            insertCmd.Parameters.Add(new NpgsqlParameter("now", now));
            insertCmd.Parameters.Add(new NpgsqlParameter("expiresAt", expiresAt));

            try
            {
                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw ProblemException.Conflict("ALREADY_HELD", "You already have an active hold for this class.");
            }
        }

        _logger.LogInformation("Hold {HoldId} created for student {StudentId} in class {ClassId}. Expires at {ExpiresAt}.", holdId, studentId, classId, expiresAt);

        return new HoldResponse(
            holdId.ToString(),
            classId.ToString(),
            studentId,
            "HELD",
            expiresAt);
    }

    public async Task<HoldStatusResponse> GetHoldByIdAsync(Guid holdId, CancellationToken cancellationToken = default)
    {
        var hold = await _context.Holds
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == holdId, cancellationToken);

        if (hold == null)
        {
            throw ProblemException.NotFound("HOLD_NOT_FOUND", $"Hold with ID '{holdId}' was not found.");
        }

        var now = DateTimeOffset.UtcNow;
        var statusStr = (hold.Status == HoldStatus.HELD && hold.ExpiresAt <= now)
            ? "EXPIRED"
            : hold.Status.ToString();

        return new HoldStatusResponse(
            hold.Id.ToString(),
            hold.ClassId.ToString(),
            statusStr,
            hold.ExpiresAt);
    }

    public async Task<ConfirmHoldResponse> ConfirmHoldAsync(Guid holdId, string studentId, CancellationToken cancellationToken = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var conn = (NpgsqlConnection)_context.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken);
        }

        var dbTx = tx.GetDbTransaction() as NpgsqlTransaction;

        // 1. Initial hold lookup
        Guid classId;
        string ownerStudentId;
        await using (var findCmd = conn.CreateCommand())
        {
            findCmd.Transaction = dbTx;
            findCmd.CommandText = "SELECT class_id, student_id FROM holds WHERE id = @id;";
            findCmd.Parameters.Add(new NpgsqlParameter("id", holdId));
            await using var reader = await findCmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw ProblemException.NotFound("HOLD_NOT_FOUND", $"Hold with ID '{holdId}' was not found.");
            }
            classId = reader.GetGuid(0);
            ownerStudentId = reader.GetString(1).Trim();
        }

        // 2. Ownership check
        if (ownerStudentId != studentId)
        {
            throw ProblemException.Forbidden("NOT_HOLD_OWNER", "You are not the owner of this hold.");
        }

        // 3. Lock class row and read DB clock
        DateTimeOffset now;
        await using (var lockCmd = conn.CreateCommand())
        {
            lockCmd.Transaction = dbTx;
            lockCmd.CommandText = "SELECT id, total_seats, clock_timestamp() AS now FROM classes WHERE id = @id FOR UPDATE;";
            lockCmd.Parameters.Add(new NpgsqlParameter("id", classId));
            await using var reader = await lockCmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw ProblemException.NotFound("CLASS_NOT_FOUND", "Associated class not found.");
            }
            now = reader.GetFieldValue<DateTimeOffset>(2);
        }

        // 4. Re-read hold under lock
        string currentStatus;
        DateTimeOffset currentExpiresAt;
        await using (var reReadCmd = conn.CreateCommand())
        {
            reReadCmd.Transaction = dbTx;
            reReadCmd.CommandText = "SELECT status, expires_at FROM holds WHERE id = @id FOR UPDATE;";
            reReadCmd.Parameters.Add(new NpgsqlParameter("id", holdId));
            await using var reader = await reReadCmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw ProblemException.NotFound("HOLD_NOT_FOUND", $"Hold with ID '{holdId}' was not found.");
            }
            currentStatus = reader.GetString(0);
            currentExpiresAt = reader.GetFieldValue<DateTimeOffset>(1);
        }

        // 5. Check status transitions
        if (currentStatus == "CONFIRMED")
        {
            await tx.CommitAsync(cancellationToken);
            return new ConfirmHoldResponse(holdId.ToString(), "CONFIRMED");
        }

        if (currentStatus == "CANCELLED")
        {
            throw ProblemException.Conflict("HOLD_CANCELLED", "Seat hold was cancelled.");
        }

        if (currentStatus == "EXPIRED" || (currentStatus == "HELD" && currentExpiresAt <= now))
        {
            if (currentStatus == "HELD")
            {
                await using var expireCmd = conn.CreateCommand();
                expireCmd.Transaction = dbTx;
                expireCmd.CommandText = "UPDATE holds SET status = 'EXPIRED', updated_at = @now WHERE id = @id;";
                expireCmd.Parameters.Add(new NpgsqlParameter("now", now));
                expireCmd.Parameters.Add(new NpgsqlParameter("id", holdId));
                await expireCmd.ExecuteNonQueryAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            throw ProblemException.Conflict("HOLD_EXPIRED", "Seat hold has expired.");
        }

        // 6. Confirm the hold
        await using (var updateCmd = conn.CreateCommand())
        {
            updateCmd.Transaction = dbTx;
            updateCmd.CommandText = @"UPDATE holds 
                                     SET status = 'CONFIRMED', confirmed_at = @now, updated_at = @now 
                                     WHERE id = @id;";
            updateCmd.Parameters.Add(new NpgsqlParameter("now", now));
            updateCmd.Parameters.Add(new NpgsqlParameter("id", holdId));
            await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
        _logger.LogInformation("Hold {HoldId} successfully confirmed for student {StudentId}.", holdId, studentId);

        return new ConfirmHoldResponse(holdId.ToString(), "CONFIRMED");
    }

    public async Task CancelHoldAsync(Guid holdId, string studentId, CancellationToken cancellationToken = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var conn = (NpgsqlConnection)_context.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync(cancellationToken);
        }

        var dbTx = tx.GetDbTransaction() as NpgsqlTransaction;

        // 1. Initial hold lookup
        Guid classId;
        string ownerStudentId;
        await using (var findCmd = conn.CreateCommand())
        {
            findCmd.Transaction = dbTx;
            findCmd.CommandText = "SELECT class_id, student_id FROM holds WHERE id = @id;";
            findCmd.Parameters.Add(new NpgsqlParameter("id", holdId));
            await using var reader = await findCmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw ProblemException.NotFound("HOLD_NOT_FOUND", $"Hold with ID '{holdId}' was not found.");
            }
            classId = reader.GetGuid(0);
            ownerStudentId = reader.GetString(1).Trim();
        }

        // 2. Ownership check
        if (ownerStudentId != studentId)
        {
            throw ProblemException.Forbidden("NOT_HOLD_OWNER", "You are not the owner of this hold.");
        }

        // 3. Lock class row and read DB clock
        DateTimeOffset now;
        await using (var lockCmd = conn.CreateCommand())
        {
            lockCmd.Transaction = dbTx;
            lockCmd.CommandText = "SELECT id, total_seats, clock_timestamp() AS now FROM classes WHERE id = @id FOR UPDATE;";
            lockCmd.Parameters.Add(new NpgsqlParameter("id", classId));
            await using var reader = await lockCmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw ProblemException.NotFound("CLASS_NOT_FOUND", "Associated class not found.");
            }
            now = reader.GetFieldValue<DateTimeOffset>(2);
        }

        // 4. Re-read hold under lock
        string currentStatus;
        DateTimeOffset currentExpiresAt;
        await using (var reReadCmd = conn.CreateCommand())
        {
            reReadCmd.Transaction = dbTx;
            reReadCmd.CommandText = "SELECT status, expires_at FROM holds WHERE id = @id FOR UPDATE;";
            reReadCmd.Parameters.Add(new NpgsqlParameter("id", holdId));
            await using var reader = await reReadCmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw ProblemException.NotFound("HOLD_NOT_FOUND", $"Hold with ID '{holdId}' was not found.");
            }
            currentStatus = reader.GetString(0);
            currentExpiresAt = reader.GetFieldValue<DateTimeOffset>(1);
        }

        // 5. Evaluate status transitions
        if (currentStatus == "CONFIRMED")
        {
            throw ProblemException.Conflict("HOLD_ALREADY_CONFIRMED", "Confirmed holds cannot be cancelled.");
        }

        if (currentStatus == "CANCELLED" || currentStatus == "EXPIRED")
        {
            // Idempotent 204
            await tx.CommitAsync(cancellationToken);
            return;
        }

        if (currentStatus == "HELD")
        {
            if (currentExpiresAt <= now)
            {
                // Expired hold freed seat automatically; update status to EXPIRED
                await using var expireCmd = conn.CreateCommand();
                expireCmd.Transaction = dbTx;
                expireCmd.CommandText = "UPDATE holds SET status = 'EXPIRED', updated_at = @now WHERE id = @id;";
                expireCmd.Parameters.Add(new NpgsqlParameter("now", now));
                expireCmd.Parameters.Add(new NpgsqlParameter("id", holdId));
                await expireCmd.ExecuteNonQueryAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return;
            }

            await using var cancelCmd = conn.CreateCommand();
            cancelCmd.Transaction = dbTx;
            cancelCmd.CommandText = @"UPDATE holds 
                                     SET status = 'CANCELLED', cancelled_at = @now, updated_at = @now 
                                     WHERE id = @id;";
            cancelCmd.Parameters.Add(new NpgsqlParameter("now", now));
            cancelCmd.Parameters.Add(new NpgsqlParameter("id", holdId));
            await cancelCmd.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            _logger.LogInformation("Hold {HoldId} cancelled by student {StudentId}.", holdId, studentId);
        }
    }

    public async Task<IReadOnlyList<StudentHoldResponse>> GetStudentHoldsAsync(string studentId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var list = await _context.Holds
            .AsNoTracking()
            .Where(h => h.StudentId == studentId)
            .Include(h => h.Class)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);

        return list.Select(h =>
        {
            var derivedStatus = (h.Status == HoldStatus.HELD && h.ExpiresAt <= now)
                ? "EXPIRED"
                : h.Status.ToString();

            return new StudentHoldResponse(
                h.Id.ToString(),
                h.ClassId.ToString(),
                h.Class.Name,
                derivedStatus,
                h.ExpiresAt);
        }).ToList();
    }
}
