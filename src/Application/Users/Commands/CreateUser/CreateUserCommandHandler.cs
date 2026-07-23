using MediatR;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler(ITaskFlowDbContext context)
    : IRequestHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var result = User.Create(request.Email, request.DisplayName);

        if (!result.IsSuccess)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Email), result.Error)
            ]);

        context.Users.Add(result.Value);
        await context.SaveChangesAsync(cancellationToken);

        return result.Value.Id;
    }
}
