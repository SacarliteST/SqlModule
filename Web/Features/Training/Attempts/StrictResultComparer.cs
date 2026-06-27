using SQLModule.Domain.Training;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;

namespace SQLModule.Web.Features.Training.Attempts;

internal sealed class StrictResultComparer : IResultComparer
{
    public CheckOutcome Compare(GoldenResult expected, QueryResultSet actual)
    {
        if (!ColumnsMatch(expected.Columns, actual.Columns))
        {
            return new CheckOutcome(false, CheckReason.ColumnMismatch);
        }

        if (expected.Rows.Count != actual.Rows.Count)
        {
            return new CheckOutcome(false, CheckReason.RowCountMismatch);
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
}
