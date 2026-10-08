using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Persistence;
using SQLModule.Sandbox;
using SQLModule.Sandbox.Pooling;

namespace SQLModule.Application.TaskMaterialization;

public sealed class DbmsSandboxPoolProfileSource(IApplicationDbContext db) : ISandboxPoolProfileSource
{
    public async Task<IReadOnlyCollection<SandboxDbmsSpec>> GetProfilesAsync(CancellationToken cancellationToken) =>
        await db.DbmsDictionaries
            .AsNoTracking()
            .Select(value => new SandboxDbmsSpec(
                value.DbmsSystemName,
                value.DockerImage,
                value.DefaultPort,
                value.EnvUserKey,
                value.DefaultUsername,
                value.EnvPasswordKey,
                value.DefaultPassword,
                value.EnvDatabaseKey,
                value.DefaultDatabase,
                value.ExtraEnvConfig))
            .ToArrayAsync(cancellationToken);
}
