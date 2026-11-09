using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Domain.Entities;
using SchoolAdmission.Api.Domain.Enums;
using SchoolAdmission.Api.Infrastructure.Persistence;

namespace SchoolAdmission.Api.Infrastructure.Database;

public class DemoDataSeeder
{
    private static readonly (string Name, int TotalSeats, int ConfirmedSeats)[] DemoClasses = new[]
    {
        ("Class 1 - Morning Shift", 60, 12),
        ("Class 2 - Morning Shift", 60, 20),
        ("Class 3 - Morning Shift", 60, 0),
        ("Class 4 - Day Shift", 50, 10),
        ("Class 5 - Morning Shift", 80, 30),
        ("Class 6 - Morning Shift", 120, 80),
        ("Class 6 - Day Shift", 100, 45),
        ("Class 7 - Morning Shift", 100, 0),
        ("Class 8 - Day Shift", 90, 60),
        ("Class 9 - Morning Shift", 80, 79),
        ("Class 10 - Morning Shift", 70, 70),
        ("Demo - Only 2 Seats", 2, 0)
    };

    public static async Task SeedAsync(AppDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Resetting and re-seeding demo classes and holds for startup...");

        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE holds, classes CASCADE;", cancellationToken);

        var now = DateTimeOffset.UtcNow;
        long studentRunningId = 1;

        foreach (var (name, totalSeats, confirmedCount) in DemoClasses)
        {
            var classEntity = new Class
            {
                Id = Guid.CreateVersion7(),
                Name = name,
                TotalSeats = totalSeats,
                CreatedAt = now
            };

            context.Classes.Add(classEntity);

            for (int i = 0; i < confirmedCount; i++)
            {
                var studentId = $"9999{studentRunningId:D13}";
                studentRunningId++;

                var hold = new Hold
                {
                    Id = Guid.CreateVersion7(),
                    ClassId = classEntity.Id,
                    StudentId = studentId,
                    Status = HoldStatus.CONFIRMED,
                    CreatedAt = now.AddHours(-1),
                    ExpiresAt = now.AddHours(-1).AddMinutes(10),
                    ConfirmedAt = now.AddHours(-1),
                    UpdatedAt = now.AddHours(-1)
                };

                context.Holds.Add(hold);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Demo data seeding completed successfully with 12 classes.");
    }
}
