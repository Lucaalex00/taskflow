namespace TaskFlow.Domain.Enums;

public enum TaskState
{
    Todo = 0,
    InProgress = 1,
    Blocked = 2,
    Done = 3,
    Cancelled = 4
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum AlertSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

public enum AlertRuleType
{
    /// <summary>User has more than N overdue tasks assigned.</summary>
    OverdueTasksThreshold = 0,

    /// <summary>Board's active task count grew by more than X% within the evaluation window.</summary>
    BoardLoadSpike = 1,

    /// <summary>User has more than N tasks marked InProgress simultaneously (context-switch risk).</summary>
    ConcurrentInProgressThreshold = 2
}
