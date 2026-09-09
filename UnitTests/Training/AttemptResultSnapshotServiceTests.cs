using System.Text;
using Microsoft.Extensions.Options;
using Shouldly;
using SQLModule.Domain.Training;
using SQLModule.Sandbox;
using SQLModule.Web.Features.Training.Attempts;

namespace SQLModule.UnitTests.Training;

public sealed class AttemptResultSnapshotServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Snapshot сохраняет NULL и отличает пустой результат от NotStored")]
    public void Create_PreservesNullAndEmptyResult()
    {
        var service = CreateService();
        var withNull = service.Create(
            new QueryResultSet(true, null, ["id"], [[null]], 1, 1), 200, Now);
        withNull.State.ShouldBe(AttemptResultSnapshotState.Available);
        withNull.Rows![0][0].ShouldBeNull();

        var empty = service.Create(
            new QueryResultSet(true, null, ["id"], [], 0, 1), 200, Now);
        empty.State.ShouldBe(AttemptResultSnapshotState.Available);
        empty.Rows.ShouldBeEmpty();
        empty.ReturnedRowCount.ShouldBe(0);

        var oldAttempt = CreateAttempt();
        service.Read(oldAttempt, Now).State.ShouldBe(AttemptResultSnapshotState.NotStored);
    }

    [Fact(DisplayName = "Snapshot усекает только при дополнительной строке")]
    public void Create_UsesActualAdditionalRowForTruncation()
    {
        var service = CreateService(maxRows: 2);
        var exact = service.Create(
            new QueryResultSet(true, null, ["id"], [["1"], ["2"]], 2, 1), 2, Now);
        exact.IsTruncated.ShouldBeFalse();

        var extra = service.Create(
            new QueryResultSet(true, null, ["id"], [["1"], ["2"], ["3"]], 3, 1), 2, Now);
        extra.IsTruncated.ShouldBeTrue();
        extra.Rows!.Count.ShouldBe(2);
        extra.ReturnedRowCount.ShouldBe(2);
    }

    [Fact(DisplayName = "Snapshot ограничивает колонки, ячейки и общий сериализованный размер")]
    public void Create_EnforcesAllSizeLimits()
    {
        var service = CreateService(maxRows: 10, maxColumns: 2, maxCellLength: 8, maxBytes: 80);
        var longValue = new string('я', 100);
        var result = service.Create(
            new QueryResultSet(true, null, ["first_column", "second_column", "third_column"],
                Enumerable.Range(0, 10)
                    .Select(_ => (IReadOnlyList<string?>)[longValue, longValue, longValue]).ToList(),
                10, 1),
            10, Now);

        result.Columns!.Count.ShouldBeLessThanOrEqualTo(2);
        result.Columns.ShouldAllBe(value => value.Length <= 8);
        result.Rows!.SelectMany(row => row).Where(value => value is not null)
            .ShouldAllBe(value => value!.Length <= 8);
        (Encoding.UTF8.GetByteCount(result.ColumnsJson!) + Encoding.UTF8.GetByteCount(result.RowsJson!))
            .ShouldBeLessThanOrEqualTo(80);
        result.IsTruncated.ShouldBeTrue();
    }

    [Fact(DisplayName = "Неуспешный результат NotProduced, просроченный Available читается как Expired")]
    public void Read_HandlesNotProducedAndExpired()
    {
        var service = CreateService(retentionDays: 30);
        var failed = service.Create(new QueryResultSet(false, "raw", [], [], 0, 1), 200, Now);
        failed.State.ShouldBe(AttemptResultSnapshotState.NotProduced);
        failed.Rows.ShouldBeNull();

        var attempt = CreateAttempt();
        var available = service.Create(
            new QueryResultSet(true, null, ["id"], [["1"]], 1, 1), 200, Now);
        service.Apply(attempt, available);
        var expired = service.Read(attempt, Now.AddDays(31));
        expired.State.ShouldBe(AttemptResultSnapshotState.Expired);
        expired.Columns.ShouldBeNull();
        expired.Rows.ShouldBeNull();
        expired.CreatedAt.ShouldBe(Now);
        expired.ExpiresAt.ShouldBe(Now.AddDays(30));
    }

    private static AttemptResultSnapshotService CreateService(
        int retentionDays = 30,
        int maxRows = 200,
        int maxColumns = 100,
        int maxCellLength = 16384,
        int maxBytes = 1048576) => new(Options.Create(new AttemptResultSnapshotsOptions
        {
            RetentionDays = retentionDays,
            MaxRows = maxRows,
            MaxColumns = maxColumns,
            MaxCellLength = maxCellLength,
            MaxSerializedBytes = maxBytes
        }));

    private static Domain.Training.Attempt CreateAttempt() => Domain.Training.Attempt.Record(
        Guid.NewGuid(), Guid.NewGuid(), "SELECT 1", ExecutionStatus.Succeeded,
        true, CheckReason.Ok, 1, 1, null, Now, Now);
}
