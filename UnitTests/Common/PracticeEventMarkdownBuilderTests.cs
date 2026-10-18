using System.Text;
using Shouldly;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Training;
using SQLModule.Web.Features.ModuleIntegration;

namespace SQLModule.UnitTests.Common;

public sealed class PracticeEventMarkdownBuilderTests
{
    [Fact(DisplayName = "Отчёт попытки содержит безопасные данные и экранирует markdown")]
    public void AttemptReport_ContainsSafePresentationOnly()
    {
        var report = new PracticeAttemptReport(
            2,
            5,
            60,
            "SELECT `x```y` FROM t",
            ExecutionStatus.Error,
            false,
            CheckReason.SqlError,
            null,
            250,
            "SQL-запрос не удалось выполнить. Проверьте синтаксис и повторите попытку.",
            [new AttemptCheckResultResponse(
                ValidationCheckKind.RequiredConstruct,
                ValidationCheckStatus.Failed,
                40,
                0,
                "Нужно проверить [условие](javascript:alert(1)) | ещё раз")],
            [new AttemptHintResponse(HintGroup.RequiredConstructs, "Попробуйте *другую* конструкцию")]);

        var markdown = PracticeEventMarkdownBuilder.BuildAttempt(report);

        markdown.ShouldContain("Попытка 2 из 5");
        markdown.ShouldContain("60 из 100");
        markdown.ShouldContain("````sql");
        markdown.ShouldContain("\\[условие\\]\\(javascript:alert\\(1\\)\\) \\|");
        markdown.ShouldContain("\\*другую\\*");
        markdown.ShouldNotContain("secret_schema");
    }

    [Fact(DisplayName = "Длинный SQL усекается без незакрытого code fence и переполнения")]
    public void AttemptReport_TruncatesSqlWithinLimit()
    {
        var sql = String.Concat(Enumerable.Repeat("SELECT '😀';\n", 20_000));
        var report = new PracticeAttemptReport(
            1, null, null, sql, ExecutionStatus.Succeeded, false,
            CheckReason.ValueMismatch, 0, 10, null, [], []);

        var markdown = PracticeEventMarkdownBuilder.BuildAttempt(report);

        Encoding.UTF8.GetByteCount(markdown).ShouldBeLessThanOrEqualTo(
            PracticeEventMarkdownBuilder.MaxMarkdownBytes);
        markdown.ShouldEndWith("_… текст усечён_");
        markdown.ShouldContain("\n```\n");
        markdown.ShouldNotContain("�");
    }

    [Fact(DisplayName = "Итог показывает лучшую и худшую попытки без полной истории")]
    public void SummaryReport_ShowsBestAndWorst()
    {
        var startedAt = DateTimeOffset.Parse("2026-10-18T10:00:00Z");
        var best = new PracticeSummaryAttempt(
            Guid.NewGuid(), 2, 90, false, CheckReason.ValueMismatch, 900, startedAt.AddMinutes(1));
        var worst = new PracticeSummaryAttempt(
            Guid.NewGuid(), 1, 20, false, CheckReason.RowCountMismatch, 400, startedAt);

        var markdown = PracticeEventMarkdownBuilder.BuildSummary(new PracticeSessionSummaryReport(
            4, 90, FinalizationReason.AttemptsExhausted, 12_000, best, worst));

        markdown.ShouldContain("Попыток: 4");
        markdown.ShouldContain("Лучшая попытка: № 2, 90 из 100");
        markdown.ShouldContain("Худшая попытка: № 1, 20 из 100");
    }

    [Theory(DisplayName = "Таймаут и закрытие платформой не объявляют лучшую попытку")]
    [InlineData(FinalizationReason.Expired, "время прохождения истекло")]
    [InlineData(FinalizationReason.EducationClosed, "прохождение закрыто платформой")]
    public void SummaryReport_DoesNotChooseExtremesForForcedClosure(
        FinalizationReason reason,
        string expected)
    {
        var attempt = new PracticeSummaryAttempt(
            Guid.NewGuid(), 1, 100, true, CheckReason.Ok, 100, DateTimeOffset.UtcNow);

        var markdown = PracticeEventMarkdownBuilder.BuildSummary(new PracticeSessionSummaryReport(
            1, 100, reason, 1_000, attempt, attempt));

        markdown.ShouldContain(expected);
        markdown.ShouldNotContain("Лучшая попытка: №");
        markdown.ShouldNotContain("Худшая попытка:");
    }
}
