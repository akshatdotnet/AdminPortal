using FluentValidation;
using PMS.Application.DTOs.Task;
using PMS.Domain.Constants;

namespace PMS.Application.Validators.Task;

public class CreateTaskValidator : AbstractValidator<CreateTaskDto>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required.")
            .MaximumLength(DomainConstants.Task.TitleMaxLength)
            .WithMessage($"Title must not exceed {DomainConstants.Task.TitleMaxLength} characters.");

        RuleFor(x => x.Description)
            .MaximumLength(DomainConstants.Task.DescriptionMaxLength)
            .WithMessage($"Description must not exceed {DomainConstants.Task.DescriptionMaxLength} characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.ProjectId)
            .GreaterThan(0).WithMessage("A valid project must be selected.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid task status.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid task priority.");

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Due date cannot be in the past.")
            .When(x => x.DueDate.HasValue);
    }
}