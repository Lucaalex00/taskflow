using FluentAssertions;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Users.Commands.ChangePassword;
using TaskFlow.Domain.Entities;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Users;

public class ChangePasswordCommandHandlerTests
{
    private static readonly FakePasswordHasher Hasher = new();

    [Fact]
    public async Task Handle_WithTheCorrectCurrentPassword_ReplacesTheStoredHash()
    {
        await using var context = new TestDbContext();
        var user = User.Create("ada@example.com", "Ada", Hasher.Hash("Old-password-1")).Value;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new ChangePasswordCommandHandler(context, new FakeCurrentUserService(user.Id), Hasher);

        await handler.Handle(new ChangePasswordCommand("Old-password-1", "New-password-2"), CancellationToken.None);

        var saved = (await context.Users.FindAsync(user.Id))!;
        Hasher.Verify(saved.PasswordHash, "New-password-2").Should().BeTrue();
        Hasher.Verify(saved.PasswordHash, "Old-password-1").Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithTheWrongCurrentPassword_ThrowsValidationExceptionAndKeepsTheOldHash()
    {
        await using var context = new TestDbContext();
        var user = User.Create("ada@example.com", "Ada", Hasher.Hash("Old-password-1")).Value;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new ChangePasswordCommandHandler(context, new FakeCurrentUserService(user.Id), Hasher);

        var act = async () => await handler.Handle(
            new ChangePasswordCommand("Not-my-password-9", "New-password-2"), CancellationToken.None);

        var thrown = await act.Should().ThrowAsync<ValidationException>();
        thrown.Which.Errors.Should().ContainKey(nameof(ChangePasswordCommand.CurrentPassword));

        var saved = (await context.Users.FindAsync(user.Id))!;
        Hasher.Verify(saved.PasswordHash, "Old-password-1").Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTheAccountWasLockedOut_ClearsTheLockout()
    {
        await using var context = new TestDbContext();
        var user = User.Create("ada@example.com", "Ada", Hasher.Hash("Old-password-1")).Value;
        var now = new DateTime(2026, 8, 14, 9, 0, 0, DateTimeKind.Utc);
        user.RegisterFailedLogin(now, maxAttempts: 1, lockoutDuration: TimeSpan.FromMinutes(15));
        context.Users.Add(user);
        await context.SaveChangesAsync();
        user.IsLockedOut(now).Should().BeTrue();

        var handler = new ChangePasswordCommandHandler(context, new FakeCurrentUserService(user.Id), Hasher);

        await handler.Handle(new ChangePasswordCommand("Old-password-1", "New-password-2"), CancellationToken.None);

        (await context.Users.FindAsync(user.Id))!.IsLockedOut(now).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenTheCurrentUserNoLongerExists_ThrowsNotFound()
    {
        await using var context = new TestDbContext();
        var handler = new ChangePasswordCommandHandler(context, new FakeCurrentUserService(Guid.NewGuid()), Hasher);

        var act = async () => await handler.Handle(
            new ChangePasswordCommand("Old-password-1", "New-password-2"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
