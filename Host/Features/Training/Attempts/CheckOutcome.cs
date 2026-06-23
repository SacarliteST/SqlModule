using SQLModule.Domain.Training;

namespace SQLModule.Host.Features.Training.Attempts;

internal sealed record CheckOutcome(bool IsCorrect, CheckReason Reason);
