using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class BankSolutionAccessConfiguration : IEntityTypeConfiguration<BankSolutionAccess>
{
    public void Configure(EntityTypeBuilder<BankSolutionAccess> builder)
    {
        builder.ToTable("BankSolutionAccesses");
        builder.HasKey(access => access.Id);
        builder.Property(access => access.UserId).IsRequired();
        builder.Property(access => access.TaskId).IsRequired();
        builder.Property(access => access.OpenedAt).IsRequired();

        builder.HasOne<SqlTask>()
            .WithMany()
            .HasForeignKey(access => access.TaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(access => new { access.UserId, access.TaskId, access.OpenedAt });
        builder.HasIndex(access => new { access.TaskId, access.OpenedAt });
    }
}
