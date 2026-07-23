using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

/// <summary>
/// A periodic snapshot of a board's workload, written by the background worker.
/// History of snapshots is what lets BoardLoadSpike-type rules compare "now" vs "N minutes ago"
/// without recomputing from the full task table each time.
/// </summary>
public class LoadMetric : Entity
{
    public Guid BoardId { get; private set; }
    public int ActiveTaskCount { get; private set; }
    public int OverdueTaskCount { get; private set; }
    public int InProgressTaskCount { get; private set; }
    public DateTime SnapshotAtUtc { get; private set; }

    private LoadMetric() { } // EF Core

    private LoadMetric(Guid boardId, int activeTaskCount, int overdueTaskCount, int inProgressTaskCount)
    {
        BoardId = boardId;
        ActiveTaskCount = activeTaskCount;
        OverdueTaskCount = overdueTaskCount;
        InProgressTaskCount = inProgressTaskCount;
        SnapshotAtUtc = DateTime.UtcNow;
    }

    public static LoadMetric Capture(Guid boardId, int activeTaskCount, int overdueTaskCount, int inProgressTaskCount)
        => new(boardId, activeTaskCount, overdueTaskCount, inProgressTaskCount);
}
