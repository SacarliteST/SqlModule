using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;
using SQLModule.Domain.Schema;

namespace SQLModule.Data.Configurations;

internal sealed class MetaRelationshipConfiguration : IEntityTypeConfiguration<MetaRelationship>
{
    public void Configure(EntityTypeBuilder<MetaRelationship> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RelationshipName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.DeleteRule).HasMaxLength(50);
        builder.Property(x => x.UpdateRule).HasMaxLength(50);
        builder.ConfigureAudit();

        builder.HasOne(x => x.SourceAttribute)
               .WithMany()
               .HasForeignKey(x => x.SourceAttributeId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TargetAttribute)
               .WithMany()
               .HasForeignKey(x => x.TargetAttributeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
