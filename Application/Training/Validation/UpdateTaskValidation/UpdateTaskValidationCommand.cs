using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Training.Validation;

namespace SQLModule.Application.Training.Validation.UpdateTaskValidation;

/// <summary>Команда обновления validation-конфигурации задания.</summary>
public sealed record UpdateTaskValidationCommand(
    Guid TaskId,
    Guid Version,
    TaskValidationDefinition Definition)
    : IRequest<Result<TaskValidationConfigurationResponse>>;
