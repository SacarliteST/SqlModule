using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.Host;

namespace SQLModule.IntegrationTests.Training.Student;

/// <summary>Общие данные для тестов открытого банка.</summary>
internal static class TaskBankTestData
{
    /// <summary>
    /// Сохраняет оценённую попытку напрямую в БД: тренировочную (<paramref name="moduleSessionId"/> = null)
    /// или платформенную. Критерий берётся из версии проверки задания.
    /// </summary>
    internal static async Task<Guid> SeedAttemptAsync(
        WebApplicationFactory<IHostMarker> app,
        Guid taskId,
        Guid userId,
        int number,
        int score,
        bool correct,
        DateTimeOffset finishedAt,
        Guid? moduleSessionId = null)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var version = await db.TaskValidationVersions.SingleAsync(value => value.TaskId == taskId);
        var check = await db.ValidationChecks.OrderBy(value => value.Order)
            .FirstAsync(value => value.Configuration.TaskId == taskId);
        var attempt = SQLModule.Domain.Training.Attempt.Record(
            userId,
            taskId,
            "SELECT 1",
            ExecutionStatus.Succeeded,
            correct,
            correct ? CheckReason.Ok : CheckReason.ValueMismatch,
            1,
            1,
            null,
            finishedAt.AddMilliseconds(-1),
            finishedAt,
            studentName: "Bank Student",
            moduleSessionId: moduleSessionId);
        attempt.AttachScoring(version.Id, number, score, true);
        db.Attempts.Add(attempt);
        db.AttemptCheckResults.Add(AttemptCheckResult.Create(
            attempt.Id,
            check.Id,
            check.Kind,
            correct ? ValidationCheckStatus.Passed : ValidationCheckStatus.Failed,
            check.Weight,
            score,
            check.Order,
            null,
            null));
        await db.SaveChangesAsync();
        return attempt.Id;
    }
}
