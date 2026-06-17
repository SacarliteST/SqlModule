using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;
using SQLModule.Domain.Schema;

namespace SQLModule.Data.Configurations;

internal sealed class MetaTableConfiguration : IEntityTypeConfiguration<MetaTable>
{
    public void Configure(EntityTypeBuilder<MetaTable> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TableName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.ConfigureAudit();

        builder.HasMany(x => x.Attributes)
               .WithOne(x => x.MetaTable)
               .HasForeignKey(x => x.MetaTableId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.DataRecords)
               .WithOne(x => x.MetaTable)
               .HasForeignKey(x => x.MetaTableId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
