using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class AttemptConfiguration : IEntityTypeConfiguration<Attempt>
{
    public void Configure(EntityTypeBuilder<Attempt> builder)
    {
        builder.HasKey(x => x.Id);
        // UserId — мягкая ссылка на Identity-сервис, FK не создаём
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.StartAttempt).IsRequired();
        builder.Property(x => x.EndAttempt).IsRequired();
        builder.ConfigureAudit();

        builder.HasOne<SqlTask>()
               .WithMany()
               .HasForeignKey(x => x.TaskId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SqlQuery>()
               .WithMany()
               .HasForeignKey(x => x.QueryId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
