using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
{
    public void Configure(EntityTypeBuilder<AlertRule> builder)
    {
        builder.ToTable("alert_rules");
        builder.HasKey(r => r.Id);
        builder.Ignore(r => r.DomainEvents);

        builder.Property(r => r.RuleType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(r => r.Threshold).IsRequired();
        builder.Property(r => r.EvaluationWindowMinutes).IsRequired();
        builder.Property(r => r.IsEnabled).IsRequired();

        builder.HasIndex(r => new { r.BoardId, r.IsEnabled });
    }
}
