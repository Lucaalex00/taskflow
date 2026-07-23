using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Alerts;

public sealed record AlertDto(
    Guid Id,
    Guid BoardId,
    AlertSeverity Severity,
    string Message,
    Guid? RelatedUserId,
    bool IsRead,
    DateTime CreatedAtUtc);
