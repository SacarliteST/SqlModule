using SQLModule.Client.TargetDb;

namespace SQLModule.IntegrationTests.infrastructure;

/// <summary>Базовый класс интеграционных тестов.</summary>
[Collection(IntegrationTestCollection.Name)]
public abstract class ApiTestBase
{
    protected readonly HttpClient HttpClient;
    protected readonly ITargetDbClient TargetDbClient;

    protected ApiTestBase(TestApplication testApplication)
    {
        HttpClient = testApplication.CreateClient();
        TargetDbClient = testApplication.TargetDbClient;
    }
}
