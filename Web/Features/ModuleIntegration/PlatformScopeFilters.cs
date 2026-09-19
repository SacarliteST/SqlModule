using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SQLModule.Common.Results;
using SQLModule.Domain.Common;

namespace SQLModule.Web.Features.ModuleIntegration;

internal static class PlatformScopeGuard
{
    /// <summary>
    /// Токен с session_id при выключенной интеграции нельзя ни обслужить как платформенный
    /// (нет ModuleSession), ни молча принять за standalone — отказываем.
    /// </summary>
    internal static IResult? FailClosed(
        ICurrentUser currentUser,
        IOptions<ModuleIntegrationOptions> options) =>
        currentUser.ModuleSessionId.HasValue && !options.Value.Enabled
            ? PlatformSessionErrors.ScopeRestricted.ToProblem()
            : null;
}

/// <summary>Операции только для standalone-режима: платформенный токен получает 403.</summary>
internal abstract class StandaloneOnlyFilter(
    ICurrentUser currentUser,
    IOptions<ModuleIntegrationOptions> options,
    Error error) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var failClosed = PlatformScopeGuard.FailClosed(currentUser, options);
        if (failClosed is not null)
        {
            return failClosed;
        }

        return currentUser.ModuleSessionId.HasValue ? error.ToProblem() : await next(context);
    }
}

/// <summary>Каталоги и история: платформенная сессия видит только своё задание.</summary>
internal sealed class StandaloneOnlyReadFilter(
    ICurrentUser currentUser,
    IOptions<ModuleIntegrationOptions> options)
    : StandaloneOnlyFilter(currentUser, options, PlatformSessionErrors.ScopeRestricted);

/// <summary>Standalone-мутации прохождения: недоступны из платформенной сессии.</summary>
internal sealed class StandaloneOnlyMutationFilter(
    ICurrentUser currentUser,
    IOptions<ModuleIntegrationOptions> options)
    : StandaloneOnlyFilter(currentUser, options, PlatformSessionErrors.StandaloneOperationForbidden);

/// <summary>Маршруты с {taskId}: платформенный токен допускается только к заданию своей сессии.</summary>
internal sealed class PlatformTaskScopeFilter(
    ICurrentUser currentUser,
    IOptions<ModuleIntegrationOptions> options,
    IPlatformStudentScope scope) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var failClosed = PlatformScopeGuard.FailClosed(currentUser, options);
        if (failClosed is not null)
        {
            return failClosed;
        }

        if (currentUser is not { UserId: { } userId, ModuleSessionId: { } sessionId })
        {
            return await next(context);
        }

        if (!Guid.TryParse(context.HttpContext.Request.RouteValues["taskId"]?.ToString(), out var taskId))
        {
            return await next(context);
        }

        var allowed = await scope.EnsureTaskAllowedAsync(
            userId, sessionId, taskId, PlatformScopeMode.Read, context.HttpContext.RequestAborted);
        return allowed.IsSuccess ? await next(context) : allowed.Error!.ToProblem();
    }
}
