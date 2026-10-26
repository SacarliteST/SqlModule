using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

/// <summary>Настраивает хранение журнала выдачи подсказок открытого банка.</summary>
internal sealed class BankHintAccessConfiguration : IEntityTypeConfiguration<BankHintAccess>
{
    public void Configure(EntityTypeBuilder<BankHintAccess> builder)
    {
        builder.ToTable("BankHintAccesses", table =>
        {
            table.HasCheckConstraint("CK_BankHintAccesses_AttemptNumber", "\"AttemptNumber\" > 0");
            table.HasCheckConstraint("CK_BankHintAccesses_HintMask", "\"AutoHintGroupsMask\" >= 0");
        });
        builder.HasKey(access => access.Id);
        builder.Property(access => access.UserId).IsRequired();
        builder.Property(access => access.TaskId).IsRequired();
        builder.Property(access => access.AttemptId).IsRequired();
        builder.Property(access => access.AttemptNumber).IsRequired();
        builder.Property(access => access.AutoHintGroupsMask).IsRequired();
        builder.Property(access => access.AuthorHintIssued).IsRequired();
        builder.Property(access => access.RequestedAt).IsRequired();

        builder.HasOne<SqlTask>()
            .WithMany()
            .HasForeignKey(access => access.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Attempt>()
            .WithMany()
            .HasForeignKey(access => access.AttemptId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(access => new { access.UserId, access.TaskId, access.RequestedAt });
        builder.HasIndex(access => new { access.TaskId, access.RequestedAt });
        builder.HasIndex(access => access.AttemptId);
    }
}
