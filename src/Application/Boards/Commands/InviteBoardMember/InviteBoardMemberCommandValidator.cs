using FluentValidation;

namespace TaskFlow.Application.Boards.Commands.InviteBoardMember;

public sealed class InviteBoardMemberCommandValidator : AbstractValidator<InviteBoardMemberCommand>
{
    public InviteBoardMemberCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
