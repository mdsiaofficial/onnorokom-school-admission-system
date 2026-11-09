namespace SchoolAdmission.Api.Contracts.Responses;

public record HoldResponse(
    string HoldId,
    string ClassId,
    string StudentId,
    string Status,
    DateTimeOffset ExpiresAt);

public record HoldStatusResponse(
    string HoldId,
    string ClassId,
    string Status,
    DateTimeOffset ExpiresAt);

public record ConfirmHoldResponse(
    string HoldId,
    string Status);

public record StudentHoldResponse(
    string HoldId,
    string ClassId,
    string ClassName,
    string Status,
    DateTimeOffset ExpiresAt);
