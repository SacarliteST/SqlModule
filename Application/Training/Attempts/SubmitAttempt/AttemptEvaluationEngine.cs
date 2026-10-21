using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;
using SQLModule.Sandbox;
using SQLModule.Application.TaskMaterialization;

namespace SQLModule.Application.Training.Attempts.SubmitAttempt;

/// <summary>Итог сравнения основного результата запроса с эталоном.</summary>
public sealed record MainResult(ExecutionStatus Status, bool IsCorrect, CheckReason Reason, string? PublicError);

/// <summary>Оценённый критерий проверки.</summary>
public sealed record EvaluatedCheck(
    ValidationCheckSnapshot Check,
    ValidationCheckStatus Status,
    int AwardedScore,
    string? Message);

/// <summary>Полная оценка попытки: основной результат, критерии и итоговый балл.</summary>
public sealed record AttemptEvaluation(MainResult Main, IReadOnlyList<EvaluatedCheck> Checks, int Score);

/// <summary>Исходные данные оценки, не зависящие от EF Core и HTTP.</summary>
/// <param name="Expected">Эталонный результат версии проверки.</param>
/// <param name="RequireRowOrder">Учитывать порядок строк при сравнении.</param>
/// <param name="Checks">Критерии конфигурации проверки.</param>
/// <param name="TableNames">Имена таблиц схемы по ключу (для критериев по таблицам).</param>
/// <param name="ComparisonRowLimit">Лимит строк, выше которого результат не сравнивается.</param>
public sealed record AttemptEvaluationInput(
    GoldenResult Expected,
    bool RequireRowOrder,
    IReadOnlyList<ValidationCheckSnapshot> Checks,
    IReadOnlyDictionary<string, string> TableNames,
    int ComparisonRowLimit);

/// <summary>
/// Чистое ядро оценки попытки: сравнивает результат, применяет критерии и считает балл.
/// Не обращается к БД, песочнице и HTTP — тестируется напрямую.
/// </summary>
public sealed class AttemptEvaluationEngine(IResultComparer comparer)
{
    public AttemptEvaluation Evaluate(
        QueryResultSet result,
        SqlSyntaxAnalysis analysis,
        AttemptEvaluationInput input)
    {
        var main = EvaluateMain(result, input);
        var checks = EvaluateChecks(input, analysis, main.IsCorrect);
        return new AttemptEvaluation(main, checks, checks.Sum(value => value.AwardedScore));
    }

    private MainResult EvaluateMain(QueryResultSet result, AttemptEvaluationInput input)
    {
        if (!result.Succeeded)
        {
            var timeout = result.Error?.Contains("timeout", StringComparison.OrdinalIgnoreCase) == true;
            return new MainResult(
                timeout ? ExecutionStatus.TimedOut : ExecutionStatus.Error,
                false,
                timeout ? CheckReason.Timeout : CheckReason.SqlError,
                timeout ? "Превышено допустимое время выполнения запроса." :
                    "SQL-запрос не удалось выполнить. Проверьте синтаксис и повторите попытку.");
        }

        if (result.IsTruncated)
        {
            return new MainResult(
                ExecutionStatus.Succeeded, false, CheckReason.ResultLimitExceeded,
                $"Результат запроса превышает безопасный лимит сравнения {input.ComparisonRowLimit} строк.");
        }

        var outcome = comparer.Compare(input.Expected, result, input.RequireRowOrder);
        return new MainResult(ExecutionStatus.Succeeded, outcome.IsCorrect, outcome.Reason, null);
    }

    private static IReadOnlyList<EvaluatedCheck> EvaluateChecks(
        AttemptEvaluationInput input,
        SqlSyntaxAnalysis analysis,
        bool mainPassed) =>
        input.Checks.OrderBy(value => value.Order).Select(check =>
        {
            var passed = check.Kind switch
            {
                ValidationCheckKind.MainDatasetResult => mainPassed,
                ValidationCheckKind.RequiredConstruct => Enum.TryParse<SqlConstruct>(check.Value, out var required) &&
                                                         analysis.IsSuccess && analysis.Constructs.Contains(required),
                ValidationCheckKind.ForbiddenConstruct => !Enum.TryParse<SqlConstruct>(check.Value, out var forbidden) ||
                                                          !analysis.IsSuccess || !analysis.Constructs.Contains(forbidden),
                ValidationCheckKind.RequiredTable => ContainsTable(check.Value, input.TableNames, analysis),
                ValidationCheckKind.ForbiddenTable => !ContainsTable(check.Value, input.TableNames, analysis),
                _ => false
            };
            return new EvaluatedCheck(
                check,
                passed ? ValidationCheckStatus.Passed : ValidationCheckStatus.Failed,
                passed ? check.Weight : 0,
                passed ? "Критерий выполнен." : "Критерий не выполнен.");
        }).ToArray();

    private static bool ContainsTable(
        string? value,
        IReadOnlyDictionary<string, string> tables,
        SqlSyntaxAnalysis analysis) =>
        value is not null && tables.TryGetValue(value, out var name) && analysis.IsSuccess &&
        analysis.ReferencedTables.Any(reference =>
            String.Equals(reference.Name, name, StringComparison.OrdinalIgnoreCase));
}
