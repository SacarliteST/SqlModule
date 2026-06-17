using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;
using SQLModule.Domain.Schema;

namespace SQLModule.Data.Configurations;

internal sealed class AttributeParameterValueConfiguration : IEntityTypeConfiguration<AttributeParameterValue>
{
    public void Configure(EntityTypeBuilder<AttributeParameterValue> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ParameterValue).IsRequired().HasMaxLength(500);
        builder.ConfigureAudit();

        builder.HasOne(x => x.MetaAttribute)
               .WithMany()
               .HasForeignKey(x => x.MetaAttributeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
