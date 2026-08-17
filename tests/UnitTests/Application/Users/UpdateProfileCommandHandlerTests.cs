using FluentAssertions;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Users.Commands.UpdateProfile;
using TaskFlow.Domain.Entities;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Users;

public class UpdateProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithANewDisplayName_RenamesTheCurrentUser()
    {
        await using var context = new TestDbContext();
        var user = User.Create("ada@example.com", "Ada", "hash").Value;
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new UpdateProfileCommandHandler(context, new FakeCurrentUserService(user.Id));

        var result = await handler.Handle(new UpdateProfileCommand("Ada Lovelace"), CancellationToken.None);

        result.DisplayName.Should().Be("Ada Lovelace");
        (await context.Users.FindAsync(user.Id))!.DisplayName.Should().Be("Ada Lovelace");
    }

    [Fact]
    public async Task Handle_RenamesOnlyTheCallerNeverAnotherUser()
    {
        await using var context = new TestDbContext();
        var caller = User.Create("ada@example.com", "Ada", "hash").Value;
        var other = User.Create("grace@example.com", "Grace", "hash").Value;
        context.Users.AddRange(caller, other);
        await context.SaveChangesAsync();

        var handler = new UpdateProfileCommandHandler(context, new FakeCurrentUserService(caller.Id));

        await handler.Handle(new UpdateProfileCommand("Renamed"), CancellationToken.None);

        (await context.Users.FindAsync(other.Id))!.DisplayName.Should().Be("Grace");
    }

    [Fact]
    public async Task Handle_WhenTheCurrentUserNoLongerExists_ThrowsNotFound()
    {
        await using var context = new TestDbContext();
        var handler = new UpdateProfileCommandHandler(context, new FakeCurrentUserService(Guid.NewGuid()));

        var act = async () => await handler.Handle(new UpdateProfileCommand("Ghost"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
