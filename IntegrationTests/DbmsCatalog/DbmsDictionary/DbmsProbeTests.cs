using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shouldly;
using SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.Sandbox;

namespace SQLModule.IntegrationTests.DbmsCatalog.DbmsDictionary;

public sealed class DbmsProbeTests
{
    private static TestcontainersDbmsProbe CreateProbe(bool enabled = true, int timeoutSeconds = 120)
    {
        var opts = Options.Create(new DbmsCatalogOptions
        {
            ProbeEnabled = enabled,
            ProbeTimeoutSeconds = timeoutSeconds,
            ProbeCacheTtl = TimeSpan.FromSeconds(1),
        });
        return new TestcontainersDbmsProbe(opts, new MemoryCache(new MemoryCacheOptions()));
    }

    private static DbmsProbeSpec PostgresSpec(string image = "postgres:latest") => new(
        DbmsSystemName: "postgres",
        DockerImage: image,
        DefaultPort: 5432,
        EnvUserKey: "POSTGRES_USER",
        EnvPasswordKey: "POSTGRES_PASSWORD",
        EnvDatabaseKey: "POSTGRES_DB",
        ExtraEnvConfig: null,
        DefaultDatabase: "testdb",
        DefaultUsername: "user",
        DefaultPassword: "pass");

    private static DbmsProbeSpec MySqlSpec(string image = "mysql:8.0") => new(
        DbmsSystemName: "mysql",
        DockerImage: image,
        DefaultPort: 3306,
        EnvUserKey: "MYSQL_USER",
        EnvPasswordKey: "MYSQL_PASSWORD",
        EnvDatabaseKey: "MYSQL_DATABASE",
        ExtraEnvConfig: "MYSQL_ROOT_PASSWORD=rootpass",
        DefaultDatabase: "testdb",
        DefaultUsername: "user",
        DefaultPassword: "pass");

    [Fact(DisplayName = "ProbeAsync → успех при корректной postgres:latest конфигурации")]
    public async Task ProbeAsync_ValidPostgresConfig_ReturnsSuccess()
    {
        // Arrange
        var probe = CreateProbe();

        // Act
        var result = await probe.ProbeAsync(PostgresSpec(), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact(DisplayName = "ProbeAsync → ошибка при несуществующем образе")]
    public async Task ProbeAsync_InvalidImage_ReturnsProbeFailed()
    {
        // Arrange
        var probe = CreateProbe(timeoutSeconds: 15);

        // Act
        var result = await probe.ProbeAsync(PostgresSpec("nonexistent-image-xyz-000:latest"), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldContain("ProbeFailed");
    }

    [Fact(DisplayName = "ProbeAsync при ProbeEnabled=false → успех без запуска контейнера")]
    public async Task ProbeAsync_ProbeDisabled_ReturnsSuccessWithoutContainer()
    {
        // Arrange
        var probe = CreateProbe(enabled: false);

        // Act
        var result = await probe.ProbeAsync(PostgresSpec("nonexistent-image-xyz-000:latest"), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact(DisplayName = "ProbeAsync → успех при корректной mysql:8.0 конфигурации (регрессия: не Postgres-протокол)")]
    public async Task ProbeAsync_ValidMySqlConfig_ReturnsSuccess()
    {
        // Arrange
        var probe = CreateProbe();

        // Act
        var result = await probe.ProbeAsync(MySqlSpec(), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact(DisplayName = "ProbeAsync → повторный вызов с тем же образом возвращает закешированный успех")]
    public async Task ProbeAsync_SecondCall_ReturnsCachedSuccess()
    {
        // Arrange
        var probe = CreateProbe(timeoutSeconds: 60);
        var spec = PostgresSpec();

        // Act
        var first = await probe.ProbeAsync(spec, CancellationToken.None);
        var second = await probe.ProbeAsync(spec, CancellationToken.None);

        // Assert
        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue();
    }
}
