using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Events;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Common;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence;

public class TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options, IPublisher publisher)
    : DbContext(options), ITaskFlowDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ProjectBoard> Boards => Set<ProjectBoard>();
    public DbSet<BoardMember> BoardMembers => Set<BoardMember>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<LoadMetric> LoadMetrics => Set<LoadMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Publishes each tracked entity's domain events (see Entity.Raise) only after they've
    /// been successfully persisted, then clears them so they aren't re-published on the next save.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entitiesWithEvents = ChangeTracker.Entries<Entity>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count != 0)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entity in entitiesWithEvents)
        {
            var domainEvents = entity.DomainEvents.ToList();
            entity.ClearDomainEvents();

            foreach (var domainEvent in domainEvents)
            {
                var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
                await publisher.Publish(notification, cancellationToken);
            }
        }

        return result;
    }
}
