using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Application.Training.Progress;
using SQLModule.Common.Results;
using SQLModule.Contracts.ModuleIntegration;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.Domain.Training;

namespace SQLModule.Application.ModuleIntegration;

/// <summary>Платформенная сессия текущего пользователя; отсутствующая или чужая сессия даёт <see cref="ModuleSessionErrors.NotFound"/>.</summary>
public record GetCurrentModuleSessionQuery(Guid UserId, Guid SessionId)
    : IRequest<Result<CurrentModuleSessionResponse>>;

/// <summary>Каталог опубликованных заданий для Education.</summary>
public record GetModuleTasksCatalogQuery : IRequest<Result<IReadOnlyList<ModuleTaskCatalogItemResponse>>>;

/// <summary>Создание или синхронизация платформенной сессии по push от Education.</summary>
public record UpsertModuleSessionCommand(UpsertModuleSessionRequest Request) : IRequest<Result>;

/// <summary>Ошибки платформенной сессии, которые эндпоинты выводят собственным ответом.</summary>
public static class ModuleSessionErrors
{
    public const string NotFoundCode = "ModuleSession.NotFound";
    public const string CompletedCode = "ModuleSession.Completed";

    public static Error NotFound { get; } = new(
        NotFoundCode, "Платформенная сессия не найдена или недоступна.", ErrorType.NotFound);

    public static Error Completed { get; } = new(
        CompletedCode, "Неактивную платформенную сессию нельзя обновить.", ErrorType.Conflict);
}

public sealed class GetCurrentModuleSessionHandler(IApplicationDbContext db)
    : IRequestHandler<GetCurrentModuleSessionQuery, Result<CurrentModuleSessionResponse>>
{
    public async Task<Result<CurrentModuleSessionResponse>> Handle(
        GetCurrentModuleSessionQuery query, CancellationToken ct)
    {
        var session = await db.ModuleSessions.AsNoTracking()
            .Where(value => value.Id == query.SessionId && value.UserId == query.UserId)
            .Select(value => new
            {
                value.TaskRef,
                value.ReturnUrl,
                value.ExpiresAt
            })
            .SingleOrDefaultAsync(ct);

        if (session is null || !Guid.TryParse(session.TaskRef, out var taskId))
        {
            ModuleIntegrationTelemetry.RecordCurrent(ModuleIntegrationTelemetryOutcomes.NotFound);
            return Result<CurrentModuleSessionResponse>.Fail(ModuleSessionErrors.NotFound);
        }

        ModuleIntegrationTelemetry.RecordCurrent(ModuleIntegrationTelemetryOutcomes.Found);
        return new CurrentModuleSessionResponse(taskId, session.ReturnUrl, session.ExpiresAt);
    }
}

public sealed class GetModuleTasksCatalogHandler(IApplicationDbContext db)
    : IRequestHandler<GetModuleTasksCatalogQuery, Result<IReadOnlyList<ModuleTaskCatalogItemResponse>>>
{
    private const int DescriptionMaxLength = 200;

    public async Task<Result<IReadOnlyList<ModuleTaskCatalogItemResponse>>> Handle(
        GetModuleTasksCatalogQuery query, CancellationToken ct)
    {
        var items = await db.SqlTasks.AsNoTracking()
            .Where(task => task.PublicationStatus == PublicationStatus.Published)
            .OrderBy(task => task.TaskName)
            .ThenBy(task => task.Id)
            .Select(task => new ModuleTaskCatalogItemResponse(
                task.Id.ToString(),
                task.TaskName,
                task.TaskText.Length <= DescriptionMaxLength
                    ? task.TaskText
                    : task.TaskText.Substring(0, DescriptionMaxLength)))
            .ToListAsync(ct);

        return Result<IReadOnlyList<ModuleTaskCatalogItemResponse>>.Success(items);
    }
}

public sealed class UpsertModuleSessionHandler(
    IApplicationDbContext db,
    IPlatformProgressService progressService)
    : IRequestHandler<UpsertModuleSessionCommand, Result>
{
    public async Task<Result> Handle(UpsertModuleSessionCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var sessionId = request.SessionId!.Value;
        var session = await db.ModuleSessions.SingleOrDefaultAsync(value => value.Id == sessionId, ct);

        if (session is null)
        {
            var created = ModuleSession.Create(
                sessionId,
                request.SessionKey!,
                request.UserId!.Value,
                request.TaskRef!,
                request.ReturnUrl!,
                request.ExpiresAt);
            db.ModuleSessions.Add(created);

            var progressResult = await progressService.EnsureCreatedAsync(created, ct);
            if (!progressResult.IsSuccess && !IsValidationVersionPending(progressResult.Error))
            {
                return Result.Fail(progressResult.Error!);
            }

            try
            {
                await db.SaveChangesAsync(ct);
                ModuleIntegrationTelemetry.RecordPush(ModuleIntegrationTelemetryOutcomes.Created);
                return Result.Success();
            }
            catch (DbUpdateException)
            {
                // Параллельный повтор того же push мог вставить строку после SELECT.
                // Если причина другая, повторное чтение не найдёт сессию и исходная ошибка уйдёт выше.
                db.ChangeTracker.Clear();
                session = await db.ModuleSessions.SingleOrDefaultAsync(value => value.Id == sessionId, ct);
                if (session is null)
                {
                    throw;
                }
            }
        }

        if (session.Status != ModuleSessionStatus.Active)
        {
            ModuleIntegrationTelemetry.RecordPush(ModuleIntegrationTelemetryOutcomes.Conflict);
            return Result.Fail(ModuleSessionErrors.Completed);
        }

        session.Synchronize(
            request.SessionKey!,
            request.UserId!.Value,
            request.TaskRef!,
            request.ReturnUrl!,
            request.ExpiresAt);
        var existingProgressResult = await progressService.EnsureCreatedAsync(session, ct);
        if (!existingProgressResult.IsSuccess && !IsValidationVersionPending(existingProgressResult.Error))
        {
            return Result.Fail(existingProgressResult.Error!);
        }

        await db.SaveChangesAsync(ct);
        ModuleIntegrationTelemetry.RecordPush(ModuleIntegrationTelemetryOutcomes.Updated);
        return Result.Success();
    }

    private static bool IsValidationVersionPending(Error? error) =>
        error?.Code == "ModuleSession.TaskValidationUnavailable";
}
