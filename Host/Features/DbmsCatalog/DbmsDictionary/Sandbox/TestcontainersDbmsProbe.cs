using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Npgsql;
using SQLModule.Common.Results;

namespace SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.Sandbox;

internal sealed class TestcontainersDbmsProbe(
    IOptions<DbmsCatalogOptions> optionsAccessor,
    IMemoryCache cache) : IDbmsProbe
{
    private readonly DbmsCatalogOptions opts = optionsAccessor.Value;

    public async Task<Result> ProbeAsync(DbmsProbeSpec spec, CancellationToken ct)
    {
        if (!opts.ProbeEnabled)
        {
            return Result.Success();
        }

        var cacheKey = $"probe:{spec.DockerImage}:{spec.DefaultPort}:{spec.DefaultDatabase}:{spec.DefaultUsername}";
        if (cache.TryGetValue(cacheKey, out _))
        {
            return Result.Success();
        }

        try
        {
            var envVars = BuildEnvVars(spec);

            await using var container = new ContainerBuilder()
                .WithImage(spec.DockerImage)
                .WithPortBinding(spec.DefaultPort, true)
                .WithEnvironment(envVars)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(spec.DefaultPort))
                .Build();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(opts.ProbeTimeoutSeconds));

            await container.StartAsync(cts.Token);

            var host = container.Hostname;
            var port = container.GetMappedPublicPort(spec.DefaultPort);
            var connString =
                $"Host={host};Port={port};Database={spec.DefaultDatabase};" +
                $"Username={spec.DefaultUsername};Password={spec.DefaultPassword};" +
                "Timeout=10;Command Timeout=10";

            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync(cts.Token);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync(cts.Token);

            cache.Set(cacheKey, true, opts.ProbeCacheTtl);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Fail(DbmsDictionaryErrors.ProbeFailed(ex.Message));
        }
    }

    private static Dictionary<string, string> BuildEnvVars(DbmsProbeSpec spec)
    {
        var env = new Dictionary<string, string>
        {
            [spec.EnvUserKey] = spec.DefaultUsername,
            [spec.EnvPasswordKey] = spec.DefaultPassword,
            [spec.EnvDatabaseKey] = spec.DefaultDatabase,
        };

        if (String.IsNullOrWhiteSpace(spec.ExtraEnvConfig))
        {
            return env;
        }

        foreach (var pair in spec.ExtraEnvConfig.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            if (idx > 0)
            {
                env[pair[..idx].Trim()] = pair[(idx + 1)..].Trim();
            }
        }

        return env;
    }
}
