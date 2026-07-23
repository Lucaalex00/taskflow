using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class LoadMetricConfiguration : IEntityTypeConfiguration<LoadMetric>
{
    public void Configure(EntityTypeBuilder<LoadMetric> builder)
    {
        builder.ToTable("load_metrics");
        builder.HasKey(m => m.Id);
        builder.Ignore(m => m.DomainEvents);

        builder.Property(m => m.ActiveTaskCount).IsRequired();
        builder.Property(m => m.OverdueTaskCount).IsRequired();
        builder.Property(m => m.InProgressTaskCount).IsRequired();
        builder.Property(m => m.SnapshotAtUtc).IsRequired();

        // Board + time index: this is exactly how BoardLoadSpike evaluation
        // queries "the most recent snapshot before N minutes ago" for a board.
        builder.HasIndex(m => new { m.BoardId, m.SnapshotAtUtc });
    }
}
