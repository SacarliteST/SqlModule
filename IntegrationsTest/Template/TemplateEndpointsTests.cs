using System.Net;
using System.Net.Http.Json;
using Contracts.Template;
using Shouldly;
using TemplateApi.IntegrationsTest.infrastructure;
using TemplateApi.TestsCommon;

namespace TemplateApi.IntegrationsTest.Template;

/// <summary>
/// Интеграционные тесты эндпоинтов шаблонов
/// </summary>
public sealed class TemplateEndpointsTests : ApiTestBase
{
    public TemplateEndpointsTests(TestApplication testApplication) : base(testApplication) { }

    [Fact(DisplayName = "GET по Id — возвращает шаблон")]
    public async Task GetTemplate_ByValidId_ShouldReturnTemplate()
    {
        var createRequest = DataGenerator.GenerateValidCreateOrUpdateTemplateRequest(DataGenerator.GenerateTemplate());
        var created = await TemplateClient.CreateAsync(createRequest);

        var result = await TemplateClient.GetByIdAsync(created.Id.ShouldNotBeNull(), default);

        result.ShouldNotBeNull();
    }

    [Fact(DisplayName = "POST — создаёт шаблон и возвращает его")]
    public async Task CreateTemplate_ShouldReturnCreatedTemplate()
    {
        var createRequest = DataGenerator.GenerateValidCreateOrUpdateTemplateRequest(DataGenerator.GenerateTemplate());

        var created = await TemplateClient.CreateAsync(createRequest);

        var result = await TemplateClient.GetByIdAsync(created.Id.ShouldNotBeNull(), default);
        result.ShouldNotBeNull();
    }

    [Fact(DisplayName = "PUT — обновляет шаблон")]
    public async Task UpdateTemplate_ShouldReturnUpdatedTemplate()
    {
        var createRequest = DataGenerator.GenerateValidCreateOrUpdateTemplateRequest(DataGenerator.GenerateTemplate());
        var created = await TemplateClient.CreateAsync(createRequest);
        var updateRequest = DataGenerator.GenerateValidCreateOrUpdateTemplateRequest(DataGenerator.GenerateTemplate());

        await TemplateClient.UpdateAsync(created.Id.ShouldNotBeNull(), updateRequest);

        var result = await TemplateClient.GetByIdAsync(created.Id.ShouldNotBeNull(), default);
        result.ShouldNotBeNull();
    }

    [Fact(DisplayName = "DELETE — удаляет шаблон без ошибок")]
    public async Task DeleteTemplate_ShouldNotThrow()
    {
        var createRequest = DataGenerator.GenerateValidCreateOrUpdateTemplateRequest(DataGenerator.GenerateTemplate());
        var created = await TemplateClient.CreateAsync(createRequest);

        var act = async () => await TemplateClient.DeleteAsync(created.Id.ShouldNotBeNull());

        await act.ShouldNotThrowAsync();
    }

    [Fact(DisplayName = "POST с пустым TemplateName → 400")]
    public async Task CreateTemplate_EmptyName_Returns400()
    {
        var response = await HttpClient.PostAsJsonAsync("api/v1/templates", new CreateOrUpdateTemplateRequest { TemplateName = "" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GET по несуществующему Id → 404")]
    public async Task GetTemplate_NonExistentId_Returns404()
    {
        var response = await HttpClient.GetAsync($"api/v1/templates/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "PUT по несуществующему Id → 404")]
    public async Task UpdateTemplate_NonExistentId_Returns404()
    {
        var response = await HttpClient.PutAsJsonAsync(
            $"api/v1/templates/{Guid.NewGuid()}",
            new CreateOrUpdateTemplateRequest { TemplateName = "test" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "DELETE по несуществующему Id → 404")]
    public async Task DeleteTemplate_NonExistentId_Returns404()
    {
        var response = await HttpClient.DeleteAsync($"api/v1/templates/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
