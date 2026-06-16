using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;

namespace SQLModule.Data.Configurations;

internal sealed class SqlTaskConfiguration : IEntityTypeConfiguration<SqlTask>
{
    public void Configure(EntityTypeBuilder<SqlTask> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TaskName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.TaskText).IsRequired();
        builder.ConfigureAudit();

        builder.HasOne(x => x.TargetDb)
               .WithMany()
               .HasForeignKey(x => x.TargetDbId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SqlQuery)
               .WithMany()
               .HasForeignKey(x => x.SqlQueryId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
