using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.IntegrationTests.infrastructure;
using SQLModule.Web.Features.ModuleIntegration;

namespace SQLModule.IntegrationTests.ModuleIntegration;

/// <summary>
/// Сервис проверки платформенного контекста. Работает на реальной БД, а не InMemory:
/// запрос к ModuleSessions — часть проверяемого поведения.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class PlatformStudentScopeServiceTests(TestApplication app)
{
    private async Task<(Guid SessionId, Guid UserId, Guid TaskId)> SeedAsync(
        DateTimeOffset? expiresAt = null,
        string? taskRef = null,
        Action<ModuleSession>? mutate = null)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var session = ModuleSession.Create(
            sessionId, $"scope-key-{sessionId:N}", userId, taskRef ?? taskId.ToString(),
            "https://platform.example/return", expiresAt);
        mutate?.Invoke(session);
        db.ModuleSessions.Add(session);
        await db.SaveChangesAsync();
        return (sessionId, userId, taskId);
    }

    private static PlatformStudentScope CreateService(IServiceScope scope) =>
        new(scope.ServiceProvider.GetRequiredService<AppDbContext>(), TimeProvider.System);

    [Fact(DisplayName = "Scope: активная сессия владельца даёт контекст с заданием из TaskRef")]
    public async Task Resolve_ActiveOwnedSession_ReturnsContext()
    {
        var (sessionId, userId, taskId) = await SeedAsync(DateTimeOffset.UtcNow.AddMinutes(10));
        using var scope = app.Services.CreateScope();

        var result = await CreateService(scope).ResolveAsync(
            userId, sessionId, PlatformScopeMode.Write, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.TaskId.ShouldBe(taskId);
        result.Value.SessionId.ShouldBe(sessionId);
        result.Value.Status.ShouldBe(ModuleSessionStatus.Active);
    }

    [Fact(DisplayName = "Scope: сессия без срока действия допустима, ExpiresAt остаётся null")]
    public async Task Resolve_SessionWithoutExpiry_IsAllowed()
    {
        var (sessionId, userId, _) = await SeedAsync(expiresAt: null);
        using var scope = app.Services.CreateScope();

        var result = await CreateService(scope).ResolveAsync(
            userId, sessionId, PlatformScopeMode.Write, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.ExpiresAt.ShouldBeNull();
    }

    [Fact(DisplayName = "Scope: отсутствующая и чужая сессия неразличимы — обе ModuleSession.NotFound")]
    public async Task Resolve_MissingOrForeignSession_IsIndistinguishable()
    {
        var (sessionId, _, _) = await SeedAsync();
        using var scope = app.Services.CreateScope();
        var service = CreateService(scope);

        var missing = await service.ResolveAsync(
            Guid.NewGuid(), Guid.NewGuid(), PlatformScopeMode.Read, CancellationToken.None);
        var foreign = await service.ResolveAsync(
            Guid.NewGuid(), sessionId, PlatformScopeMode.Read, CancellationToken.None);

        missing.Error!.Code.ShouldBe("ModuleSession.NotFound");
        foreign.Error!.Code.ShouldBe(missing.Error.Code);
        foreign.Error.Message.ShouldBe(missing.Error.Message);
    }

    [Fact(DisplayName = "Scope: некорректный TaskRef скрывается как отсутствующая сессия")]
    public async Task Resolve_InvalidTaskRef_IsNotFound()
    {
        var (sessionId, userId, _) = await SeedAsync(taskRef: "not-a-guid");
        using var scope = app.Services.CreateScope();

        var result = await CreateService(scope).ResolveAsync(
            userId, sessionId, PlatformScopeMode.Read, CancellationToken.None);

        result.Error!.Code.ShouldBe("ModuleSession.NotFound");
    }

    [Fact(DisplayName = "Scope: другое задание — PlatformSession.TaskNotFound, и раньше проверки состояния")]
    public async Task EnsureTaskAllowed_OtherTask_IsNotFoundBeforeStateCheck()
    {
        var (sessionId, userId, taskId) = await SeedAsync(
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));
        using var scope = app.Services.CreateScope();
        var service = CreateService(scope);

        var other = await service.EnsureTaskAllowedAsync(
            userId, sessionId, Guid.NewGuid(), PlatformScopeMode.Write, CancellationToken.None);
        var own = await service.EnsureTaskAllowedAsync(
            userId, sessionId, taskId, PlatformScopeMode.Write, CancellationToken.None);

        other.Error!.Code.ShouldBe("PlatformSession.TaskNotFound");
        own.Error!.Code.ShouldBe("ModuleSession.Expired");
    }

    [Fact(DisplayName = "Scope: истёкшая сессия — Expired для Write, но читается в режиме Read")]
    public async Task Resolve_ExpiredSession_ReadAllowedWriteRejected()
    {
        var (sessionId, userId, _) = await SeedAsync(DateTimeOffset.UtcNow.AddMinutes(-5));
        using var scope = app.Services.CreateScope();
        var service = CreateService(scope);

        var write = await service.ResolveAsync(
            userId, sessionId, PlatformScopeMode.Write, CancellationToken.None);
        var read = await service.ResolveAsync(
            userId, sessionId, PlatformScopeMode.Read, CancellationToken.None);

        write.Error!.Code.ShouldBe("ModuleSession.Expired");
        read.IsSuccess.ShouldBeTrue();
    }

    [Fact(DisplayName = "Scope: сессия в статусе Expired — Expired для Write")]
    public async Task Resolve_ExpiredStatus_RejectedForWrite()
    {
        var (sessionId, userId, _) = await SeedAsync(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            mutate: session => session.MarkExpired(DateTimeOffset.UtcNow));
        using var scope = app.Services.CreateScope();

        var result = await CreateService(scope).ResolveAsync(
            userId, sessionId, PlatformScopeMode.Write, CancellationToken.None);

        result.Error!.Code.ShouldBe("ModuleSession.Expired");
    }

    [Theory(DisplayName = "Scope: завершённая сессия — Closed для Write, читается в режиме Read")]
    [InlineData("pending")]
    [InlineData("completed")]
    [InlineData("failed")]
    public async Task Resolve_ClosedSession_ReadAllowedWriteRejected(string state)
    {
        var (sessionId, userId, _) = await SeedAsync(mutate: session =>
        {
            session.MarkCompletionPending();
            if (state == "completed")
            {
                session.MarkCompleted();
            }
            else if (state == "failed")
            {
                session.MarkCompletionFailed();
            }
        });
        using var scope = app.Services.CreateScope();
        var service = CreateService(scope);

        var write = await service.ResolveAsync(
            userId, sessionId, PlatformScopeMode.Write, CancellationToken.None);
        var read = await service.ResolveAsync(
            userId, sessionId, PlatformScopeMode.Read, CancellationToken.None);

        write.Error!.Code.ShouldBe("ModuleSession.Closed");
        read.IsSuccess.ShouldBeTrue();
    }

    [Fact(DisplayName = "Scope: сессия читается из БД один раз на экземпляр сервиса")]
    public async Task Resolve_IsMemoizedWithinOneServiceInstance()
    {
        var (sessionId, userId, _) = await SeedAsync();
        using var scope = app.Services.CreateScope();
        var service = CreateService(scope);

        (await service.ResolveAsync(userId, sessionId, PlatformScopeMode.Write, CancellationToken.None))
            .IsSuccess.ShouldBeTrue();
        using (var other = app.Services.CreateScope())
        {
            var db = other.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.ModuleSessions.Where(value => value.Id == sessionId)
                .ExecuteDeleteAsync();
        }

        (await service.ResolveAsync(userId, sessionId, PlatformScopeMode.Write, CancellationToken.None))
            .IsSuccess.ShouldBeTrue();
        using var fresh = app.Services.CreateScope();
        (await CreateService(fresh).ResolveAsync(
                userId, sessionId, PlatformScopeMode.Write, CancellationToken.None))
            .Error!.Code.ShouldBe("ModuleSession.NotFound");
    }
}
