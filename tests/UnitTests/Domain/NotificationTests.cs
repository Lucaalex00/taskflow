using FluentAssertions;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using Xunit;

namespace TaskFlow.UnitTests.Domain;

public class NotificationTests
{
    [Fact]
    public void Create_WithValidInput_StartsUnread()
    {
        var result = Notification.Create(Guid.NewGuid(), NotificationType.TaskAssigned, "You were assigned a task.");

        result.IsSuccess.Should().BeTrue();
        result.Value.IsRead.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptyRecipient_ReturnsFailure()
    {
        var result = Notification.Create(Guid.Empty, NotificationType.TaskAssigned, "message");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptyMessage_ReturnsFailure()
    {
        var result = Notification.Create(Guid.NewGuid(), NotificationType.TaskAssigned, "   ");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void MarkAsRead_SetsIsReadToTrue()
    {
        var notification = Notification.Create(Guid.NewGuid(), NotificationType.TaskAssigned, "message").Value;

        notification.MarkAsRead();

        notification.IsRead.Should().BeTrue();
    }
}
