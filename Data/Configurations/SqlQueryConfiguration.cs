using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class SqlQueryConfiguration : IEntityTypeConfiguration<SqlQuery>
{
    public void Configure(EntityTypeBuilder<SqlQuery> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QueryText).IsRequired();
        builder.ConfigureAudit();
    }
}
