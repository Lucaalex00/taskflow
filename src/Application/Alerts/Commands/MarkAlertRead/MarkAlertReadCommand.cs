using MediatR;

namespace TaskFlow.Application.Alerts.Commands.MarkAlertRead;

public sealed record MarkAlertReadCommand(Guid AlertId) : IRequest;
