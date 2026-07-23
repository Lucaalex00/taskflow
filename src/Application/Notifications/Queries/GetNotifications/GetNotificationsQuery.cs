using MediatR;

namespace TaskFlow.Application.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery : IRequest<IReadOnlyList<NotificationDto>>;
