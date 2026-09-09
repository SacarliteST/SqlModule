using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.Schema;

namespace SQLModule.Data.Configurations;

internal sealed class MutationReceiptConfiguration : IEntityTypeConfiguration<MutationReceipt>
{
    public void Configure(EntityTypeBuilder<MutationReceipt> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Scope).HasMaxLength(300).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PayloadHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ResponseJson).IsRequired();
        builder.HasIndex(x => new { x.Scope, x.IdempotencyKey }).IsUnique();
    }
}
