using FluentAssertions;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Entities;
using Xunit;

namespace TaskFlow.UnitTests.Domain;

public class ProjectBoardTests
{
    [Fact]
    public void Create_WithoutAColor_AssignsOneFromThePalette()
    {
        var result = ProjectBoard.Create("Sprint 1", Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        ColorPalette.Colors.Should().Contain(result.Value.Color);
    }

    [Fact]
    public void Create_WithAValidColor_UsesIt()
    {
        var result = ProjectBoard.Create("Sprint 1", Guid.NewGuid(), "#63b3ed");

        result.IsSuccess.Should().BeTrue();
        result.Value.Color.Should().Be("#63b3ed");
    }

    [Fact]
    public void Create_WithAnInvalidColor_ReturnsFailure()
    {
        var result = ProjectBoard.Create("Sprint 1", Guid.NewGuid(), "not-a-color");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Rename_WithANewName_UpdatesTheName()
    {
        var board = ProjectBoard.Create("Old name", Guid.NewGuid()).Value;

        var result = board.Rename("  New name  ");

        result.IsSuccess.Should().BeTrue();
        board.Name.Should().Be("New name");
    }

    [Fact]
    public void Rename_WithAnEmptyName_ReturnsFailureAndKeepsTheName()
    {
        var board = ProjectBoard.Create("Keep me", Guid.NewGuid()).Value;

        var result = board.Rename("   ");

        result.IsSuccess.Should().BeFalse();
        board.Name.Should().Be("Keep me");
    }

    [Fact]
    public void Archive_MarksTheBoardArchived_AndIsIdempotent()
    {
        var board = ProjectBoard.Create("Sprint 1", Guid.NewGuid()).Value;

        board.Archive(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var firstArchivedAt = board.ArchivedAtUtc;
        board.Archive(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        board.IsArchived.Should().BeTrue();
        board.ArchivedAtUtc.Should().Be(firstArchivedAt); // unchanged on the second call
    }
}
