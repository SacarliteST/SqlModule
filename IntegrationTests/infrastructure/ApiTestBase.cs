using SQLModule.Client.Attempt;
using SQLModule.Client.MetaRelationship;
using SQLModule.Client.MetaTable;
using SQLModule.Client.SqlQuery;
using SQLModule.Client.SqlTask;
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

    /// <summary>Типизированный клиент для работы с SQL-заданиями тренажёра.</summary>
    protected readonly ISqlTaskClient SqlTaskClient;

    /// <summary>Типизированный клиент для работы с эталонными SQL-запросами.</summary>
    protected readonly ISqlQueryClient SqlQueryClient;

    /// <summary>Типизированный клиент для работы с попытками выполнения заданий.</summary>
    protected readonly IAttemptClient AttemptClient;

    /// <summary>Типизированный клиент для работы с мета-таблицами.</summary>
    protected readonly IMetaTableClient MetaTableClient;

    /// <summary>Типизированный клиент для работы с FK-связями между мета-атрибутами.</summary>
    protected readonly IMetaRelationshipClient MetaRelationshipClient;

    protected ApiTestBase(TestApplication testApplication)
    {
        HttpClient = testApplication.CreateClient();
        TargetDbClient = testApplication.TargetDbClient;
        TopicClient = testApplication.TopicClient;
        SqlTaskClient = testApplication.SqlTaskClient;
        SqlQueryClient = testApplication.SqlQueryClient;
        AttemptClient = testApplication.AttemptClient;
        MetaTableClient = testApplication.MetaTableClient;
        MetaRelationshipClient = testApplication.MetaRelationshipClient;
    }
}
