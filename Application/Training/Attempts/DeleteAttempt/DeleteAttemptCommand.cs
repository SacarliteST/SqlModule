using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;

namespace SQLModule.Application.Training.Attempts;

/// <summary>Команда административного удаления попытки.</summary>
public sealed record DeleteAttemptCommand(Guid Id) : IRequest<Result>;
