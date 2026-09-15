using Shouldly;
using SQLModule.Domain.Training;

namespace SQLModule.UnitTests.Training;

public sealed class Phase2bDomainModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "Конфигурация хранит группы подсказок без дублей и меняет concurrency version")]
    public void Configuration_StoresHintMaskAndChangesVersion()
    {
        var configuration = TaskValidationConfiguration.Create(
            Guid.NewGuid(), 70, null,
            [HintGroup.Result, HintGroup.RequiredTables, HintGroup.Result]);
        var initialVersion = configuration.Version;

        configuration.GetVisibleHintGroups().ShouldBe([HintGroup.Result, HintGroup.RequiredTables]);

        configuration.UpdateSettings(80, 5, [HintGroup.ForbiddenConstructs]);

        configuration.PassingScore.ShouldBe(80);
        configuration.MaxAttempts.ShouldBe(5);
        configuration.GetVisibleHintGroups().ShouldBe([HintGroup.ForbiddenConstructs]);
        configuration.Version.ShouldNotBe(initialVersion);
    }

    [Fact(DisplayName = "Progress выдаёт монотонные номера попыток и не уменьшает лучший балл")]
    public void Progress_ReservesNumbersAndKeepsBestScore()
    {
        var progress = StudentTaskProgress.CreateStandalone(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        progress.ReserveAttemptNumber().ShouldBe(1);
        progress.ReserveAttemptNumber().ShouldBe(2);
        progress.RecordCountedAttempt(80);
        progress.RecordCountedAttempt(40);

        progress.AttemptsUsed.ShouldBe(2);
        progress.NextAttemptNumber.ShouldBe(3);
        progress.BestScore.ShouldBe(80);
    }

    [Fact(DisplayName = "Финализация фиксирует лучший балл и проходит явные состояния")]
    public void Progress_FinalizationFreezesBestScore()
    {
        var progress = StudentTaskProgress.CreatePlatform(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now.AddHours(1));
        progress.RecordCountedAttempt(85);

        progress.BeginFinalization(FinalizationReason.Manual, Now);

        progress.Status.ShouldBe(ProgressStatus.Finalizing);
        progress.FinalScore.ShouldBe(85);
        progress.FinalizationReason.ShouldBe(FinalizationReason.Manual);
        progress.FinalizedAt.ShouldBe(Now);

        progress.MarkCompletionPending();
        progress.Status.ShouldBe(ProgressStatus.CompletionPending);
        progress.MarkCompleted();
        progress.Status.ShouldBe(ProgressStatus.Completed);
        progress.FinalScore.ShouldBe(85);
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
            Guid.NewGuid(), 1, Guid.NewGuid(), 70, null,
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

    [Fact(DisplayName = "Reservation сохраняет идемпотентный ключ и результат выполнения")]
    public void Reservation_TracksLifecycle()
    {
        var attemptId = Guid.NewGuid();
        var reservation = AttemptReservation.Create(
            Guid.NewGuid(), 3, Guid.NewGuid(), new string('A', 64), Now);

        reservation.State.ShouldBe(AttemptReservationState.Reserved);
        reservation.Complete(attemptId, Now.AddSeconds(1));

        reservation.State.ShouldBe(AttemptReservationState.Completed);
        reservation.AttemptId.ShouldBe(attemptId);
        reservation.UpdatedAt.ShouldBe(Now.AddSeconds(1));
    }

    [Fact(DisplayName = "Legacy Attempt получает Phase 2b scoring без изменения бинарного результата")]
    public void Attempt_AttachesScoringWithoutChangingMainResult()
    {
        var attempt = Attempt.Record(
            Guid.NewGuid(), Guid.NewGuid(), "SELECT 1",
            ExecutionStatus.Succeeded, true, CheckReason.Ok,
            1, 10, null, Now, Now.AddMilliseconds(10));

        attempt.AttachScoring(Guid.NewGuid(), Guid.NewGuid(), 1, 90, true);

        attempt.ProgressId.ShouldNotBeNull();
        attempt.ValidationVersionId.ShouldNotBeNull();
        attempt.AttemptNumber.ShouldBe(1);
        attempt.Score.ShouldBe(90);
        attempt.CountsTowardLimit.ShouldBeTrue();
        attempt.IsCorrect.ShouldBeTrue();
        attempt.Reason.ShouldBe(CheckReason.Ok);
    }
}
