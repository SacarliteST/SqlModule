using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class StudentTaskProgressConfiguration : IEntityTypeConfiguration<StudentTaskProgress>
{
    public void Configure(EntityTypeBuilder<StudentTaskProgress> builder)
    {
        builder.ToTable("StudentTaskProgresses", table =>
        {
            table.HasCheckConstraint("CK_StudentTaskProgresses_AttemptsUsed", "\"AttemptsUsed\" >= 0");
            table.HasCheckConstraint("CK_StudentTaskProgresses_NextAttemptNumber", "\"NextAttemptNumber\" > 0");
            table.HasCheckConstraint("CK_StudentTaskProgresses_BestScore", "\"BestScore\" BETWEEN 0 AND 100");
            table.HasCheckConstraint(
                "CK_StudentTaskProgresses_FinalScore",
                "\"FinalScore\" IS NULL OR \"FinalScore\" BETWEEN 0 AND 100");
            table.HasCheckConstraint(
                "CK_StudentTaskProgresses_Finalization",
                "(\"FinalScore\" IS NULL AND \"FinalizationReason\" IS NULL AND \"FinalizedAt\" IS NULL) OR " +
                "(\"FinalScore\" IS NOT NULL AND \"FinalizationReason\" IS NOT NULL AND \"FinalizedAt\" IS NOT NULL)");
        });
        builder.HasKey(progress => progress.Id);
        builder.Property(progress => progress.UserId).IsRequired();
        builder.Property(progress => progress.TaskId).IsRequired();
        builder.Property(progress => progress.ValidationVersionId).IsRequired();
        builder.Property(progress => progress.AttemptsUsed).IsRequired();
        builder.Property(progress => progress.NextAttemptNumber).IsRequired();
        builder.Property(progress => progress.BestScore).IsRequired();
        builder.Property(progress => progress.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(progress => progress.FinalizationReason).HasConversion<string>().HasMaxLength(32);
        builder.Property(progress => progress.ConcurrencyVersion).IsConcurrencyToken().IsRequired();
        builder.ConfigureAudit();

        builder.HasOne(progress => progress.Task)
            .WithMany()
            .HasForeignKey(progress => progress.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(progress => progress.ValidationVersion)
            .WithMany()
            .HasForeignKey(progress => progress.ValidationVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ModuleSession>()
            .WithMany()
            .HasForeignKey(progress => progress.ModuleSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(progress => progress.ModuleSessionId)
            .IsUnique()
            .HasFilter("\"ModuleSessionId\" IS NOT NULL");
        builder.HasIndex(progress => new { progress.UserId, progress.TaskId })
            .IsUnique()
            .HasFilter(
                "\"ModuleSessionId\" IS NULL AND \"Status\" IN " +
                "('Active', 'Finalizing', 'CompletionPending', 'CompletionFailed')");
        builder.HasIndex(progress => progress.ValidationVersionId);
        builder.HasIndex(progress => new { progress.Status, progress.ExpiresAt });
    }
}
