using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using SQLModule.Client;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.Schema.TargetDb;

[Collection(IntegrationTestCollection.Name)]
public sealed class TargetDbTests : ApiTestBase
{
    public TargetDbTests(TestApplication testApplication) : base(testApplication) { }

    // ─── helpers ──────────────────────────────────────────────────────────────

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

    // ─── happy path ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "Create→GetById→GetAll→Update→Delete — happy path")]
    public async Task CrudHappyPath()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();

        // Create
        var created = await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, "HappyDB", "desc", false));
        created.DbName.ShouldBe("HappyDB");
        created.IsReadOnly.ShouldBeFalse();

        // GetById
        var found = await TargetDbClient.GetByIdAsync(created.Id);
        found.ShouldNotBeNull();
        found!.DbName.ShouldBe("HappyDB");

        // GetAll
        var page = await TargetDbClient.GetAllAsync(0, 50);
        page.Items.ShouldContain(x => x.Id == created.Id);

        // Update
        await TargetDbClient.UpdateAsync(
            created.Id, new UpdateTargetDbRequest("UpdatedDB", null, true));

        var afterUpdate = await TargetDbClient.GetByIdAsync(created.Id);
        afterUpdate!.DbName.ShouldBe("UpdatedDB");
        afterUpdate.IsReadOnly.ShouldBeTrue();

        // Delete
        await TargetDbClient.DeleteAsync(created.Id);

        var afterDelete = await TargetDbClient.GetByIdAsync(created.Id);
        afterDelete.ShouldBeNull();
    }

    // ─── 404 cases ────────────────────────────────────────────────────────────

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        var result = await TargetDbClient.GetByIdAsync(Guid.NewGuid());
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        await Should.NotThrowAsync(() => TargetDbClient.DeleteAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        await Should.ThrowAsync<NotFoundException>(
            () => TargetDbClient.UpdateAsync(
                Guid.NewGuid(), new UpdateTargetDbRequest("DB", null, false)));
    }

    // ─── conflict / validation ────────────────────────────────────────────────

    [Fact(DisplayName = "Create с несуществующим DbmsId → ConflictException (409)")]
    public async Task Create_NonExistentDbmsId_ThrowsConflictException()
    {
        await Should.ThrowAsync<ConflictException>(
            () => TargetDbClient.CreateAsync(
                new CreateTargetDbRequest(Guid.NewGuid(), "SomeDB", null, false)));
    }

    [Fact(DisplayName = "Create с пустым DbName → ValidationException (с Errors)")]
    public async Task Create_EmptyDbName_ThrowsValidationException()
    {
        var ex = await Should.ThrowAsync<ValidationException>(
            () => TargetDbClient.CreateAsync(
                new CreateTargetDbRequest(Guid.NewGuid(), "", null, false)));
        ex.Errors.ShouldNotBeEmpty();
        ex.Errors.ShouldContainKey("DbName");
    }

    [Fact(DisplayName = "Update с пустым DbName → ValidationException (с Errors)")]
    public async Task Update_EmptyDbName_ThrowsValidationException()
    {
        var dbmsId = await CreateDbmsDictionaryAsync();
        var created = await TargetDbClient.CreateAsync(
            new CreateTargetDbRequest(dbmsId, "TestDB", null, false));

        var ex = await Should.ThrowAsync<ValidationException>(
            () => TargetDbClient.UpdateAsync(
                created.Id, new UpdateTargetDbRequest("", null, false)));
        ex.Errors.ShouldNotBeEmpty();
        ex.Errors.ShouldContainKey("DbName");
    }
}
