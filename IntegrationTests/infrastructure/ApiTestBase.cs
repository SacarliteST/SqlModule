using SQLModule.Client.Attempt;
using SQLModule.Client.AttributeParameterValue;
using SQLModule.Client.DataRecord;
using SQLModule.Client.DbmsDictionary;
using SQLModule.Client.MetaAttribute;
using SQLModule.Client.MetaRelationship;
using SQLModule.Client.MetaTable;
using SQLModule.Client.ParameterDefinition;
using SQLModule.Client.PhysicalType;
using SQLModule.Client.SchemaBuilder;
using SQLModule.Client.SqlQuery;
using SQLModule.Client.SqlTask;
using SQLModule.Client.TargetDb;
using SQLModule.Client.Topic;
using SQLModule.Web.Common.Auth;

namespace SQLModule.IntegrationTests.infrastructure;

/// <summary>Базовый класс интеграционных тестов.</summary>
[Collection(IntegrationTestCollection.Name)]
public abstract class ApiTestBase
{
    protected readonly TestApplication App;

    /// <summary>HTTP-клиент без типизации — для вызовов эндпоинтов без клиентского SDK.</summary>
    protected readonly HttpClient HttpClient;

    /// <summary>Типизированный клиент для работы со справочником СУБД.</summary>
    protected readonly IDbmsDictionaryClient DbmsDictionaryClient;

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

    /// <summary>Типизированный клиент для построителя схемы.</summary>
    protected readonly ISchemaBuilderClient SchemaBuilderClient;

    protected ApiTestBase(TestApplication testApplication)
    {
        App = testApplication;
        HttpClient = testApplication.CreateClient();
        AsTeacher();
        DbmsDictionaryClient = testApplication.DbmsDictionaryClient;
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
        SchemaBuilderClient = testApplication.SchemaBuilderClient;
    }

    /// <summary>Переключает контекст на пользователя с ролью Teacher (ContentAuthor).</summary>
    protected void AsTeacher(Guid? userId = null) => SetUser(userId, Roles.Teacher);

    /// <summary>Переключает контекст на пользователя с ролью Student.</summary>
    protected void AsStudent(Guid? userId = null) => SetUser(userId, Roles.Student);

    /// <summary>Переключает контекст на пользователя с ролью Admin.</summary>
    protected void AsAdmin(Guid? userId = null) => SetUser(userId, Roles.Admin);

    private void SetUser(Guid? userId, string role)
    {
        var id = (userId ?? Guid.NewGuid()).ToString();
        App.UserContext.UserId = id;
        App.UserContext.Roles = role;
        App.UserContext.DisplayName = $"Test {role}";
        HttpClient.DefaultRequestHeaders.Remove("X-Test-UserId");
        HttpClient.DefaultRequestHeaders.Remove("X-Test-Roles");
        HttpClient.DefaultRequestHeaders.Remove("X-Test-DisplayName");
        HttpClient.DefaultRequestHeaders.Add("X-Test-UserId", id);
        HttpClient.DefaultRequestHeaders.Add("X-Test-Roles", role);
        HttpClient.DefaultRequestHeaders.Add("X-Test-DisplayName", App.UserContext.DisplayName);
    }
}
