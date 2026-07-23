using FluentAssertions;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using Xunit;

namespace TaskFlow.UnitTests.Domain;

public class AlertRuleTests
{
    [Theory]
    [InlineData(0, 30)]
    [InlineData(-1, 30)]
    [InlineData(5, 0)]
    [InlineData(5, -10)]
    public void Create_WithNonPositiveNumbers_ReturnsFailure(int threshold, int windowMinutes)
    {
        var result = AlertRule.Create(Guid.NewGuid(), AlertRuleType.OverdueTasksThreshold, threshold, windowMinutes);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptyBoardId_ReturnsFailure()
    {
        var result = AlertRule.Create(Guid.Empty, AlertRuleType.BoardLoadSpike, 50, 60);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Disable_ThenEnable_TogglesIsEnabled()
    {
        var rule = AlertRule.Create(Guid.NewGuid(), AlertRuleType.ConcurrentInProgressThreshold, 3, 15).Value;

        rule.Disable();
        rule.IsEnabled.Should().BeFalse();

        rule.Enable();
        rule.IsEnabled.Should().BeTrue();
    }
}

public class ResultTests
{
    [Fact]
    public void Success_Generic_ExposesValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Failure_Generic_ThrowsWhenAccessingValue()
    {
        var result = Result.Failure<int>("something went wrong");

        result.IsSuccess.Should().BeFalse();
        var act = () => result.Value;
        act.Should().Throw<InvalidOperationException>();
    }
}
