using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Alerts;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Infrastructure.Workers;

/// <summary>
/// Periodically (1) snapshots every board's current workload into LoadMetric history,
/// then (2) runs every enabled AlertRule through its matching strategy evaluator and
/// raises + broadcasts any resulting Alert. This is the "anomaly detection" feature
/// described in the project brief.
/// </summary>
public sealed class LoadMonitorWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<LoadMonitorOptions> options,
    ILogger<LoadMonitorWorker> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(options.Value.IntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("LoadMonitorWorker started, interval = {Interval}", _interval);

        using var timer = new PeriodicTimer(_interval);

        do
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A single failed cycle (e.g. transient DB hiccup) must not kill the worker —
                // it should simply try again next tick.
                logger.LogError(ex, "LoadMonitorWorker cycle failed, will retry next interval.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ITaskFlowDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var notifier = scope.ServiceProvider.GetRequiredService<IAlertNotifier>();
        var evaluators = scope.ServiceProvider.GetServices<IAlertRuleEvaluator>()
            .ToDictionary(e => e.RuleType);

        var now = clock.UtcNow;
        var boardIds = await context.Boards.Select(b => b.Id).ToListAsync(cancellationToken);

        foreach (var boardId in boardIds)
        {
            await SnapshotBoardLoadAsync(boardId, context, now, cancellationToken);
            await EvaluateBoardRulesAsync(boardId, context, clock, notifier, evaluators, cancellationToken);
        }

        logger.LogInformation("LoadMonitorWorker cycle completed for {BoardCount} board(s).", boardIds.Count);
    }

    private static async Task SnapshotBoardLoadAsync(
        Guid boardId, ITaskFlowDbContext context, DateTime now, CancellationToken cancellationToken)
    {
        var activeTasks = await context.Tasks
            .Where(t => t.BoardId == boardId && t.State != TaskState.Done && t.State != TaskState.Cancelled)
            .ToListAsync(cancellationToken);

        var snapshot = LoadMetric.Capture(
            boardId,
            activeTaskCount: activeTasks.Count,
            overdueTaskCount: activeTasks.Count(t => t.IsOverdue(now)),
            inProgressTaskCount: activeTasks.Count(t => t.State == TaskState.InProgress));

        context.LoadMetrics.Add(snapshot);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task EvaluateBoardRulesAsync(
        Guid boardId,
        ITaskFlowDbContext context,
        IDateTimeProvider clock,
        IAlertNotifier notifier,
        IReadOnlyDictionary<AlertRuleType, IAlertRuleEvaluator> evaluators,
        CancellationToken cancellationToken)
    {
        var rules = await context.AlertRules
            .Where(r => r.BoardId == boardId && r.IsEnabled)
            .ToListAsync(cancellationToken);

        foreach (var rule in rules)
        {
            if (!evaluators.TryGetValue(rule.RuleType, out var evaluator))
            {
                logger.LogWarning("No evaluator registered for rule type {RuleType}, skipping.", rule.RuleType);
                continue;
            }

            var results = await evaluator.EvaluateAsync(rule, context, clock, cancellationToken);

            foreach (var result in results)
            {
                if (await WasRecentlyRaisedAsync(rule, result.RelatedUserId, context, clock, cancellationToken))
                    continue; // Avoid re-notifying every cycle for the same standing condition.

                var alertResult = Alert.Create(boardId, rule.Id, result.Severity, result.Message, result.RelatedUserId);
                if (!alertResult.IsSuccess)
                    continue;

                context.Alerts.Add(alertResult.Value);
                await context.SaveChangesAsync(cancellationToken);
                await notifier.NotifyBoardAsync(alertResult.Value, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Suppresses duplicate alerts: if this exact rule (and, when applicable, the same user)
    /// already fired within its own evaluation window, don't raise it again on every tick.
    /// </summary>
    private static Task<bool> WasRecentlyRaisedAsync(
        AlertRule rule, Guid? relatedUserId, ITaskFlowDbContext context, IDateTimeProvider clock,
        CancellationToken cancellationToken)
    {
        var windowStart = clock.UtcNow.AddMinutes(-rule.EvaluationWindowMinutes);

        return context.Alerts.AnyAsync(a =>
                a.AlertRuleId == rule.Id
                && a.RelatedUserId == relatedUserId
                && a.CreatedAtUtc >= windowStart,
            cancellationToken);
    }
}
