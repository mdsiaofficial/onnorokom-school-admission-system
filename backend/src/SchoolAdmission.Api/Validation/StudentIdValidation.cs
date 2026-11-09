using System.Text.RegularExpressions;
using SchoolAdmission.Api.Domain.Exceptions;

namespace SchoolAdmission.Api.Validation;

public static partial class StudentIdValidation
{
    [GeneratedRegex("^[0-9]{17}$", RegexOptions.Compiled)]
    private static partial Regex StudentIdRegex();

    public static string ExtractAndValidate(HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.TryGetValue("X-Student-Id", out var values) || values.Count == 0)
        {
            throw ProblemException.BadRequest("INVALID_STUDENT_ID", "Header 'X-Student-Id' is required.");
        }

        var studentId = values.ToString().Trim();
        if (string.IsNullOrEmpty(studentId) || !StudentIdRegex().IsMatch(studentId))
        {
            throw ProblemException.BadRequest("INVALID_STUDENT_ID", "Student ID must be exactly 17 digits.");
        }

        return studentId;
    }
}
