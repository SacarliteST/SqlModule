using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SQLModule.Domain.Training;

namespace SQLModule.Data.Configurations;

internal sealed class AttemptReservationConfiguration : IEntityTypeConfiguration<AttemptReservation>
{
    public void Configure(EntityTypeBuilder<AttemptReservation> builder)
    {
        builder.ToTable("AttemptReservations", table =>
            table.HasCheckConstraint("CK_AttemptReservations_Number", "\"AttemptNumber\" > 0"));
        builder.HasKey(reservation => reservation.Id);
        builder.Property(reservation => reservation.ProgressId).IsRequired();
        builder.Property(reservation => reservation.AttemptNumber).IsRequired();
        builder.Property(reservation => reservation.IdempotencyKey).IsRequired();
        builder.Property(reservation => reservation.PayloadHash).HasMaxLength(64).IsRequired();
        builder.Property(reservation => reservation.State).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(reservation => reservation.CreatedAt).IsRequired();
        builder.Property(reservation => reservation.UpdatedAt).IsRequired();

        builder.HasOne(reservation => reservation.Progress)
            .WithMany()
            .HasForeignKey(reservation => reservation.ProgressId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Attempt>()
            .WithMany()
            .HasForeignKey(reservation => reservation.AttemptId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(reservation => new { reservation.ProgressId, reservation.AttemptNumber }).IsUnique();
        builder.HasIndex(reservation => new { reservation.ProgressId, reservation.IdempotencyKey }).IsUnique();
        builder.HasIndex(reservation => reservation.AttemptId)
            .IsUnique()
            .HasFilter("\"AttemptId\" IS NOT NULL");
        builder.HasIndex(reservation => new { reservation.State, reservation.UpdatedAt });
    }
}
