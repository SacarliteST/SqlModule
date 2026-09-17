using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Domain.Common;
using SQLModule.Domain.Schema;
using SQLModule.Domain.Training;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Validation.PublishTaskValidation;

internal sealed record PublishTaskValidationCommand(Guid TaskId, Guid Version, Guid IdempotencyKey)
    : IRequest<Result<TaskValidationConfigurationResponse>>;

internal sealed class PublishTaskValidationHandler(
    AppDbContext db,
    ITaskValidationEvaluationService evaluationService,
    ITaskValidationSnapshotFactory snapshotFactory,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<PublishTaskValidationHandler> logger)
    : IRequestHandler<PublishTaskValidationCommand, Result<TaskValidationConfigurationResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<TaskValidationConfigurationResponse>> Handle(
        PublishTaskValidationCommand command,
        CancellationToken ct)
    {
        var receiptScope = $"validation:{command.TaskId:D}:publish";
        var receiptKey = command.IdempotencyKey.ToString("D");
        var payloadHash = Hash(command.TaskId, command.Version);
        var receipt = await db.MutationReceipts.AsNoTracking()
            .SingleOrDefaultAsync(value =>
                value.Scope == receiptScope && value.IdempotencyKey == receiptKey, ct);
        if (receipt is not null)
        {
            return Replay(receipt, payloadHash);
        }

        var task = await db.SqlTasks
            .Include(value => value.ValidationConfiguration)
                .ThenInclude(configuration => configuration!.Checks)
            .Include(value => value.ActiveValidationVersion)
            .Include(value => value.SqlQuery)
            .SingleOrDefaultAsync(value => value.Id == command.TaskId, ct);
        if (task is null)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(
                TaskValidationErrors.TaskNotFound(command.TaskId));
        }

        var configuration = task.ValidationConfiguration;
        if (configuration is null)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(
                TaskValidationErrors.ConfigurationNotFound(command.TaskId));
        }

        if (configuration.Version != command.Version)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(TaskValidationErrors.StaleVersion());
        }

        if (task.ActiveValidationVersion?.ConfigurationVersion == configuration.Version)
        {
            var existingResponse = await ToResponseAsync(task, configuration, task.ActiveValidationVersion, ct);
            db.MutationReceipts.Add(MutationReceipt.Create(
                receiptScope, receiptKey, payloadHash, JsonSerializer.Serialize(existingResponse, JsonOptions)));
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
                var concurrentReceipt = await db.MutationReceipts.AsNoTracking()
                    .SingleOrDefaultAsync(value =>
                        value.Scope == receiptScope && value.IdempotencyKey == receiptKey, ct);
                if (concurrentReceipt is not null)
                {
                    return Replay(concurrentReceipt, payloadHash);
                }

                throw;
            }

            return existingResponse;
        }

        var definition = TaskValidationMappings.ToDefinition(configuration);
        var evaluation = await evaluationService.EvaluateAsync(
            command.TaskId,
            definition,
            TaskValidationMappings.ToIdentities(definition),
            ct);
        if (!evaluation.IsSuccess)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(evaluation.Error!);
        }

        if (!evaluation.Value!.Response.IsValid || evaluation.Value.ReferenceResult is null)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(
                TaskValidationMappings.ToError(evaluation.Value.Response));
        }

        var snapshot = await snapshotFactory.CreateAsync(
            command.TaskId,
            configuration,
            evaluation.Value.ReferenceResult,
            evaluation.Value.Response.AnalyzerVersion,
            ct);
        if (!snapshot.IsSuccess)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(snapshot.Error!);
        }

        var currentVersion = await db.TaskValidationConfigurations
            .AsNoTracking()
            .Where(value => value.Id == configuration.Id)
            .Select(value => value.Version)
            .SingleAsync(ct);
        if (currentVersion != command.Version)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(TaskValidationErrors.StaleVersion());
        }

        var nextVersionNumber = await db.TaskValidationVersions
            .Where(version => version.TaskId == command.TaskId)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(ct) + 1 ?? 1;
        var userId = currentUser.UserId ?? SystemUser.Id;
        var userName = currentUser.DisplayName ?? userId.ToString();
        var version = TaskValidationVersion.Publish(
            command.TaskId,
            nextVersionNumber,
            configuration.Version,
            configuration.PassingScore,
            configuration.MaxAttempts,
            configuration.VisibleHintGroupsMask,
            snapshot.Value!,
            userId,
            userName,
            timeProvider.GetUtcNow());
        db.TaskValidationVersions.Add(version);
        task.ActivateValidationVersion(version.Id);
        var response = await ToResponseAsync(task, configuration, version, ct);
        db.MutationReceipts.Add(MutationReceipt.Create(
            receiptScope, receiptKey, payloadHash, JsonSerializer.Serialize(response, JsonOptions)));

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception)
        {
            var concurrentlyPublished = await db.TaskValidationVersions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    value => value.TaskId == command.TaskId &&
                             value.ConfigurationVersion == command.Version,
                    ct);
            if (concurrentlyPublished is not null)
            {
                logger.LogInformation(
                    "Версия проверки SQL-задания {TaskId} уже опубликована параллельным запросом",
                    command.TaskId);
                return await ToResponseAsync(task, configuration, concurrentlyPublished, ct);
            }

            logger.LogWarning(
                exception,
                "Версия проверки SQL-задания {TaskId} не опубликована из-за конкурентного изменения",
                command.TaskId);
            return Result<TaskValidationConfigurationResponse>.Fail(TaskValidationErrors.StaleVersion());
        }

        logger.LogInformation(
            "Опубликована версия {VersionNumber} проверки SQL-задания {TaskId}",
            version.VersionNumber,
            command.TaskId);
        return response;
    }

    private async Task<TaskValidationConfigurationResponse> ToResponseAsync(
        SqlTask task,
        TaskValidationConfiguration configuration,
        TaskValidationVersion version,
        CancellationToken ct)
    {
        var tableNames = await db.MetaTables
            .AsNoTracking()
            .Where(table => table.TargetDbId == task.SqlQuery.TargetDbId)
            .ToDictionaryAsync(table => table.Id, table => table.TableName, ct);
        return TaskValidationMappings.ToResponse(configuration, version, tableNames);
    }

    private static string Hash(Guid taskId, Guid version) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes($"{taskId:D}\n{version:D}")));

    private static Result<TaskValidationConfigurationResponse> Replay(
        MutationReceipt receipt,
        string payloadHash)
    {
        if (receipt.PayloadHash != payloadHash)
        {
            return Result<TaskValidationConfigurationResponse>.Fail(
                SQLModule.Web.Features.Training.Progress.ProgressErrors.IdempotencyPayloadMismatch);
        }

        var response = JsonSerializer.Deserialize<TaskValidationConfigurationResponse>(
            receipt.ResponseJson, JsonOptions);
        return response is null
            ? Result<TaskValidationConfigurationResponse>.Fail(Error.Conflict(
                "IdempotencyRequestInProgress", "Запрос с этим Idempotency-Key ещё выполняется."))
            : response;
    }
}
