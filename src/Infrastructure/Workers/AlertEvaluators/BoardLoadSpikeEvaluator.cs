using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Alerts;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Infrastructure.Workers.AlertEvaluators;

/// <summary>
/// Compares the current active-task count against the closest snapshot taken
/// at least EvaluationWindowMinutes ago. Fires when growth exceeds the threshold
/// (Threshold is interpreted as a percentage, e.g. 50 = 50% growth).
/// </summary>
public sealed class BoardLoadSpikeEvaluator : IAlertRuleEvaluator
{
    public AlertRuleType RuleType => AlertRuleType.BoardLoadSpike;

    public async Task<IReadOnlyList<AlertEvaluationResult>> EvaluateAsync(
        AlertRule rule, ITaskFlowDbContext context, IDateTimeProvider clock, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var windowStart = now.AddMinutes(-rule.EvaluationWindowMinutes);

        var baselineSnapshot = await context.LoadMetrics
            .Where(m => m.BoardId == rule.BoardId && m.SnapshotAtUtc <= windowStart)
            .OrderByDescending(m => m.SnapshotAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        // No baseline yet (board too new / worker just started) — nothing to compare against.
        if (baselineSnapshot is null || baselineSnapshot.ActiveTaskCount == 0)
            return [];

        var currentActiveCount = await context.Tasks.CountAsync(
            t => t.BoardId == rule.BoardId && t.State != TaskState.Done && t.State != TaskState.Cancelled,
            cancellationToken);

        var growthPercent = (currentActiveCount - baselineSnapshot.ActiveTaskCount) * 100.0
                             / baselineSnapshot.ActiveTaskCount;

        if (growthPercent <= rule.Threshold)
            return [];

        return
        [
            new AlertEvaluationResult(
                AlertSeverity.Critical,
                $"Active task count grew by {growthPercent:F0}% in the last {rule.EvaluationWindowMinutes} " +
                $"minutes ({baselineSnapshot.ActiveTaskCount} -> {currentActiveCount}), exceeding the " +
                $"{rule.Threshold}% threshold.",
                RelatedUserId: null)
        ];
    }
}
