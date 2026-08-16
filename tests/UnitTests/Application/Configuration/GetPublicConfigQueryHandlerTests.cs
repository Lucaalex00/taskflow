using FluentAssertions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Configuration.Queries.GetPublicConfig;
using TaskFlow.Domain.Entities;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Configuration;

public class GetPublicConfigQueryHandlerTests
{
    private sealed class StubDemoAccount(bool enabled, string email = "demo@taskflow.dev") : IDemoAccountProvider
    {
        public bool IsEnabled { get; } = enabled;
        public string Email { get; } = email;
        public string Password => "Demo-password-2026";
    }

    [Fact]
    public async Task Handle_WhenSeedingIsDisabled_ReportsNoDemoAccount()
    {
        await using var context = new TestDbContext();
        var handler = new GetPublicConfigQueryHandler(context, new StubDemoAccount(enabled: false));

        var result = await handler.Handle(new GetPublicConfigQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(new PublicConfigDto(false, null, null));
    }

    [Fact]
    public async Task Handle_WhenSeedingIsEnabledButTheAccountWasNeverCreated_ReportsNoDemoAccount()
    {
        await using var context = new TestDbContext();
        context.Users.Add(User.Create("someone@example.com", "Someone", "hash").Value);
        await context.SaveChangesAsync();

        var handler = new GetPublicConfigQueryHandler(context, new StubDemoAccount(enabled: true));

        var result = await handler.Handle(new GetPublicConfigQuery(), CancellationToken.None);

        result.DemoAccountAvailable.Should().BeFalse();
        result.DemoPassword.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenTheDemoAccountExists_ReturnsItsCredentials()
    {
        await using var context = new TestDbContext();
        context.Users.Add(User.Create("demo@taskflow.dev", "Dana", "hash").Value);
        await context.SaveChangesAsync();

        var handler = new GetPublicConfigQueryHandler(context, new StubDemoAccount(enabled: true));

        var result = await handler.Handle(new GetPublicConfigQuery(), CancellationToken.None);

        result.DemoAccountAvailable.Should().BeTrue();
        result.DemoEmail.Should().Be("demo@taskflow.dev");
        result.DemoPassword.Should().Be("Demo-password-2026");
    }
}
