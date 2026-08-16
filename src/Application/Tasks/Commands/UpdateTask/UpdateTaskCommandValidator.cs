using FluentValidation;

namespace TaskFlow.Application.Tasks.Commands.UpdateTask;

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        // A past due date is rejected in the domain only when it's a *change*, so it isn't
        // validated here (editing an unrelated field on a task whose due date has since passed
        // must not fail). See TaskItem.UpdateDetails.
    }
}
