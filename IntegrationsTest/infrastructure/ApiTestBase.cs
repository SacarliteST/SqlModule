using TemplateApi.Client.Template;

namespace TemplateApi.IntegrationsTest.infrastructure;

/// <summary>
/// Базовый класс тестирования контроллеров
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public abstract class ApiTestBase
{
    protected readonly ITemplateClient TemplateClient;
    protected readonly HttpClient HttpClient;

    protected ApiTestBase(TestApplication testApplication)
    {
        TemplateClient = testApplication.TemplateClient;
        HttpClient = testApplication.CreateClient();
    }
}
