using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using SchoolAdmission.Api.Domain.Exceptions;

namespace SchoolAdmission.Api.Middleware;

public class ProblemDetailsExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ProblemDetailsExceptionHandler> _logger;

    public ProblemDetailsExceptionHandler(ILogger<ProblemDetailsExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int statusCode;
        string code;
        string title;
        string detail;

        if (exception is ProblemException problemEx)
        {
            statusCode = problemEx.StatusCode;
            code = problemEx.Code;
            title = problemEx.Title;
            detail = problemEx.Detail;
        }
        else if (exception is BadHttpRequestException badHttpEx)
        {
            statusCode = StatusCodes.Status400BadRequest;
            code = "BAD_REQUEST";
            title = "Bad Request";
            detail = badHttpEx.Message;
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
            statusCode = StatusCodes.Status500InternalServerError;
            code = "INTERNAL_SERVER_ERROR";
            title = "Internal Server Error";
            detail = "An unexpected error occurred.";
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var response = new Dictionary<string, object?>
        {
            ["type"] = $"https://httpstatuses.com/{statusCode}",
            ["title"] = title,
            ["status"] = statusCode,
            ["code"] = code,
            ["detail"] = detail
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions), cancellationToken);
        return true;
    }
}
