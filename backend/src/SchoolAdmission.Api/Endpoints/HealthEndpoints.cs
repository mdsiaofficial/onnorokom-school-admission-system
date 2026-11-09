using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Infrastructure.Persistence;

namespace SchoolAdmission.Api.Endpoints;

public static class HealthEndpoints
{
    private static readonly string[] HealthMethods = ["GET", "HEAD"];

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/health").WithTags("Health");

        group.MapMethods("/live", HealthMethods, () => Results.Ok(new { status = "Healthy" }))
            .WithName("HealthLive")
            .Produces(StatusCodes.Status200OK);

        group.MapMethods("/ready", HealthMethods, async (AppDbContext context, CancellationToken cancellationToken) =>
        {
            try
            {
                var canConnect = await context.Database.CanConnectAsync(cancellationToken);
                if (canConnect)
                {
                    return Results.Ok(new { status = "Healthy" });
                }
                return Results.Json(new { status = "Unhealthy", error = "Database connection check returned false" }, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (Exception ex)
            {
                return Results.Json(new { status = "Unhealthy", error = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("HealthReady")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status503ServiceUnavailable);

        return routes;
    }
}
