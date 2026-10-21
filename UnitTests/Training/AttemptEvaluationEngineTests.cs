using Shouldly;
using SQLModule.Application.TaskMaterialization;
using SQLModule.Application.Training.Attempts;
using SQLModule.Application.Training.Attempts.SubmitAttempt;
using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;
using SQLModule.Sandbox;

namespace SQLModule.UnitTests.Training;

/// <summary>Ядро оценки попытки проверяется без БД, песочницы и HTTP (REF-006).</summary>
public sealed class AttemptEvaluationEngineTests
{
    private static readonly Guid UsersTableKey = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid OrdersTableKey = Guid.Parse("00000000-0000-0000-0000-0000000000a2");

    private readonly AttemptEvaluationEngine engine = new(new StrictResultComparer());

    private static GoldenResult Golden() => new(["id"], [["1"], ["2"]]);

    private static QueryResultSet Result(
        IReadOnlyList<IReadOnlyList<string?>> rows,
        bool succeeded = true,
        string? error = null,
        bool truncated = false) =>
        new(succeeded, error, ["id"], rows, rows.Count, 5, truncated);

    private static SqlSyntaxAnalysis Analysis(
        IEnumerable<SqlConstruct>? constructs = null,
        IEnumerable<string>? tables = null,
        SqlSyntaxAnalysisStatus status = SqlSyntaxAnalysisStatus.Succeeded) =>
        new(
            status,
            (constructs ?? []).ToHashSet(),
            (tables ?? []).Select(name => new SqlTableReference(null, name)).ToArray(),
            1,
            true,
            null,
            null);

    private static ValidationCheckSnapshot Check(
        ValidationCheckKind kind, string? value, int weight, int order) =>
        new(Guid.NewGuid(), kind, value, weight, order);

    private static AttemptEvaluationInput Input(params ValidationCheckSnapshot[] checks) => new(
        Golden(),
        RequireRowOrder: true,
        checks,
        new Dictionary<string, string>
        {
            [UsersTableKey.ToString()] = "users",
            [OrdersTableKey.ToString()] = "orders"
        },
        ComparisonRowLimit: 100);

    [Fact(DisplayName = "Верный результат даёт полный балл по основному критерию")]
    public void Evaluate_CorrectResult_AwardsMainCheck()
    {
        var evaluation = engine.Evaluate(
            Result([["1"], ["2"]]),
            Analysis(),
            Input(Check(ValidationCheckKind.MainDatasetResult, null, 100, 0)));

        evaluation.Main.IsCorrect.ShouldBeTrue();
        evaluation.Main.Status.ShouldBe(ExecutionStatus.Succeeded);
        evaluation.Main.Reason.ShouldBe(CheckReason.Ok);
        evaluation.Score.ShouldBe(100);
        evaluation.Checks.ShouldHaveSingleItem().Status.ShouldBe(ValidationCheckStatus.Passed);
    }

    [Fact(DisplayName = "Неверный результат не даёт баллов за основной критерий")]
    public void Evaluate_WrongResult_FailsMainCheck()
    {
        var evaluation = engine.Evaluate(
            Result([["1"], ["3"]]),
            Analysis(),
            Input(Check(ValidationCheckKind.MainDatasetResult, null, 100, 0)));

        evaluation.Main.IsCorrect.ShouldBeFalse();
        evaluation.Score.ShouldBe(0);
        evaluation.Checks.ShouldHaveSingleItem().Status.ShouldBe(ValidationCheckStatus.Failed);
    }

    [Fact(DisplayName = "Ошибка выполнения SQL → Error/SqlError с публичным сообщением")]
    public void Evaluate_SqlError_ReportsErrorWithoutLeakingDetails()
    {
        var evaluation = engine.Evaluate(
            Result([], succeeded: false, error: "syntax error at or near FROM"),
            Analysis(),
            Input(Check(ValidationCheckKind.MainDatasetResult, null, 100, 0)));

        evaluation.Main.Status.ShouldBe(ExecutionStatus.Error);
        evaluation.Main.Reason.ShouldBe(CheckReason.SqlError);
        evaluation.Main.PublicError.ShouldNotBeNull().ShouldNotContain("syntax error at or near");
        evaluation.Score.ShouldBe(0);
    }

    [Fact(DisplayName = "Ошибка с timeout → TimedOut/Timeout")]
    public void Evaluate_Timeout_ReportsTimedOut()
    {
        var evaluation = engine.Evaluate(
            Result([], succeeded: false, error: "Query TIMEOUT exceeded"),
            Analysis(),
            Input(Check(ValidationCheckKind.MainDatasetResult, null, 100, 0)));

        evaluation.Main.Status.ShouldBe(ExecutionStatus.TimedOut);
        evaluation.Main.Reason.ShouldBe(CheckReason.Timeout);
    }

    [Fact(DisplayName = "Усечённый результат не сравнивается и сообщает лимит")]
    public void Evaluate_TruncatedResult_ReportsLimit()
    {
        var evaluation = engine.Evaluate(
            Result([["1"], ["2"]], truncated: true),
            Analysis(),
            Input(Check(ValidationCheckKind.MainDatasetResult, null, 100, 0)));

        evaluation.Main.Reason.ShouldBe(CheckReason.ResultLimitExceeded);
        evaluation.Main.IsCorrect.ShouldBeFalse();
        evaluation.Main.PublicError.ShouldNotBeNull().ShouldContain("100");
    }

    [Fact(DisplayName = "Обязательная конструкция засчитывается при наличии и не засчитывается при отсутствии")]
    public void Evaluate_RequiredConstruct_DependsOnAnalysis()
    {
        var input = Input(Check(ValidationCheckKind.RequiredConstruct, nameof(SqlConstruct.GroupBy), 30, 0));

        engine.Evaluate(Result([["1"], ["2"]]), Analysis([SqlConstruct.GroupBy]), input).Score.ShouldBe(30);
        engine.Evaluate(Result([["1"], ["2"]]), Analysis(), input).Score.ShouldBe(0);
    }

    [Fact(DisplayName = "Запрещённая конструкция снимает баллы, если она использована")]
    public void Evaluate_ForbiddenConstruct_FailsWhenUsed()
    {
        var input = Input(Check(ValidationCheckKind.ForbiddenConstruct, nameof(SqlConstruct.Subquery), 20, 0));

        engine.Evaluate(Result([["1"], ["2"]]), Analysis(), input).Score.ShouldBe(20);
        engine.Evaluate(Result([["1"], ["2"]]), Analysis([SqlConstruct.Subquery]), input).Score.ShouldBe(0);
    }

    [Fact(DisplayName = "Критерии по таблицам сравнивают имена без учёта регистра")]
    public void Evaluate_TableChecks_IgnoreCase()
    {
        var input = Input(
            Check(ValidationCheckKind.RequiredTable, UsersTableKey.ToString(), 10, 0),
            Check(ValidationCheckKind.ForbiddenTable, OrdersTableKey.ToString(), 10, 1));

        var passed = engine.Evaluate(Result([["1"], ["2"]]), Analysis(tables: ["USERS"]), input);
        passed.Score.ShouldBe(20);

        var failed = engine.Evaluate(Result([["1"], ["2"]]), Analysis(tables: ["orders"]), input);
        failed.Score.ShouldBe(0);
    }

    [Fact(DisplayName = "Критерии по конструкциям не засчитываются, если разбор SQL неуспешен")]
    public void Evaluate_FailedAnalysis_FailsRequiredAndForbiddenChecks()
    {
        var input = Input(
            Check(ValidationCheckKind.RequiredConstruct, nameof(SqlConstruct.GroupBy), 10, 0),
            Check(ValidationCheckKind.ForbiddenConstruct, nameof(SqlConstruct.Subquery), 10, 1));

        var evaluation = engine.Evaluate(
            Result([["1"], ["2"]]),
            Analysis(status: SqlSyntaxAnalysisStatus.InvalidSql),
            input);

        evaluation.Checks.Select(value => value.Status)
            .ShouldBe([ValidationCheckStatus.Failed, ValidationCheckStatus.Passed]);
        evaluation.Score.ShouldBe(10);
    }

    [Fact(DisplayName = "Критерии оцениваются по порядку, баллы суммируются")]
    public void Evaluate_MultipleChecks_OrderedAndSummed()
    {
        var input = Input(
            Check(ValidationCheckKind.RequiredConstruct, nameof(SqlConstruct.GroupBy), 20, 2),
            Check(ValidationCheckKind.MainDatasetResult, null, 60, 0),
            Check(ValidationCheckKind.ForbiddenConstruct, nameof(SqlConstruct.Subquery), 20, 1));

        var evaluation = engine.Evaluate(Result([["1"], ["2"]]), Analysis([SqlConstruct.GroupBy]), input);

        evaluation.Checks.Select(value => value.Check.Order).ShouldBe([0, 1, 2]);
        evaluation.Score.ShouldBe(100);
    }
}
