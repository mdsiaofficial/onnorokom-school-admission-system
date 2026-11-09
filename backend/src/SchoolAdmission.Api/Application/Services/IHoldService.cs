using SchoolAdmission.Api.Contracts.Responses;

namespace SchoolAdmission.Api.Application.Services;

public interface IHoldService
{
    Task<HoldResponse> ApplyHoldAsync(Guid classId, string studentId, CancellationToken cancellationToken = default);
    Task<HoldStatusResponse> GetHoldByIdAsync(Guid holdId, CancellationToken cancellationToken = default);
    Task<ConfirmHoldResponse> ConfirmHoldAsync(Guid holdId, string studentId, CancellationToken cancellationToken = default);
    Task CancelHoldAsync(Guid holdId, string studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentHoldResponse>> GetStudentHoldsAsync(string studentId, CancellationToken cancellationToken = default);
}
