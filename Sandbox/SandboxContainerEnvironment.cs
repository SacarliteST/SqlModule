namespace SQLModule.Sandbox;

internal static class SandboxContainerEnvironment
{
    internal static IReadOnlyDictionary<string, string> Build(SandboxDbmsSpec dbms)
    {
        var environment = new Dictionary<string, string>
        {
            [dbms.EnvUserKey] = dbms.DefaultUsername,
            [dbms.EnvPasswordKey] = dbms.DefaultPassword,
        };
        if (dbms.EnvDatabaseKey is not null)
        {
            environment[dbms.EnvDatabaseKey] = dbms.DefaultDatabase;
        }

        if (String.IsNullOrWhiteSpace(dbms.ExtraEnvConfig))
        {
            return environment;
        }

        foreach (var pair in dbms.ExtraEnvConfig.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator > 0)
            {
                environment[pair[..separator].Trim()] = pair[(separator + 1)..].Trim();
            }
        }

        return environment;
    }
}
