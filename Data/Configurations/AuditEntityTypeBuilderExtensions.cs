using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;

namespace SQLModule.Data.Configurations;

internal static class AuditEntityTypeBuilderExtensions
{
    internal static void ConfigureAudit<T>(this EntityTypeBuilder<T> builder) where T : AuditableEntity
    {
        builder.Property(x => x.CreatedById).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedById).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
    }
}
