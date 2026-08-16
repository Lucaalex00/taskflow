using FluentAssertions;
using TaskFlow.Application.Boards.Commands.ArchiveBoard;
using TaskFlow.Application.Boards.Commands.RenameBoard;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Entities;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Boards;

public class RenameAndArchiveBoardHandlerTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static async Task<(TestDbContext Context, ProjectBoard Board)> SeedBoardAsync()
    {
        var context = new TestDbContext();
        var board = ProjectBoard.Create("Original", Guid.NewGuid()).Value;
        context.Boards.Add(board);
        await context.SaveChangesAsync();
        return (context, board);
    }

    [Fact]
    public async Task Rename_ChangesTheBoardName()
    {
        var (context, board) = await SeedBoardAsync();
        var handler = new RenameBoardCommandHandler(context, new FakeBoardAuthorizer());

        await handler.Handle(new RenameBoardCommand(board.Id, "Renamed"), CancellationToken.None);

        (await context.Boards.FindAsync(board.Id))!.Name.Should().Be("Renamed");
    }

    [Fact]
    public async Task Rename_WithAnEmptyName_ThrowsValidationException()
    {
        var (context, board) = await SeedBoardAsync();
        var handler = new RenameBoardCommandHandler(context, new FakeBoardAuthorizer());

        var act = async () => await handler.Handle(new RenameBoardCommand(board.Id, "  "), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Archive_MarksTheBoardArchived()
    {
        var (context, board) = await SeedBoardAsync();
        var handler = new ArchiveBoardCommandHandler(context, new FakeBoardAuthorizer(), new FakeDateTimeProvider(Now));

        await handler.Handle(new ArchiveBoardCommand(board.Id), CancellationToken.None);

        (await context.Boards.FindAsync(board.Id))!.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task Archive_ForAMissingBoard_ThrowsNotFound()
    {
        var context = new TestDbContext();
        var handler = new ArchiveBoardCommandHandler(context, new FakeBoardAuthorizer(), new FakeDateTimeProvider(Now));

        var act = async () => await handler.Handle(new ArchiveBoardCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
