using FluentValidation;

namespace TaskFlow.Application.Tasks.Commands.CreateTask;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.DueAtUtc)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .When(x => x.DueAtUtc is not null)
            .WithMessage("Due date cannot be in the past.");
    }
}
