using FluentAssertions;
using TaskFlow.Application.Users.Queries.GetUsers;
using TaskFlow.Domain.Entities;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Users;

public class GetUsersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsEveryUserOrderedByDisplayName()
    {
        await using var context = new TestDbContext();
        context.Users.AddRange(
            User.Create("zack@example.com", "Zack", "hash").Value,
            User.Create("ada@example.com", "Ada", "hash").Value);
        await context.SaveChangesAsync();

        var handler = new GetUsersQueryHandler(context);

        var users = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        users.Select(u => u.DisplayName).Should().Equal("Ada", "Zack");
    }

    [Fact]
    public async Task Handle_WithNoUsers_ReturnsEmptyList()
    {
        await using var context = new TestDbContext();
        var handler = new GetUsersQueryHandler(context);

        var users = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        users.Should().BeEmpty();
    }
}
