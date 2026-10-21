using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.IntegrationTests.Infrastructure;

namespace SQLModule.IntegrationTests.ModuleIntegration;

/// <summary>Конкурентный claim сообщений outbox с арендой (REF-008).</summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class OutboxClaimTests(TestApplication app)
{
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(2);

    [Fact(DisplayName = "Два экземпляра одновременно забирают непересекающиеся наборы, вместе покрывающие все сообщения")]
    public async Task ConcurrentClaims_ReturnDisjointSetsCoveringAllMessages()
    {
        var ids = await SeedAsync(20);
        var now = DateTimeOffset.UtcNow;

        var claims = await Task.WhenAll(
            ClaimAsync("instance-a", now, 20),
            ClaimAsync("instance-b", now, 20),
            ClaimAsync("instance-c", now, 20));

        var mine = claims.Select(claim => claim.Where(ids.Contains).ToHashSet()).ToArray();
        mine.Sum(set => set.Count).ShouldBe(ids.Count, "каждое сообщение должно быть забрано ровно одним экземпляром");
        mine.SelectMany(set => set).Order().ShouldBe(ids.Order());
    }

    [Fact(DisplayName = "Пока аренда действует, сообщение недоступно другому экземпляру, после истечения — доступно")]
    public async Task LeasedMessages_ReturnAfterLeaseExpiry()
    {
        var ids = await SeedAsync(5);
        var now = DateTimeOffset.UtcNow;

        var first = (await ClaimAsync("instance-a", now, 50)).Where(ids.Contains).ToList();
        var duringLease = (await ClaimAsync("instance-b", now.AddSeconds(30), 50)).Where(ids.Contains).ToList();
        var afterExpiry = (await ClaimAsync("instance-b", now.Add(Lease).AddSeconds(1), 50)).Where(ids.Contains).ToList();

        first.Count.ShouldBe(5);
        duringLease.ShouldBeEmpty();
        afterExpiry.Count.ShouldBe(5);
    }

    [Fact(DisplayName = "Claim фиксирует владельца и срок аренды и не берёт отправленные и будущие сообщения")]
    public async Task Claim_RecordsOwnerAndSkipsIneligibleMessages()
    {
        var ids = await SeedAsync(1);
        var now = DateTimeOffset.UtcNow;
        var futureId = Guid.NewGuid();
        var sentId = Guid.NewGuid();
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var future = PendingPublish.Create(
                futureId, PendingPublishKind.Event, Guid.NewGuid(), $"event:{futureId}", "{}", now.AddHours(1));
            var sent = PendingPublish.Create(
                sentId, PendingPublishKind.Event, Guid.NewGuid(), $"event:{sentId}", "{}", now.AddMinutes(-5));
            sent.MarkSent(now.AddMinutes(-1));
            db.PendingPublishes.AddRange(future, sent);
            await db.SaveChangesAsync();
        }

        var claimed = await ClaimAsync("instance-a", now, 50);

        claimed.ShouldContain(ids[0]);
        claimed.ShouldNotContain(futureId);
        claimed.ShouldNotContain(sentId);
        using var verify = app.Services.CreateScope();
        var reader = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await reader.PendingPublishes.AsNoTracking().SingleAsync(value => value.Id == ids[0]);
        row.LockedBy.ShouldBe("instance-a");
        row.LockedUntil.ShouldNotBeNull();
        row.LockedUntil!.Value.ShouldBe(now.Add(Lease), TimeSpan.FromSeconds(1));
    }

    [Fact(DisplayName = "Неудачная отправка снимает аренду, и повтор не ждёт её истечения")]
    public void RegisterFailure_ReleasesLease()
    {
        var now = DateTimeOffset.UtcNow;
        var message = PendingPublish.Create(
            Guid.NewGuid(), PendingPublishKind.Event, Guid.NewGuid(), "event:x", "{}", now);

        message.RegisterFailure(now, now.AddSeconds(5), moveToDeadLetter: false);

        message.LockedBy.ShouldBeNull();
        message.LockedUntil.ShouldBeNull();
        message.Attempts.ShouldBe(1);
    }

    private async Task<List<Guid>> SeedAsync(int count)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // Таблица общая для всей коллекции тестов — убираем чужие необработанные сообщения,
        // чтобы они не занимали место в пачке claim.
        await db.PendingPublishes
            .Where(value => value.SentAt == null && value.DeadLetterAt == null)
            .ExecuteDeleteAsync();
        var created = DateTimeOffset.UtcNow.AddMinutes(-10);
        var ids = new List<Guid>();
        for (var index = 0; index < count; index++)
        {
            var id = Guid.NewGuid();
            ids.Add(id);
            db.PendingPublishes.Add(PendingPublish.Create(
                id, PendingPublishKind.Event, Guid.NewGuid(), $"event:{id}", "{}", created.AddSeconds(index)));
        }

        await db.SaveChangesAsync();
        return ids;
    }

    private async Task<IReadOnlyList<Guid>> ClaimAsync(string owner, DateTimeOffset now, int batchSize)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await new PostgresPendingPublishClaimer(db).ClaimAsync(owner, now, Lease, batchSize, CancellationToken.None);
    }
}
