using Microsoft.Extensions.Options;

namespace SQLModule.Sandbox;

internal sealed class SandboxOptionsValidator : IValidateOptions<SandboxOptions>
{
    public ValidateOptionsResult Validate(string? name, SandboxOptions options)
    {
        var failures = new List<string>();
        var pool = options.Pool;

        ValidatePositive(pool.AcquireTimeoutSeconds, "Sandbox:Pool:AcquireTimeoutSeconds", failures);
        ValidatePositive(pool.PreparationTimeoutSeconds, "Sandbox:Pool:PreparationTimeoutSeconds", failures);
        ValidatePositive(pool.CleanupTimeoutSeconds, "Sandbox:Pool:CleanupTimeoutSeconds", failures);
        ValidatePositive(pool.ShutdownTimeoutSeconds, "Sandbox:Pool:ShutdownTimeoutSeconds", failures);
        ValidatePositive(pool.HealthCheckIntervalSeconds, "Sandbox:Pool:HealthCheckIntervalSeconds", failures);
        ValidatePositive(pool.RestartBackoffMaxSeconds, "Sandbox:Pool:RestartBackoffMaxSeconds", failures);
        ValidatePositive(
            pool.Resources.MemoryLimitMegabytes,
            "Sandbox:Pool:Resources:MemoryLimitMegabytes",
            failures);
        ValidatePositive(pool.Resources.PidsLimit, "Sandbox:Pool:Resources:PidsLimit", failures);
        if (!Double.IsFinite(pool.Resources.CpuLimit) || pool.Resources.CpuLimit <= 0)
        {
            failures.Add("Sandbox:Pool:Resources:CpuLimit должен быть конечным числом больше нуля.");
        }

        if (pool.Enabled && pool.Profiles.Count == 0)
        {
            failures.Add("Sandbox:Pool:Profiles должен содержать хотя бы один профиль при включённом пуле.");
        }

        foreach (var (profileName, profile) in pool.Profiles)
        {
            var path = $"Sandbox:Pool:Profiles:{profileName}";
            if (String.IsNullOrWhiteSpace(profileName))
            {
                failures.Add("Sandbox:Pool:Profiles содержит пустое имя профиля.");
            }

            if (profile.MinSize < 0)
            {
                failures.Add($"{path}:MinSize должен быть больше или равен нулю.");
            }

            if (profile.MaxSize < 1)
            {
                failures.Add($"{path}:MaxSize должен быть больше или равен единице.");
            }

            if (profile.MinSize > profile.MaxSize)
            {
                failures.Add($"{path}:MinSize не может быть больше MaxSize.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidatePositive(int value, string path, ICollection<string> failures)
    {
        if (value <= 0)
        {
            failures.Add($"{path} должен быть больше нуля.");
        }
    }
}
