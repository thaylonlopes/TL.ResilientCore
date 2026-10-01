using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TL.ResilientCore.Infrastructure.Outbox;

namespace TL.ResilientCore.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.RetryCount)
            .HasDefaultValue(0);

        builder.HasIndex(x => x.OccurredOnUtc)
               .HasFilter("\"ProcessedOnUtc\" IS NULL AND \"RetryCount\" < 5");
    }
}