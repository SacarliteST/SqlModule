using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class SqlQueryConfiguration : IEntityTypeConfiguration<SqlQuery>
{
    public void Configure(EntityTypeBuilder<SqlQuery> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QueryText).IsRequired();

        builder.Property(x => x.ExpectedResult)
            .HasColumnType(IsNpgsql(builder) ? "jsonb" : "TEXT");

        builder.HasOne<TargetDb>()
               .WithMany()
               .HasForeignKey(x => x.TargetDbId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.ConfigureAudit();
    }

    private static bool IsNpgsql(EntityTypeBuilder<SqlQuery> builder) =>
        builder.Metadata.Model.FindAnnotation("Relational:MaxIdentifierLength")?.Value is 63;
}
