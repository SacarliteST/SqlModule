using System.Globalization;
using System.Text;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Training;

namespace SQLModule.Web.Features.ModuleIntegration;

internal sealed record PracticeAttemptReport(
    int? AttemptNumber,
    int? MaxAttempts,
    int? Score,
    string SubmittedSql,
    ExecutionStatus Status,
    bool IsCorrect,
    CheckReason Reason,
    int? RowCount,
    long? DurationMs,
    string? PublicError,
    IReadOnlyList<AttemptCheckResultResponse> Checks,
    IReadOnlyList<AttemptHintResponse> Hints);

internal sealed record PracticeSummaryAttempt(
    Guid Id,
    int? AttemptNumber,
    int Score,
    bool IsCorrect,
    CheckReason Reason,
    long? DurationMs,
    DateTimeOffset StartedAt);

internal sealed record PracticeSessionSummaryReport(
    int TotalAttempts,
    int FinalGrade,
    FinalizationReason Reason,
    long? DurationMs,
    PracticeSummaryAttempt? BestAttempt,
    PracticeSummaryAttempt? WorstAttempt);

internal static class PracticeEventMarkdownBuilder
{
    internal const int MaxMarkdownBytes = 16 * 1024;
    internal const int MaxSqlBytes = 8 * 1024;
    private const string TruncatedMarker = "_… текст усечён_";

    internal static string BuildAttempt(PracticeAttemptReport report)
    {
        var builder = new StringBuilder();
        var truncated = false;
        var attempt = report.AttemptNumber.HasValue ? report.AttemptNumber.Value.ToString(CultureInfo.InvariantCulture) : "—";
        var limit = report.MaxAttempts.HasValue ? $" из {report.MaxAttempts.Value}" : String.Empty;
        var score = report.Score.HasValue ? $" · {report.Score.Value} из 100" : String.Empty;
        builder.Append("**Попытка ").Append(attempt).Append(limit).Append(" — ")
            .Append(ToAttemptResult(report)).Append(score).AppendLine("**").AppendLine();
        builder.Append("Статус: ").Append(ToStatus(report.Status));
        if (report.RowCount.HasValue)
        {
            builder.Append(" · Строк: ").Append(report.RowCount.Value);
        }
        if (report.DurationMs.HasValue)
        {
            builder.Append(" · Время: ").Append(FormatDuration(report.DurationMs.Value));
        }
        builder.Append(" · Причина: ").Append(ToReason(report.Reason)).AppendLine().AppendLine();

        var fence = new string('`', Math.Max(3, LongestBacktickRun(report.SubmittedSql) + 1));
        var sql = TruncateUtf8(report.SubmittedSql, MaxSqlBytes, out var sqlTruncated);
        truncated |= sqlTruncated;
        builder.Append(fence).AppendLine("sql").Append(sql);
        if (sqlTruncated)
        {
            builder.AppendLine().Append("-- … усечено");
        }
        builder.AppendLine().Append(fence);

        if (!String.IsNullOrWhiteSpace(report.PublicError))
        {
            TryAppend(builder, $"\n\nОшибка: {Escape(report.PublicError)}", ref truncated);
        }

        if (report.Checks.Count > 0)
        {
            var table = new StringBuilder("\n\n| Критерий | Результат | Баллы |\n|---|---|---|");
            foreach (var check in report.Checks)
            {
                table.Append("\n| ").Append(ToCheckKind(check.Kind)).Append(" | ")
                    .Append(ToCheckStatus(check.Status)).Append(" | ")
                    .Append(check.AwardedScore).Append(" / ").Append(check.Weight).Append(" |");
            }
            TryAppend(builder, table.ToString(), ref truncated);

            var comments = report.Checks.Where(value => !String.IsNullOrWhiteSpace(value.Message)).ToArray();
            if (comments.Length > 0)
            {
                var section = new StringBuilder("\n\nКомментарии:");
                foreach (var check in comments)
                {
                    section.Append("\n- ").Append(ToCheckKind(check.Kind)).Append(": ").Append(Escape(check.Message!));
                }
                TryAppend(builder, section.ToString(), ref truncated);
            }
        }

        if (report.Hints.Count > 0)
        {
            var section = new StringBuilder("\n\nПодсказки:");
            foreach (var hint in report.Hints)
            {
                section.Append("\n- ").Append(Escape(hint.Message));
            }
            TryAppend(builder, section.ToString(), ref truncated);
        }

        AppendTruncationMarker(builder, truncated);
        return builder.ToString();
    }

    internal static string BuildSummary(PracticeSessionSummaryReport report)
    {
        var builder = new StringBuilder("**Итог прохождения**\n\n");
        builder.Append("Попыток: ").Append(report.TotalAttempts)
            .Append(" · Итоговая оценка: ").Append(report.FinalGrade).Append(" из 100")
            .Append(" · Причина: ").Append(ToFinalizationReason(report.Reason));
        if (report.DurationMs.HasValue)
        {
            builder.Append(" · Длительность: ").Append(FormatDuration(report.DurationMs.Value));
        }

        if (report.Reason == FinalizationReason.Expired)
        {
            builder.Append("\n\nЛучшая попытка не определена: время прохождения истекло.");
        }
        else if (report.Reason == FinalizationReason.EducationClosed)
        {
            builder.Append("\n\nЛучшая попытка не определена: прохождение закрыто платформой.");
        }
        else if (report.BestAttempt is null)
        {
            builder.Append("\n\nУчитываемых попыток не было.");
        }
        else
        {
            builder.Append("\n\nЛучшая попытка: ").Append(FormatAttempt(report.BestAttempt));
            if (report.WorstAttempt is not null && report.WorstAttempt.Id != report.BestAttempt.Id)
            {
                builder.Append("\n\nХудшая попытка: ").Append(FormatAttempt(report.WorstAttempt));
            }
        }

        return builder.ToString();
    }

    private static string FormatAttempt(PracticeSummaryAttempt attempt)
    {
        var number = attempt.AttemptNumber?.ToString(CultureInfo.InvariantCulture) ?? "—";
        var duration = attempt.DurationMs.HasValue ? $", {FormatDuration(attempt.DurationMs.Value)}" : String.Empty;
        return $"№ {number}, {attempt.Score} из 100, {ToReason(attempt.Reason)}{duration}";
    }

    private static void TryAppend(StringBuilder builder, string section, ref bool truncated)
    {
        var candidate = builder.ToString() + section + "\n\n" + TruncatedMarker;
        if (Encoding.UTF8.GetByteCount(candidate) <= MaxMarkdownBytes)
        {
            builder.Append(section);
            return;
        }

        truncated = true;
    }

    private static void AppendTruncationMarker(StringBuilder builder, bool truncated)
    {
        if (!truncated)
        {
            return;
        }

        builder.Append("\n\n").Append(TruncatedMarker);
    }

    private static string TruncateUtf8(string value, int maxBytes, out bool truncated)
    {
        if (Encoding.UTF8.GetByteCount(value) <= maxBytes)
        {
            truncated = false;
            return value;
        }

        var builder = new StringBuilder();
        var bytes = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            var count = rune.Utf8SequenceLength;
            if (bytes + count > maxBytes)
            {
                break;
            }
            builder.Append(rune.ToString());
            bytes += count;
        }
        truncated = true;
        return builder.ToString();
    }

    private static int LongestBacktickRun(string value)
    {
        var longest = 0;
        var current = 0;
        foreach (var character in value)
        {
            current = character == '`' ? current + 1 : 0;
            longest = Math.Max(longest, current);
        }
        return longest;
    }

    private static string Escape(string value)
    {
        const string special = "\\*_`[]()#|<>!~";
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal))
        {
            if (special.Contains(character, StringComparison.Ordinal))
            {
                builder.Append('\\');
            }
            builder.Append(character);
        }
        return builder.ToString();
    }

    private static string ToAttemptResult(PracticeAttemptReport report) => report.Status switch
    {
        ExecutionStatus.TimedOut => "превышено время",
        ExecutionStatus.Error => "ошибка выполнения",
        _ => report.IsCorrect ? "верно" : "неверно"
    };

    private static string ToStatus(ExecutionStatus status) => status switch
    {
        ExecutionStatus.Succeeded => "выполнено",
        ExecutionStatus.Error => "ошибка выполнения",
        ExecutionStatus.TimedOut => "превышено время",
        _ => "статус не определён"
    };

    private static string ToReason(CheckReason reason) => reason switch
    {
        CheckReason.Ok => "результат совпал с эталоном",
        CheckReason.ColumnMismatch => "столбцы результата не совпали с эталоном",
        CheckReason.RowCountMismatch => "число строк не совпало с эталоном",
        CheckReason.ValueMismatch => "значения в результате не совпали с эталоном",
        CheckReason.SqlError => "ошибка выполнения запроса",
        CheckReason.Timeout => "превышено время выполнения",
        CheckReason.NotRun => "запрос не выполнялся",
        CheckReason.ResultLimitExceeded => "результат превышает допустимый размер",
        _ => "причина не указана"
    };

    private static string ToCheckKind(ValidationCheckKind kind) => kind switch
    {
        ValidationCheckKind.MainDatasetResult => "Совпадение результата с эталоном",
        ValidationCheckKind.RequiredConstruct => "Обязательная конструкция",
        ValidationCheckKind.ForbiddenConstruct => "Запрещённая конструкция",
        ValidationCheckKind.RequiredTable => "Обязательная таблица",
        ValidationCheckKind.ForbiddenTable => "Запрещённая таблица",
        _ => "Критерий"
    };

    private static string ToCheckStatus(ValidationCheckStatus status) => status switch
    {
        ValidationCheckStatus.Passed => "пройден",
        ValidationCheckStatus.Failed => "не пройден",
        ValidationCheckStatus.NotEvaluated => "не проверялся",
        _ => "не определён"
    };

    private static string ToFinalizationReason(FinalizationReason reason) => reason switch
    {
        FinalizationReason.PerfectScore => "набрано 100 баллов",
        FinalizationReason.AttemptsExhausted => "исчерпаны попытки",
        FinalizationReason.Expired => "время вышло",
        FinalizationReason.Manual => "завершено вручную",
        FinalizationReason.EducationClosed => "прохождение закрыто платформой",
        FinalizationReason.Restarted => "прохождение перезапущено",
        _ => "прохождение завершено"
    };

    private static string FormatDuration(long durationMs)
    {
        if (durationMs < 100)
        {
            return "< 0,1 с";
        }
        return $"{durationMs / 1000d:0.0} с".Replace('.', ',');
    }
}
