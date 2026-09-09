using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.ModuleIntegration;

namespace SQLModule.Data.Configurations;

internal sealed class PendingPublishConfiguration : IEntityTypeConfiguration<PendingPublish>
{
    public void Configure(EntityTypeBuilder<PendingPublish> builder)
    {
        builder.ToTable("PendingPublishes");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Kind)
            .HasConversion(
                kind => kind.ToString().ToUpperInvariant(),
                value => Enum.Parse<PendingPublishKind>(value, true))
            .HasMaxLength(16)
            .IsRequired();
        builder.Property(message => message.SessionId).IsRequired();
        builder.Property(message => message.DeduplicationKey).HasMaxLength(200).IsRequired();
        builder.Property(message => message.MessageJson).HasColumnType("jsonb").IsRequired();
        builder.Property(message => message.Attempts).HasDefaultValue(0).IsRequired();
        builder.Property(message => message.NextAttemptAt).IsRequired();
        builder.Property(message => message.CreatedAt).IsRequired();
        builder.HasIndex(message => message.DeduplicationKey).IsUnique();
        builder.HasIndex(message => new { message.SentAt, message.DeadLetterAt, message.NextAttemptAt });
        builder.HasIndex(message => message.SessionId);
    }
}
