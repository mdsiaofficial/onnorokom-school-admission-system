using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolAdmission.Api.Infrastructure.Persistence;

namespace SchoolAdmission.Api.Infrastructure.Database;

public static class DatabaseMigrator
{
    private const long AdvisoryLockId = 8371946282914102941L;

    public static async Task MigrateAndSeedAsync(IServiceProvider serviceProvider, IConfiguration configuration, ILogger logger, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        logger.LogInformation("Waiting for database connection...");
        const int maxAttempts = 30;
        int attempt = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            attempt++;
            try
            {
                if (await context.Database.CanConnectAsync(cancellationToken))
                {
                    logger.LogInformation("Database connection established.");
                    break;
                }
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Database connection attempt {Attempt}/{MaxAttempts} failed. Retrying in 1s...", attempt, maxAttempts);
            }

            if (attempt >= maxAttempts)
            {
                throw new TimeoutException($"Failed to connect to database after {maxAttempts} attempts.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        logger.LogInformation("Acquiring Postgres advisory lock {LockId} for migration...", AdvisoryLockId);
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT pg_advisory_lock(@lockId);";
            cmd.Parameters.Add(new NpgsqlParameter("lockId", AdvisoryLockId));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        try
        {
            logger.LogInformation("Applying EF Core migrations...");
            await context.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("EF Core migrations applied successfully.");

            var seedDemoData = configuration.GetValue("SEED_DEMO_DATA", true);
            if (seedDemoData)
            {
                await DemoDataSeeder.SeedAsync(context, logger, cancellationToken);
            }
            else
            {
                logger.LogInformation("SEED_DEMO_DATA is false; skipping seeder.");
            }
        }
        finally
        {
            logger.LogInformation("Releasing Postgres advisory lock {LockId}...", AdvisoryLockId);
            try
            {
                await using var unlockCmd = connection.CreateCommand();
                unlockCmd.CommandText = "SELECT pg_advisory_unlock(@lockId);";
                unlockCmd.Parameters.Add(new NpgsqlParameter("lockId", AdvisoryLockId));
                await unlockCmd.ExecuteNonQueryAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to release advisory lock {LockId}.", AdvisoryLockId);
            }
        }
    }
}
