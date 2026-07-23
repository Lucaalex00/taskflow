using MediatR;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.AlertRules.Commands.CreateAlertRule;

public sealed record CreateAlertRuleCommand(
    Guid BoardId,
    AlertRuleType RuleType,
    int Threshold,
    int EvaluationWindowMinutes) : IRequest<Guid>;
