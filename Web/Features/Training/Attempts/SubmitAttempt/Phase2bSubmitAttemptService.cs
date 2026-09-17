using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;
using SQLModule.PlatformIntegration.Contracts;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;
using SQLModule.Web.Features.Training.Progress;

namespace SQLModule.Web.Features.Training.Attempts.SubmitAttempt;

internal sealed record Phase2bSubmitResult(bool Handled, Result<SubmitAttemptResponse>? Result);

internal interface IPhase2bSubmitAttemptService
{
    Task<Phase2bSubmitResult> TryHandleAsync(
        SubmitAttemptCommand command,
        ModuleSession? moduleSession,
        CancellationToken ct);
}

internal sealed class Phase2bSubmitAttemptService(
    AppDbContext db,
    IAttemptReservationService reservationService,
    IProgressFinalizationService finalizationService,
    IPlatformProgressService platformProgressService,
    IPhase2bValidationRuntimeReader runtimeReader,
    ISqlSyntaxAnalyzerResolver analyzerResolver,
    ISandboxExecutor executor,
    IResultComparer comparer,
    IAttemptResultSnapshotService snapshotService,
    IOptions<SandboxOptions> sandboxOptions,
    TimeProvider timeProvider,
    ILogger<Phase2bSubmitAttemptService> logger) : IPhase2bSubmitAttemptService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Phase2bSubmitResult> TryHandleAsync(
        SubmitAttemptCommand command,
        ModuleSession? moduleSession,
        CancellationToken ct)
    {
        var progress = moduleSession is null
            ? await db.StudentTaskProgresses.Include(value => value.ValidationVersion)
                .SingleOrDefaultAsync(value =>
                    value.UserId == command.UserId && value.TaskId == command.TaskId &&
                    value.ModuleSessionId == null && value.Status == ProgressStatus.Active, ct)
            : await db.StudentTaskProgresses.Include(value => value.ValidationVersion)
                .SingleOrDefaultAsync(value => value.ModuleSessionId == moduleSession.Id, ct);
        if (progress is null && moduleSession is not null)
        {
            // Проверяем закрытие/истечение сессии только когда для неё ещё нет прохождения:
            // если прохождение уже существует, эту же проверку по статусу прохождения делает
            // ReserveAsync ниже — но она корректно пропускает идемпотентный повтор (тот же
            // Idempotency-Key), в отличие от проверки здесь, которая касается только
            // по-настоящему нового запроса на ещё не начатую платформенную сессию.
            if (moduleSession.Status != ModuleSessionStatus.Active || moduleSession.IsExpired(timeProvider.GetUtcNow()))
            {
                return Handled(Result<SubmitAttemptResponse>.Fail(AttemptErrors.ModuleSessionClosed));
            }

            var created = await platformProgressService.EnsureCreatedAsync(moduleSession, ct);
            if (!created.IsSuccess)
            {
                return Handled(Result<SubmitAttemptResponse>.Fail(created.Error!));
            }

            try
            {
                await db.SaveChangesAsync(ct);
                progress = await db.StudentTaskProgresses.Include(value => value.ValidationVersion)
                    .SingleAsync(value => value.Id == created.Value!.Id, ct);
            }
            catch (DbUpdateException)
            {
                // Конкурентный запрос уже создал прохождение для этой же ModuleSessionId
                // (уникальный индекс IX_StudentTaskProgresses_ModuleSessionId) — подхватываем
                // его вместо падения в 500, дальше решает ReserveAsync ниже. Отсоединяем только
                // неудавшуюся Added-запись, а не весь ChangeTracker.Clear() — иначе заодно
                // "теряется" отслеживание moduleSession, и его MarkCompletionPending() дальше
                // по коду молча не сохранится.
                var failedEntry = db.ChangeTracker.Entries<StudentTaskProgress>()
                    .FirstOrDefault(entry => entry.State == EntityState.Added);
                if (failedEntry is not null)
                {
                    failedEntry.State = EntityState.Detached;
                }

                progress = await db.StudentTaskProgresses.Include(value => value.ValidationVersion)
                    .SingleAsync(value => value.ModuleSessionId == moduleSession.Id, ct);
            }
        }

        if (progress is null && moduleSession is null)
        {
            // Нет активного standalone-прохождения — но это может быть идемпотентный повтор
            // запроса, который сам это прохождение завершил (например, сразу набрал 100 баллов).
            // ReserveAsync ниже уже умеет отвечать replay-ом для существующей резервации вне
            // зависимости от статуса прохождения — не хватало только найти его без progress.Id,
            // который standalone-клиент не передаёт явно.
            progress = await FindProgressByIdempotencyKeyAsync(command, ct);
        }

        if (progress is null)
        {
            return new Phase2bSubmitResult(false, null);
        }

        var payloadHash = HashPayload(command.TaskId, command.SubmittedSql);
        var reservationResult = await reservationService.ReserveAsync(
            progress.Id, Guid.Parse(command.IdempotencyKey), payloadHash, ct);
        if (!reservationResult.IsSuccess)
        {
            return Handled(Result<SubmitAttemptResponse>.Fail(reservationResult.Error!));
        }

        var reservation = reservationResult.Value!.Reservation;
        if (reservationResult.Value.IsReplay)
        {
            if (reservation.State != AttemptReservationState.Completed || !reservation.AttemptId.HasValue)
            {
                return Handled(Result<SubmitAttemptResponse>.Fail(Error.Conflict(
                    "IdempotencyRequestInProgress",
                    "Запрос с этим Idempotency-Key ещё выполняется.")));
            }

            return Handled(await ReplayAsync(reservation.AttemptId.Value, ct));
        }

        // ReserveAsync при конкурентной гонке за номер попытки мог внутри своего retry
        // сделать db.ChangeTracker.Clear() и заново отследить прогресс новым CLR-инстансом —
        // подхватываем именно его, чтобы не держать вторую, уже отсоединённую ссылку на тот
        // же Id (иначе db.Entry(progress).ReloadAsync ниже упадёт с "another instance ...
        // already tracked").
        progress = db.ChangeTracker.Entries<StudentTaskProgress>()
            .Select(entry => entry.Entity)
            .FirstOrDefault(entity => entity.Id == progress.Id) ?? progress;

        var runtimeResult = await runtimeReader.ReadAsync(progress.ValidationVersionId, ct);
        if (!runtimeResult.IsSuccess)
        {
            await reservationService.ReleaseAsync(reservation.Id, ct);
            return Handled(Result<SubmitAttemptResponse>.Fail(runtimeResult.Error!));
        }

        var runtime = runtimeResult.Value!;
        var analyzer = analyzerResolver.Resolve(runtime.Dbms.DbmsSystemName);
        if (analyzer is null || analyzer.AnalyzerVersion != runtime.Version.AnalyzerVersion)
        {
            await reservationService.ReleaseAsync(reservation.Id, ct);
            return Handled(Result<SubmitAttemptResponse>.Fail(Error.Unavailable(
                "Validation.AnalyzerUnavailable",
                "Анализатор SQL временно недоступен.")));
        }

        SqlSyntaxAnalysis analysis;
        try
        {
            analysis = analyzer.Analyze(command.SubmittedSql);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Анализатор SQL аварийно завершил проверку прохождения {ProgressId}",
                progress.Id);
            await ReleaseReservationSafelyAsync(reservation.Id);
            return Handled(Result<SubmitAttemptResponse>.Fail(Error.Unavailable(
                "Validation.AnalyzerUnavailable",
                "Анализатор SQL временно недоступен.")));
        }

        if (analysis.Status == SqlSyntaxAnalysisStatus.InfrastructureFailure)
        {
            await reservationService.ReleaseAsync(reservation.Id, ct);
            return Handled(Result<SubmitAttemptResponse>.Fail(Error.Unavailable(
                "Validation.AnalyzerUnavailable",
                "Анализатор SQL временно недоступен.")));
        }

        var options = sandboxOptions.Value;
        var comparisonRowLimit = Math.Max(1, options.ComparisonMaxRows);
        var startedAt = timeProvider.GetUtcNow();
        Result<QueryResultSet> run;
        try
        {
            run = await executor.RunAsync(
                runtime.Dbms.ToSandboxSpec(),
                runtime.Setup,
                new SandboxQuery(command.SubmittedSql, options.DefaultQueryTimeoutSeconds, comparisonRowLimit),
                ct);
        }
        catch (OperationCanceledException)
        {
            await ReleaseReservationSafelyAsync(reservation.Id);
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Песочница аварийно завершила попытку прохождения {ProgressId}",
                progress.Id);
            await ReleaseReservationSafelyAsync(reservation.Id);
            return Handled(Result<SubmitAttemptResponse>.Fail(Error.Unavailable(
                "Validation.SandboxUnavailable",
                "Среда выполнения SQL временно недоступна.")));
        }

        var finishedAt = timeProvider.GetUtcNow();
        if (!run.IsSuccess)
        {
            await reservationService.ReleaseAsync(reservation.Id, ct);
            return Handled(Result<SubmitAttemptResponse>.Fail(Error.Unavailable(
                "Validation.SandboxUnavailable",
                "Среда выполнения SQL временно недоступна.")));
        }

        var result = run.Value!;
        var main = EvaluateMain(result, runtime, comparer, comparisonRowLimit);
        var checks = EvaluateChecks(runtime, analysis, main.IsCorrect);
        var score = checks.Sum(value => value.AwardedScore);
        var attempt = Attempt.Record(
            command.UserId, command.TaskId, command.SubmittedSql,
            main.Status, main.IsCorrect, main.Reason,
            result.Succeeded ? result.RowCount : null,
            result.Succeeded ? result.DurationMs : null,
            main.PublicError, startedAt, finishedAt,
            studentName: command.StudentName,
            studentEmail: command.StudentEmail,
            moduleSessionId: moduleSession?.Id);
        attempt.AttachScoring(progress.Id, runtime.Version.Id, reservation.AttemptNumber, score, true);
        var snapshot = snapshotService.Create(
            result, snapshotService.GetEffectiveRowLimit(options.MaxRows), finishedAt);
        snapshotService.Apply(attempt, snapshot);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await LockProgressAsync(progress.Id, ct);
            await db.Entry(progress).ReloadAsync(ct);
            if (progress.Status != ProgressStatus.Active)
            {
                // Пока эта попытка выполнялась в песочнице, конкурентная попытка уже заняла
                // последнее место и завершила прохождение (например, тоже набрала 100 баллов) —
                // ReserveAsync выше не мог это поймать заранее, статус проверяем только теперь,
                // под блокировкой строки прохождения.
                await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();
                await ReleaseReservationSafelyAsync(reservation.Id);
                return Handled(Result<SubmitAttemptResponse>.Fail(ProgressErrors.Closed));
            }

            progress.RecordCountedAttempt(score);
            FinalizationReason? finalizationReason = null;
            if (score == 100)
            {
                finalizationReason = FinalizationReason.PerfectScore;
            }
            else if (runtime.Version.MaxAttempts.HasValue &&
                     progress.AttemptsUsed >= runtime.Version.MaxAttempts.Value)
            {
                finalizationReason = FinalizationReason.AttemptsExhausted;
            }

            if (finalizationReason.HasValue)
            {
                await finalizationService.PrepareAsync(
                    progress, moduleSession, finalizationReason.Value, finishedAt, attempt, ct);
            }

            db.Attempts.Add(attempt);
            db.AttemptCheckResults.AddRange(checks.Select(value => AttemptCheckResult.Create(
                attempt.Id, value.Check.Id, value.Check.Kind, value.Status,
                value.Check.Weight, value.AwardedScore, value.Check.Order, value.Message, null)));
            reservation.Complete(attempt.Id, finishedAt);
            if (moduleSession is not null)
            {
                AddPracticeEvent(moduleSession, attempt, finishedAt);
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            await ReleaseReservationSafelyAsync(reservation.Id);
            throw;
        }

        logger.LogInformation(
            "Попытка {AttemptId} прохождения {ProgressId} оценена в {Score} баллов",
            attempt.Id, progress.Id, score);

        return Handled(ToResponse(
            attempt, progress, runtime, checks, snapshot, finishedAt));
    }

    /// <summary>
    /// Находит standalone-прохождение по чужой (уже не Active) резервации с тем же
    /// Idempotency-Key — единственный способ повторно найти прохождение для replay,
    /// когда клиент не передаёт progress.Id явно (в отличие от platform-flow, где
    /// прохождение всегда однозначно определяется по moduleSession.Id).
    /// </summary>
    private async Task<StudentTaskProgress?> FindProgressByIdempotencyKeyAsync(
        SubmitAttemptCommand command, CancellationToken ct)
    {
        var idempotencyKey = Guid.Parse(command.IdempotencyKey);
        var reservation = await db.AttemptReservations
            .Include(value => value.Progress).ThenInclude(value => value.ValidationVersion)
            .Where(value =>
                value.IdempotencyKey == idempotencyKey &&
                value.Progress.UserId == command.UserId &&
                value.Progress.TaskId == command.TaskId &&
                value.Progress.ModuleSessionId == null)
            .OrderByDescending(value => value.CreatedAt)
            .FirstOrDefaultAsync(ct);
        return reservation?.Progress;
    }

    private async Task LockProgressAsync(Guid progressId, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = """SELECT "Id" FROM "StudentTaskProgresses" WHERE "Id" = @progressId FOR UPDATE""";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "progressId";
        parameter.Value = progressId;
        command.Parameters.Add(parameter);
        _ = await command.ExecuteScalarAsync(ct);
    }

    private async Task ReleaseReservationSafelyAsync(Guid reservationId)
    {
        try
        {
            db.ChangeTracker.Clear();
            await reservationService.ReleaseAsync(reservationId, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Не удалось освободить резервацию попытки {ReservationId}",
                reservationId);
        }
    }

    private async Task<Result<SubmitAttemptResponse>> ReplayAsync(Guid attemptId, CancellationToken ct)
    {
        var attempt = await db.Attempts.AsNoTracking().SingleAsync(value => value.Id == attemptId, ct);
        var progress = await db.StudentTaskProgresses.AsNoTracking()
            .SingleAsync(value => value.Id == attempt.ProgressId, ct);
        var runtimeResult = await runtimeReader.ReadAsync(attempt.ValidationVersionId!.Value, ct);
        if (!runtimeResult.IsSuccess)
        {
            return Result<SubmitAttemptResponse>.Fail(runtimeResult.Error!);
        }

        var storedChecks = await db.AttemptCheckResults.AsNoTracking()
            .Where(value => value.AttemptId == attemptId)
            .OrderBy(value => value.Order)
            .ToArrayAsync(ct);
        var runtime = runtimeResult.Value!;
        var byId = runtime.Configuration.Checks.ToDictionary(value => value.Id);
        var checks = storedChecks.Select(value => new EvaluatedCheck(
            byId[value.ValidationCheckId], value.Status, value.AwardedScore, value.Message)).ToArray();
        return ToResponse(
            attempt, progress, runtime, checks,
            snapshotService.Read(attempt, timeProvider.GetUtcNow()), timeProvider.GetUtcNow());
    }

    private static MainResult EvaluateMain(
        QueryResultSet result,
        Phase2bValidationRuntime runtime,
        IResultComparer comparer,
        int rowLimit)
    {
        if (!result.Succeeded)
        {
            var timeout = result.Error?.Contains("timeout", StringComparison.OrdinalIgnoreCase) == true;
            return new MainResult(
                timeout ? ExecutionStatus.TimedOut : ExecutionStatus.Error,
                false,
                timeout ? CheckReason.Timeout : CheckReason.SqlError,
                timeout ? "Превышено допустимое время выполнения запроса." :
                    "SQL-запрос не удалось выполнить. Проверьте синтаксис и повторите попытку.");
        }

        if (result.IsTruncated)
        {
            return new MainResult(
                ExecutionStatus.Succeeded, false, CheckReason.ResultLimitExceeded,
                $"Результат запроса превышает безопасный лимит сравнения {rowLimit} строк.");
        }

        var outcome = comparer.Compare(runtime.Expected, result, runtime.Reference.IsRequiredRowOrder);
        return new MainResult(ExecutionStatus.Succeeded, outcome.IsCorrect, outcome.Reason, null);
    }

    private static IReadOnlyList<EvaluatedCheck> EvaluateChecks(
        Phase2bValidationRuntime runtime,
        SqlSyntaxAnalysis analysis,
        bool mainPassed)
    {
        var tables = runtime.Schema.Tables.ToDictionary(value => value.Key, value => value.Name);
        return runtime.Configuration.Checks.OrderBy(value => value.Order).Select(check =>
        {
            var passed = check.Kind switch
            {
                ValidationCheckKind.MainDatasetResult => mainPassed,
                ValidationCheckKind.RequiredConstruct => Enum.TryParse<SqlConstruct>(check.Value, out var required) &&
                                                         analysis.IsSuccess && analysis.Constructs.Contains(required),
                ValidationCheckKind.ForbiddenConstruct => !Enum.TryParse<SqlConstruct>(check.Value, out var forbidden) ||
                                                          !analysis.IsSuccess || !analysis.Constructs.Contains(forbidden),
                ValidationCheckKind.RequiredTable => ContainsTable(check.Value, tables, analysis),
                ValidationCheckKind.ForbiddenTable => !ContainsTable(check.Value, tables, analysis),
                _ => false
            };
            return new EvaluatedCheck(
                check,
                passed ? ValidationCheckStatus.Passed : ValidationCheckStatus.Failed,
                passed ? check.Weight : 0,
                passed ? "Критерий выполнен." : "Критерий не выполнен.");
        }).ToArray();
    }

    private static bool ContainsTable(
        string? value,
        IReadOnlyDictionary<string, string> tables,
        SqlSyntaxAnalysis analysis) =>
        value is not null && tables.TryGetValue(value, out var name) && analysis.IsSuccess &&
        analysis.ReferencedTables.Any(reference =>
            String.Equals(reference.Name, name, StringComparison.OrdinalIgnoreCase));

    private SubmitAttemptResponse ToResponse(
        Attempt attempt,
        StudentTaskProgress progress,
        Phase2bValidationRuntime runtime,
        IReadOnlyList<EvaluatedCheck> checks,
        AttemptResultSnapshot snapshot,
        DateTimeOffset now)
    {
        var progressResponse = ProgressMappings.ToResponse(progress, runtime.Version, now);
        var visible = runtime.Configuration.VisibleHintGroups.ToHashSet();
        var visibleChecks = checks.Where(value =>
        {
            var group = AttemptScoringReadService.ToHintGroup(value.Check.Kind);
            return group.HasValue && visible.Contains(group.Value);
        }).ToArray();
        var hints = checks.Where(value => value.Status == ValidationCheckStatus.Failed)
            .Select(value => ToHint(value.Check.Kind))
            .Where(group => group.HasValue && visible.Contains(group.Value))
            .Distinct()
            .Select(group => new AttemptHintResponse(group!.Value, "Проверьте соответствующую часть решения."))
            .ToArray();
        return new SubmitAttemptResponse(
            attempt.Id, attempt.Status, attempt.IsCorrect, attempt.Reason,
            attempt.RowCount, attempt.DurationMs, attempt.ErrorMessage,
            snapshot.Columns, snapshot.Rows, snapshot.IsTruncated, snapshot.State,
            snapshot.ReturnedRowCount, snapshot.RowLimit, snapshot.CreatedAt, snapshot.ExpiresAt,
            progress.Id, runtime.Version.Id, attempt.AttemptNumber, attempt.Score,
            progress.BestScore, runtime.Version.PassingScore, progressResponse.IsPassed,
            progress.AttemptsUsed, progressResponse.AttemptsRemaining,
            progressResponse.CanSubmit, progressResponse.CanFinalize,
            visibleChecks.Select(value => new AttemptCheckResultResponse(
                value.Check.Kind, value.Status, value.Check.Weight, value.AwardedScore, value.Message)).ToArray(),
            hints);
    }

    private void AddPracticeEvent(ModuleSession session, Attempt attempt, DateTimeOffset occurredAt)
    {
        var eventId = Guid.NewGuid();
        var message = new PracticeEventMessage(
            session.Id, session.SessionKey, eventId, "sql_submit", occurredAt,
            new PracticeEventPayload(
                attempt.SubmittedSql, ToIntegrationStatus(attempt.Status),
                attempt.RowCount, attempt.DurationMs, attempt.IsCorrect, attempt.Reason.ToString()));
        db.PendingPublishes.Add(PendingPublish.Create(
            eventId, PendingPublishKind.Event, session.Id, $"event:{attempt.Id:D}",
            JsonSerializer.Serialize(message, JsonOptions), occurredAt));
    }

    /// <summary>
    /// Тот же формат статуса в outbox-событии, что и у legacy-обработчика
    /// (<c>SubmitAttemptCommand.ToIntegrationStatus</c>) — потребители события
    /// (Education) ожидают "SUCCESS"/"ERROR"/"TIMEOUT", а не сырое имя enum.
    /// </summary>
    private static string ToIntegrationStatus(ExecutionStatus status) => status switch
    {
        ExecutionStatus.Succeeded => "SUCCESS",
        ExecutionStatus.Error => "ERROR",
        ExecutionStatus.TimedOut => "TIMEOUT",
        _ => "UNKNOWN"
    };

    private static HintGroup? ToHint(ValidationCheckKind kind) => kind switch
    {
        ValidationCheckKind.MainDatasetResult => HintGroup.Result,
        ValidationCheckKind.RequiredConstruct => HintGroup.RequiredConstructs,
        ValidationCheckKind.ForbiddenConstruct => HintGroup.ForbiddenConstructs,
        ValidationCheckKind.RequiredTable => HintGroup.RequiredTables,
        ValidationCheckKind.ForbiddenTable => HintGroup.ForbiddenTables,
        _ => null
    };

    private static string HashPayload(Guid taskId, string submittedSql) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes($"{taskId:D}\n{submittedSql}")));

    private static Phase2bSubmitResult Handled(Result<SubmitAttemptResponse> result) => new(true, result);

    private sealed record MainResult(
        ExecutionStatus Status, bool IsCorrect, CheckReason Reason, string? PublicError);

    private sealed record EvaluatedCheck(
        ValidationCheckSnapshot Check,
        ValidationCheckStatus Status,
        int AwardedScore,
        string? Message);
}
