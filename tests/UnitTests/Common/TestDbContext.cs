using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.UnitTests.Common;

/// <summary>
/// Minimal EF Core InMemory-backed context used only by Application-layer unit tests,
/// so CQRS handlers can be exercised without spinning up a real Postgres instance.
/// Full round-trip behavior against a real database is covered by IntegrationTests instead.
/// </summary>
public sealed class TestDbContext : DbContext, ITaskFlowDbContext
{
    public TestDbContext() : base(new DbContextOptionsBuilder<TestDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<ProjectBoard> Boards => Set<ProjectBoard>();
    public DbSet<BoardMember> BoardMembers => Set<BoardMember>();
    public DbSet<BoardInvitation> BoardInvitations => Set<BoardInvitation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<LoadMetric> LoadMetrics => Set<LoadMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Only what's needed for InMemory: private-field-backed navigation + ignored domain events.
        modelBuilder.Entity<ProjectBoard>(b =>
        {
            b.Metadata.FindNavigation(nameof(ProjectBoard.Tasks))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
            b.HasMany(x => x.Tasks).WithOne().HasForeignKey(t => t.BoardId);
            b.Ignore(x => x.DomainEvents);
        });

        modelBuilder.Entity<User>().Ignore(x => x.DomainEvents);
        modelBuilder.Entity<BoardMember>().Ignore(x => x.DomainEvents);
        modelBuilder.Entity<BoardInvitation>().Ignore(x => x.DomainEvents);
        modelBuilder.Entity<Notification>().Ignore(x => x.DomainEvents);
        modelBuilder.Entity<TaskItem>().Ignore(x => x.DomainEvents);
        modelBuilder.Entity<AlertRule>().Ignore(x => x.DomainEvents);
        modelBuilder.Entity<Alert>().Ignore(x => x.DomainEvents);
        modelBuilder.Entity<LoadMetric>().Ignore(x => x.DomainEvents);

        base.OnModelCreating(modelBuilder);
    }
}
