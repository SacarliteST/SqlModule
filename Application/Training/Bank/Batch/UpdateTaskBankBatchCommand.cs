using SQLModule.Application.Cqrs;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;

namespace SQLModule.Application.Training.Bank;

/// <summary>Команда пакетного изменения видимости заданий банка.</summary>
/// <param name="TaskIds">Идентификаторы заданий.</param>
/// <param name="Open">true — открыть, false — закрыть.</param>
public sealed record UpdateTaskBankBatchCommand(IReadOnlyList<Guid> TaskIds, bool Open)
    : IRequest<Result<TaskBankBatchResponse>>;
