using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;

namespace SQLModule.Web.Features.ModuleIntegration;

/// <summary>Что должна допускать платформенная сессия для конкретной операции.</summary>
internal enum PlatformScopeMode
{
    /// <summary>Чтение состояния: закрытая и истёкшая сессия не отказывает — студент видит итог.</summary>
    Read,

    /// <summary>Отправка решения: сессия обязана допускать продолжение прохождения.</summary>
    Write
}

internal sealed record PlatformStudentContext(
    Guid SessionId,
    Guid UserId,
    Guid TaskId,
    DateTimeOffset? ExpiresAt,
    ModuleSessionStatus Status);

internal static class PlatformSessionErrors
{
    internal static Error SessionNotFound => new(
        "ModuleSession.NotFound",
        "Платформенная сессия не найдена или недоступна.",
        ErrorType.NotFound);

    internal static Error TaskNotFound => new(
        "PlatformSession.TaskNotFound",
        "Задание не найдено или недоступно в текущей платформенной сессии.",
        ErrorType.NotFound);

    internal static Error SessionExpired => Error.Conflict(
        "ModuleSession.Expired",
        "Срок платформенной сессии истёк.");

    internal static Error SessionClosed => Error.Conflict(
        "ModuleSession.Closed",
        "Платформенная сессия уже завершена.");

    internal static Error ScopeRestricted => Error.Forbidden(
        "PlatformSession.ScopeRestricted",
        "Платформенная сессия предоставляет доступ только к назначенному заданию.");

    internal static Error StandaloneOperationForbidden => Error.Forbidden(
        "PlatformSession.StandaloneOperationForbidden",
        "Операция недоступна в платформенной сессии.");
}

/// <summary>
/// Единая проверка контекста платформенного студента: сессия из доверенного JWT
/// определяет единственное разрешённое задание.
/// </summary>
internal interface IPlatformStudentScope
{
    Task<Result<PlatformStudentContext>> ResolveAsync(
        Guid userId,
        Guid sessionId,
        PlatformScopeMode mode,
        CancellationToken cancellationToken);

    Task<Result<PlatformStudentContext>> EnsureTaskAllowedAsync(
        Guid userId,
        Guid sessionId,
        Guid taskId,
        PlatformScopeMode mode,
        CancellationToken cancellationToken);
}

internal sealed class PlatformStudentScope(AppDbContext db, TimeProvider timeProvider)
    : IPlatformStudentScope
{
    private sealed record SessionSnapshot(
        Guid Id,
        Guid UserId,
        string TaskRef,
        DateTimeOffset? ExpiresAt,
        ModuleSessionStatus Status);

    private SessionSnapshot? cached;

    public async Task<Result<PlatformStudentContext>> ResolveAsync(
        Guid userId,
        Guid sessionId,
        PlatformScopeMode mode,
        CancellationToken cancellationToken)
    {
        var context = await LoadContextAsync(userId, sessionId, cancellationToken);
        if (!context.IsSuccess)
        {
            return context;
        }

        var state = CheckState(context.Value!, mode);
        return state.IsSuccess ? context : Result<PlatformStudentContext>.Fail(state.Error!);
    }

    public async Task<Result<PlatformStudentContext>> EnsureTaskAllowedAsync(
        Guid userId,
        Guid sessionId,
        Guid taskId,
        PlatformScopeMode mode,
        CancellationToken cancellationToken)
    {
        var context = await LoadContextAsync(userId, sessionId, cancellationToken);
        if (!context.IsSuccess)
        {
            return context;
        }

        // Чужое задание отдаём как 404 раньше проверки состояния — не подтверждаем его существование.
        if (context.Value!.TaskId != taskId)
        {
            return Result<PlatformStudentContext>.Fail(PlatformSessionErrors.TaskNotFound);
        }

        var state = CheckState(context.Value, mode);
        return state.IsSuccess ? context : Result<PlatformStudentContext>.Fail(state.Error!);
    }

    private async Task<Result<PlatformStudentContext>> LoadContextAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (cached is null || cached.Id != sessionId)
        {
            cached = await db.ModuleSessions.AsNoTracking()
                .Where(value => value.Id == sessionId)
                .Select(value => new SessionSnapshot(
                    value.Id, value.UserId, value.TaskRef, value.ExpiresAt, value.Status))
                .SingleOrDefaultAsync(cancellationToken);
        }

        // Чужая сессия неотличима от отсутствующей.
        if (cached is null || cached.UserId != userId || !Guid.TryParse(cached.TaskRef, out var taskId))
        {
            return Result<PlatformStudentContext>.Fail(PlatformSessionErrors.SessionNotFound);
        }

        return new PlatformStudentContext(cached.Id, cached.UserId, taskId, cached.ExpiresAt, cached.Status);
    }

    private Result CheckState(PlatformStudentContext context, PlatformScopeMode mode)
    {
        if (mode == PlatformScopeMode.Read)
        {
            return Result.Success();
        }

        if (context.Status == ModuleSessionStatus.Expired)
        {
            return Result.Fail(PlatformSessionErrors.SessionExpired);
        }

        if (context.Status != ModuleSessionStatus.Active)
        {
            return Result.Fail(PlatformSessionErrors.SessionClosed);
        }

        return context.ExpiresAt.HasValue && context.ExpiresAt.Value < timeProvider.GetUtcNow()
            ? Result.Fail(PlatformSessionErrors.SessionExpired)
            : Result.Success();
    }
}
