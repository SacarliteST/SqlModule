using FluentValidation;
using Microsoft.Extensions.Options;
using SQLModule.Application.Training.Bank;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Web.Features.Training.SqlTasks.TaskBank;

/// <summary>Проверяет запрос изменения материалов задания открытого банка.</summary>
internal sealed class UpdateTaskBankContentValidator : AbstractValidator<UpdateTaskBankContentRequest>
{
    /// <summary>Создаёт валидатор с настроенными ограничениями длины.</summary>
    public UpdateTaskBankContentValidator(IOptions<TaskBankOptions> options)
    {
        RuleFor(value => value.ShowSolution).NotNull();
        RuleFor(value => value.BankHint)
            .MaximumLength(options.Value.HintMaxLength)
            .When(value => value.BankHint is not null);
        RuleFor(value => value.SolutionExplanation)
            .MaximumLength(options.Value.SolutionExplanationMaxLength)
            .When(value => value.SolutionExplanation is not null);
    }
}
