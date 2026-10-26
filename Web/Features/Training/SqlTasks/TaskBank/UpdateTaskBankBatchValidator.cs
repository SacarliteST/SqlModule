using FluentValidation;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Web.Features.Training.SqlTasks.TaskBank;

/// <summary>Проверяет запрос пакетного изменения заданий банка.</summary>
internal sealed class UpdateTaskBankBatchValidator : AbstractValidator<UpdateTaskBankBatchRequest>
{
    /// <summary>Создаёт валидатор пакетной операции.</summary>
    public UpdateTaskBankBatchValidator()
    {
        RuleFor(request => request.TaskIds)
            .NotNull()
            .NotEmpty()
            .Must(taskIds => taskIds is null || taskIds.Count <= 100)
            .WithMessage("За один запрос можно изменить не более 100 заданий.");
        RuleForEach(request => request.TaskIds).NotEmpty();
        RuleFor(request => request.Open).NotNull();
    }
}
