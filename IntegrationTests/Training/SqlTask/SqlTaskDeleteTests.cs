using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.Infrastructure;
using DomainAttempt = SQLModule.Domain.Training.Attempt;

namespace SQLModule.IntegrationTests.Training.SqlTask;

/// <summary>Удаление SQL-задания: правило «никем не использовалось», каскад, причины отказа без 500.</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class SqlTaskDeleteTests(TestApplication app) : ApiTestBase(app)
{
    private sealed record TaskContext(Guid TaskId, Guid TargetDbId, Guid TopicId, Guid SqlQueryId);

    private async Task<TaskContext> CreateTaskAsync()
    {
        AsAdmin();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var dbms = await DbmsDictionaryClient.CreateAsync(new CreateDbmsDictionaryRequest(
            $"Delete_{suffix}", "postgres", "postgres:latest", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB", null,
            "training", "user", "password"));
        AsTeacher();
        var targetDb = await TargetDbClient.CreateAsync(new CreateTargetDbRequest(
            dbms.Id, $"delete_{suffix}", null, false));
        var topic = await TopicClient.CreateAsync(new CreateTopicRequest($"Delete_{suffix}", null));
        var task = await SqlTaskClient.CreateAsync(new CreateSqlTaskRequest(
            topic.Id, $"Delete task {suffix}", "Проверить запрос", 2,
            new ReferenceQueryRequest(targetDb.Id, "SELECT 1", false, false)));
        return new TaskContext(task.Id, targetDb.Id, topic.Id, task.SqlQueryId);
    }

    private Task<HttpResponseMessage> DeleteAsync(Guid taskId) =>
        HttpClient.DeleteAsync(ApiRoutes.Training.SqlTasks.ForId(taskId));

    private static async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private async Task AssertDetailsMatchDeleteAsync(Guid taskId, string? expectedCode)
    {
        var details = await SqlTaskClient.GetTeacherDetailsAsync(taskId);
        details.ShouldNotBeNull();
        using var response = await DeleteAsync(taskId);
        if (expectedCode is null)
        {
            details.CanDelete.ShouldBeTrue();
            details.DeleteBlockReasons.ShouldBeEmpty();
            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }
        else
        {
            details.CanDelete.ShouldBeFalse();
            details.DeleteBlockReasons.ShouldNotBeEmpty();
            details.DeleteBlockReasons.First().Code.ShouldBe(expectedCode);
            response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await CodeAsync(response)).ShouldBe(expectedCode);
        }
    }

    private async Task SeedAttemptAsync(Guid taskId)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Attempts.Add(DomainAttempt.Record(
            Guid.NewGuid(), taskId, "SELECT 1", ExecutionStatus.Succeeded, true, CheckReason.Ok,
            0, 0, null, DateTimeOffset.UtcNow.AddSeconds(-10), DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
    }

    private async Task SeedProgressAsync(Guid taskId, Guid? moduleSessionId = null)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var versionId = await db.SqlTasks.AsNoTracking().Where(value => value.Id == taskId)
            .Select(value => value.ActiveValidationVersionId).SingleAsync();
        var userId = Guid.NewGuid();
        StudentTaskProgress progress;
        if (moduleSessionId.HasValue)
        {
            db.ModuleSessions.Add(ModuleSession.Create(
                moduleSessionId.Value, $"delete-{moduleSessionId:N}", userId, taskId.ToString(),
                "https://platform.example/return", null));
            progress = StudentTaskProgress.CreatePlatform(userId, taskId, versionId!.Value, moduleSessionId.Value, null);
        }
        else
        {
            progress = StudentTaskProgress.CreateStandalone(userId, taskId, versionId!.Value);
        }

        db.StudentTaskProgresses.Add(progress);
        await db.SaveChangesAsync();
    }

    [Fact(DisplayName = "Удаление: черновик без версии оценки удаляется, повторное удаление — 404")]
    public async Task Draft_WithoutValidationVersion_IsDeleted()
    {
        var context = await CreateTaskAsync();

        await AssertDetailsMatchDeleteAsync(context.TaskId, null);

        using var again = await DeleteAsync(context.TaskId);
        again.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Удаление: архивное задание с активной версией удаляется вместе с версиями, конфигурацией и эталоном")]
    public async Task Archived_WithActiveVersion_IsDeletedWithEverythingOwned()
    {
        var context = await CreateTaskAsync();
        await PublishValidationAsync(context.TaskId);
        await SqlTaskClient.PublishAsync(context.TaskId);
        await SqlTaskClient.ArchiveAsync(context.TaskId);

        await AssertDetailsMatchDeleteAsync(context.TaskId, null);

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.SqlTasks.AnyAsync(value => value.Id == context.TaskId)).ShouldBeFalse();
        (await db.TaskValidationVersions.AnyAsync(value => value.TaskId == context.TaskId)).ShouldBeFalse();
        (await db.TaskValidationConfigurations.AnyAsync(value => value.TaskId == context.TaskId)).ShouldBeFalse();
        (await db.SqlQueries.AnyAsync(value => value.Id == context.SqlQueryId)).ShouldBeFalse();
        var page = await SqlTaskClient.GetAllAsync(0, 100);
        page.Items.ShouldNotContain(item => item.Id == context.TaskId);
    }

    [Fact(DisplayName = "Удаление: опубликованное задание — 409 SqlTask.PublishedCannotBeDeleted")]
    public async Task Published_IsRejected()
    {
        var context = await CreateTaskAsync();
        await PublishValidationAsync(context.TaskId);
        await SqlTaskClient.PublishAsync(context.TaskId);

        await AssertDetailsMatchDeleteAsync(context.TaskId, "SqlTask.PublishedCannotBeDeleted");

        (await SqlTaskClient.GetByIdAsync(context.TaskId)).ShouldNotBeNull();
    }

    [Fact(DisplayName = "Удаление: задание с попыткой — 409 SqlTask.HasAttempts")]
    public async Task WithAttempt_IsRejected()
    {
        var context = await CreateTaskAsync();
        await SeedAttemptAsync(context.TaskId);

        await AssertDetailsMatchDeleteAsync(context.TaskId, "SqlTask.HasAttempts");
    }

    [Theory(DisplayName = "Удаление: прохождение без попыток (standalone и платформенное) — 409 SqlTask.HasStudentProgress")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WithStudentProgress_IsRejected(bool platform)
    {
        var context = await CreateTaskAsync();
        await PublishValidationAsync(context.TaskId);
        await SeedProgressAsync(context.TaskId, platform ? Guid.NewGuid() : null);

        await AssertDetailsMatchDeleteAsync(context.TaskId, "SqlTask.HasStudentProgress");
    }

    [Fact(DisplayName = "Удаление: все причины отказа в errors, code — первая")]
    public async Task SeveralBlockers_AreAllReported()
    {
        var context = await CreateTaskAsync();
        await PublishValidationAsync(context.TaskId);
        await SqlTaskClient.PublishAsync(context.TaskId);
        await SeedProgressAsync(context.TaskId);

        using var response = await DeleteAsync(context.TaskId);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("code").GetString().ShouldBe("SqlTask.PublishedCannotBeDeleted");
        body.RootElement.GetProperty("errors").EnumerateObject().Select(pair => pair.Name).ToArray()
            .ShouldBe(["SqlTask.PublishedCannotBeDeleted", "SqlTask.HasStudentProgress"]);
        var details = await SqlTaskClient.GetTeacherDetailsAsync(context.TaskId);
        details!.DeleteBlockReasons.Select(reason => reason.Code).ShouldBe(
            ["SqlTask.PublishedCannotBeDeleted", "SqlTask.HasStudentProgress"]);
    }

    [Fact(DisplayName = "Удаление: внешний ключ вне известных причин → 409 SqlTask.InUse, не 500, транзакция откатывается")]
    public async Task UnknownForeignKey_IsConflictNotServerError()
    {
        var context = await CreateTaskAsync();
        await PublishValidationAsync(context.TaskId);
        await SqlTaskClient.PublishAsync(context.TaskId);
        await SqlTaskClient.ArchiveAsync(context.TaskId);
        using (var scope = App.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "CREATE TABLE IF NOT EXISTS zz_task_fk_probe (task_id uuid REFERENCES \"SqlTasks\"(\"Id\"))");
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO zz_task_fk_probe (task_id) VALUES ({0})", context.TaskId);
        }

        try
        {
            using var response = await DeleteAsync(context.TaskId);

            response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            (await CodeAsync(response)).ShouldBe("SqlTask.InUse");
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var task = await db.SqlTasks.AsNoTracking().SingleAsync(value => value.Id == context.TaskId);
            task.ActiveValidationVersionId.ShouldNotBeNull();
            (await db.TaskValidationVersions.CountAsync(value => value.TaskId == context.TaskId)).ShouldBe(1);
            (await db.SqlQueries.AnyAsync(value => value.Id == context.SqlQueryId)).ShouldBeTrue();
        }
        finally
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS zz_task_fk_probe");
        }
    }

    [Fact(DisplayName = "Удаление: после удаления задания его учебную базу можно удалить — «сиротского» запроса нет")]
    public async Task AfterDelete_TargetDbCanBeDeleted()
    {
        var context = await CreateTaskAsync();
        await PublishValidationAsync(context.TaskId);
        await SqlTaskClient.PublishAsync(context.TaskId);
        await SqlTaskClient.ArchiveAsync(context.TaskId);
        using (var response = await DeleteAsync(context.TaskId))
        {
            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        await TargetDbClient.DeleteAsync(context.TargetDbId);

        (await TargetDbClient.GetByIdAsync(context.TargetDbId)).ShouldBeNull();
    }
}
