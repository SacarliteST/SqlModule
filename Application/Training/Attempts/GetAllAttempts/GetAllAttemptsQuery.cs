using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;

namespace SQLModule.Application.Training.Attempts;

/// <summary>Запрос журнала попыток преподавателя.</summary>
public sealed record GetAllAttemptsQuery(
    int Offset,
    int Limit,
    Guid? TaskId,
    Guid? UserId,
    Guid? TopicId,
    Domain.Training.ExecutionStatus? Status,
    bool? IsCorrect,
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    Guid? ValidationVersionId,
    int? ScoreFrom,
    int? ScoreTo,
    Domain.Training.FinalizationReason? FinalizationReason,
    AttemptOrigin? Origin)
    : IRequest<Result<PageResponse<AttemptListItemResponse>>>;
