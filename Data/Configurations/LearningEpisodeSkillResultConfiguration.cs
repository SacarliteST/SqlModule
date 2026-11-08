using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class LearningEpisodeSkillResultConfiguration
    : IEntityTypeConfiguration<LearningEpisodeSkillResult>
{
    public void Configure(EntityTypeBuilder<LearningEpisodeSkillResult> builder)
    {
        builder.ToTable("LearningEpisodeSkillResults");
        builder.HasKey(result => result.Id);
        builder.Property(result => result.SkillExternalId).HasMaxLength(100).IsRequired();
        builder.Property(result => result.Source).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(result => result.Outcome).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.HasOne(result => result.Episode)
            .WithMany(episode => episode.SkillResults)
            .HasForeignKey(result => result.EpisodeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(result => result.Skill)
            .WithMany()
            .HasForeignKey(result => result.SkillId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(result => new { result.EpisodeId, result.SkillId }).IsUnique();
        builder.HasIndex(result => result.SkillId);
    }
}
