using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class AttemptConfiguration : IEntityTypeConfiguration<Attempt>
{
    public void Configure(EntityTypeBuilder<Attempt> builder)
    {
        builder.ToTable("Attempts", table =>
        {
            table.HasCheckConstraint(
                "CK_Attempts_Phase2bScoring",
                "(\"ProgressId\" IS NULL AND \"ValidationVersionId\" IS NULL AND " +
                "\"AttemptNumber\" IS NULL AND \"Score\" IS NULL) OR " +
                "(\"ProgressId\" IS NOT NULL AND \"ValidationVersionId\" IS NOT NULL AND " +
                "\"AttemptNumber\" IS NOT NULL AND \"Score\" IS NOT NULL)");
            table.HasCheckConstraint(
                "CK_Attempts_AttemptNumber",
                "\"AttemptNumber\" IS NULL OR \"AttemptNumber\" > 0");
            table.HasCheckConstraint(
                "CK_Attempts_Score",
                "\"Score\" IS NULL OR \"Score\" BETWEEN 0 AND 100");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.StudentName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.StudentEmail).HasMaxLength(320);
        builder.Property(x => x.TaskId).IsRequired();
        builder.Property(x => x.ModuleSessionId);
        builder.Property(x => x.ProgressId);
        builder.Property(x => x.ValidationVersionId);
        builder.Property(x => x.AttemptNumber);
        builder.Property(x => x.Score);
        builder.Property(x => x.CountsTowardLimit).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.SubmittedSql).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().IsRequired();
        builder.Property(x => x.Reason).HasConversion<string>().IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.FinishedAt).IsRequired();
        builder.Property(x => x.ResultSnapshotState).HasConversion<string>().IsRequired();
        builder.Property(x => x.ActualColumnsJson).HasColumnType("jsonb");
        builder.Property(x => x.ActualRowsJson).HasColumnType("jsonb");
        builder.ConfigureAudit();

        builder.HasOne<SqlTask>()
               .WithMany()
               .HasForeignKey(x => x.TaskId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ModuleSession>()
               .WithMany()
               .HasForeignKey(x => x.ModuleSessionId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StudentTaskProgress>()
               .WithMany()
               .HasForeignKey(x => x.ProgressId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TaskValidationVersion>()
               .WithMany()
               .HasForeignKey(x => x.ValidationVersionId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ModuleSessionId);
        builder.HasIndex(x => new { x.ProgressId, x.AttemptNumber }).IsUnique();
        builder.HasIndex(x => x.ValidationVersionId);
    }
}
