using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

/// <summary>
/// A generated notification produced when an AlertRule's condition is met.
/// Pushed to clients in real time over SignalR by the worker that creates it.
/// </summary>
public class Alert : Entity
{
    public Guid BoardId { get; private set; }
    public Guid AlertRuleId { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public string Message { get; private set; } = null!;
    public Guid? RelatedUserId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Alert() { } // EF Core

    private Alert(Guid boardId, Guid alertRuleId, AlertSeverity severity, string message, Guid? relatedUserId)
    {
        BoardId = boardId;
        AlertRuleId = alertRuleId;
        Severity = severity;
        Message = message;
        RelatedUserId = relatedUserId;
        IsRead = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static Result<Alert> Create(
        Guid boardId, Guid alertRuleId, AlertSeverity severity, string message, Guid? relatedUserId = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            return Result.Failure<Alert>("Alert message cannot be empty.");

        return Result.Success(new Alert(boardId, alertRuleId, severity, message.Trim(), relatedUserId));
    }

    public void MarkAsRead() => IsRead = true;
}
