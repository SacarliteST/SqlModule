using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Schema.TargetDb;

[Collection(IntegrationTestCollection.Name)]
public sealed class TargetDbTests : ApiTestBase
{
    public TargetDbTests(TestApplication testApplication) : base(testApplication) { }

    // ─── helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid> CreateDbmsDictionaryAsync()
    {
        var response = await HttpClient.PostAsJsonAsync("api/v1/dbms-dictionaries", new
        {
            DbmsName = "Scanner_" + Guid.NewGuid(),
            DbmsSystemName = "scanner",
            DockerImage = "postgres:latest",
            DefaultPort = 5432,
            EnvUserKey = "POSTGRES_USER",
            EnvPasswordKey = "POSTGRES_PASSWORD",
            EnvDatabaseKey = "POSTGRES_DB",
            ExtraEnvConfig = (string?)null,
            DefaultDatabase = "testdb",
            DefaultUsername = "user",
            DefaultPassword = "pass"
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return json.GetProperty("id").GetGuid();
    }

    // ─── happy path ────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Create→GetById→Update→Delete — happy path")]
    public async Task CrudHappyPath()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();

        // Create
        var createReq = new CreateTargetDbRequest(dbmsId, "HappyDB", "desc", false);
        var createResp = await HttpClient.PostAsJsonAsync("api/v1/target-dbs", createReq);
        createResp.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<TargetDbResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var id = created!.Id;

        // GetById
        var getResp = await HttpClient.GetAsync($"api/v1/target-dbs/{id}");
        getResp.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await getResp.Content.ReadFromJsonAsync<TargetDbResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        dto!.DbName.ShouldBe("HappyDB");
        dto.IsReadOnly.ShouldBeFalse();

        // Update
        var updateReq = new UpdateTargetDbRequest("UpdatedDB", null, true);
        var updateResp = await HttpClient.PutAsJsonAsync($"api/v1/target-dbs/{id}", updateReq);
        updateResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getAfterUpdate = await HttpClient.GetAsync($"api/v1/target-dbs/{id}");
        var updated = await getAfterUpdate.Content.ReadFromJsonAsync<TargetDbResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        updated!.DbName.ShouldBe("UpdatedDB");
        updated.IsReadOnly.ShouldBeTrue();

        // Delete
        var deleteResp = await HttpClient.DeleteAsync($"api/v1/target-dbs/{id}");
        deleteResp.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // GetById after delete → 404
        var afterDelete = await HttpClient.GetAsync($"api/v1/target-dbs/{id}");
        afterDelete.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── negative ──────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Create с несуществующим DbmsId → 404 (Result.NotFound)")]
    public async Task Create_NonExistentDbmsId_Returns404()
    {
        var req = new CreateTargetDbRequest(Guid.NewGuid(), "SomeDB", null, false);
        var resp = await HttpClient.PostAsJsonAsync("api/v1/target-dbs", req);
        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact(DisplayName = "Create с пустым DbName → 400 (ValidationProblem)")]
    public async Task Create_EmptyDbName_Returns400()
    {
        var req = new CreateTargetDbRequest(Guid.NewGuid(), "", null, false);
        var resp = await HttpClient.PostAsJsonAsync("api/v1/target-dbs", req);
        resp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "GetById по несуществующему Id → 404")]
    public async Task GetById_UnknownId_Returns404()
    {
        var resp = await HttpClient.GetAsync($"api/v1/target-dbs/{Guid.NewGuid()}");
        resp.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
