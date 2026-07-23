using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common.Interfaces;

/// <summary>
/// Application depends on this abstraction, not on EF Core's DbContext directly —
/// keeps the layer testable and swappable without a real database.
/// </summary>
public interface ITaskFlowDbContext
{
    DbSet<User> Users { get; }
    DbSet<ProjectBoard> Boards { get; }
    DbSet<BoardMember> BoardMembers { get; }
    DbSet<TaskItem> Tasks { get; }
    DbSet<AlertRule> AlertRules { get; }
    DbSet<Alert> Alerts { get; }
    DbSet<LoadMetric> LoadMetrics { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
