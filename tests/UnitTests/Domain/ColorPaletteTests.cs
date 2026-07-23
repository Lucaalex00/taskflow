using FluentAssertions;
using TaskFlow.Domain.Common;
using Xunit;

namespace TaskFlow.UnitTests.Domain;

public class ColorPaletteTests
{
    [Theory]
    [InlineData("#4fd1c5", true)]
    [InlineData("#FFFFFF", true)]
    [InlineData("4fd1c5", false)]
    [InlineData("#fff", false)]
    [InlineData("not-a-color", false)]
    [InlineData("", false)]
    public void IsValidHex_ValidatesTheExpectedFormat(string color, bool expected)
    {
        ColorPalette.IsValidHex(color).Should().Be(expected);
    }

    [Fact]
    public void PickFor_IsDeterministicForTheSameId()
    {
        var id = Guid.NewGuid();

        ColorPalette.PickFor(id).Should().Be(ColorPalette.PickFor(id));
    }

    [Fact]
    public void PickFor_AlwaysReturnsAColorFromThePalette()
    {
        ColorPalette.Colors.Should().Contain(ColorPalette.PickFor(Guid.NewGuid()));
    }
}
