using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.ModuleIntegration;

namespace SQLModule.Data.Configurations;

internal sealed class ModuleSessionConfiguration : IEntityTypeConfiguration<ModuleSession>
{
    public void Configure(EntityTypeBuilder<ModuleSession> builder)
    {
        builder.ToTable("ModuleSessions", table =>
        {
            table.HasCheckConstraint(
                "CK_ModuleSessions_MaxAttempts",
                "\"MaxAttempts\" IS NULL OR \"MaxAttempts\" > 0");
            table.HasCheckConstraint(
                "CK_ModuleSessions_FinalScore",
                "\"FinalScore\" IS NULL OR \"FinalScore\" BETWEEN 0 AND 100");
        });
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).HasColumnName("SessionId");
        builder.Property(session => session.SessionKey).HasMaxLength(512).IsRequired();
        builder.Property(session => session.UserId).IsRequired();
        builder.Property(session => session.TaskRef).HasMaxLength(128).IsRequired();
        builder.Property(session => session.ReturnUrl).HasMaxLength(2048).IsRequired();
        builder.Property(session => session.MaxAttempts);
        builder.Property(session => session.ValidationVersionId);
        builder.Property(session => session.FinalScore);
        builder.Property(session => session.FinalizationReason).HasConversion<string>().HasMaxLength(32);
        builder.Property(session => session.FinalizedAt);
        builder.Property(session => session.Status)
            .HasConversion(
                status => status.ToString().ToUpperInvariant(),
                value => Enum.Parse<ModuleSessionStatus>(value, true))
            .HasMaxLength(24)
            .IsRequired();
        builder.ConfigureAudit();

        builder.HasIndex(session => session.UserId);
        builder.HasIndex(session => new { session.Status, session.ExpiresAt });
    }
}
