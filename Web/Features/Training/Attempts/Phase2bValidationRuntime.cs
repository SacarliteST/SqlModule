using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Training;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;

namespace SQLModule.Web.Features.Training.Attempts;

internal sealed record ValidationCheckSnapshot(
    Guid Id, ValidationCheckKind Kind, string? Value, int Weight, int Order);

internal sealed record ValidationConfigurationSnapshot(
    int PassingScore,
    int? MaxAttempts,
    IReadOnlyList<HintGroup> VisibleHintGroups,
    IReadOnlyList<ValidationCheckSnapshot> Checks);

internal sealed record ReferenceQuerySnapshot(
    Guid TargetDbId,
    string SqlText,
    bool IsRequiredColumnOrder,
    bool IsRequiredRowOrder);

internal sealed record Phase2bValidationRuntime(
    TaskValidationVersion Version,
    DbmsDictionary Dbms,
    SandboxSetup Setup,
    GoldenResult Expected,
    SchemaSpec Schema,
    ValidationConfigurationSnapshot Configuration,
    ReferenceQuerySnapshot Reference);

internal interface IPhase2bValidationRuntimeReader
{
    Task<Result<Phase2bValidationRuntime>> ReadAsync(Guid validationVersionId, CancellationToken ct);
}

internal sealed class Phase2bValidationRuntimeReader(
    AppDbContext db,
    ISqlSyntaxFactory syntaxFactory,
    ISchemaSqlGenerator generator) : IPhase2bValidationRuntimeReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<Phase2bValidationRuntime>> ReadAsync(
        Guid validationVersionId,
        CancellationToken ct)
    {
        var version = await db.TaskValidationVersions.AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == validationVersionId, ct);
        if (version is null)
        {
            return Result<Phase2bValidationRuntime>.Fail(Error.Conflict(
                "Progress.ValidationVersionNotFound",
                "Опубликованная версия проверки прохождения недоступна."));
        }

        try
        {
            var schema = JsonSerializer.Deserialize<SchemaSpec>(version.SchemaSnapshotJson, JsonOptions)!;
            var data = JsonSerializer.Deserialize<DataSpec>(version.DatasetSnapshotJson, JsonOptions)!;
            var reference = JsonSerializer.Deserialize<ReferenceQuerySnapshot>(
                version.ReferenceQuerySnapshotJson, JsonOptions)!;
            var configuration = JsonSerializer.Deserialize<ValidationConfigurationSnapshot>(
                version.ValidationConfigurationSnapshotJson, JsonOptions)!;
            var expected = GoldenResult.Deserialize(version.ExpectedResultSnapshotJson);
            var dbms = await db.TargetDbs.AsNoTracking()
                .Where(value => value.Id == reference.TargetDbId)
                .Select(value => value.Dbms)
                .SingleOrDefaultAsync(ct);
            if (expected is null || dbms is null || schema is null || data is null ||
                reference is null || configuration is null)
            {
                return Result<Phase2bValidationRuntime>.Fail(Error.Unavailable(
                    "Validation.SnapshotInvalid",
                    "Снимок опубликованной версии временно недоступен."));
            }

            var syntax = syntaxFactory.For(dbms.DbmsSystemName);
            if (syntax is null)
            {
                return Result<Phase2bValidationRuntime>.Fail(Error.Unavailable(
                    "Validation.RuntimeUnavailable",
                    "Среда выполнения опубликованной версии временно недоступна."));
            }

            var setup = new SandboxSetup([
                .. generator.GenerateDdl(syntax, schema),
                .. generator.GenerateInserts(syntax, schema, data)
            ]);
            return new Phase2bValidationRuntime(
                version, dbms, setup, expected, schema, configuration, reference);
        }
        catch (JsonException)
        {
            return Result<Phase2bValidationRuntime>.Fail(Error.Unavailable(
                "Validation.SnapshotInvalid",
                "Снимок опубликованной версии временно недоступен."));
        }
    }
}
