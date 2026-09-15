using System.Net;
using System.Net.Http.Json;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Contracts.DbmsCatalog.Validation;
using SQLModule.Domain.Training;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Training.Validation;

[Collection(IntegrationTestCollection.Name)]
public sealed class ValidationCapabilitiesTests(TestApplication app) : ApiTestBase(app)
{
    [Fact(DisplayName = "Capabilities: PostgreSQL возвращает весь проверенный AST-набор")]
    public async Task PostgreSql_ReturnsProvenCapabilities()
    {
        var dbmsId = await CreateDbmsAsync("postgres");
        AsTeacher();

        var response = await HttpClient.GetAsync(
            ApiRoutes.DbmsCatalog.ValidationCapabilities.ForDbms(dbmsId));
        var capabilities = await response.Content.ReadFromJsonAsync<DbmsValidationCapabilitiesResponse>(
            ClientJson.Options);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        capabilities.ShouldNotBeNull();
        capabilities.DbmsId.ShouldBe(dbmsId);
        capabilities.SupportedCheckKinds.ShouldBe(Enum.GetValues<ValidationCheckKind>());
        capabilities.SupportedConstructs.ShouldBe(Enum.GetValues<SqlConstruct>());
        capabilities.SupportedHintGroups.ShouldBe(Enum.GetValues<HintGroup>());
        capabilities.MaxAttemptsLimit.ShouldBe(100);
        capabilities.AnalyzerVersion.ShouldBe("0.6.5");
    }

    [Fact(DisplayName = "Capabilities: MySQL не объявляет неподдерживаемый FullJoin")]
    public async Task MySql_DoesNotAdvertiseFullJoin()
    {
        var dbmsId = await CreateDbmsAsync("mysql");
        AsAdmin();

        var capabilities = await HttpClient.GetFromJsonAsync<DbmsValidationCapabilitiesResponse>(
            ApiRoutes.DbmsCatalog.ValidationCapabilities.ForDbms(dbmsId),
            ClientJson.Options);

        capabilities.ShouldNotBeNull();
        capabilities.SupportedConstructs.ShouldNotContain(SqlConstruct.FullJoin);
        capabilities.SupportedConstructs.ShouldContain(SqlConstruct.Cte);
        capabilities.SupportedConstructs.ShouldContain(SqlConstruct.WindowFunction);
    }

    [Fact(DisplayName = "Capabilities: неизвестная СУБД даёт 404, неподдерживаемый диалект — 422")]
    public async Task UnknownDbmsAndUnsupportedDialect_ReturnControlledErrors()
    {
        AsTeacher();
        var notFound = await HttpClient.GetAsync(
            ApiRoutes.DbmsCatalog.ValidationCapabilities.ForDbms(Guid.NewGuid()));

        var unsupportedDbmsId = await CreateDbmsAsync("sql-server");
        AsTeacher();
        var unsupported = await HttpClient.GetAsync(
            ApiRoutes.DbmsCatalog.ValidationCapabilities.ForDbms(unsupportedDbmsId));

        notFound.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        unsupported.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact(DisplayName = "Capabilities: доступны Teacher/Admin, Student получает 403, anonymous — 401")]
    public async Task Endpoint_UsesContentAuthorPolicy()
    {
        var path = ApiRoutes.DbmsCatalog.ValidationCapabilities.ForDbms(Guid.NewGuid());

        AsTeacher();
        (await HttpClient.GetAsync(path)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        AsAdmin();
        (await HttpClient.GetAsync(path)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        AsStudent();
        (await HttpClient.GetAsync(path)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        var anonymousClient = App.CreateClient();
        anonymousClient.DefaultRequestHeaders.Add("X-Test-Anonymous", "true");
        (await anonymousClient.GetAsync(path)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private async Task<Guid> CreateDbmsAsync(string systemName)
    {
        AsAdmin();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var response = await DbmsDictionaryClient.CreateAsync(new CreateDbmsDictionaryRequest(
            DbmsName: $"Capabilities_{systemName}_{suffix}",
            DbmsSystemName: systemName,
            DockerImage: $"{systemName}:latest",
            DefaultPort: systemName == "mysql" ? 3306 : 5432,
            EnvUserKey: "DB_USER",
            EnvPasswordKey: "DB_PASSWORD",
            EnvDatabaseKey: "DB_NAME",
            ExtraEnvConfig: null,
            DefaultDatabase: "training",
            DefaultUsername: "user",
            DefaultPassword: "password"));

        return response.Id;
    }
}
