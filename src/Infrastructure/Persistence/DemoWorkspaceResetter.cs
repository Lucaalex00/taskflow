using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Infrastructure.Persistence;

/// <summary>
/// Returns a demo instance to its pristine seeded state on a schedule, so a link that's been
/// sitting in someone's inbox — or in a CV — still shows the intended workspace rather than
/// whatever previous visitors left behind.
///
/// The staleness clock is the demo owner's own <c>CreatedAtUtc</c>: it's set when the seeder
/// runs and by definition survives restarts, which a timer started at boot would not (a demo
/// host that sleeps between visits might otherwise never reach its first tick).
/// </summary>
public sealed class DemoWorkspaceResetter(
    TaskFlowDbContext context,
    DemoDataSeeder seeder,
    IDateTimeProvider clock,
    IOptions<SeedOptions> options,
    ILogger<DemoWorkspaceResetter> logger)
{
    /// <summary>True only when this instance is a demo that was told to refresh itself.</summary>
    public bool IsEnabled => options.Value.Enabled && options.Value.ResetIntervalHours > 0;

    /// <summary>
    /// Wipes and re-seeds if the workspace has aged past the configured interval. Returns true
    /// when a reset actually happened, so the caller can log it once rather than every check.
    /// </summary>
    public async Task<bool> ResetIfStaleAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            return false;

        var owner = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == DemoDataSeeder.OwnerEmail, cancellationToken);

        // No demo owner means this isn't a seeded demo instance (someone pointed a real
        // database at it, or the seeder skipped a non-empty one). Never wipe that.
        if (owner is null)
            return false;

        var age = clock.UtcNow - owner.CreatedAtUtc;
        var interval = TimeSpan.FromHours(options.Value.ResetIntervalHours);
        if (age < interval)
            return false;

        logger.LogInformation(
            "Demo workspace is {Age} old (limit {Interval}) — wiping and re-seeding.", age, interval);

        await WipeAsync(cancellationToken);
        await seeder.SeedAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Deletes everything, including accounts visitors registered themselves — that's the point
    /// of a demo reset. Order is explicit and follows the foreign keys inwards-out, since these
    /// entities reference each other by plain Guid rather than by navigation properties, so EF
    /// can't work the ordering out on its own.
    /// </summary>
    private async Task WipeAsync(CancellationToken cancellationToken)
    {
        await context.Alerts.ExecuteDeleteAsync(cancellationToken);
        await context.LoadMetrics.ExecuteDeleteAsync(cancellationToken);
        await context.AlertRules.ExecuteDeleteAsync(cancellationToken);
        await context.Notifications.ExecuteDeleteAsync(cancellationToken);
        await context.BoardInvitations.ExecuteDeleteAsync(cancellationToken);
        await context.Tasks.ExecuteDeleteAsync(cancellationToken);
        await context.BoardMembers.ExecuteDeleteAsync(cancellationToken);
        await context.Boards.ExecuteDeleteAsync(cancellationToken);
        await context.Users.ExecuteDeleteAsync(cancellationToken);

        // ExecuteDelete bypasses the change tracker, so anything this scope had loaded is now
        // a phantom. Clearing it keeps the seeder's inserts from colliding with stale entries.
        context.ChangeTracker.Clear();
    }
}
