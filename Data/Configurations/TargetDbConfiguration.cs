using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;
using SQLModule.Domain.Schema;

namespace SQLModule.Data.Configurations;

internal sealed class TargetDbConfiguration : IEntityTypeConfiguration<TargetDb>
{
    public void Configure(EntityTypeBuilder<TargetDb> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DbName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.SchemaVersion).IsConcurrencyToken();
        builder.ConfigureAudit();

        builder.HasIndex(x => new { x.DbmsId, x.DbName }).IsUnique();

        builder.HasMany(x => x.MetaTables)
               .WithOne(x => x.TargetDb)
               .HasForeignKey(x => x.TargetDbId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
