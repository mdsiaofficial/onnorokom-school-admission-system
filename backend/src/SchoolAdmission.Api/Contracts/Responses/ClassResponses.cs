namespace SchoolAdmission.Api.Contracts.Responses;

public record ClassResponse(string Id, string Name, int TotalSeats);

public record ClassSummaryResponse(
    string Id,
    string Name,
    int TotalSeats,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats);
