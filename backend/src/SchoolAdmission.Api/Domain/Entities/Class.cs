namespace SchoolAdmission.Api.Domain.Entities;

public class Class
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Hold> Holds { get; set; } = new List<Hold>();
}
