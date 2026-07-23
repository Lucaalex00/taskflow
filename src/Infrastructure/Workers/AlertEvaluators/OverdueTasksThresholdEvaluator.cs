using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Alerts;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Infrastructure.Workers.AlertEvaluators;

/// <summary>Raises an alert for each user who has more overdue tasks than the rule's threshold.</summary>
public sealed class OverdueTasksThresholdEvaluator : IAlertRuleEvaluator
{
    public AlertRuleType RuleType => AlertRuleType.OverdueTasksThreshold;

    public async Task<IReadOnlyList<AlertEvaluationResult>> EvaluateAsync(
        AlertRule rule, ITaskFlowDbContext context, IDateTimeProvider clock, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        var overdueCountsByAssignee = await context.Tasks
            .Where(t => t.BoardId == rule.BoardId
                        && t.AssigneeId != null
                        && t.DueAtUtc != null
                        && t.DueAtUtc < now
                        && t.State != TaskState.Done
                        && t.State != TaskState.Cancelled)
            .GroupBy(t => t.AssigneeId)
            .Select(g => new { AssigneeId = g.Key!.Value, Count = g.Count() })
            .Where(x => x.Count > rule.Threshold)
            .ToListAsync(cancellationToken);

        return overdueCountsByAssignee
            .Select(x => new AlertEvaluationResult(
                AlertSeverity.Warning,
                $"User has {x.Count} overdue tasks, exceeding the threshold of {rule.Threshold}.",
                x.AssigneeId))
            .ToList();
    }
}
