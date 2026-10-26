using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Persistence;
using SQLModule.Application.Training.Attempts;
using SQLModule.Application.Training.SqlTasks;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Domain.Training;

namespace SQLModule.Application.Training.Bank;

/// <summary>Рассчитывает готовность задания к публикации в открытом банке.</summary>
public sealed class TaskBankReadiness : ITaskBankReadiness
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IApplicationDbContext db;
    private readonly ISqlTaskPublishReadiness publishReadiness;

    /// <summary>Создаёт проверку готовности задания.</summary>
    public TaskBankReadiness(
        IApplicationDbContext db,
        ISqlTaskPublishReadiness publishReadiness)
    {
        this.db = db;
        this.publishReadiness = publishReadiness;
    }

    /// <inheritdoc />
    public async Task<TaskBankReadinessResponse> EvaluateAsync(Guid taskId, CancellationToken ct)
    {
        var task = await db.SqlTasks.AsNoTracking()
            .Where(value => value.Id == taskId)
            .Select(value => new
            {
                value.PublicationStatus,
                value.TopicId,
                value.TaskText,
                value.BankHint
            })
            .SingleAsync(ct);
        var blockers = new List<SQLModule.Common.Results.Error>();
        if (task.PublicationStatus != PublicationStatus.Published)
        {
            blockers.Add(TaskBankErrors.NotPublished);
        }

        if (!await db.Topics.AsNoTracking().AnyAsync(value => value.Id == task.TopicId, ct))
        {
            blockers.Add(TaskBankErrors.TopicMissing);
        }

        if (String.IsNullOrWhiteSpace(task.TaskText))
        {
            blockers.Add(TaskBankErrors.TaskTextMissing);
        }

        var publishBlockers = await publishReadiness.EvaluateAsync(taskId, ct);
        blockers.AddRange(publishBlockers.Where(blocker => blocker.Code != SqlTaskErrors.HasAttemptsOnPublish.Code));

        if (String.IsNullOrWhiteSpace(task.BankHint) && !await HasAutomaticHintAsync(taskId, ct))
        {
            blockers.Add(TaskBankErrors.HintMissing);
        }

        var usedInControl = await IsUsedInControlAsync(taskId, ct);
        return new TaskBankReadinessResponse(
            blockers.Count == 0,
            usedInControl,
            blockers.Select(value => new TaskBankBlockerResponse(value.Code, value.Message)).ToArray());
    }

    /// <inheritdoc />
    public async Task<bool> HasAutomaticHintAsync(Guid taskId, CancellationToken ct)
    {
        var version = await db.SqlTasks.AsNoTracking()
            .Where(value => value.Id == taskId && value.ActiveValidationVersionId != null)
            .Select(value => value.ActiveValidationVersion)
            .SingleOrDefaultAsync(ct);
        if (version is null)
        {
            return false;
        }

        try
        {
            var configuration = JsonSerializer.Deserialize<ValidationConfigurationSnapshot>(
                version.ValidationConfigurationSnapshotJson,
                JsonOptions);
            return configuration is not null && TaskBankHintRules.HasAutomaticHint(
                version.GetVisibleHintGroups(),
                configuration.Checks.Select(check => check.Kind));
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task<bool> IsUsedInControlAsync(Guid taskId, CancellationToken ct) =>
        await db.ModuleSessions.AsNoTracking().AnyAsync(value => value.TaskRef == taskId.ToString(), ct) ||
        await db.Attempts.AsNoTracking().AnyAsync(
            value => value.TaskId == taskId && value.ModuleSessionId != null,
            ct);

}
