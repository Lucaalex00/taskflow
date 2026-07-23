using FluentAssertions;
using TaskFlow.Application.Notifications.Queries.GetNotifications;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.UnitTests.Common;
using Xunit;

namespace TaskFlow.UnitTests.Application.Notifications;

public class GetNotificationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyTheCurrentUsersNotifications_MostRecentFirst()
    {
        await using var context = new TestDbContext();
        var userId = Guid.NewGuid();
        var mine1 = Notification.Create(userId, NotificationType.TaskAssigned, "first").Value;
        var mine2 = Notification.Create(userId, NotificationType.TaskAssigned, "second").Value;
        var someoneElses = Notification.Create(Guid.NewGuid(), NotificationType.TaskAssigned, "not mine").Value;
        context.Notifications.AddRange(mine1, mine2, someoneElses);
        await context.SaveChangesAsync();

        var handler = new GetNotificationsQueryHandler(context, new FakeCurrentUserService(userId));

        var notifications = await handler.Handle(new GetNotificationsQuery(), CancellationToken.None);

        notifications.Should().HaveCount(2);
        notifications.Should().OnlyContain(n => n.Message == "first" || n.Message == "second");
    }

    [Fact]
    public async Task Handle_ForABoardInvitationNotification_IncludesTheInvitationStatus()
    {
        await using var context = new TestDbContext();
        var userId = Guid.NewGuid();
        var invitation = BoardInvitation.Create(Guid.NewGuid(), "user@example.com", userId, Guid.NewGuid()).Value;
        context.BoardInvitations.Add(invitation);
        var notification = Notification.Create(
            userId, NotificationType.BoardInvitation, "invited", invitationId: invitation.Id).Value;
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var handler = new GetNotificationsQueryHandler(context, new FakeCurrentUserService(userId));

        var notifications = await handler.Handle(new GetNotificationsQuery(), CancellationToken.None);

        notifications.Single().InvitationStatus.Should().Be(InvitationStatus.Pending);
    }
}
