using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Alerts.Commands.MarkAlertRead;

public sealed class MarkAlertReadCommandHandler(ITaskFlowDbContext context, IBoardAuthorizer boardAuthorizer)
    : IRequestHandler<MarkAlertReadCommand>
{
    public async Task Handle(MarkAlertReadCommand request, CancellationToken cancellationToken)
    {
        var alert = await context.Alerts
            .FirstOrDefaultAsync(a => a.Id == request.AlertId, cancellationToken)
            ?? throw new NotFoundException(nameof(Alert), request.AlertId);

        await boardAuthorizer.EnsureMemberAsync(alert.BoardId, cancellationToken);

        alert.MarkAsRead();
        await context.SaveChangesAsync(cancellationToken);
    }
}
