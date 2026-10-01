using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TL.ResilientCore.Infrastructure.Outbox;

namespace TL.ResilientCore.Infrastructure.Persistence.Configurations;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ReceivedOnUtc)
            .IsRequired();

        builder.Property(x => x.ProcessedOnUtc);

        builder.Property(x => x.Error)
            .HasMaxLength(2000);

        builder.HasIndex(x => new { x.Id, x.Type })
            .IsUnique();
    }
}
