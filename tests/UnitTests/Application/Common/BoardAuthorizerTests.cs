using FluentAssertions;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Services;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Common;

public class BoardAuthorizerTests
{
    private static async Task<(TestDbContext Context, Guid BoardId)> SeedBoardWithMemberAsync(
        Guid userId, BoardRole role)
    {
        var context = new TestDbContext();
        var board = ProjectBoard.Create("Sprint 1", userId).Value;
        context.Boards.Add(board);
        context.BoardMembers.Add(BoardMember.Create(board.Id, userId, role).Value);
        await context.SaveChangesAsync();
        return (context, board.Id);
    }

    [Fact]
    public async Task EnsureMemberAsync_WhenUserIsAMember_DoesNotThrow()
    {
        var userId = Guid.NewGuid();
        var (context, boardId) = await SeedBoardWithMemberAsync(userId, BoardRole.Member);
        var authorizer = new BoardAuthorizer(context, new FakeCurrentUserService(userId));

        var act = async () => await authorizer.EnsureMemberAsync(boardId, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureMemberAsync_WhenUserIsNotAMember_ThrowsForbiddenException()
    {
        var (context, boardId) = await SeedBoardWithMemberAsync(Guid.NewGuid(), BoardRole.Owner);
        var authorizer = new BoardAuthorizer(context, new FakeCurrentUserService(Guid.NewGuid()));

        var act = async () => await authorizer.EnsureMemberAsync(boardId, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task EnsureOwnerAsync_WhenUserIsOnlyAMember_ThrowsForbiddenException()
    {
        var userId = Guid.NewGuid();
        var (context, boardId) = await SeedBoardWithMemberAsync(userId, BoardRole.Member);
        var authorizer = new BoardAuthorizer(context, new FakeCurrentUserService(userId));

        var act = async () => await authorizer.EnsureOwnerAsync(boardId, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task EnsureOwnerAsync_WhenUserIsOwner_DoesNotThrow()
    {
        var userId = Guid.NewGuid();
        var (context, boardId) = await SeedBoardWithMemberAsync(userId, BoardRole.Owner);
        var authorizer = new BoardAuthorizer(context, new FakeCurrentUserService(userId));

        var act = async () => await authorizer.EnsureOwnerAsync(boardId, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
