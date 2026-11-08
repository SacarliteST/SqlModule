using System.Net;
using System.Net.Http.Json;
using EducationSPD.Kafka.Contracts;
using EducationSPD.Kafka.Contracts.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Skill;
using SQLModule.Data.Core;
using SQLModule.Domain.ModuleIntegration;
using SQLModule.IntegrationTests.Infrastructure;

namespace SQLModule.IntegrationTests.Training.Skill;

[Collection(IntegrationTestCollection.Name)]
public sealed class SkillCatalogTests : ApiTestBase
{
    public SkillCatalogTests(TestApplication testApplication) : base(testApplication) { }

    [Fact(DisplayName = "Каталог навыков поддерживает полный soft-delete сценарий и атомарный outbox")]
    public async Task Catalog_FullLifecycle_PersistsSkillAndOutboxEvents()
    {
        var skillId = $"join-{Guid.NewGuid():N}";
        using var createResponse = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Training.Skills.Collection,
            new CreateSkillRequest(skillId, "Соединение таблиц", "Исходное описание"),
            ClientJson.Options);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<SkillResponse>(ClientJson.Options);
        created.ShouldNotBeNull();
        created.SkillId.ShouldBe(skillId);
        created.Origin.ShouldBe(SkillOriginResponse.TeacherDefined);
        created.IsDeleted.ShouldBeFalse();
        created.SemanticChangeWarning.ShouldNotBeNullOrWhiteSpace();

        using var updateResponse = await HttpClient.PutAsJsonAsync(
            ApiRoutes.Training.Skills.ForId(created.Id),
            new UpdateSkillRequest("JOIN", "Новое описание"),
            ClientJson.Options);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<SkillResponse>(ClientJson.Options);
        updated!.Id.ShouldBe(created.Id);
        updated.SkillId.ShouldBe(skillId);

        using var deleteResponse = await HttpClient.DeleteAsync(ApiRoutes.Training.Skills.ForId(created.Id));
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var hiddenPageResponse = await HttpClient.GetAsync(ApiRoutes.Training.Skills.Collection);
        var hiddenPage = await hiddenPageResponse.Content.ReadFromJsonAsync<PageResponse<SkillResponse>>(
            ClientJson.Options);
        hiddenPage!.Items.ShouldNotContain(skill => skill.Id == created.Id);

        using var deletedPageResponse = await HttpClient.GetAsync(
            $"{ApiRoutes.Training.Skills.Collection}?includeDeleted=true&search={skillId}");
        var deletedPage = await deletedPageResponse.Content.ReadFromJsonAsync<PageResponse<SkillResponse>>(
            ClientJson.Options);
        deletedPage!.Items.Single(skill => skill.Id == created.Id).IsDeleted.ShouldBeTrue();

        using var restoreResponse = await HttpClient.PostAsync(
            ApiRoutes.Training.Skills.ForRestore(created.Id),
            null);
        restoreResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var events = await db.PendingPublishes.AsNoTracking()
            .Where(message =>
                message.Kind == PendingPublishKind.SkillCatalog &&
                message.SessionId == created.Id)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync();
        events.Count.ShouldBe(4);
        events.ShouldAllBe(message => message.Topic == "learning.skill-catalog");
        events.ShouldAllBe(message => message.MessageKey == $"sql:{skillId}");
        events.Select(message => message.Id).Distinct().Count().ShouldBe(4);
        var payload = KafkaContractJson.Deserialize<SkillCatalogChanged>(events.Last().MessageJson);
        payload.EventId.ShouldBe(events.Last().Id);
        payload.IsDeleted.ShouldBeFalse();
    }

    [Fact(DisplayName = "Student не имеет доступа к каталогу навыков")]
    public async Task Catalog_Student_ReturnsForbidden()
    {
        AsStudent();

        using var response = await HttpClient.GetAsync(ApiRoutes.Training.Skills.Collection);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "SkillId валидируется и остаётся уникальным")]
    public async Task Create_InvalidOrDuplicateSkillId_ReturnsExpectedErrors()
    {
        using var invalidResponse = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Training.Skills.Collection,
            new CreateSkillRequest("Bad Skill", "Некорректный"),
            ClientJson.Options);
        invalidResponse.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        var skillId = $"unique-{Guid.NewGuid():N}";
        using var firstResponse = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Training.Skills.Collection,
            new CreateSkillRequest(skillId, "Первый"),
            ClientJson.Options);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        using var duplicateResponse = await HttpClient.PostAsJsonAsync(
            ApiRoutes.Training.Skills.Collection,
            new CreateSkillRequest(skillId, "Второй"),
            ClientJson.Options);
        duplicateResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }
}
