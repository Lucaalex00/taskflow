using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Alerts;

/// <summary>Outcome of evaluating a rule: null means "condition not met, no alert".</summary>
public sealed record AlertEvaluationResult(AlertSeverity Severity, string Message, Guid? RelatedUserId);

/// <summary>
/// Strategy interface — one implementation per AlertRuleType. The worker looks up
/// the matching evaluator for each enabled rule instead of a giant switch statement,
/// so a new rule type is added by dropping in a new class, never editing this one.
/// </summary>
public interface IAlertRuleEvaluator
{
    AlertRuleType RuleType { get; }

    /// <summary>Empty list means "condition not met for anyone, no alerts to raise".</summary>
    Task<IReadOnlyList<AlertEvaluationResult>> EvaluateAsync(
        AlertRule rule,
        ITaskFlowDbContext context,
        IDateTimeProvider clock,
        CancellationToken cancellationToken);
}
