using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class TaskValidationVersionConfiguration : IEntityTypeConfiguration<TaskValidationVersion>
{
    public void Configure(EntityTypeBuilder<TaskValidationVersion> builder)
    {
        builder.ToTable("TaskValidationVersions", table =>
        {
            table.HasCheckConstraint("CK_TaskValidationVersions_Number", "\"VersionNumber\" > 0");
            table.HasCheckConstraint(
                "CK_TaskValidationVersions_PassingScore",
                "\"PassingScore\" BETWEEN 1 AND 100");
            table.HasCheckConstraint(
                "CK_TaskValidationVersions_MaxAttempts",
                "\"MaxAttempts\" IS NULL OR \"MaxAttempts\" > 0");
            table.HasCheckConstraint(
                "CK_TaskValidationVersions_HintMask",
                "\"VisibleHintGroupsMask\" >= 0");
        });
        builder.HasKey(version => version.Id);
        builder.Property(version => version.TaskId).IsRequired();
        builder.Property(version => version.VersionNumber).IsRequired();
        builder.Property(version => version.ConfigurationVersion).IsRequired();
        builder.Property(version => version.PassingScore).IsRequired();
        builder.Property(version => version.VisibleHintGroupsMask).IsRequired();
        builder.Property(version => version.SchemaSnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(version => version.DatasetSnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(version => version.ReferenceQuerySnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(version => version.ExpectedResultSnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(version => version.ValidationConfigurationSnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(version => version.AnalyzerVersion).HasMaxLength(128).IsRequired();
        builder.Property(version => version.PublishedAt).IsRequired();
        builder.Property(version => version.PublishedById).IsRequired();
        builder.Property(version => version.PublishedByName).HasMaxLength(200).IsRequired();

        builder.HasOne(version => version.Task)
            .WithMany()
            .HasForeignKey(version => version.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(version => new { version.TaskId, version.VersionNumber }).IsUnique();
        builder.HasIndex(version => new { version.TaskId, version.ConfigurationVersion }).IsUnique();
    }
}
