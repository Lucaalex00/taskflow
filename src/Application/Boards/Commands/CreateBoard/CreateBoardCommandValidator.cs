using FluentValidation;

namespace TaskFlow.Application.Boards.Commands.CreateBoard;

public sealed class CreateBoardCommandValidator : AbstractValidator<CreateBoardCommand>
{
    public CreateBoardCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.OwnerId).NotEmpty();
    }
}
