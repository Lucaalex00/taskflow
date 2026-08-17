using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Workers;

/// <summary>
/// Asks <see cref="DemoWorkspaceResetter"/> whether the demo workspace has gone stale, on a
/// short poll rather than on a long timer set to the reset interval itself: a demo host that
/// sleeps between visits would never survive long enough to reach a weekly tick, whereas a
/// short poll re-evaluates a persisted age every time the process is awake.
/// </summary>
public sealed class DemoResetWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DemoResetWorker> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        if (!scope.ServiceProvider.GetRequiredService<DemoWorkspaceResetter>().IsEnabled)
            return; // not a self-refreshing demo instance — don't even start the loop

        logger.LogWarning(
            "Demo auto-reset is ENABLED: this database will be wiped and re-seeded once the "
            + "demo workspace is older than Seed:ResetIntervalHours. Never enable this on an "
            + "instance holding data you care about.");

        using var timer = new PeriodicTimer(CheckInterval);

        do
        {
            try
            {
                using var cycleScope = scopeFactory.CreateScope();
                var resetter = cycleScope.ServiceProvider.GetRequiredService<DemoWorkspaceResetter>();
                await resetter.ResetIfStaleAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Same reasoning as LoadMonitorWorker: one failed cycle (a transient database
                // hiccup, a host waking up mid-query) must not take the loop down for good.
                logger.LogError(ex, "Demo reset check failed, will retry next interval.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
