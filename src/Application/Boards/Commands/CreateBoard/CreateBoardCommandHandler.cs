using MediatR;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Boards.Commands.CreateBoard;

public sealed class CreateBoardCommandHandler(ITaskFlowDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<CreateBoardCommand, Guid>
{
    public async Task<Guid> Handle(CreateBoardCommand request, CancellationToken cancellationToken)
    {
        var result = ProjectBoard.Create(request.Name, currentUser.UserId, request.Color);

        if (!result.IsSuccess)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Name), result.Error)
            ]);

        var membership = BoardMember.Create(result.Value.Id, currentUser.UserId, BoardRole.Owner);

        context.Boards.Add(result.Value);
        context.BoardMembers.Add(membership.Value);
        await context.SaveChangesAsync(cancellationToken);

        return result.Value.Id;
    }
}
