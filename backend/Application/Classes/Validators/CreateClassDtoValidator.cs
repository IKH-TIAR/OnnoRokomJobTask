using FluentValidation;
using SchoolAdmission.Application.Classes.Dtos;
using SchoolAdmission.Domain.Entities;

namespace SchoolAdmission.Application.Classes.Validators;

public class CreateClassDtoValidator : AbstractValidator<CreateClassDto>
{
    public CreateClassDtoValidator()
    {
        RuleFor(x => x.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Class name is required.")
            .MaximumLength(SchoolClass.NameMaxLength)
            .WithMessage($"Class name must be at most {SchoolClass.NameMaxLength} characters.");

        RuleFor(x => x.TotalSeats)
            .NotNull().WithMessage("Total seats is required.")
            .InclusiveBetween(1, SchoolClass.MaxSeats)
            .WithMessage($"Total seats must be between 1 and {SchoolClass.MaxSeats}.");
    }
}
