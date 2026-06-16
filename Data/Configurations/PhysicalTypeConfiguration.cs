using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;

namespace SQLModule.Data.Configurations;

internal sealed class PhysicalTypeConfiguration : IEntityTypeConfiguration<PhysicalType>
{
    public void Configure(EntityTypeBuilder<PhysicalType> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TypeName).IsRequired().HasMaxLength(100);
        builder.ConfigureAudit();

        builder.HasMany(x => x.ParameterDefinitions)
               .WithOne(x => x.PhysicalType)
               .HasForeignKey(x => x.PhysicalTypeId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
