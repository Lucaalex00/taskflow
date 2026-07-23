using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Alerts;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Infrastructure.Workers.AlertEvaluators;

/// <summary>Raises an alert for each user juggling more "InProgress" tasks than the threshold.</summary>
public sealed class ConcurrentInProgressThresholdEvaluator : IAlertRuleEvaluator
{
    public AlertRuleType RuleType => AlertRuleType.ConcurrentInProgressThreshold;

    public async Task<IReadOnlyList<AlertEvaluationResult>> EvaluateAsync(
        AlertRule rule, ITaskFlowDbContext context, IDateTimeProvider clock, CancellationToken cancellationToken)
    {
        var inProgressCountsByAssignee = await context.Tasks
            .Where(t => t.BoardId == rule.BoardId
                        && t.AssigneeId != null
                        && t.State == TaskState.InProgress)
            .GroupBy(t => t.AssigneeId)
            .Select(g => new { AssigneeId = g.Key!.Value, Count = g.Count() })
            .Where(x => x.Count > rule.Threshold)
            .ToListAsync(cancellationToken);

        return inProgressCountsByAssignee
            .Select(x => new AlertEvaluationResult(
                AlertSeverity.Info,
                $"User has {x.Count} tasks in progress at once, exceeding the threshold of " +
                $"{rule.Threshold}. Possible context-switching risk.",
                x.AssigneeId))
            .ToList();
    }
}
