namespace SQLModule.Domain;

/// <summary>Справочник СУБД с Docker-конфигурацией.</summary>
public sealed class DbmsDictionary : AuditableEntity
{
    public string DbmsName { get; private set; }
    public string DbmsSystemName { get; private set; }
    public string DockerImage { get; private set; }
    public int DefaultPort { get; private set; }
    public string EnvUserKey { get; private set; }
    public string EnvPasswordKey { get; private set; }
    public string EnvDatabaseKey { get; private set; }
    public string? ExtraEnvConfig { get; private set; }
    public string DefaultDatabase { get; private set; }
    public string DefaultUsername { get; private set; }
    public string DefaultPassword { get; private set; }

    private readonly List<PhysicalType> physicalTypes = [];
    public IReadOnlyCollection<PhysicalType> PhysicalTypes => physicalTypes.AsReadOnly();

    private DbmsDictionary(
        Guid id,
        string dbmsName, string dbmsSystemName,
        string dockerImage, int defaultPort,
        string envUserKey, string envPasswordKey, string envDatabaseKey,
        string? extraEnvConfig,
        string defaultDatabase, string defaultUsername, string defaultPassword) : base(id)
    {
        DbmsName = dbmsName;
        DbmsSystemName = dbmsSystemName;
        DockerImage = dockerImage;
        DefaultPort = defaultPort;
        EnvUserKey = envUserKey;
        EnvPasswordKey = envPasswordKey;
        EnvDatabaseKey = envDatabaseKey;
        ExtraEnvConfig = extraEnvConfig;
        DefaultDatabase = defaultDatabase;
        DefaultUsername = defaultUsername;
        DefaultPassword = defaultPassword;
    }

    public static DbmsDictionary Create(
        string dbmsName, string dbmsSystemName,
        string dockerImage, int defaultPort,
        string envUserKey, string envPasswordKey, string envDatabaseKey,
        string? extraEnvConfig,
        string defaultDatabase, string defaultUsername, string defaultPassword,
        Guid? id = null)
        => new(id ?? Guid.NewGuid(),
            dbmsName, dbmsSystemName, dockerImage, defaultPort,
            envUserKey, envPasswordKey, envDatabaseKey, extraEnvConfig,
            defaultDatabase, defaultUsername, defaultPassword);

    public void Update(
        string dbmsName, string dbmsSystemName,
        string dockerImage, int defaultPort,
        string envUserKey, string envPasswordKey, string envDatabaseKey,
        string? extraEnvConfig,
        string defaultDatabase, string defaultUsername, string defaultPassword)
    {
        DbmsName = dbmsName;
        DbmsSystemName = dbmsSystemName;
        DockerImage = dockerImage;
        DefaultPort = defaultPort;
        EnvUserKey = envUserKey;
        EnvPasswordKey = envPasswordKey;
        EnvDatabaseKey = envDatabaseKey;
        ExtraEnvConfig = extraEnvConfig;
        DefaultDatabase = defaultDatabase;
        DefaultUsername = defaultUsername;
        DefaultPassword = defaultPassword;
    }
}
