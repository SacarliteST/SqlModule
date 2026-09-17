using Shouldly;
using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;

namespace SQLModule.UnitTests.Training;

public sealed class TaskValidationConfigurationValidatorTests
{
    private readonly TaskValidationConfigurationValidator validator = new();
    private readonly Guid tableId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact(DisplayName = "Простая конфигурация только с MainDatasetResult валидна")]
    public void Validate_MainResultOnly_IsValid()
    {
        var definition = Definition(
            passingScore: 1,
            maxAttempts: null,
            checks: [Check(ValidationCheckKind.MainDatasetResult, null, 100, 0)]);

        var result = Validate(definition);

        result.IsValid.ShouldBeTrue();
        result.Violations.ShouldBeEmpty();
    }

    [Theory(DisplayName = "PassingScore обязан требовать правильный основной результат")]
    [InlineData(80, false)]
    [InlineData(81, true)]
    [InlineData(100, true)]
    public void Validate_PassingScoreRequiresMainDatasetResult(int passingScore, bool expectedValid)
    {
        var definition = Definition(
            passingScore,
            5,
            [
                Check(ValidationCheckKind.MainDatasetResult, null, 20, 0),
                Check(ValidationCheckKind.RequiredConstruct, nameof(SqlConstruct.Join), 80, 1)
            ]);

        var result = Validate(definition);

        result.IsValid.ShouldBe(expectedValid);
        result.Violations.Any(value =>
                value.Code == "Validation.MainDatasetResultRequiredForPassing" &&
                value.Path == "passingScore")
            .ShouldBe(!expectedValid);
    }

    [Theory(DisplayName = "PassingScore допускает только диапазон 1–100")]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_PassingScoreOutsideRange_ReturnsViolation(int passingScore)
    {
        var result = Validate(Definition(
            passingScore,
            null,
            [Check(ValidationCheckKind.MainDatasetResult, null, 100, 0)]));

        result.ShouldContain("Validation.PassingScoreOutOfRange", "passingScore");
    }

    [Fact(DisplayName = "MaxAttempts null валиден, ноль и превышение capability запрещены")]
    public void Validate_MaxAttempts_UsesCapabilityLimit()
    {
        Validate(Definition(70, null, ValidCompositeChecks())).IsValid.ShouldBeTrue();

        Validate(Definition(70, 0, ValidCompositeChecks()))
            .ShouldContain("Validation.MaxAttemptsOutOfRange", "maxAttempts");
        Validate(Definition(70, 101, ValidCompositeChecks()))
            .ShouldContain("Validation.MaxAttemptsOutOfRange", "maxAttempts");
    }

    [Fact(DisplayName = "Отсутствующий и повторный MainDatasetResult возвращают точные пути")]
    public void Validate_MainDatasetResultCardinality_ReturnsViolations()
    {
        var missing = Validate(Definition(
            100,
            null,
            [Check(ValidationCheckKind.RequiredConstruct, nameof(SqlConstruct.Join), 100, 0)]));
        missing.ShouldContain("Validation.MainDatasetResultRequired", "checks");

        var duplicate = Validate(Definition(
            100,
            null,
            [
                Check(ValidationCheckKind.MainDatasetResult, null, 50, 0),
                Check(ValidationCheckKind.MainDatasetResult, null, 50, 1)
            ]));
        duplicate.ShouldContain("Validation.MainDatasetResultDuplicate", "checks[1].kind");
        duplicate.ShouldContain("Validation.DuplicateCheck", "checks[1]");
    }

    [Fact(DisplayName = "Веса, order и id проверяются агрегированно")]
    public void Validate_WeightsOrderAndIds_ReturnAllViolations()
    {
        var id = Guid.NewGuid();
        var result = Validate(Definition(
            100,
            null,
            [
                new ValidationCheckDefinition(id, ValidationCheckKind.MainDatasetResult, null, 0, -1),
                new ValidationCheckDefinition(id, ValidationCheckKind.RequiredConstruct, nameof(SqlConstruct.Join), 20, 0),
                Check(ValidationCheckKind.RequiredTable, tableId.ToString("D"), 20, 0)
            ]));

        result.ShouldContain("Validation.WeightOutOfRange", "checks[0].weight");
        result.ShouldContain("Validation.OrderOutOfRange", "checks[0].order");
        result.ShouldContain("Validation.DuplicateCheckId", "checks[1].id");
        result.ShouldContain("Validation.DuplicateOrder", "checks[2].order");
        result.ShouldContain("Validation.WeightsMustSumTo100", "checks");
    }

    [Fact(DisplayName = "Required и forbidden критерии не могут противоречить друг другу")]
    public void Validate_ContradictingCriteria_ReturnViolations()
    {
        var result = Validate(Definition(
            80,
            null,
            [
                Check(ValidationCheckKind.MainDatasetResult, null, 60, 0),
                Check(ValidationCheckKind.RequiredConstruct, nameof(SqlConstruct.Cte), 10, 1),
                Check(ValidationCheckKind.ForbiddenConstruct, nameof(SqlConstruct.Cte), 10, 2),
                Check(ValidationCheckKind.RequiredTable, tableId.ToString("D"), 10, 3),
                Check(ValidationCheckKind.ForbiddenTable, tableId.ToString("D"), 10, 4)
            ]));

        result.ShouldContain("Validation.ContradictingConstruct", "checks[2].value");
        result.ShouldContain("Validation.ContradictingTable", "checks[4].value");
    }

    [Fact(DisplayName = "Конструкции и таблицы проверяются по capability и snapshot-схеме")]
    public void Validate_ConstructAndTableValues_ReturnPreciseViolations()
    {
        var unknownTable = Guid.NewGuid();
        var capabilities = Capabilities(
            constructs: new HashSet<SqlConstruct> { SqlConstruct.Join },
            checkKinds: new HashSet<ValidationCheckKind>
            {
                ValidationCheckKind.MainDatasetResult,
                ValidationCheckKind.RequiredConstruct
            });
        var definition = Definition(
            80,
            null,
            [
                Check(ValidationCheckKind.MainDatasetResult, "unexpected", 70, 0),
                Check(ValidationCheckKind.RequiredConstruct, nameof(SqlConstruct.Cte), 10, 1),
                Check(ValidationCheckKind.RequiredTable, unknownTable.ToString("D"), 10, 2),
                Check(ValidationCheckKind.ForbiddenTable, "not-a-guid", 10, 3)
            ]);

        var result = validator.Validate(
            definition,
            capabilities,
            new ValidationSchemaContext(new HashSet<Guid> { tableId }));

        result.ShouldContain("Validation.MainDatasetResultValueMustBeEmpty", "checks[0].value");
        result.ShouldContain("Validation.UnsupportedConstruct", "checks[1].value");
        result.ShouldContain("Validation.UnsupportedCheckKind", "checks[2].kind");
        result.ShouldContain("Validation.TableNotFound", "checks[2].value");
        result.ShouldContain("Validation.InvalidTableId", "checks[3].value");
    }

    [Fact(DisplayName = "Неподдерживаемые и повторные группы подсказок запрещены")]
    public void Validate_Hints_ReturnPreciseViolations()
    {
        var definition = new TaskValidationDefinition(
            70,
            null,
            [HintGroup.Result, HintGroup.RequiredTables, HintGroup.RequiredTables],
            ValidCompositeChecks());
        var capabilities = Capabilities(hints: new HashSet<HintGroup> { HintGroup.Result });

        var result = validator.Validate(
            definition,
            capabilities,
            new ValidationSchemaContext(new HashSet<Guid> { tableId }));

        result.ShouldContain("Validation.UnsupportedHintGroup", "visibleHintGroups[1]");
        result.ShouldContain("Validation.DuplicateHintGroup", "visibleHintGroups[2]");
    }

    [Fact(DisplayName = "Неизвестные enum и экстремальные веса не приводят к исключению")]
    public void Validate_UnknownEnumAndLargeWeights_ReturnViolations()
    {
        var definition = Definition(
            100,
            null,
            [
                Check(ValidationCheckKind.MainDatasetResult, null, Int32.MaxValue, 0),
                Check((ValidationCheckKind)999, null, Int32.MaxValue, 1)
            ]);

        var result = Should.NotThrow(() => Validate(definition));

        result.ShouldContain("Validation.UnknownCheckKind", "checks[1].kind");
        result.ShouldContain("Validation.WeightOutOfRange", "checks[1].weight");
        result.ShouldContain("Validation.WeightsMustSumTo100", "checks");
    }

    [Fact(DisplayName = "Числовое значение enum не принимается как SQL-конструкция")]
    public void Validate_NumericConstructValue_ReturnsViolation()
    {
        var result = Validate(Definition(
            80,
            null,
            [
                Check(ValidationCheckKind.MainDatasetResult, null, 80, 0),
                Check(ValidationCheckKind.RequiredConstruct, "1", 20, 1)
            ]));

        result.ShouldContain("Validation.InvalidConstruct", "checks[1].value");
    }

    [Fact(DisplayName = "Одинаковые UUID таблицы с разным регистром считаются дублем")]
    public void Validate_TableIdsWithDifferentCase_AreDuplicates()
    {
        var result = Validate(Definition(
            90,
            null,
            [
                Check(ValidationCheckKind.MainDatasetResult, null, 80, 0),
                Check(ValidationCheckKind.RequiredTable, tableId.ToString("D").ToLowerInvariant(), 10, 1),
                Check(ValidationCheckKind.RequiredTable, tableId.ToString("D").ToUpperInvariant(), 10, 2)
            ]));

        result.ShouldContain("Validation.DuplicateCheck", "checks[2]");
    }

    private TaskValidationRulesResult Validate(TaskValidationDefinition definition) =>
        validator.Validate(
            definition,
            Capabilities(),
            new ValidationSchemaContext(new HashSet<Guid> { tableId }));

    private TaskValidationDefinition Definition(
        int passingScore,
        int? maxAttempts,
        IReadOnlyList<ValidationCheckDefinition> checks) =>
        new(passingScore, maxAttempts, [HintGroup.Result], checks);

    private IReadOnlyList<ValidationCheckDefinition> ValidCompositeChecks() =>
    [
        Check(ValidationCheckKind.MainDatasetResult, null, 70, 0),
        Check(ValidationCheckKind.RequiredTable, tableId.ToString("D"), 30, 1)
    ];

    private static ValidationCheckDefinition Check(
        ValidationCheckKind kind,
        string? value,
        int weight,
        int order) => new(null, kind, value, weight, order);

    private static ValidationRuleCapabilities Capabilities(
        IReadOnlySet<ValidationCheckKind>? checkKinds = null,
        IReadOnlySet<SqlConstruct>? constructs = null,
        IReadOnlySet<HintGroup>? hints = null) =>
        new(
            checkKinds ?? Enum.GetValues<ValidationCheckKind>().ToHashSet(),
            constructs ?? Enum.GetValues<SqlConstruct>().ToHashSet(),
            hints ?? Enum.GetValues<HintGroup>().ToHashSet(),
            100);
}

internal static class TaskValidationRulesResultShouldlyExtensions
{
    internal static void ShouldContain(
        this TaskValidationRulesResult result,
        string code,
        string path) =>
        result.Violations.ShouldContain(value => value.Code == code && value.Path == path);
}
