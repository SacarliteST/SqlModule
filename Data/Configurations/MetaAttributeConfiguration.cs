using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;
using SQLModule.Domain.Schema;

namespace SQLModule.Data.Configurations;

internal sealed class MetaAttributeConfiguration : IEntityTypeConfiguration<MetaAttribute>
{
    public void Configure(EntityTypeBuilder<MetaAttribute> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AttributeName).IsRequired().HasMaxLength(200);
        builder.ConfigureAudit();

        builder.HasOne(x => x.PhysicalType)
               .WithMany()
               .HasForeignKey(x => x.PhysicalTypeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.CellValues)
               .WithOne(x => x.MetaAttribute)
               .HasForeignKey(x => x.MetaAttributeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
