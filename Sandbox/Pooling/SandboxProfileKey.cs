using System.Security.Cryptography;
using System.Text;

namespace SQLModule.Sandbox.Pooling;

/// <summary>Нормализованный безопасный идентификатор конфигурации sandbox-воркера.</summary>
internal sealed record SandboxProfileKey(
    string SystemName,
    string DockerImage,
    int DefaultPort,
    string EnvironmentFingerprint)
{
    internal static SandboxProfileKey Create(SandboxDbmsSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentException.ThrowIfNullOrWhiteSpace(spec.SystemName);
        ArgumentException.ThrowIfNullOrWhiteSpace(spec.DockerImage);
        ArgumentException.ThrowIfNullOrWhiteSpace(spec.EnvUserKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(spec.EnvPasswordKey);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(spec.DefaultPort);

        var environment = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [spec.EnvUserKey.Trim()] = spec.DefaultUsername ?? String.Empty,
            [spec.EnvPasswordKey.Trim()] = spec.DefaultPassword ?? String.Empty,
        };

        if (!String.IsNullOrWhiteSpace(spec.EnvDatabaseKey))
        {
            environment[spec.EnvDatabaseKey.Trim()] = spec.DefaultDatabase ?? String.Empty;
        }

        if (!String.IsNullOrWhiteSpace(spec.ExtraEnvConfig))
        {
            foreach (var pair in spec.ExtraEnvConfig.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = pair.IndexOf('=');
                if (separator > 0)
                {
                    environment[pair[..separator].Trim()] = pair[(separator + 1)..].Trim();
                }
            }
        }

        var canonicalEnvironment = new StringBuilder();
        foreach (var (key, value) in environment)
        {
            AppendLengthPrefixed(canonicalEnvironment, key.ToUpperInvariant());
            AppendLengthPrefixed(canonicalEnvironment, value);
        }

        var fingerprint = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(canonicalEnvironment.ToString())))
            .ToLowerInvariant();

        return new SandboxProfileKey(
            spec.SystemName.Trim().ToLowerInvariant(),
            spec.DockerImage.Trim(),
            spec.DefaultPort,
            fingerprint);
    }

    public override string ToString() =>
        $"{SystemName}:{DockerImage}:{DefaultPort}:{EnvironmentFingerprint[..12]}";

    private static void AppendLengthPrefixed(StringBuilder builder, string value) =>
        builder.Append(value.Length).Append(':').Append(value).Append(';');
}
