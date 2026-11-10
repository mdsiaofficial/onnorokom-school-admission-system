using FluentValidation;
using SchoolAdmission.Api.Contracts.Requests;

namespace SchoolAdmission.Api.Validation;

public class UpdateClassRequestValidator : AbstractValidator<UpdateClassRequest>
{
    public UpdateClassRequestValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Name is required.")
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Name cannot be empty or whitespace.")
            .Length(1, 120).WithMessage("Name must be between 1 and 120 characters.");

        RuleFor(x => x.TotalSeats)
            .InclusiveBetween(1, 100000)
            .WithMessage("Total seats must be an integer between 1 and 100000.");
    }
}
