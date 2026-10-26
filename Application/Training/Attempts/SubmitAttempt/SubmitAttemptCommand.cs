using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Attempt;

namespace SQLModule.Application.Training.Attempts.SubmitAttempt;

/// <summary>Команда отправки SQL-решения студентом.</summary>
public sealed record SubmitAttemptCommand(
    Guid UserId,
    string StudentName,
    string? StudentEmail,
    Guid TaskId,
    string SubmittedSql,
    string IdempotencyKey,
    bool RequireModuleSession,
    Guid? ModuleSessionId)
    : IRequest<Result<SubmitAttemptResponse>>;
