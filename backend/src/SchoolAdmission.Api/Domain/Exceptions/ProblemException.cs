namespace SchoolAdmission.Api.Domain.Exceptions;

public class ProblemException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }
    public string Title { get; }
    public string Detail { get; }

    public ProblemException(int statusCode, string code, string title, string detail)
        : base(detail)
    {
        StatusCode = statusCode;
        Code = code;
        Title = title;
        Detail = detail;
    }

    public static ProblemException NotFound(string code, string detail) =>
        new(StatusCodes.Status404NotFound, code, "Not Found", detail);

    public static ProblemException BadRequest(string code, string detail) =>
        new(StatusCodes.Status400BadRequest, code, "Bad Request", detail);

    public static ProblemException Forbidden(string code, string detail) =>
        new(StatusCodes.Status403Forbidden, code, "Forbidden", detail);

    public static ProblemException Conflict(string code, string detail) =>
        new(StatusCodes.Status409Conflict, code, "Conflict", detail);
}
