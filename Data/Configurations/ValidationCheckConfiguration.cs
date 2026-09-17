using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class ValidationCheckConfiguration : IEntityTypeConfiguration<ValidationCheck>
{
    public void Configure(EntityTypeBuilder<ValidationCheck> builder)
    {
        builder.ToTable("ValidationChecks", table =>
        {
            table.HasCheckConstraint("CK_ValidationChecks_Weight", "\"Weight\" BETWEEN 1 AND 100");
            table.HasCheckConstraint("CK_ValidationChecks_Order", "\"Order\" >= 0");
        });
        builder.HasKey(check => check.Id);
        builder.Property(check => check.ConfigurationId).IsRequired();
        builder.Property(check => check.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(check => check.Value).HasMaxLength(256);
        builder.Property(check => check.UniquenessValue).HasMaxLength(256).IsRequired();
        builder.Property(check => check.Weight).IsRequired();
        builder.Property(check => check.Order).IsRequired();

        builder.HasIndex(check => new { check.ConfigurationId, check.Kind, check.UniquenessValue })
            .IsUnique();
        builder.HasIndex(check => new { check.ConfigurationId, check.Order }).IsUnique();
    }
}
