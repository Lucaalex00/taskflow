using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.IntegrationTests;

/// <summary>
/// The reset is the one operation in the codebase that deliberately destroys data, so it's
/// exercised against a real database rather than an in-memory stand-in: the delete order has to
/// satisfy actual foreign keys, and "it wiped everything and rebuilt" is only meaningful if the
/// constraints were real.
/// </summary>
public class DemoWorkspaceResetterTests(SeededApiFactory factory) : IClassFixture<SeededApiFactory>
{
    /// <summary>Ages the seeded workspace by moving the demo owner's creation date backwards —
    /// that timestamp is what the resetter treats as the workspace's age.</summary>
    private static async Task AgeWorkspaceAsync(TaskFlowDbContext context, TimeSpan by)
    {
        var owner = await context.Users.SingleAsync(u => u.Email == DemoDataSeeder.OwnerEmail);
        context.Entry(owner).Property(nameof(User.CreatedAtUtc)).CurrentValue =
            owner.CreatedAtUtc - by;
        await context.SaveChangesAsync();
    }

    private static async Task<T> WithScopeAsync<T>(SeededApiFactory factory, Func<IServiceScope, Task<T>> action)
    {
        using var scope = factory.Services.CreateScope();
        return await action(scope);
    }

    [Fact]
    public async Task AFreshWorkspace_IsLeftAlone()
    {
        var reset = await WithScopeAsync(factory, async scope =>
        {
            var resetter = scope.ServiceProvider.GetRequiredService<DemoWorkspaceResetter>();
            return await resetter.ResetIfStaleAsync();
        });

        reset.Should().BeFalse();
    }

    [Fact]
    public async Task AStaleWorkspace_IsWipedAndRebuiltBackToTheSeededState()
    {
        // A visitor has been poking around: their account and board must not survive the reset.
        var visitorEmail = $"visitor-{Guid.NewGuid()}@example.com";

        await WithScopeAsync(factory, async scope =>
        {
            var context = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
            var visitor = User.Create(visitorEmail, "Curious Visitor", "hash").Value;
            context.Users.Add(visitor);
            context.Boards.Add(ProjectBoard.Create("Visitor scratch board", visitor.Id).Value);
            await context.SaveChangesAsync();

            await AgeWorkspaceAsync(context, TimeSpan.FromDays(30));
            return true;
        });

        var reset = await WithScopeAsync(factory, async scope =>
            await scope.ServiceProvider.GetRequiredService<DemoWorkspaceResetter>().ResetIfStaleAsync());

        reset.Should().BeTrue();

        await WithScopeAsync(factory, async scope =>
        {
            var context = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();

            (await context.Users.AnyAsync(u => u.Email == visitorEmail)).Should().BeFalse();
            (await context.Boards.AnyAsync(b => b.Name == "Visitor scratch board")).Should().BeFalse();

            // ...and the demo workspace is back, in full.
            (await context.Users.CountAsync()).Should().Be(3);
            (await context.Boards.CountAsync()).Should().Be(3);
            (await context.Tasks.AnyAsync()).Should().BeTrue();
            (await context.AlertRules.AnyAsync()).Should().BeTrue();
            (await context.BoardInvitations.AnyAsync()).Should().BeTrue();

            // The rebuilt workspace is young again, so the next check is a no-op rather than a
            // wipe-every-cycle loop.
            var owner = await context.Users.SingleAsync(u => u.Email == DemoDataSeeder.OwnerEmail);
            (DateTime.UtcNow - owner.CreatedAtUtc).Should().BeLessThan(TimeSpan.FromMinutes(5));
            return true;
        });

        var secondReset = await WithScopeAsync(factory, async scope =>
            await scope.ServiceProvider.GetRequiredService<DemoWorkspaceResetter>().ResetIfStaleAsync());

        secondReset.Should().BeFalse();
    }
}
