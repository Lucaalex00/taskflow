using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence;

public class TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options)
    : DbContext(options), ITaskFlowDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ProjectBoard> Boards => Set<ProjectBoard>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<LoadMetric> LoadMetrics => Set<LoadMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
