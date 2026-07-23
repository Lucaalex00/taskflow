using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Boards.Commands.CreateBoard;

public sealed class CreateBoardCommandHandler(ITaskFlowDbContext context)
    : IRequestHandler<CreateBoardCommand, Guid>
{
    public async Task<Guid> Handle(CreateBoardCommand request, CancellationToken cancellationToken)
    {
        var ownerExists = await context.Users.AnyAsync(u => u.Id == request.OwnerId, cancellationToken);
        if (!ownerExists)
            throw new NotFoundException(nameof(Domain.Entities.User), request.OwnerId);

        var result = ProjectBoard.Create(request.Name, request.OwnerId);

        if (!result.IsSuccess)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Name), result.Error)
            ]);

        context.Boards.Add(result.Value);
        await context.SaveChangesAsync(cancellationToken);

        return result.Value.Id;
    }
}
