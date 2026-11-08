using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class TaskValidationSkillTagConfiguration
    : IEntityTypeConfiguration<TaskValidationSkillTag>
{
    public void Configure(EntityTypeBuilder<TaskValidationSkillTag> builder)
    {
        builder.ToTable("TaskValidationSkillTags");
        builder.HasKey(tag => tag.Id);
        builder.HasIndex(tag => new { tag.ConfigurationId, tag.SkillId }).IsUnique();
        builder.HasOne(tag => tag.Configuration)
            .WithMany(configuration => configuration.SkillTags)
            .HasForeignKey(tag => tag.ConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(tag => tag.Skill)
            .WithMany()
            .HasForeignKey(tag => tag.SkillId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
