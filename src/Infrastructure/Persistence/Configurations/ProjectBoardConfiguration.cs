using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class ProjectBoardConfiguration : IEntityTypeConfiguration<ProjectBoard>
{
    public void Configure(EntityTypeBuilder<ProjectBoard> builder)
    {
        builder.ToTable("project_boards");
        builder.HasKey(b => b.Id);
        builder.Ignore(b => b.DomainEvents);

        builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
        builder.Property(b => b.OwnerId).IsRequired();
        builder.Property(b => b.Color).HasMaxLength(7).IsRequired();
        builder.Property(b => b.CreatedAtUtc).IsRequired();

        // Tasks collection is backed by a private field (encapsulation) — EF Core
        // reads/writes it directly instead of requiring a public setter.
        builder.Metadata.FindNavigation(nameof(ProjectBoard.Tasks))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(b => b.Tasks)
            .WithOne()
            .HasForeignKey(t => t.BoardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
