using FluentValidation;
using SchoolAdmission.Api.Application.Services;
using SchoolAdmission.Api.Contracts.Requests;
using SchoolAdmission.Api.Domain.Exceptions;

namespace SchoolAdmission.Api.Endpoints;

public static class ClassEndpoints
{
    public static IEndpointRouteBuilder MapClassEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/classes").WithTags("Classes");

        group.MapPost("", async (
            CreateClassRequest request,
            IValidator<CreateClassRequest> validator,
            IClassService classService,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMsg = validationResult.Errors.First().ErrorMessage;
                throw ProblemException.BadRequest("INVALID_CLASS_DATA", errorMsg);
            }

            var result = await classService.CreateClassAsync(request, cancellationToken);
            return Results.Created($"/classes/{result.Id}", result);
        })
        .WithName("CreateClass")
        .Produces<Contracts.Responses.ClassResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{classId}", async (
            string classId,
            UpdateClassRequest request,
            IValidator<UpdateClassRequest> validator,
            IClassService classService,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(classId, out var parsedId))
            {
                throw ProblemException.NotFound("CLASS_NOT_FOUND", $"Class with ID '{classId}' was not found.");
            }

            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMsg = validationResult.Errors.First().ErrorMessage;
                throw ProblemException.BadRequest("INVALID_CLASS_DATA", errorMsg);
            }

            var result = await classService.UpdateClassAsync(parsedId, request, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("UpdateClass")
        .Produces<Contracts.Responses.ClassSummaryResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{classId}", async (
            string classId,
            IClassService classService,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(classId, out var parsedId))
            {
                throw ProblemException.NotFound("CLASS_NOT_FOUND", $"Class with ID '{classId}' was not found.");
            }

            await classService.DeleteClassAsync(parsedId, cancellationToken);
            return Results.NoContent();
        })
        .WithName("DeleteClass")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("", async (
            IClassService classService,
            CancellationToken cancellationToken) =>
        {
            var results = await classService.GetClassesAsync(cancellationToken);
            return Results.Ok(results);
        })
        .WithName("GetClasses")
        .Produces<IReadOnlyList<Contracts.Responses.ClassSummaryResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{classId}", async (
            string classId,
            IClassService classService,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(classId, out var parsedId))
            {
                throw ProblemException.NotFound("CLASS_NOT_FOUND", $"Class with ID '{classId}' was not found.");
            }

            var result = await classService.GetClassByIdAsync(parsedId, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetClassById")
        .Produces<Contracts.Responses.ClassSummaryResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return routes;
    }
}
