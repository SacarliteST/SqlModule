using System.Security.Cryptography;

namespace SQLModule.Sandbox.Pooling;

/// <summary>
/// Уникальная БД и временные учётные записи одного lease. Секреты доступны только инфраструктуре
/// sandbox и намеренно исключены из строкового представления.
/// </summary>
internal sealed class SandboxIsolationNamespace
{
    private SandboxIsolationNamespace(Guid id)
    {
        Id = id;
        var suffix = id.ToString("N");
        DatabaseName = $"sqlm_{suffix}";
        SetupUsername = $"sqlm_setup_{suffix[..20]}";
        RunnerUsername = $"sqlm_runner_{suffix[..20]}";
        SetupPassword = CreatePassword();
        RunnerPassword = CreatePassword();
    }

    internal Guid Id { get; }
    internal string DatabaseName { get; }
    internal string SetupUsername { get; }
    internal string SetupPassword { get; }
    internal string RunnerUsername { get; }
    internal string RunnerPassword { get; }

    internal static SandboxIsolationNamespace Create() => new(Guid.NewGuid());

    public override string ToString() => $"namespace={Id:N}; database={DatabaseName}";

    private static string CreatePassword() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}
