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

    /// <summary>Chosen by whoever creates the board (falls back to a random palette color if
    /// omitted) — used to tell boards apart at a glance in the board list.</summary>
    public string Color { get; private set; } = null!;

    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Set when the board is logically deleted ("archived"): its row (and its tasks)
    /// stay in the database but it no longer appears in anyone's board list.</summary>
    public DateTime? ArchivedAtUtc { get; private set; }
    public bool IsArchived => ArchivedAtUtc is not null;

    private readonly List<TaskItem> _tasks = [];
    public IReadOnlyCollection<TaskItem> Tasks => _tasks.AsReadOnly();

    private ProjectBoard() { } // EF Core

    private ProjectBoard(string name, Guid ownerId, string color)
    {
        Name = name;
        OwnerId = ownerId;
        Color = color;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static Result<ProjectBoard> Create(string name, Guid ownerId, string? color = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<ProjectBoard>("Board name cannot be empty.");

        if (ownerId == Guid.Empty)
            return Result.Failure<ProjectBoard>("A board must have a valid owner.");

        if (!string.IsNullOrWhiteSpace(color) && !ColorPalette.IsValidHex(color))
            return Result.Failure<ProjectBoard>("Color must be a hex code like #4fd1c5.");

        var resolvedColor = string.IsNullOrWhiteSpace(color) ? ColorPalette.PickRandom() : color;

        return Result.Success(new ProjectBoard(name.Trim(), ownerId, resolvedColor));
    }

    public Result Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return Result.Failure("Board name cannot be empty.");

        Name = newName.Trim();
        return Result.Success();
    }

    /// <summary>Logical delete — keeps the board (and its tasks) in the database but removes it
    /// from board lists. Idempotent.</summary>
    public void Archive(DateTime nowUtc)
    {
        ArchivedAtUtc ??= nowUtc;
    }
}
