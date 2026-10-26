using SQLModule.Application.ModuleIntegration;
using SQLModule.Common.Results;

namespace SQLModule.Application.Training.Student;

/// <summary>Проверяет доступ студента к заданию в standalone- и платформенном режимах.</summary>
public interface IStudentTaskAccess
{
    /// <summary>Проверяет, может ли студент выполнить запрошенную операцию с заданием.</summary>
    /// <param name="userId">Идентификатор студента.</param>
    /// <param name="moduleSessionId">Идентификатор платформенной сессии или null для standalone.</param>
    /// <param name="taskId">Идентификатор задания.</param>
    /// <param name="mode">Требуемый режим доступа.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task<Result> EnsureAsync(
        Guid userId,
        Guid? moduleSessionId,
        Guid taskId,
        PlatformScopeMode mode,
        CancellationToken cancellationToken);
}
