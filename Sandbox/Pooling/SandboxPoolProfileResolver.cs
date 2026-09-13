namespace SQLModule.Sandbox.Pooling;

internal static class SandboxPoolProfileResolver
{
    internal static SandboxPoolProfileOptions? Find(
        SandboxPoolOptions options,
        string systemName)
    {
        if (options.Profiles.TryGetValue(systemName, out var exact))
        {
            return exact;
        }

        return systemName.Equals("mariadb", StringComparison.OrdinalIgnoreCase) &&
               options.Profiles.TryGetValue("mysql", out var mysql)
            ? mysql
            : null;
    }

    internal static bool Matches(string configuredProfile, string systemName) =>
        configuredProfile.Equals(systemName, StringComparison.OrdinalIgnoreCase) ||
        configuredProfile.Equals("mysql", StringComparison.OrdinalIgnoreCase) &&
        systemName.Equals("mariadb", StringComparison.OrdinalIgnoreCase);
}
