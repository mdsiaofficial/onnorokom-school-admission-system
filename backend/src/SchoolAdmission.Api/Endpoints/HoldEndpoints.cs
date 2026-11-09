using SchoolAdmission.Api.Application.Services;
using SchoolAdmission.Api.Domain.Exceptions;
using SchoolAdmission.Api.Validation;

namespace SchoolAdmission.Api.Endpoints;

public static class HoldEndpoints
{
    public static IEndpointRouteBuilder MapHoldEndpoints(this IEndpointRouteBuilder routes)
    {
        // POST /classes/{classId}/holds
        routes.MapPost("/classes/{classId}/holds", async (
            string classId,
            HttpContext httpContext,
            IHoldService holdService,
            CancellationToken cancellationToken) =>
        {
            var studentId = StudentIdValidation.ExtractAndValidate(httpContext);

            if (!Guid.TryParse(classId, out var parsedClassId))
            {
                throw ProblemException.NotFound("CLASS_NOT_FOUND", $"Class with ID '{classId}' was not found.");
            }

            var result = await holdService.ApplyHoldAsync(parsedClassId, studentId, cancellationToken);
            return Results.Created($"/holds/{result.HoldId}", result);
        })
        .WithTags("Holds")
        .WithName("ApplyHold")
        .Produces<Contracts.Responses.HoldResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        var holdGroup = routes.MapGroup("/holds").WithTags("Holds");

        // GET /holds/{holdId}
        holdGroup.MapGet("/{holdId}", async (
            string holdId,
            IHoldService holdService,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(holdId, out var parsedHoldId))
            {
                throw ProblemException.NotFound("HOLD_NOT_FOUND", $"Hold with ID '{holdId}' was not found.");
            }

            var result = await holdService.GetHoldByIdAsync(parsedHoldId, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetHoldById")
        .Produces<Contracts.Responses.HoldStatusResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // POST /holds/{holdId}/confirm
        holdGroup.MapPost("/{holdId}/confirm", async (
            string holdId,
            HttpContext httpContext,
            IHoldService holdService,
            CancellationToken cancellationToken) =>
        {
            var studentId = StudentIdValidation.ExtractAndValidate(httpContext);

            if (!Guid.TryParse(holdId, out var parsedHoldId))
            {
                throw ProblemException.NotFound("HOLD_NOT_FOUND", $"Hold with ID '{holdId}' was not found.");
            }

            var result = await holdService.ConfirmHoldAsync(parsedHoldId, studentId, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("ConfirmHold")
        .Produces<Contracts.Responses.ConfirmHoldResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        // DELETE /holds/{holdId}
        holdGroup.MapDelete("/{holdId}", async (
            string holdId,
            HttpContext httpContext,
            IHoldService holdService,
            CancellationToken cancellationToken) =>
        {
            var studentId = StudentIdValidation.ExtractAndValidate(httpContext);

            if (!Guid.TryParse(holdId, out var parsedHoldId))
            {
                throw ProblemException.NotFound("HOLD_NOT_FOUND", $"Hold with ID '{holdId}' was not found.");
            }

            await holdService.CancelHoldAsync(parsedHoldId, studentId, cancellationToken);
            return Results.NoContent();
        })
        .WithName("CancelHold")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        return routes;
    }
}
