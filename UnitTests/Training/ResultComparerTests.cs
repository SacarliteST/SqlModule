using Shouldly;
using SQLModule.Domain.Training;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;
using SQLModule.Web.Features.Training.Attempts;

namespace SQLModule.UnitTests.Training;

public sealed class ResultComparerTests
{
    private readonly IResultComparer comparer = new StrictResultComparer();

    private static GoldenResult Golden(IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string?>> rows)
        => new(columns, rows);

    private static QueryResultSet Ok(IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<string?>> rows)
        => new(true, null, columns, rows, rows.Count, 0);

    [Fact(DisplayName = "Идентичные пустые результаты → Ok")]
    public void Compare_BothEmpty_Ok()
    {
        var outcome = comparer.Compare(Golden([], []), Ok([], []));
        outcome.IsCorrect.ShouldBeTrue();
        outcome.Reason.ShouldBe(CheckReason.Ok);
    }

    [Fact(DisplayName = "Идентичные непустые результаты → Ok")]
    public void Compare_IdenticalResults_Ok()
    {
        var golden = Golden(["id", "name"], [["1", "Alice"], ["2", "Bob"]]);
        var actual = Ok(["id", "name"], [["1", "Alice"], ["2", "Bob"]]);
        var outcome = comparer.Compare(golden, actual);
        outcome.IsCorrect.ShouldBeTrue();
        outcome.Reason.ShouldBe(CheckReason.Ok);
    }

    [Fact(DisplayName = "Разные имена колонок при одинаковом числе → Ok (имена не сравниваются)")]
    public void Compare_DifferentColumnNames_SameCount_Ok()
    {
        var golden = Golden(["id", "name"], []);
        var actual = Ok(["id", "email"], []);
        var outcome = comparer.Compare(golden, actual);
        outcome.IsCorrect.ShouldBeTrue();
        outcome.Reason.ShouldBe(CheckReason.Ok);
    }

    [Fact(DisplayName = "Разное количество колонок → ColumnMismatch")]
    public void Compare_DifferentColumnCount_ColumnMismatch()
    {
        var golden = Golden(["id"], []);
        var actual = Ok(["id", "name"], []);
        var outcome = comparer.Compare(golden, actual);
        outcome.IsCorrect.ShouldBeFalse();
        outcome.Reason.ShouldBe(CheckReason.ColumnMismatch);
    }

    [Fact(DisplayName = "Регистр имён колонок не важен — только количество")]
    public void Compare_ColumnNamesCaseAndContent_DoNotMatter()
    {
        var golden = Golden(["ID", "Name"], []);
        var actual = Ok(["completely_different", "columns"], []);
        var outcome = comparer.Compare(golden, actual);
        outcome.IsCorrect.ShouldBeTrue();
    }

    [Fact(DisplayName = "Разное количество строк → RowCountMismatch")]
    public void Compare_DifferentRowCount_RowCountMismatch()
    {
        var golden = Golden(["id"], [["1"], ["2"]]);
        var actual = Ok(["id"], [["1"]]);
        var outcome = comparer.Compare(golden, actual);
        outcome.IsCorrect.ShouldBeFalse();
        outcome.Reason.ShouldBe(CheckReason.RowCountMismatch);
    }

    [Fact(DisplayName = "Разные значения ячеек → ValueMismatch")]
    public void Compare_DifferentCellValues_ValueMismatch()
    {
        var golden = Golden(["id"], [["1"]]);
        var actual = Ok(["id"], [["2"]]);
        var outcome = comparer.Compare(golden, actual);
        outcome.IsCorrect.ShouldBeFalse();
        outcome.Reason.ShouldBe(CheckReason.ValueMismatch);
    }

    [Fact(DisplayName = "null vs non-null ячейка → ValueMismatch")]
    public void Compare_NullVsValue_ValueMismatch()
    {
        var golden = Golden(["id"], [[null]]);
        var actual = Ok(["id"], [["1"]]);
        var outcome = comparer.Compare(golden, actual);
        outcome.IsCorrect.ShouldBeFalse();
        outcome.Reason.ShouldBe(CheckReason.ValueMismatch);
    }

    [Fact(DisplayName = "Оба null в ячейке → Ok")]
    public void Compare_BothNull_Ok()
    {
        var golden = Golden(["id"], [[null]]);
        var actual = Ok(["id"], [[null]]);
        var outcome = comparer.Compare(golden, actual);
        outcome.IsCorrect.ShouldBeTrue();
    }

    [Fact(DisplayName = "ColumnMismatch имеет приоритет над RowCountMismatch")]
    public void Compare_ColumnAndRowMismatch_ColumnFirst()
    {
        var golden = Golden(["a", "b"], [["1", "2"]]);
        var actual = Ok(["a"], []);
        var outcome = comparer.Compare(golden, actual);
        outcome.Reason.ShouldBe(CheckReason.ColumnMismatch);
    }

    [Fact(DisplayName = "RowCountMismatch имеет приоритет над ValueMismatch")]
    public void Compare_RowCountAndValueMismatch_RowCountFirst()
    {
        var golden = Golden(["id"], [["1"], ["2"]]);
        var actual = Ok(["id"], [["9"]]);
        var outcome = comparer.Compare(golden, actual);
        outcome.Reason.ShouldBe(CheckReason.RowCountMismatch);
    }
}
