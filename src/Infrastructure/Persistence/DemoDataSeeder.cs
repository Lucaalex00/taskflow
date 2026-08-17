using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Infrastructure.Persistence;

/// <summary>
/// Populates an empty database with a workspace that shows what TaskFlow actually does.
/// Without it the first thing a reviewer sees after `docker compose up` is an empty board —
/// the anomaly detection, roles, invitations and notifications only become visible once
/// there's real load to look at.
///
/// Everything here goes through the same domain factories and the same password hasher the
/// application uses, so the seeded accounts are ordinary accounts: nothing about them is
/// special-cased at login, and no rule is bypassed to create them.
/// </summary>
public sealed class DemoDataSeeder(
    TaskFlowDbContext context,
    IPasswordHasher passwordHasher,
    IDateTimeProvider clock,
    IOptions<SeedOptions> options,
    ILogger<DemoDataSeeder> logger)
{
    public const string OwnerEmail = "demo@taskflow.dev";
    public const string TeammateEmail = "sam@taskflow.dev";
    public const string DesignerEmail = "priya@taskflow.dev";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled)
            return;

        // Only ever runs against a genuinely empty instance: re-seeding a database someone has
        // been using would silently resurrect data they deleted, and duplicate every board.
        if (await context.Users.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Demo seed skipped — the database already contains users.");
            return;
        }

        var now = clock.UtcNow;
        var passwordHash = passwordHasher.Hash(options.Value.Password);

        var owner = AddUser(OwnerEmail, "Dana Okafor", passwordHash);
        var teammate = AddUser(TeammateEmail, "Sam Rivera", passwordHash);
        var designer = AddUser(DesignerEmail, "Priya Nair", passwordHash);

        SeedLaunchBoard(owner, teammate, designer, now);
        SeedMaintenanceBoard(owner, teammate, now);
        SeedPendingInvitationForOwner(designer, owner);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Demo workspace seeded — sign in as {Email} (password from Seed:Password).", OwnerEmail);
    }

    /// <summary>The board the demo lands on: enough spread across the state machine to make the
    /// Kanban readable, plus deliberately unhealthy load on one assignee so the workload monitor
    /// raises real alerts on its first cycle rather than showing an empty alert console.</summary>
    private void SeedLaunchBoard(User owner, User teammate, User designer, DateTime now)
    {
        var board = AddBoard("Product launch", owner, "#a855f7", [teammate, designer]);

        // Sam is the overloaded one: 3 overdue and 3 concurrent in-progress tasks, against
        // thresholds of 2. Due dates are set to midnight today — the domain refuses to create a
        // task already due in the past, so "today, 00:00" is the earliest genuinely-overdue date
        // a task can legitimately be created with.
        var overdue = now.Date;

        AddTask(board, "Ship the pricing page copy", teammate, TaskPriority.Critical, overdue, TaskState.InProgress,
            "Legal signed off on the wording; needs to go live with the launch post.");
        AddTask(board, "Migrate billing webhooks", teammate, TaskPriority.High, overdue, TaskState.InProgress,
            "Stripe test mode is done, production endpoints still point at the old handler.");
        AddTask(board, "Fix onboarding email links", teammate, TaskPriority.High, overdue, TaskState.Blocked,
            "Blocked on DNS for the new tracking domain.");
        AddTask(board, "Load-test the checkout flow", teammate, TaskPriority.Medium, now.Date.AddDays(4), TaskState.InProgress);

        AddTask(board, "Design the launch announcement", designer, TaskPriority.High, now.Date.AddDays(2), TaskState.InProgress,
            "Hero illustration plus three feature cards.");
        AddTask(board, "Dark-mode pass on the marketing site", designer, TaskPriority.Low, now.Date.AddDays(9), TaskState.Todo);

        AddTask(board, "Write the launch-day runbook", owner, TaskPriority.Medium, now.Date.AddDays(3), TaskState.Todo);
        AddTask(board, "Line up the beta customer quotes", owner, TaskPriority.Medium, now.Date.AddDays(5), TaskState.Todo);
        AddTask(board, "Book the launch retrospective", owner, TaskPriority.Low, null, TaskState.Done);
        AddTask(board, "Draft the press outreach list", owner, TaskPriority.Low, null, TaskState.Done, archived: true);
        AddTask(board, "Rebrand the changelog", designer, TaskPriority.Low, null, TaskState.Cancelled);

        AddRule(board, AlertRuleType.OverdueTasksThreshold, threshold: 2, windowMinutes: 60);
        AddRule(board, AlertRuleType.ConcurrentInProgressThreshold, threshold: 2, windowMinutes: 60);
        AddRule(board, AlertRuleType.BoardLoadSpike, threshold: 50, windowMinutes: 60);

        AddNotification(teammate, NotificationType.TaskAssigned,
            "Dana assigned you \"Migrate billing webhooks\" on Product launch.", board.Id);
        AddNotification(teammate, NotificationType.TaskAssigned,
            "Dana assigned you \"Fix onboarding email links\" on Product launch.", board.Id);
    }

    /// <summary>A second, healthy board — so the board list isn't a single card, and so the
    /// alert console has an obvious "nothing wrong here" counterpoint to the launch board.</summary>
    private void SeedMaintenanceBoard(User owner, User teammate, DateTime now)
    {
        var board = AddBoard("Platform maintenance", owner, "#4fd1c5", [teammate]);

        AddTask(board, "Rotate the staging database credentials", owner, TaskPriority.Medium, now.Date.AddDays(6), TaskState.Todo);
        AddTask(board, "Upgrade the Postgres image to 16.4", teammate, TaskPriority.Low, now.Date.AddDays(12), TaskState.InProgress);
        AddTask(board, "Prune unused Docker volumes on CI", teammate, TaskPriority.Low, null, TaskState.Todo);

        AddRule(board, AlertRuleType.OverdueTasksThreshold, threshold: 3, windowMinutes: 120);
    }

    /// <summary>Leaves one invitation un-answered so the demo user's notification bell has a real,
    /// actionable item on first login — accepting it is the shortest path to seeing the
    /// invitation → membership flow work.</summary>
    private void SeedPendingInvitationForOwner(User inviter, User invitee)
    {
        var board = AddBoard("Design system", inviter, "#ec4899", members: []);

        var invitation = BoardInvitation.Create(board.Id, invitee.Email, invitee.Id, inviter.Id).Value;
        context.BoardInvitations.Add(invitation);

        AddNotification(invitee, NotificationType.BoardInvitation,
            $"You've been invited to join the board \"{board.Name}\".", board.Id, invitation.Id);
    }

    private User AddUser(string email, string displayName, string passwordHash)
    {
        var user = User.Create(email, displayName, passwordHash).Value;
        context.Users.Add(user);
        return user;
    }

    private ProjectBoard AddBoard(string name, User owner, string color, IReadOnlyList<User> members)
    {
        var board = ProjectBoard.Create(name, owner.Id, color).Value;
        context.Boards.Add(board);
        context.BoardMembers.Add(BoardMember.Create(board.Id, owner.Id, BoardRole.Owner).Value);

        foreach (var member in members)
            context.BoardMembers.Add(BoardMember.Create(board.Id, member.Id, BoardRole.Member).Value);

        return board;
    }

    /// <summary>Creates a task and walks it to <paramref name="state"/> through the real state
    /// machine (Todo → InProgress → …), so nothing lands in a state the domain wouldn't allow.</summary>
    private void AddTask(
        ProjectBoard board, string title, User assignee, TaskPriority priority,
        DateTime? dueAtUtc, TaskState state, string? description = null, bool archived = false)
    {
        var task = TaskItem.Create(board.Id, title, description, priority, dueAtUtc).Value;

        // Assign before transitioning: the domain refuses to assign a task that's already
        // Done or Cancelled, which is exactly the rule a seeder shouldn't be working around.
        if (state != TaskState.Cancelled)
            task.AssignTo(assignee.Id);

        foreach (var step in PathTo(state))
            task.TransitionTo(step);

        if (archived)
            task.Archive(clock.UtcNow);

        // Seeded history shouldn't page anyone: these events never actually happened live.
        task.ClearDomainEvents();
        context.Tasks.Add(task);
    }

    private static IEnumerable<TaskState> PathTo(TaskState state) => state switch
    {
        TaskState.Todo => [],
        TaskState.InProgress => [TaskState.InProgress],
        TaskState.Blocked => [TaskState.InProgress, TaskState.Blocked],
        TaskState.Done => [TaskState.InProgress, TaskState.Done],
        TaskState.Cancelled => [TaskState.Cancelled],
        _ => []
    };

    private void AddRule(ProjectBoard board, AlertRuleType type, int threshold, int windowMinutes) =>
        context.AlertRules.Add(AlertRule.Create(board.Id, type, threshold, windowMinutes).Value);

    private void AddNotification(
        User recipient, NotificationType type, string message, Guid? boardId = null, Guid? invitationId = null) =>
        context.Notifications.Add(
            Notification.Create(recipient.Id, type, message, boardId: boardId, invitationId: invitationId).Value);
}
