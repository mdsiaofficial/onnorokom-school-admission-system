using SchoolAdmission.Api.Application.Services;
using SchoolAdmission.Api.Validation;

namespace SchoolAdmission.Api.Endpoints;

public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/me").WithTags("Student");

        group.MapGet("/holds", async (
            HttpContext httpContext,
            IHoldService holdService,
            CancellationToken cancellationToken) =>
        {
            var studentId = StudentIdValidation.ExtractAndValidate(httpContext);
            var results = await holdService.GetStudentHoldsAsync(studentId, cancellationToken);
            return Results.Ok(results);
        })
        .WithName("GetMyHolds")
        .Produces<IReadOnlyList<Contracts.Responses.StudentHoldResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        return routes;
    }
}
