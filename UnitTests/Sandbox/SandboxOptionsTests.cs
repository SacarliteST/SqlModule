using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using SQLModule.Sandbox;
using SQLModule.Sandbox.Pooling;

namespace SQLModule.UnitTests.Sandbox;

public sealed class SandboxOptionsTests
{
    private readonly SandboxOptionsValidator validator = new();

    [Fact(DisplayName = "Pool options: значения по умолчанию валидны и пул выключен")]
    public void Defaults_AreValidAndDisabled()
    {
        var options = new SandboxOptions();

        var result = validator.Validate(null, options);

        result.Succeeded.ShouldBeTrue();
        options.Pool.Enabled.ShouldBeFalse();
    }

    [Fact(DisplayName = "Pool options: все временные интервалы должны быть положительными")]
    public void NonPositiveTimeouts_ReturnPathsForEveryInvalidSetting()
    {
        var options = new SandboxOptions
        {
            Pool = new SandboxPoolOptions
            {
                AcquireTimeoutSeconds = 0,
                PreparationTimeoutSeconds = -1,
                CleanupTimeoutSeconds = 0,
                ShutdownTimeoutSeconds = -1,
                HealthCheckIntervalSeconds = 0,
                StartupGracePeriodSeconds = 0,
                RestartBackoffMaxSeconds = -1,
            },
        };

        var failures = validator.Validate(null, options).Failures!.ToArray();

        failures.Length.ShouldBe(7);
        failures.ShouldAllBe(message => message.StartsWith("Sandbox:Pool:", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Pool options: проверяются границы MinSize и MaxSize")]
    public void InvalidProfileSizes_ReturnProfileSpecificFailures()
    {
        var options = new SandboxOptions();
        options.Pool.Profiles.Add("postgres", new SandboxPoolProfileOptions
        {
            MinSize = -1,
            MaxSize = 0,
        });
        options.Pool.Profiles.Add("mysql", new SandboxPoolProfileOptions
        {
            MinSize = 2,
            MaxSize = 1,
        });

        var failures = validator.Validate(null, options).Failures!.ToArray();

        failures.ShouldContain(message => message.Contains("Profiles:postgres:MinSize", StringComparison.Ordinal));
        failures.ShouldContain(message => message.Contains("Profiles:postgres:MaxSize", StringComparison.Ordinal));
        failures.ShouldContain(message => message.Contains("Profiles:mysql:MinSize", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Pool options: лимиты ресурсов должны быть положительными")]
    public void InvalidResourceLimits_ReturnSpecificFailures()
    {
        var options = new SandboxOptions
        {
            Pool = new SandboxPoolOptions
            {
                Resources = new SandboxPoolResourceOptions
                {
                    MemoryLimitMegabytes = 0,
                    CpuLimit = Double.NaN,
                    PidsLimit = -1,
                },
            },
        };

        var failures = validator.Validate(null, options).Failures!.ToArray();

        failures.ShouldContain(message => message.Contains("MemoryLimitMegabytes", StringComparison.Ordinal));
        failures.ShouldContain(message => message.Contains("CpuLimit", StringComparison.Ordinal));
        failures.ShouldContain(message => message.Contains("PidsLimit", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Pool container: Docker получает лимиты и безопасный режим")]
    public void ContainerSecurity_AppliesResourceLimitsWithoutHostAccess()
    {
        var parameters = new CreateContainerParameters();
        var resources = new SandboxPoolResourceOptions
        {
            MemoryLimitMegabytes = 384,
            CpuLimit = 1.5,
            PidsLimit = 128,
        };

        SandboxContainerSecurity.Apply(parameters, resources);

        parameters.HostConfig.Memory.ShouldBe(384L * 1024 * 1024);
        parameters.HostConfig.NanoCPUs.ShouldBe(1_500_000_000);
        parameters.HostConfig.PidsLimit.ShouldBe(128);
        parameters.HostConfig.Privileged.ShouldBeFalse();
        parameters.HostConfig.NetworkMode.ShouldBe("bridge");
        parameters.HostConfig.Binds.ShouldBeEmpty();
        parameters.HostConfig.Mounts.ShouldBeEmpty();
    }

    [Theory(DisplayName = "Pool container: образ должен иметь точный tag или digest")]
    [InlineData("postgres", false)]
    [InlineData("postgres:latest", false)]
    [InlineData("postgres:15-alpine", true)]
    [InlineData("registry.local:5000/sql/postgres:15.4", true)]
    [InlineData("postgres@sha256:abcdef", true)]
    public void ContainerSecurity_ValidatesPinnedImage(string image, bool expected)
    {
        SandboxContainerSecurity.IsImagePinned(image).ShouldBe(expected);
    }

    [Fact(DisplayName = "Pool options: включённому пулу нужен хотя бы один профиль")]
    public void EnabledPoolWithoutProfiles_IsInvalid()
    {
        var options = new SandboxOptions { Pool = new SandboxPoolOptions { Enabled = true } };

        var result = validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.Failures!.ShouldContain(message => message.Contains("Profiles", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Pool options: имена профилей связываются без учёта регистра")]
    public void ConfigurationBinding_CreatesCaseInsensitiveProfiles()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Sandbox:Pool:Profiles:PoStGrEs:MinSize"] = "1",
            ["Sandbox:Pool:Profiles:PoStGrEs:MaxSize"] = "2",
        });
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSandbox();
        using var provider = services.BuildServiceProvider();

        var profiles = provider.GetRequiredService<IOptions<SandboxOptions>>().Value.Pool.Profiles;

        profiles.ContainsKey("postgres").ShouldBeTrue();
        profiles["POSTGRES"].MinSize.ShouldBe(1);
        profiles["POSTGRES"].MaxSize.ShouldBe(2);
    }

    [Theory(DisplayName = "Pool options: неверная конфигурация останавливает host при старте")]
    [InlineData("Sandbox:Pool:AcquireTimeoutSeconds", "0")]
    [InlineData("Sandbox:Pool:Profiles:postgres:MinSize", "2")]
    public async Task InvalidConfiguration_FailsOnHostStart(string key, string value)
    {
        using var host = new HostBuilder()
            .ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [key] = value,
                }))
            .ConfigureServices(services => services.AddSandbox())
            .Build();

        var exception = await Should.ThrowAsync<OptionsValidationException>(() => host.StartAsync());

        exception.Failures.ShouldNotBeEmpty();
    }

    [Fact(DisplayName = "Pool options: выключенный feature flag сохраняет one-shot executor")]
    public void DisabledPool_UsesOneShotExecutor()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Sandbox:Pool:Enabled"] = "false",
        });
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSandbox();
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISandboxExecutor>().ShouldBeOfType<TestcontainersSandboxExecutor>();
    }

    [Fact(DisplayName = "Pool options: включённый feature flag выбирает pooled executor")]
    public void EnabledPool_UsesPooledExecutor()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Sandbox:Pool:Enabled"] = "true",
            ["Sandbox:Pool:Profiles:postgres:MinSize"] = "0",
            ["Sandbox:Pool:Profiles:postgres:MaxSize"] = "1",
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSandbox();
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ISandboxExecutor>().ShouldBeOfType<PooledSandboxExecutor>();
    }

    [Fact(DisplayName = "Pool options: MariaDB использует границы профиля MySQL")]
    public void MariaDb_UsesMySqlPoolLimits()
    {
        var options = new SandboxPoolOptions();
        var mysql = new SandboxPoolProfileOptions { MinSize = 1, MaxSize = 2 };
        options.Profiles.Add("mysql", mysql);

        var resolved = SQLModule.Sandbox.Pooling.SandboxPoolProfileResolver.Find(options, "MaRiAdB");

        resolved.ShouldBeSameAs(mysql);
    }

    [Fact(DisplayName = "Pool executor: ненастроенный профиль не обходит пул через one-shot")]
    public async Task EnabledPool_MissingProfileReturnsErrorWithoutFallback()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Sandbox:Pool:Enabled"] = "true",
            ["Sandbox:Pool:Profiles:postgres:MinSize"] = "0",
            ["Sandbox:Pool:Profiles:postgres:MaxSize"] = "1",
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSandbox();
        using var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<ISandboxExecutor>();
        var mysql = new SandboxDbmsSpec(
            "mysql",
            "mysql:8.0",
            3306,
            "MYSQL_USER",
            "sandbox",
            "MYSQL_PASSWORD",
            "password",
            "MYSQL_DATABASE",
            "training",
            "MYSQL_ROOT_PASSWORD=root-password");

        var result = await executor.ValidateSetupAsync(
            mysql,
            new SandboxSetup([]),
            CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Sandbox.PoolProfileNotConfigured");
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
