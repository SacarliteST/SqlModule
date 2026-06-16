using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;

namespace SQLModule.Data.Configurations;

internal sealed class DataRecordConfiguration : IEntityTypeConfiguration<DataRecord>
{
    public void Configure(EntityTypeBuilder<DataRecord> builder)
    {
        builder.HasKey(x => x.Id);
        builder.ConfigureAudit();

        builder.HasMany(x => x.CellValues)
               .WithOne(x => x.DataRecord)
               .HasForeignKey(x => x.DataRecordId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
