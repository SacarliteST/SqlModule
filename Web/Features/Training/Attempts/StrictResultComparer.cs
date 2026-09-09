using System.Text.Json;
using SQLModule.Domain.Training;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;

namespace SQLModule.Web.Features.Training.Attempts;

internal sealed class StrictResultComparer : IResultComparer
{
    public CheckOutcome Compare(GoldenResult expected, QueryResultSet actual, bool strictRowOrder = true)
    {
        if (!ColumnsMatch(expected.Columns, actual.Columns))
        {
            return new CheckOutcome(false, CheckReason.ColumnMismatch);
        }

        if (expected.Rows.Count != actual.Rows.Count)
        {
            return new CheckOutcome(false, CheckReason.RowCountMismatch);
        }

        if (!strictRowOrder)
        {
            return RowsMatchWithoutOrder(expected.Rows, actual.Rows)
                ? new CheckOutcome(true, CheckReason.Ok)
                : new CheckOutcome(false, CheckReason.ValueMismatch);
        }

        for (var i = 0; i < expected.Rows.Count; i++)
        {
            var expectedRow = expected.Rows[i];
            var actualRow = actual.Rows[i];

            if (expectedRow.Count != actualRow.Count)
            {
                return new CheckOutcome(false, CheckReason.ValueMismatch);
            }

            for (var j = 0; j < expectedRow.Count; j++)
            {
                if (expectedRow[j] != actualRow[j])
                {
                    return new CheckOutcome(false, CheckReason.ValueMismatch);
                }
            }
        }

        return new CheckOutcome(true, CheckReason.Ok);
    }

    private static bool ColumnsMatch(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
        => expected.Count == actual.Count;

    private static bool RowsMatchWithoutOrder(
        IReadOnlyList<IReadOnlyList<string?>> expected,
        IReadOnlyList<IReadOnlyList<string?>> actual)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in expected)
        {
            var key = JsonSerializer.Serialize(row);
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        foreach (var row in actual)
        {
            var key = JsonSerializer.Serialize(row);
            if (!counts.TryGetValue(key, out var count))
            {
                return false;
            }

            if (count == 1)
            {
                counts.Remove(key);
            }
            else
            {
                counts[key] = count - 1;
            }
        }

        return counts.Count == 0;
    }
}
