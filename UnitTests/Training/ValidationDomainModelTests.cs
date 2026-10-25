using Shouldly;
using SQLModule.Domain.Training;

namespace SQLModule.UnitTests.Training;

public sealed class ValidationDomainModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Конфигурация хранит группы подсказок без дублей и меняет concurrency version")]
    public void Configuration_StoresHintMaskAndChangesVersion()
    {
        var configuration = TaskValidationConfiguration.Create(
            Guid.NewGuid(), 70,
            [HintGroup.Result, HintGroup.RequiredTables, HintGroup.Result]);
        var initialVersion = configuration.Version;

        configuration.GetVisibleHintGroups().ShouldBe([HintGroup.Result, HintGroup.RequiredTables]);

        configuration.UpdateSettings(80, [HintGroup.ForbiddenConstructs]);

        configuration.PassingScore.ShouldBe(80);
        configuration.GetVisibleHintGroups().ShouldBe([HintGroup.ForbiddenConstructs]);
        configuration.Version.ShouldNotBe(initialVersion);
    }

    [Fact(DisplayName = "Validation version сохраняет полный immutable snapshot")]
    public void ValidationVersion_PreservesSnapshot()
    {
        var snapshot = new TaskValidationVersionSnapshot(
            "{\"schema\":1}",
            "{\"rows\":[]}",
            "{\"sql\":\"SELECT 1\"}",
            "{\"columns\":[\"?column?\"]}",
            "{\"checks\":[]}",
            "postgres-ast/1.0");

        var version = TaskValidationVersion.Publish(
            Guid.NewGuid(), 1, Guid.NewGuid(), 70,
            (1L << (int)HintGroup.Result), snapshot,
            Guid.NewGuid(), "Преподаватель", Now);

        version.SchemaSnapshotJson.ShouldBe(snapshot.SchemaJson);
        version.DatasetSnapshotJson.ShouldBe(snapshot.DatasetJson);
        version.ReferenceQuerySnapshotJson.ShouldBe(snapshot.ReferenceQueryJson);
        version.ExpectedResultSnapshotJson.ShouldBe(snapshot.ExpectedResultJson);
        version.ValidationConfigurationSnapshotJson.ShouldBe(snapshot.ConfigurationJson);
        version.AnalyzerVersion.ShouldBe(snapshot.AnalyzerVersion);
        version.GetVisibleHintGroups().ShouldBe([HintGroup.Result]);
    }

    [Fact(DisplayName = "Attempt получает scoring версионированной проверки без изменения бинарного результата")]
    public void Attempt_AttachesScoringWithoutChangingMainResult()
    {
        var attempt = Attempt.Record(
            Guid.NewGuid(), Guid.NewGuid(), "SELECT 1",
            ExecutionStatus.Succeeded, true, CheckReason.Ok,
            1, 10, null, Now, Now.AddMilliseconds(10));

        attempt.AttachScoring(Guid.NewGuid(), 1, 90, true);

        attempt.ValidationVersionId.ShouldNotBeNull();
        attempt.AttemptNumber.ShouldBe(1);
        attempt.Score.ShouldBe(90);
        attempt.CountsTowardLimit.ShouldBeTrue();
        attempt.IsCorrect.ShouldBeTrue();
        attempt.Reason.ShouldBe(CheckReason.Ok);
    }
}
