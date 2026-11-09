using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using SchoolAdmission.Api.Contracts.Requests;
using SchoolAdmission.Api.Domain.Exceptions;
using SchoolAdmission.Api.Validation;
using Xunit;

namespace SchoolAdmission.Tests;

public class ValidationTests
{
    private readonly CreateClassRequestValidator _validator = new();

    [Theory]
    [InlineData("Class 1", 50)]
    [InlineData("Class 10 - Morning Shift", 1)]
    [InlineData("A", 100000)]
    public void CreateClassRequest_ValidInput_Passes(string name, int totalSeats)
    {
        var model = new CreateClassRequest(name, totalSeats);
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateClassRequest_InvalidName_Fails(string? name)
    {
        var model = new CreateClassRequest(name!, 50);
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateClassRequest_NameExceeds120Chars_Fails()
    {
        var model = new CreateClassRequest(new string('A', 121), 50);
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    [InlineData(100001)]
    public void CreateClassRequest_InvalidSeats_Fails(int seats)
    {
        var model = new CreateClassRequest("Valid Name", seats);
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.TotalSeats);
    }

    [Fact]
    public void StudentIdValidation_MissingHeader_ThrowsProblemException()
    {
        var context = new DefaultHttpContext();

        var ex = Assert.Throws<ProblemException>(() => StudentIdValidation.ExtractAndValidate(context));
        Assert.Equal("INVALID_STUDENT_ID", ex.Code);
        Assert.Equal(400, ex.StatusCode);
    }

    [Theory]
    [InlineData("1234567890123456")]     // 16 digits
    [InlineData("123456789012345678")]   // 18 digits
    [InlineData("1234567890123456a")]   // 17 chars but has letter
    [InlineData("                 ")]   // 17 spaces
    [InlineData("")]                    // empty
    public void StudentIdValidation_InvalidFormat_ThrowsProblemException(string badId)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Student-Id"] = badId;

        var ex = Assert.Throws<ProblemException>(() => StudentIdValidation.ExtractAndValidate(context));
        Assert.Equal("INVALID_STUDENT_ID", ex.Code);
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public void StudentIdValidation_Valid17Digits_ReturnsTrimmedId()
    {
        var context = new DefaultHttpContext();
        const string validId = "12345678901234567";
        context.Request.Headers["X-Student-Id"] = $"  {validId}  ";

        var result = StudentIdValidation.ExtractAndValidate(context);
        Assert.Equal(validId, result);
    }
}
