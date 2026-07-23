using FluentAssertions;
using TaskFlow.Application.Boards.Commands.UpdateBoardMemberRole;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Services;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Boards;

public class UpdateBoardMemberRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_PromotingAMemberToOwner_Succeeds()
    {
        await using var context = new TestDbContext();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var board = ProjectBoard.Create("Sprint 1", ownerId).Value;
        context.Boards.Add(board);
        context.BoardMembers.Add(BoardMember.Create(board.Id, ownerId, BoardRole.Owner).Value);
        context.BoardMembers.Add(BoardMember.Create(board.Id, memberId, BoardRole.Member).Value);
        await context.SaveChangesAsync();

        var handler = new UpdateBoardMemberRoleCommandHandler(
            context, new BoardAuthorizer(context, new FakeCurrentUserService(ownerId)));

        await handler.Handle(new UpdateBoardMemberRoleCommand(board.Id, memberId, BoardRole.Owner), CancellationToken.None);

        context.BoardMembers.Single(m => m.UserId == memberId).Role.Should().Be(BoardRole.Owner);
    }

    [Fact]
    public async Task Handle_DemotingTheLastOwner_ThrowsValidationException()
    {
        await using var context = new TestDbContext();
        var ownerId = Guid.NewGuid();
        var board = ProjectBoard.Create("Sprint 1", ownerId).Value;
        context.Boards.Add(board);
        context.BoardMembers.Add(BoardMember.Create(board.Id, ownerId, BoardRole.Owner).Value);
        await context.SaveChangesAsync();

        var handler = new UpdateBoardMemberRoleCommandHandler(
            context, new BoardAuthorizer(context, new FakeCurrentUserService(ownerId)));

        var act = async () => await handler.Handle(
            new UpdateBoardMemberRoleCommand(board.Id, ownerId, BoardRole.Member), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_AsNonOwner_ThrowsForbiddenException()
    {
        await using var context = new TestDbContext();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var board = ProjectBoard.Create("Sprint 1", ownerId).Value;
        context.Boards.Add(board);
        context.BoardMembers.Add(BoardMember.Create(board.Id, ownerId, BoardRole.Owner).Value);
        context.BoardMembers.Add(BoardMember.Create(board.Id, memberId, BoardRole.Member).Value);
        await context.SaveChangesAsync();

        var handler = new UpdateBoardMemberRoleCommandHandler(
            context, new BoardAuthorizer(context, new FakeCurrentUserService(memberId)));

        var act = async () => await handler.Handle(
            new UpdateBoardMemberRoleCommand(board.Id, memberId, BoardRole.Owner), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
