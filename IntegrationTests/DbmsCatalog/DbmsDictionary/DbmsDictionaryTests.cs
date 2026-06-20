using Shouldly;
using SQLModule.Client;
using SQLModule.Client.DbmsDictionary;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.IntegrationTests.infrastructure;

namespace SQLModule.IntegrationTests.DbmsCatalog.DbmsDictionary;

[Collection(IntegrationTestCollection.Name)]
public sealed class DbmsDictionaryTests : ApiTestBase
{
    public DbmsDictionaryTests(TestApplication testApplication) : base(testApplication)
    {
    }

    private static CreateDbmsDictionaryRequest MakeRequest(string? name = null) => new(
        DbmsName: name ?? "СУБД_" + Guid.NewGuid().ToString("N")[..8],
        DbmsSystemName: "postgres",
        DockerImage: "postgres:latest",
        DefaultPort: 5432,
        EnvUserKey: "POSTGRES_USER",
        EnvPasswordKey: "POSTGRES_PASSWORD",
        EnvDatabaseKey: "POSTGRES_DB",
        ExtraEnvConfig: null,
        DefaultDatabase: "testdb",
        DefaultUsername: "user",
        DefaultPassword: "pass");

    [Fact(DisplayName = "Create → возвращает DbmsDictionaryResponse с корректными полями")]
    public async Task Create_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var request = MakeRequest();

        // Act
        var response = await DbmsDictionaryClient.CreateAsync(request);

        // Assert
        response.Id.ShouldNotBe(Guid.Empty);
        response.DbmsName.ShouldBe(request.DbmsName);
        response.DbmsSystemName.ShouldBe(request.DbmsSystemName);
        response.DockerImage.ShouldBe(request.DockerImage);
        response.DefaultPort.ShouldBe(request.DefaultPort);
        response.DefaultUsername.ShouldBe(request.DefaultUsername);
    }

    [Fact(DisplayName = "Create → DefaultPassword не возвращается в ответе")]
    public async Task Create_DoesNotReturnDefaultPassword()
    {
        // Arrange
        var request = MakeRequest();

        // Act
        var response = await DbmsDictionaryClient.CreateAsync(request);

        // Assert
        response.ShouldNotBeNull();
        typeof(DbmsDictionaryResponse).GetProperty("DefaultPassword").ShouldBeNull();
    }

    [Fact(DisplayName = "GetById → возвращает ранее созданную СУБД")]
    public async Task GetById_ExistingId_ReturnsResponse()
    {
        // Arrange
        var created = await DbmsDictionaryClient.CreateAsync(MakeRequest());

        // Act
        var found = await DbmsDictionaryClient.GetByIdAsync(created.Id);

        // Assert
        found.ShouldNotBeNull();
        found!.Id.ShouldBe(created.Id);
        found.DbmsName.ShouldBe(created.DbmsName);
    }

    [Fact(DisplayName = "GetAll → страница содержит созданную СУБД")]
    public async Task GetAll_ContainsCreatedDbms()
    {
        // Arrange
        var created = await DbmsDictionaryClient.CreateAsync(MakeRequest());

        // Act
        var page = await DbmsDictionaryClient.GetAllAsync(0, 100);

        // Assert
        page.Items.ShouldContain(d => d.Id == created.Id);
    }

    [Fact(DisplayName = "Update → изменения сохранены в БД")]
    public async Task Update_ExistingId_PersistsChanges()
    {
        // Arrange
        var created = await DbmsDictionaryClient.CreateAsync(MakeRequest());
        var newName = "Updated_" + Guid.NewGuid().ToString("N")[..8];

        // Act
        await DbmsDictionaryClient.UpdateAsync(created.Id, new UpdateDbmsDictionaryRequest(
            newName, "postgres", "postgres:15", 5432,
            "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB",
            null, "testdb", "user", "pass"));

        // Assert
        var updated = await DbmsDictionaryClient.GetByIdAsync(created.Id);
        updated!.DbmsName.ShouldBe(newName);
        updated.DockerImage.ShouldBe("postgres:15");
    }

    [Fact(DisplayName = "Delete → СУБД больше не возвращается GetById")]
    public async Task Delete_ExistingId_EntityRemoved()
    {
        // Arrange
        var created = await DbmsDictionaryClient.CreateAsync(MakeRequest());

        // Act
        await DbmsDictionaryClient.DeleteAsync(created.Id);

        // Assert
        var found = await DbmsDictionaryClient.GetByIdAsync(created.Id);
        found.ShouldBeNull();
    }

    [Fact(DisplayName = "GetById несуществующего → null")]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        // Act
        var result = await DbmsDictionaryClient.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Delete несуществующего → без исключения (no-op)")]
    public async Task Delete_UnknownId_NoException()
    {
        // Act + Assert
        await Should.NotThrowAsync(() => DbmsDictionaryClient.DeleteAsync(Guid.NewGuid()));
    }

    [Fact(DisplayName = "Update несуществующего → NotFoundException")]
    public async Task Update_UnknownId_ThrowsNotFoundException()
    {
        // Act + Assert
        await Should.ThrowAsync<NotFoundException>(
            () => DbmsDictionaryClient.UpdateAsync(Guid.NewGuid(), new UpdateDbmsDictionaryRequest(
                "X", "postgres", "postgres:latest", 5432,
                "POSTGRES_USER", "POSTGRES_PASSWORD", "POSTGRES_DB",
                null, "testdb", "user", "pass")));
    }

    [Fact(DisplayName = "Create с дублирующимся DbmsName → ConflictException с кодом AlreadyExists")]
    public async Task Create_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        var name = "Dup_" + Guid.NewGuid().ToString("N")[..8];
        await DbmsDictionaryClient.CreateAsync(MakeRequest(name));

        // Act
        var ex = await Should.ThrowAsync<ConflictException>(
            () => DbmsDictionaryClient.CreateAsync(MakeRequest(name)));

        // Assert
        ex.Problem!.Title.ShouldNotBeNull();
        ex.Problem.Title!.ShouldContain("AlreadyExists");
    }

    [Fact(DisplayName = "Create с пустым DbmsName → ValidationException")]
    public async Task Create_EmptyDbmsName_ThrowsValidationException()
    {
        // Arrange
        var request = MakeRequest() with { DbmsName = "" };

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => DbmsDictionaryClient.CreateAsync(request));

        // Assert
        ex.Errors.ShouldContainKey("DbmsName");
    }

    [Fact(DisplayName = "Create с портом вне диапазона → ValidationException")]
    public async Task Create_InvalidPort_ThrowsValidationException()
    {
        // Arrange
        var request = MakeRequest() with { DefaultPort = 99999 };

        // Act
        var ex = await Should.ThrowAsync<ValidationException>(
            () => DbmsDictionaryClient.CreateAsync(request));

        // Assert
        ex.Errors.ShouldContainKey("DefaultPort");
    }

    [Fact(DisplayName = "Validate → 204 при корректной конфигурации (фейк-проб)")]
    public async Task Validate_ValidConfig_ReturnsNoContent()
    {
        // Arrange
        var request = MakeRequest();

        // Act + Assert
        await Should.NotThrowAsync(() => DbmsDictionaryClient.ValidateAsync(request));
    }
}
