using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;

namespace SQLModule.Data.Configurations;

internal sealed class ParameterDefinitionConfiguration : IEntityTypeConfiguration<ParameterDefinition>
{
    public void Configure(EntityTypeBuilder<ParameterDefinition> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ParameterKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.InputType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.DefaultValue).HasMaxLength(500);
        builder.Property(x => x.SqlFragment).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ValuePrefix).HasMaxLength(50);
        builder.Property(x => x.ValueSuffix).HasMaxLength(50);
        builder.Property(x => x.Separator).HasMaxLength(10);
        builder.ConfigureAudit();

        builder.HasMany(x => x.AttributeParameterValues)
               .WithOne(x => x.ParameterDefinition)
               .HasForeignKey(x => x.ParameterDefinitionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
