using SQLModule.Client.TargetDb;
using SQLModule.Client.Topic;

namespace SQLModule.IntegrationTests.infrastructure;

/// <summary>Базовый класс интеграционных тестов.</summary>
[Collection(IntegrationTestCollection.Name)]
public abstract class ApiTestBase
{
    /// <summary>HTTP-клиент без типизации — для вызовов эндпоинтов без клиентского SDK.</summary>
    protected readonly HttpClient HttpClient;

    /// <summary>Типизированный клиент модуля — основной способ взаимодействия с API в тестах.</summary>
    protected readonly ITargetDbClient TargetDbClient;

    /// <summary>Типизированный клиент для работы с темами тренажёра.</summary>
    protected readonly ITopicClient TopicClient;

    protected ApiTestBase(TestApplication testApplication)
    {
        HttpClient = testApplication.CreateClient();
        TargetDbClient = testApplication.TargetDbClient;
        TopicClient = testApplication.TopicClient;
    }
}
