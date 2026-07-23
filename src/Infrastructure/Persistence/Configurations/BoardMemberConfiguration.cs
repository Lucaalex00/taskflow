using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class BoardMemberConfiguration : IEntityTypeConfiguration<BoardMember>
{
    public void Configure(EntityTypeBuilder<BoardMember> builder)
    {
        builder.ToTable("board_members");
        builder.HasKey(m => m.Id);
        builder.Ignore(m => m.DomainEvents);

        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.JoinedAtUtc).IsRequired();

        builder.HasIndex(m => new { m.BoardId, m.UserId }).IsUnique();
    }
}
