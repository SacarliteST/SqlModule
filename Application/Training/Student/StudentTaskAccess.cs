using Microsoft.EntityFrameworkCore;
using SQLModule.Application.ModuleIntegration;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Student;

/// <summary>Единая серверная проверка доступа студента к заданию.</summary>
public interface IStudentTaskAccess
{
    Task<Result> EnsureAsync(
        Guid userId,
        Guid? moduleSessionId,
        Guid taskId,
        PlatformScopeMode mode,
        CancellationToken cancellationToken);
}

public sealed class StudentTaskAccess(
    IApplicationDbContext db,
    IPlatformStudentScope platformScope) : IStudentTaskAccess
{
    public async Task<Result> EnsureAsync(
        Guid userId,
        Guid? moduleSessionId,
        Guid taskId,
        PlatformScopeMode mode,
        CancellationToken cancellationToken)
    {
        if (moduleSessionId.HasValue)
        {
            var platformResult = await platformScope.EnsureTaskAllowedAsync(
                userId,
                moduleSessionId.Value,
                taskId,
                mode,
                cancellationToken);
            return platformResult.IsSuccess
                ? Result.Success()
                : Result.Fail(platformResult.Error!);
        }

        var available = await db.SqlTasks.AsNoTracking().AnyAsync(
            task => task.Id == taskId &&
                    task.PublicationStatus == PublicationStatus.Published &&
                    task.Visibility == TaskVisibility.Open,
            cancellationToken);
        return available ? Result.Success() : Result.Fail(StudentErrors.NotFound);
    }
}
