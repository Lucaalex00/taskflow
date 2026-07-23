using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Boards.Commands.AddBoardMember;

public sealed class AddBoardMemberCommandHandler(ITaskFlowDbContext context, IBoardAuthorizer boardAuthorizer)
    : IRequestHandler<AddBoardMemberCommand>
{
    public async Task Handle(AddBoardMemberCommand request, CancellationToken cancellationToken)
    {
        await boardAuthorizer.EnsureOwnerAsync(request.BoardId, cancellationToken);

        var userExists = await context.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!userExists)
            throw new NotFoundException(nameof(Domain.Entities.User), request.UserId);

        var alreadyMember = await context.BoardMembers
            .AnyAsync(m => m.BoardId == request.BoardId && m.UserId == request.UserId, cancellationToken);
        if (alreadyMember)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.UserId), "This user is already a member of the board.")
            ]);

        var result = BoardMember.Create(request.BoardId, request.UserId, request.Role);

        if (!result.IsSuccess)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.UserId), result.Error)
            ]);

        context.BoardMembers.Add(result.Value);
        await context.SaveChangesAsync(cancellationToken);
    }
}
