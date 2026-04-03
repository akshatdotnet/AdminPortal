using FluentValidation;
using PMS.Application.DTOs.Project;
using PMS.Domain.Constants;

namespace PMS.Application.Validators.Project;

public class CreateProjectValidator : AbstractValidator<CreateProjectDto>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(DomainConstants.Project.NameMaxLength)
            .WithMessage($"Name must not exceed {DomainConstants.Project.NameMaxLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(DomainConstants.Project.DescriptionMaxLength)
            .WithMessage($"Description must not exceed {DomainConstants.Project.DescriptionMaxLength} characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date.")
            .When(x => x.EndDate.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid project status.");
    }
}