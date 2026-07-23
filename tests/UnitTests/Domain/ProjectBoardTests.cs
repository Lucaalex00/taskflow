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
}
