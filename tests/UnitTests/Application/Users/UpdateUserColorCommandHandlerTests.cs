using FluentAssertions;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Users.Commands.UpdateUserColor;
using TaskFlow.Domain.Entities;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Users;

public class UpdateUserColorCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithAValidColor_UpdatesTheCurrentUsersColor()
    {
        await using var context = new TestDbContext();
        var user = User.Create("ada@example.com", "Ada", "hash").Value;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UpdateUserColorCommandHandler(context, new FakeCurrentUserService(user.Id));

        var result = await handler.Handle(new UpdateUserColorCommand("#a855f7"), CancellationToken.None);

        result.Color.Should().Be("#a855f7");
        (await context.Users.FindAsync(user.Id))!.Color.Should().Be("#a855f7");
    }

    [Fact]
    public async Task Handle_WithAnInvalidColor_ThrowsValidationExceptionAndLeavesColorUnchanged()
    {
        await using var context = new TestDbContext();
        var user = User.Create("ada@example.com", "Ada", "hash").Value;
        var original = user.Color;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UpdateUserColorCommandHandler(context, new FakeCurrentUserService(user.Id));

        var act = async () => await handler.Handle(new UpdateUserColorCommand("not-a-color"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        (await context.Users.FindAsync(user.Id))!.Color.Should().Be(original);
    }
}
