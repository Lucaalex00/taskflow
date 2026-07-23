using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class BoardInvitationConfiguration : IEntityTypeConfiguration<BoardInvitation>
{
    public void Configure(EntityTypeBuilder<BoardInvitation> builder)
    {
        builder.ToTable("board_invitations");
        builder.HasKey(i => i.Id);
        builder.Ignore(i => i.DomainEvents);

        builder.Property(i => i.InviteeEmail).HasMaxLength(320).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.CreatedAtUtc).IsRequired();

        builder.HasIndex(i => i.InviteeEmail);
        builder.HasIndex(i => new { i.BoardId, i.InviteeEmail });
    }
}
