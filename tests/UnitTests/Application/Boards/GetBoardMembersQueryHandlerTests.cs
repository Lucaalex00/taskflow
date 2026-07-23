using FluentAssertions;
using TaskFlow.Application.Boards.Queries.GetBoardMembers;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Boards;

public class GetBoardMembersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsEveryMemberWithTheirRole()
    {
        await using var context = new TestDbContext();
        var owner = User.Create("owner@example.com", "Owner", "hash").Value;
        var member = User.Create("member@example.com", "Member", "hash").Value;
        var board = ProjectBoard.Create("Sprint 1", owner.Id).Value;
        context.Users.AddRange(owner, member);
        context.Boards.Add(board);
        context.BoardMembers.Add(BoardMember.Create(board.Id, owner.Id, BoardRole.Owner).Value);
        context.BoardMembers.Add(BoardMember.Create(board.Id, member.Id, BoardRole.Member).Value);
        await context.SaveChangesAsync();

        var handler = new GetBoardMembersQueryHandler(context, new FakeBoardAuthorizer());

        var members = await handler.Handle(new GetBoardMembersQuery(board.Id), CancellationToken.None);

        members.Should().HaveCount(2);
        members.Should().Contain(m => m.UserId == owner.Id && m.Role == BoardRole.Owner);
        members.Should().Contain(m => m.UserId == member.Id && m.Role == BoardRole.Member);
    }
}
