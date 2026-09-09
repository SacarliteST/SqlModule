using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class AttemptConfiguration : IEntityTypeConfiguration<Attempt>
{
    public void Configure(EntityTypeBuilder<Attempt> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.StudentName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.StudentEmail).HasMaxLength(320);
        builder.Property(x => x.TaskId).IsRequired();
        builder.Property(x => x.ModuleSessionId);
        builder.Property(x => x.SubmittedSql).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().IsRequired();
        builder.Property(x => x.Reason).HasConversion<string>().IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.FinishedAt).IsRequired();
        builder.Property(x => x.ResultSnapshotState).HasConversion<string>().IsRequired();
        builder.Property(x => x.ActualColumnsJson).HasColumnType("jsonb");
        builder.Property(x => x.ActualRowsJson).HasColumnType("jsonb");
        builder.ConfigureAudit();

        builder.HasOne<SqlTask>()
               .WithMany()
               .HasForeignKey(x => x.TaskId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ModuleSession>()
               .WithMany()
               .HasForeignKey(x => x.ModuleSessionId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ModuleSessionId);
    }
}
