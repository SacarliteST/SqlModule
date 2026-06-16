using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;

namespace SQLModule.Data.Configurations;

internal sealed class CellValueConfiguration : IEntityTypeConfiguration<CellValue>
{
    public void Configure(EntityTypeBuilder<CellValue> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TextValue).HasMaxLength(4000);
        builder.ConfigureAudit();
    }
}
