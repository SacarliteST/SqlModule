using SQLModule.Domain.Training;

namespace SQLModule.Web.Features.Training.Attempts;

internal sealed record CheckOutcome(bool IsCorrect, CheckReason Reason);
