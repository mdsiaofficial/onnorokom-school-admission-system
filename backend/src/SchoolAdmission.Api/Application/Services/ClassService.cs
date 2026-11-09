using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Contracts.Requests;
using SchoolAdmission.Api.Contracts.Responses;
using SchoolAdmission.Api.Domain.Entities;
using SchoolAdmission.Api.Domain.Enums;
using SchoolAdmission.Api.Domain.Exceptions;
using SchoolAdmission.Api.Infrastructure.Persistence;

namespace SchoolAdmission.Api.Application.Services;

public class ClassService : IClassService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ClassService> _logger;

    public ClassService(AppDbContext context, ILogger<ClassService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ClassResponse> CreateClassAsync(CreateClassRequest request, CancellationToken cancellationToken = default)
    {
        var trimmedName = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName) || trimmedName.Length > 120 || request.TotalSeats <= 0)
        {
            throw ProblemException.BadRequest("INVALID_CLASS_DATA", "Name must be 1-120 characters and totalSeats must be greater than 0.");
        }

        var classEntity = new Class
        {
            Id = Guid.CreateVersion7(),
            Name = trimmedName,
            TotalSeats = request.TotalSeats,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Classes.Add(classEntity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created class {ClassId} ({Name}) with {TotalSeats} seats.", classEntity.Id, classEntity.Name, classEntity.TotalSeats);

        return new ClassResponse(
            classEntity.Id.ToString(),
            classEntity.Name,
            classEntity.TotalSeats);
    }

    public async Task<IReadOnlyList<ClassSummaryResponse>> GetClassesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var list = await _context.Classes
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,
                HeldSeats = c.Holds.Count(h => h.Status == HoldStatus.HELD && h.ExpiresAt > now),
                ConfirmedSeats = c.Holds.Count(h => h.Status == HoldStatus.CONFIRMED)
            })
            .Select(c => new ClassSummaryResponse(
                c.Id.ToString(),
                c.Name,
                c.TotalSeats,
                c.HeldSeats,
                c.ConfirmedSeats,
                Math.Max(0, c.TotalSeats - (c.HeldSeats + c.ConfirmedSeats))))
            .ToListAsync(cancellationToken);

        return list;
    }

    public async Task<ClassSummaryResponse> GetClassByIdAsync(Guid classId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var item = await _context.Classes
            .AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,
                HeldSeats = c.Holds.Count(h => h.Status == HoldStatus.HELD && h.ExpiresAt > now),
                ConfirmedSeats = c.Holds.Count(h => h.Status == HoldStatus.CONFIRMED)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (item == null)
        {
            throw ProblemException.NotFound("CLASS_NOT_FOUND", $"Class with ID '{classId}' was not found.");
        }

        return new ClassSummaryResponse(
            item.Id.ToString(),
            item.Name,
            item.TotalSeats,
            item.HeldSeats,
            item.ConfirmedSeats,
            Math.Max(0, item.TotalSeats - (item.HeldSeats + item.ConfirmedSeats)));
    }
}
