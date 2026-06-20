using SQLModule.Client.Attempt;
using SQLModule.Client.AttributeParameterValue;
using SQLModule.Client.DataRecord;
using SQLModule.Client.MetaAttribute;
using SQLModule.Client.MetaRelationship;
using SQLModule.Client.MetaTable;
using SQLModule.Client.ParameterDefinition;
using SQLModule.Client.PhysicalType;
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

    /// <summary>Типизированный клиент для работы с мета-атрибутами (колонками).</summary>
    protected readonly IMetaAttributeClient MetaAttributeClient;

    /// <summary>Типизированный клиент для работы со строками данных (EAV-якоря).</summary>
    protected readonly IDataRecordClient DataRecordClient;

    /// <summary>Типизированный клиент для работы со значениями параметров атрибутов.</summary>
    protected readonly IAttributeParameterValueClient AttributeParameterValueClient;

    /// <summary>Типизированный клиент для работы с физическими типами данных.</summary>
    protected readonly IPhysicalTypeClient PhysicalTypeClient;

    /// <summary>Типизированный клиент для работы с определениями параметров физических типов.</summary>
    protected readonly IParameterDefinitionClient ParameterDefinitionClient;

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
        MetaAttributeClient = testApplication.MetaAttributeClient;
        DataRecordClient = testApplication.DataRecordClient;
        AttributeParameterValueClient = testApplication.AttributeParameterValueClient;
        PhysicalTypeClient = testApplication.PhysicalTypeClient;
        ParameterDefinitionClient = testApplication.ParameterDefinitionClient;
    }
}
