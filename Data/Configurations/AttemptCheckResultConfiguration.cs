using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class AttemptCheckResultConfiguration : IEntityTypeConfiguration<AttemptCheckResult>
{
    public void Configure(EntityTypeBuilder<AttemptCheckResult> builder)
    {
        builder.ToTable("AttemptCheckResults", table =>
        {
            table.HasCheckConstraint("CK_AttemptCheckResults_Weight", "\"Weight\" BETWEEN 1 AND 100");
            table.HasCheckConstraint(
                "CK_AttemptCheckResults_AwardedScore",
                "\"AwardedScore\" >= 0 AND \"AwardedScore\" <= \"Weight\"");
            table.HasCheckConstraint(
                "CK_AttemptCheckResults_BinaryScore",
                "(\"Status\" = 'Passed' AND \"AwardedScore\" = \"Weight\") OR " +
                "(\"Status\" IN ('Failed', 'NotEvaluated') AND \"AwardedScore\" = 0)");
            table.HasCheckConstraint("CK_AttemptCheckResults_Order", "\"Order\" >= 0");
        });
        builder.HasKey(result => result.Id);
        builder.Property(result => result.AttemptId).IsRequired();
        builder.Property(result => result.ValidationCheckId).IsRequired();
        builder.Property(result => result.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(result => result.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(result => result.Weight).IsRequired();
        builder.Property(result => result.AwardedScore).IsRequired();
        builder.Property(result => result.Order).IsRequired();
        builder.Property(result => result.Message).HasMaxLength(2000);
        builder.Property(result => result.DiagnosticJson).HasColumnType("jsonb");

        builder.HasOne(result => result.Attempt)
            .WithMany()
            .HasForeignKey(result => result.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(result => new { result.AttemptId, result.Order }).IsUnique();
        builder.HasIndex(result => result.ValidationCheckId);
    }
}
