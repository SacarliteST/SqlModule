using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;

namespace SQLModule.Application.Training.Validation.PublishTaskValidation;

/// <summary>Команда публикации неизменяемой validation-версии задания.</summary>
/// <param name="TaskId">Идентификатор задания.</param>
/// <param name="Version">Версия редактируемой конфигурации.</param>
/// <param name="IdempotencyKey">Ключ идемпотентности.</param>
public sealed record PublishTaskValidationCommand(Guid TaskId, Guid Version, Guid IdempotencyKey)
    : IRequest<Result<TaskValidationConfigurationResponse>>;
