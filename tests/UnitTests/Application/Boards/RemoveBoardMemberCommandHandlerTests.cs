using FluentAssertions;
using TaskFlow.Application.Boards.Commands.RemoveBoardMember;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Services;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Boards;

public class RemoveBoardMemberCommandHandlerTests
{
    [Fact]
    public async Task Handle_RemovingAMember_Succeeds()
    {
        await using var context = new TestDbContext();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var board = ProjectBoard.Create("Sprint 1", ownerId).Value;
        context.Boards.Add(board);
        context.BoardMembers.Add(BoardMember.Create(board.Id, ownerId, BoardRole.Owner).Value);
        context.BoardMembers.Add(BoardMember.Create(board.Id, memberId, BoardRole.Member).Value);
        await context.SaveChangesAsync();

        var handler = new RemoveBoardMemberCommandHandler(
            context, new BoardAuthorizer(context, new FakeCurrentUserService(ownerId)));

        await handler.Handle(new RemoveBoardMemberCommand(board.Id, memberId), CancellationToken.None);

        context.BoardMembers.Should().NotContain(m => m.UserId == memberId);
    }

    [Fact]
    public async Task Handle_RemovingTheLastOwner_ThrowsValidationException()
    {
        await using var context = new TestDbContext();
        var ownerId = Guid.NewGuid();
        var board = ProjectBoard.Create("Sprint 1", ownerId).Value;
        context.Boards.Add(board);
        context.BoardMembers.Add(BoardMember.Create(board.Id, ownerId, BoardRole.Owner).Value);
        await context.SaveChangesAsync();

        var handler = new RemoveBoardMemberCommandHandler(
            context, new BoardAuthorizer(context, new FakeCurrentUserService(ownerId)));

        var act = async () => await handler.Handle(
            new RemoveBoardMemberCommand(board.Id, ownerId), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
