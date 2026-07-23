using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.AlertRules.Commands.CreateAlertRule;

public sealed class CreateAlertRuleCommandHandler(ITaskFlowDbContext context, IBoardAuthorizer boardAuthorizer)
    : IRequestHandler<CreateAlertRuleCommand, Guid>
{
    public async Task<Guid> Handle(CreateAlertRuleCommand request, CancellationToken cancellationToken)
    {
        var boardExists = await context.Boards.AnyAsync(b => b.Id == request.BoardId, cancellationToken);
        if (!boardExists)
            throw new NotFoundException(nameof(ProjectBoard), request.BoardId);

        // Rule thresholds affect what fires for the whole board, so only the Owner configures them.
        await boardAuthorizer.EnsureOwnerAsync(request.BoardId, cancellationToken);

        var result = AlertRule.Create(
            request.BoardId, request.RuleType, request.Threshold, request.EvaluationWindowMinutes);

        if (!result.IsSuccess)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(nameof(request.Threshold), result.Error)
            ]);

        context.AlertRules.Add(result.Value);
        await context.SaveChangesAsync(cancellationToken);

        return result.Value.Id;
    }
}
