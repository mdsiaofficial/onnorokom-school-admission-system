using SchoolAdmission.Api.Domain.Enums;

namespace SchoolAdmission.Api.Domain.Entities;

public class Hold
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid ClassId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public HoldStatus Status { get; set; } = HoldStatus.HELD;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Class Class { get; set; } = null!;
}
