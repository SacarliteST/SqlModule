using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("Skills");
        builder.HasKey(skill => skill.Id);
        builder.Property(skill => skill.SkillId).HasMaxLength(100).IsRequired();
        builder.Property(skill => skill.Name).HasMaxLength(300).IsRequired();
        builder.Property(skill => skill.Description).HasMaxLength(2000);
        builder.Property(skill => skill.Origin).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(skill => skill.IsDeleted).HasDefaultValue(false).IsRequired();
        builder.HasIndex(skill => skill.SkillId).IsUnique();
        builder.HasIndex(skill => new { skill.IsDeleted, skill.Name });
        builder.ConfigureAudit();
    }
}
