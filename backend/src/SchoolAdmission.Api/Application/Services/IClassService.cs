using SchoolAdmission.Api.Contracts.Requests;
using SchoolAdmission.Api.Contracts.Responses;

namespace SchoolAdmission.Api.Application.Services;

public interface IClassService
{
    Task<ClassResponse> CreateClassAsync(CreateClassRequest request, CancellationToken cancellationToken = default);
    Task<ClassSummaryResponse> UpdateClassAsync(Guid classId, UpdateClassRequest request, CancellationToken cancellationToken = default);
    Task DeleteClassAsync(Guid classId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClassSummaryResponse>> GetClassesAsync(CancellationToken cancellationToken = default);
    Task<ClassSummaryResponse> GetClassByIdAsync(Guid classId, CancellationToken cancellationToken = default);
}
