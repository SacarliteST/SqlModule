using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;

namespace SQLModule.Web.Features.Training.Progress;

internal sealed record AttemptReservationResult(AttemptReservation Reservation, bool IsReplay);

internal interface IAttemptReservationService
{
    Task<Result<AttemptReservationResult>> ReserveAsync(
        Guid progressId,
        Guid idempotencyKey,
        string payloadHash,
        CancellationToken ct);

    Task CompleteAsync(Guid reservationId, Guid attemptId, CancellationToken ct);

    Task ReleaseAsync(Guid reservationId, CancellationToken ct);
}

internal sealed class AttemptReservationService(
    AppDbContext db,
    TimeProvider timeProvider,
    ILogger<AttemptReservationService> logger) : IAttemptReservationService
{
    private const int MaxConcurrencyRetries = 3;

    public async Task<Result<AttemptReservationResult>> ReserveAsync(
        Guid progressId,
        Guid idempotencyKey,
        string payloadHash,
        CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var existing = await db.AttemptReservations
                .SingleOrDefaultAsync(value =>
                    value.ProgressId == progressId && value.IdempotencyKey == idempotencyKey, ct);
            if (existing is not null && existing.PayloadHash != payloadHash)
            {
                return Result<AttemptReservationResult>.Fail(ProgressErrors.IdempotencyPayloadMismatch);
            }

            if (existing is not null && existing.State != AttemptReservationState.Released)
            {
                return new AttemptReservationResult(existing, true);
            }

            var progress = await db.StudentTaskProgresses
                .Include(value => value.ValidationVersion)
                .SingleOrDefaultAsync(value => value.Id == progressId, ct);
            if (progress is null)
            {
                return Result<AttemptReservationResult>.Fail(ProgressErrors.NotFound);
            }

            if (progress.Status != ProgressStatus.Active ||
                progress.ExpiresAt.HasValue && progress.ExpiresAt.Value <= timeProvider.GetUtcNow())
            {
                return Result<AttemptReservationResult>.Fail(ProgressErrors.Closed);
            }

            var reservedCount = await db.AttemptReservations.CountAsync(value =>
                value.ProgressId == progressId && value.State == AttemptReservationState.Reserved &&
                value.IdempotencyKey != idempotencyKey, ct);
            if (progress.ValidationVersion.MaxAttempts.HasValue &&
                progress.AttemptsUsed + reservedCount >= progress.ValidationVersion.MaxAttempts.Value)
            {
                return Result<AttemptReservationResult>.Fail(ProgressErrors.AttemptsExhausted);
            }

            var now = timeProvider.GetUtcNow();
            var number = progress.ReserveAttemptNumber();
            var reservation = existing ?? AttemptReservation.Create(
                progressId, number, idempotencyKey, payloadHash, now);
            if (existing is null)
            {
                db.AttemptReservations.Add(reservation);
            }
            else
            {
                reservation.Reopen(number, now);
            }

            try
            {
                await db.SaveChangesAsync(ct);
                logger.LogDebug(
                    "Для прохождения {ProgressId} зарезервирована попытка {AttemptNumber}",
                    progressId, number);
                return new AttemptReservationResult(reservation, false);
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
                if (attempt == MaxConcurrencyRetries)
                {
                    break;
                }
            }
        }

        logger.LogWarning(
            "Не удалось зарезервировать попытку для прохождения {ProgressId} из-за конкурентных запросов",
            progressId);
        return Result<AttemptReservationResult>.Fail(Error.Conflict(
            "Progress.ConcurrentReservation",
            "Не удалось зарезервировать попытку. Повторите запрос."));
    }

    public async Task CompleteAsync(Guid reservationId, Guid attemptId, CancellationToken ct)
    {
        var reservation = await db.AttemptReservations.SingleAsync(value => value.Id == reservationId, ct);
        reservation.Complete(attemptId, timeProvider.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    public async Task ReleaseAsync(Guid reservationId, CancellationToken ct)
    {
        var reservation = await db.AttemptReservations.SingleAsync(value => value.Id == reservationId, ct);
        reservation.Release(timeProvider.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }
}
