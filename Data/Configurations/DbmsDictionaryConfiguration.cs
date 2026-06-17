using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;
using SQLModule.Domain.DbmsCatalog;

namespace SQLModule.Data.Configurations;

internal sealed class DbmsDictionaryConfiguration : IEntityTypeConfiguration<DbmsDictionary>
{
    public void Configure(EntityTypeBuilder<DbmsDictionary> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DbmsName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DbmsSystemName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DockerImage).IsRequired().HasMaxLength(200);
        builder.Property(x => x.DefaultPort).IsRequired();
        builder.Property(x => x.EnvUserKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.EnvPasswordKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.EnvDatabaseKey).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ExtraEnvConfig).HasMaxLength(500);
        builder.Property(x => x.DefaultDatabase).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DefaultUsername).IsRequired().HasMaxLength(100);
        builder.Property(x => x.DefaultPassword).IsRequired().HasMaxLength(100);
        builder.ConfigureAudit();

        builder.HasMany(x => x.PhysicalTypes)
               .WithOne(x => x.Dbms)
               .HasForeignKey(x => x.DbmsId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
