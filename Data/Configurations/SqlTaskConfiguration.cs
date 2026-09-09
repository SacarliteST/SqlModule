using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class SqlTaskConfiguration : IEntityTypeConfiguration<SqlTask>
{
    public void Configure(EntityTypeBuilder<SqlTask> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TaskName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.TaskText).IsRequired();
        builder.Property(x => x.PublicationStatus).HasConversion<string>().IsRequired();
        builder.ConfigureAudit();

        builder.HasOne(x => x.SqlQuery)
               .WithOne(x => x.Task)
               .HasForeignKey<SqlTask>(x => x.SqlQueryId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
