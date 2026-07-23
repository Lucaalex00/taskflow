using FluentAssertions;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Notifications.Commands.MarkNotificationRead;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Notifications;

public class MarkNotificationReadCommandHandlerTests
{
    [Fact]
    public async Task Handle_MarksTheCallersOwnNotificationAsRead()
    {
        await using var context = new TestDbContext();
        var userId = Guid.NewGuid();
        var notification = Notification.Create(userId, NotificationType.TaskAssigned, "message").Value;
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var handler = new MarkNotificationReadCommandHandler(context, new FakeCurrentUserService(userId));

        await handler.Handle(new MarkNotificationReadCommand(notification.Id), CancellationToken.None);

        (await context.Notifications.FindAsync(notification.Id))!.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ForSomeoneElsesNotification_ThrowsNotFoundException()
    {
        await using var context = new TestDbContext();
        var notification = Notification.Create(Guid.NewGuid(), NotificationType.TaskAssigned, "message").Value;
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var handler = new MarkNotificationReadCommandHandler(context, new FakeCurrentUserService(Guid.NewGuid()));

        var act = async () => await handler.Handle(new MarkNotificationReadCommand(notification.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
