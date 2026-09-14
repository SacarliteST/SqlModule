using Microsoft.EntityFrameworkCore;
using SQLModule.Data.Core;
using SQLModule.Sandbox;
using SQLModule.Sandbox.Pooling;

namespace SQLModule.Web.Common.Sandbox;

internal sealed class DbmsSandboxPoolProfileSource(AppDbContext db) : ISandboxPoolProfileSource
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
