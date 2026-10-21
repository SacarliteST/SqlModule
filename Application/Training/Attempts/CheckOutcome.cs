using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Attempts;

public sealed record CheckOutcome(bool IsCorrect, CheckReason Reason);
