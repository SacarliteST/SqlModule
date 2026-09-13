using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SQLModule.Common.Results;
using SQLModule.Sandbox;

namespace SQLModule.Web.Common.Sandbox;

internal sealed class TargetDbDataValidator(
    ITargetDbReferentialIntegrityValidator referentialIntegrityValidator,
    ITaskMaterializer materializer,
    ISandboxExecutor sandbox,
    ILogger<TargetDbDataValidator> logger) : ITargetDbDataValidator
{
    private const string SetupFailedCode = "Sandbox.SetupFailed";

    public async Task<Result> ValidateAsync(Guid targetDbId, CancellationToken ct)
    {
        var startedAt = Stopwatch.GetTimestamp();
        logger.LogInformation(
            "Начата физическая проверка учебных данных базы {TargetDbId}",
            targetDbId);

        var referentialIntegrity = await referentialIntegrityValidator.ValidateAsync(targetDbId, ct);
        if (!referentialIntegrity.IsSuccess)
        {
            logger.LogWarning(
                "Проверка ссылочной целостности учебных данных базы {TargetDbId} отклонена; код ошибки {ErrorCode}",
                targetDbId,
                referentialIntegrity.Error!.Code);
            return referentialIntegrity;
        }

        var materialized = await materializer.MaterializeAsync(targetDbId, ct);
        if (!materialized.IsSuccess)
        {
            logger.LogWarning(
                "Не удалось материализовать учебные данные базы {TargetDbId}; код ошибки {ErrorCode}",
                targetDbId,
                materialized.Error!.Code);
            return Result.Fail(materialized.Error!);
        }

        var candidate = materialized.Value!;
        var validation = await sandbox.ValidateSetupAsync(
            candidate.Dbms.ToSandboxSpec(),
            candidate.Setup,
            ct);
        if (validation.IsSuccess)
        {
            logger.LogInformation(
                "Физическая проверка учебных данных базы {TargetDbId} завершена успешно за {ElapsedMs} мс",
                targetDbId,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            return Result.Success();
        }

        logger.LogWarning(
            "Физическая проверка учебных данных базы {TargetDbId} отклонена; код ошибки {ErrorCode}, длительность {ElapsedMs} мс",
            targetDbId,
            validation.Error!.Code,
            Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);

        return validation.Error.Code == SetupFailedCode
            ? Result.Fail(TargetDbDataValidationErrors.ConstraintViolation)
            : Result.Fail(validation.Error);
    }
}

internal static class TargetDbDataValidationErrors
{
    internal static Error ReferenceNotFound(string columnName, string tableName, string value) =>
        Error.Validation(
            "TargetDbData.ReferenceNotFound",
            $"Значение \"{value}\" колонки \"{columnName}\" не найдено в таблице \"{tableName}\".");

    internal static Error ConstraintViolation => Error.Validation(
        "TargetDbData.ConstraintViolation",
        "Данные нарушают ограничения учебной базы. Проверьте внешние ключи, уникальность и обязательные значения.");
}
