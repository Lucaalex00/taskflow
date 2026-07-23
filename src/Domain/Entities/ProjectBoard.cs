using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

/// <summary>
/// A workspace that groups tasks. Load metrics and alert rules are scoped to a board,
/// so a team can have thresholds that differ from another team's.
/// </summary>
public class ProjectBoard : Entity
{
    public string Name { get; private set; } = null!;
    public Guid OwnerId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private readonly List<TaskItem> _tasks = [];
    public IReadOnlyCollection<TaskItem> Tasks => _tasks.AsReadOnly();

    private ProjectBoard() { } // EF Core

    private ProjectBoard(string name, Guid ownerId)
    {
        Name = name;
        OwnerId = ownerId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static Result<ProjectBoard> Create(string name, Guid ownerId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<ProjectBoard>("Board name cannot be empty.");

        if (ownerId == Guid.Empty)
            return Result.Failure<ProjectBoard>("A board must have a valid owner.");

        return Result.Success(new ProjectBoard(name.Trim(), ownerId));
    }

    public void Rename(string newName)
    {
        if (!string.IsNullOrWhiteSpace(newName))
            Name = newName.Trim();
    }
}
