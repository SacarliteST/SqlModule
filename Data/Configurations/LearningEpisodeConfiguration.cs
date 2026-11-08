using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class LearningEpisodeConfiguration : IEntityTypeConfiguration<LearningEpisode>
{
    public void Configure(EntityTypeBuilder<LearningEpisode> builder)
    {
        builder.ToTable("LearningEpisodes", table =>
        {
            table.HasCheckConstraint(
                "CK_LearningEpisodes_Finalization",
                "(\"Status\" = 'Active' AND \"FinalizedAt\" IS NULL AND \"FinalizationReason\" IS NULL) OR " +
                "(\"Status\" = 'Finalized' AND \"FinalizedAt\" IS NOT NULL AND \"FinalizationReason\" IS NOT NULL)");
        });
        builder.HasKey(episode => episode.Id);
        builder.Property(episode => episode.StudentId).IsRequired();
        builder.Property(episode => episode.TaskId).IsRequired();
        builder.Property(episode => episode.ValidationVersionId).IsRequired();
        builder.Property(episode => episode.ModuleSessionId);
        builder.Property(episode => episode.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(episode => episode.StartedAt).IsRequired();
        builder.Property(episode => episode.LastHeartbeatAt).IsRequired();
        builder.Property(episode => episode.FinalizedAt);
        builder.Property(episode => episode.FinalizationReason).HasConversion<string>().HasMaxLength(32);
        builder.Property(episode => episode.FinalAttemptId);
        builder.Property(episode => episode.ProducesObservation).IsRequired();

        builder.HasOne<SqlTask>()
            .WithMany()
            .HasForeignKey(episode => episode.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TaskValidationVersion>()
            .WithMany()
            .HasForeignKey(episode => episode.ValidationVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ModuleSession>()
            .WithMany()
            .HasForeignKey(episode => episode.ModuleSessionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Attempt>()
            .WithMany()
            .HasForeignKey(episode => episode.FinalAttemptId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(episode => episode.ModuleSessionId)
            .IsUnique()
            .HasFilter("\"ModuleSessionId\" IS NOT NULL");
        builder.HasIndex(episode => new { episode.StudentId, episode.TaskId })
            .IsUnique()
            .HasFilter("\"ModuleSessionId\" IS NULL AND \"Status\" = 'Active'");
        builder.HasIndex(episode => new { episode.Status, episode.LastHeartbeatAt });
        builder.Navigation(episode => episode.SkillResults)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
