using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Data.Core;
using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;
using SQLModule.Sandbox;
using SQLModule.Web.Features.Training.SqlQueries;

namespace SQLModule.Web.Features.Training.Validation;

internal sealed record ValidationPreviewIdentity(Guid? CheckId, string? ClientKey);

internal sealed record TaskValidationEvaluation(
    TaskValidationPreviewResponse Response,
    QueryResultSet? ReferenceResult);

internal interface ITaskValidationEvaluationService
{
    Task<Result<TaskValidationEvaluation>> EvaluateAsync(
        Guid taskId,
        TaskValidationDefinition definition,
        IReadOnlyList<ValidationPreviewIdentity> identities,
        CancellationToken ct);
}

internal sealed class TaskValidationEvaluationService(
    AppDbContext db,
    ISqlSyntaxAnalyzerResolver analyzerResolver,
    ITaskValidationConfigurationValidator configurationValidator,
    ISqlQueryValidationRunner validationRunner,
    IOptions<TaskValidationOptions> options)
    : ITaskValidationEvaluationService
{
    private static readonly IReadOnlySet<ValidationCheckKind> CheckKinds =
        Enum.GetValues<ValidationCheckKind>().ToHashSet();
    private static readonly IReadOnlySet<HintGroup> HintGroups =
        Enum.GetValues<HintGroup>().ToHashSet();

    public async Task<Result<TaskValidationEvaluation>> EvaluateAsync(
        Guid taskId,
        TaskValidationDefinition definition,
        IReadOnlyList<ValidationPreviewIdentity> identities,
        CancellationToken ct)
    {
        var task = await db.SqlTasks
            .AsNoTracking()
            .Include(value => value.SqlQuery)
            .SingleOrDefaultAsync(value => value.Id == taskId, ct);
        if (task is null)
        {
            return Result<TaskValidationEvaluation>.Fail(TaskValidationErrors.TaskNotFound(taskId));
        }

        var dbmsSystemName = await db.TargetDbs
            .AsNoTracking()
            .Where(value => value.Id == task.SqlQuery.TargetDbId)
            .Select(value => value.Dbms.DbmsSystemName)
            .SingleOrDefaultAsync(ct);
        if (dbmsSystemName is null)
        {
            return Result<TaskValidationEvaluation>.Fail(
                TaskValidationErrors.ReferenceInvalid(
                    "Validation.TargetDbNotFound",
                    "Учебная база эталонного запроса недоступна.",
                    "referenceQuery.targetDbId"));
        }

        var analyzer = analyzerResolver.Resolve(dbmsSystemName);
        if (analyzer is null)
        {
            return Result<TaskValidationEvaluation>.Fail(
                TaskValidationErrors.UnsupportedAnalyzer(dbmsSystemName));
        }

        var tables = await db.MetaTables
            .AsNoTracking()
            .Where(table => table.TargetDbId == task.SqlQuery.TargetDbId)
            .ToDictionaryAsync(table => table.Id, table => table.TableName, ct);
        var capabilities = new ValidationRuleCapabilities(
            CheckKinds,
            analyzer.SupportedConstructs,
            HintGroups,
            options.Value.MaxAttemptsLimit);
        var ruleResult = configurationValidator.Validate(
            definition,
            capabilities,
            new ValidationSchemaContext(tables.Keys.ToHashSet()));

        if (!ruleResult.IsValid)
        {
            return Invalid(
                analyzer.AnalyzerVersion,
                definition,
                identities,
                ruleResult.Violations);
        }

        var syntax = analyzer.Analyze(task.SqlQuery.QueryText);
        if (syntax.Status == SqlSyntaxAnalysisStatus.InfrastructureFailure)
        {
            return Result<TaskValidationEvaluation>.Fail(TaskValidationErrors.InfrastructureUnavailable());
        }

        if (!syntax.IsSuccess)
        {
            return Invalid(
                analyzer.AnalyzerVersion,
                definition,
                identities,
                [new ValidationRuleViolation(
                    "referenceQuery.sqlText",
                    syntax.ErrorCode ?? "Validation.ReferenceSqlInvalid",
                    ValidationViolationSeverity.Error,
                    syntax.PublicError ?? "Эталонный SQL не прошёл синтаксическую проверку.")]);
        }

        var run = await validationRunner.ValidateAsync(
            task.SqlQuery.TargetDbId,
            task.SqlQuery.QueryText,
            ct);
        if (!run.IsSuccess)
        {
            if (run.Error!.Type is ErrorType.Unavailable or ErrorType.Failure)
            {
                return Result<TaskValidationEvaluation>.Fail(TaskValidationErrors.InfrastructureUnavailable());
            }

            return Invalid(
                analyzer.AnalyzerVersion,
                definition,
                identities,
                [new ValidationRuleViolation(
                    "referenceQuery.sqlText",
                    "Validation.ReferenceExecutionFailed",
                    ValidationViolationSeverity.Error,
                    "Эталонный SQL не выполняется на текущей учебной базе.")]);
        }

        var checks = definition.Checks
            .Select((check, index) => EvaluateCheck(
                check,
                identities[index],
                syntax,
                tables))
            .ToArray();
        var score = checks.Sum(check => check.AwardedScore);
        var violations = score == 100
            ? Array.Empty<ValidationViolationResponse>()
            : new[]
            {
                new ValidationViolationResponse(
                    "checks",
                    "Validation.ReferenceMustScore100",
                    ValidationViolationSeverity.Error,
                    $"Эталонное решение набирает {score} из 100 баллов.")
            };

        return new TaskValidationEvaluation(
            new TaskValidationPreviewResponse(
                score == 100,
                score,
                analyzer.AnalyzerVersion,
                checks,
                violations),
            run.Value);
    }

    private static Result<TaskValidationEvaluation> Invalid(
        string analyzerVersion,
        TaskValidationDefinition definition,
        IReadOnlyList<ValidationPreviewIdentity> identities,
        IReadOnlyList<ValidationRuleViolation> violations) =>
        new TaskValidationEvaluation(
            new TaskValidationPreviewResponse(
                false,
                0,
                analyzerVersion,
                definition.Checks.Select((check, index) => new ValidationCheckPreviewResponse(
                    identities[index].CheckId,
                    identities[index].ClientKey,
                    check.Kind,
                    ValidationCheckStatus.NotEvaluated,
                    0,
                    "Критерий не проверен из-за ошибок конфигурации.")).ToArray(),
                violations.Select(violation => new ValidationViolationResponse(
                    violation.Path,
                    violation.Code,
                    violation.Severity,
                    violation.Message)).ToArray()),
            null);

    private static ValidationCheckPreviewResponse EvaluateCheck(
        ValidationCheckDefinition check,
        ValidationPreviewIdentity identity,
        SqlSyntaxAnalysis syntax,
        IReadOnlyDictionary<Guid, string> tables)
    {
        var passed = check.Kind switch
        {
            ValidationCheckKind.MainDatasetResult => true,
            ValidationCheckKind.RequiredConstruct => ContainsConstruct(check.Value, syntax),
            ValidationCheckKind.ForbiddenConstruct => !ContainsConstruct(check.Value, syntax),
            ValidationCheckKind.RequiredTable => ContainsTable(check.Value, syntax, tables),
            ValidationCheckKind.ForbiddenTable => !ContainsTable(check.Value, syntax, tables),
            _ => false
        };

        return new ValidationCheckPreviewResponse(
            identity.CheckId,
            identity.ClientKey,
            check.Kind,
            passed ? ValidationCheckStatus.Passed : ValidationCheckStatus.Failed,
            passed ? check.Weight : 0,
            passed ? "Критерий выполнен." : "Эталонное решение не выполняет критерий.");
    }

    private static bool ContainsConstruct(string? value, SqlSyntaxAnalysis syntax) =>
        Enum.TryParse<SqlConstruct>(value, out var construct) && syntax.Constructs.Contains(construct);

    private static bool ContainsTable(
        string? value,
        SqlSyntaxAnalysis syntax,
        IReadOnlyDictionary<Guid, string> tables) =>
        Guid.TryParse(value, out var tableId) &&
        tables.TryGetValue(tableId, out var tableName) &&
        syntax.ReferencedTables.Any(reference =>
            String.Equals(reference.Name, tableName, StringComparison.OrdinalIgnoreCase));
}
