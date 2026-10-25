using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class SqlTaskConfiguration : IEntityTypeConfiguration<SqlTask>
{
    public void Configure(EntityTypeBuilder<SqlTask> builder)
    {
        builder.ToTable("SqlTasks", table =>
        {
            table.HasCheckConstraint(
                "CK_SqlTasks_BankPublication",
                "(\"BankNumber\" IS NULL AND \"BankPublishedAt\" IS NULL) OR " +
                "(\"BankNumber\" IS NOT NULL AND \"BankNumber\" > 0 AND \"BankPublishedAt\" IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TaskName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.TaskText).IsRequired();
        builder.Property(x => x.PublicationStatus).HasConversion<string>().IsRequired();
        builder.Property(x => x.ActiveValidationVersionId);
        builder.Property(x => x.Visibility)
            .HasConversion<string>()
            .HasDefaultValue(TaskVisibility.Control)
            .IsRequired();
        builder.Property(x => x.BankNumber);
        builder.Property(x => x.BankPublishedAt);
        builder.Property(x => x.BankHint);
        builder.Property(x => x.ShowSolution).HasDefaultValue(true).IsRequired();
        builder.Property(x => x.SolutionExplanation);
        builder.ConfigureAudit();

        builder.HasIndex(x => x.BankNumber).IsUnique();
        builder.HasIndex(x => new { x.Visibility, x.PublicationStatus, x.BankPublishedAt });

        builder.HasOne(x => x.SqlQuery)
               .WithOne(x => x.Task)
               .HasForeignKey<SqlTask>(x => x.SqlQueryId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ActiveValidationVersion)
               .WithMany()
               .HasForeignKey(x => x.ActiveValidationVersionId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
