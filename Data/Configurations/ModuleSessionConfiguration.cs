using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.ModuleIntegration;

namespace SQLModule.Data.Configurations;

internal sealed class ModuleSessionConfiguration : IEntityTypeConfiguration<ModuleSession>
{
    public void Configure(EntityTypeBuilder<ModuleSession> builder)
    {
        builder.ToTable("ModuleSessions");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).HasColumnName("SessionId");
        builder.Property(session => session.SessionKey).HasMaxLength(512).IsRequired();
        builder.Property(session => session.UserId).IsRequired();
        builder.Property(session => session.TaskRef).HasMaxLength(128).IsRequired();
        builder.Property(session => session.ReturnUrl).HasMaxLength(2048).IsRequired();
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
