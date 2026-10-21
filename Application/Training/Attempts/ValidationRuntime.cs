using System.Text.Json;
using SQLModule.Application.TaskMaterialization;
using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Application.Persistence;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Domain.Training;
using SQLModule.Sandbox;

namespace SQLModule.Application.Training.Attempts;

public sealed record ValidationCheckSnapshot(
    Guid Id, ValidationCheckKind Kind, string? Value, int Weight, int Order);

public sealed record ValidationConfigurationSnapshot(
    int PassingScore,
    int? MaxAttempts,
    IReadOnlyList<HintGroup> VisibleHintGroups,
    IReadOnlyList<ValidationCheckSnapshot> Checks);

public sealed record ReferenceQuerySnapshot(
    Guid TargetDbId,
    string SqlText,
    bool IsRequiredColumnOrder,
    bool IsRequiredRowOrder);

public sealed record ValidationRuntime(
    TaskValidationVersion Version,
    DbmsDictionary Dbms,
    SandboxSetup Setup,
    GoldenResult Expected,
    SchemaSpec Schema,
    ValidationConfigurationSnapshot Configuration,
    ReferenceQuerySnapshot Reference);

public interface IValidationRuntimeReader
{
    Task<Result<ValidationRuntime>> ReadAsync(Guid validationVersionId, CancellationToken ct);
}

public sealed class ValidationRuntimeReader(
    IApplicationDbContext db,
    ISqlSyntaxFactory syntaxFactory,
    ISchemaSqlGenerator generator) : IValidationRuntimeReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Result<ValidationRuntime>> ReadAsync(
        Guid validationVersionId,
        CancellationToken ct)
    {
        var version = await db.TaskValidationVersions.AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == validationVersionId, ct);
        if (version is null)
        {
            return Result<ValidationRuntime>.Fail(Error.Conflict(
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
                return Result<ValidationRuntime>.Fail(Error.Unavailable(
                    "Validation.SnapshotInvalid",
                    "Снимок опубликованной версии временно недоступен."));
            }

            var syntax = syntaxFactory.For(dbms.DbmsSystemName);
            if (syntax is null)
            {
                return Result<ValidationRuntime>.Fail(Error.Unavailable(
                    "Validation.RuntimeUnavailable",
                    "Среда выполнения опубликованной версии временно недоступна."));
            }

            var setup = new SandboxSetup([
                .. generator.GenerateDdl(syntax, schema),
                .. generator.GenerateInserts(syntax, schema, data)
            ]);
            return new ValidationRuntime(
                version, dbms, setup, expected, schema, configuration, reference);
        }
        catch (JsonException)
        {
            return Result<ValidationRuntime>.Fail(Error.Unavailable(
                "Validation.SnapshotInvalid",
                "Снимок опубликованной версии временно недоступен."));
        }
    }
}
