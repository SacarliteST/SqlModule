using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.Sandbox;

namespace SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.UpdateDbmsDictionary;

internal record UpdateDbmsDictionaryCommand(
    Guid Id,
    string DbmsName,
    string DbmsSystemName,
    string DockerImage,
    int DefaultPort,
    string EnvUserKey,
    string EnvPasswordKey,
    string EnvDatabaseKey,
    string? ExtraEnvConfig,
    string DefaultDatabase,
    string DefaultUsername,
    string DefaultPassword) : IRequest<Result<DbmsDictionaryResponse>>;

internal sealed class UpdateDbmsDictionaryHandler(AppDbContext db, IDbmsProbe probe)
    : IRequestHandler<UpdateDbmsDictionaryCommand, Result<DbmsDictionaryResponse>>
{
    public async Task<Result<DbmsDictionaryResponse>> Handle(
        UpdateDbmsDictionaryCommand command, CancellationToken ct)
    {
        var entity = await db.DbmsDictionaries.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result<DbmsDictionaryResponse>.Fail(DbmsDictionaryErrors.NotFound(command.Id));
        }

        if (await db.DbmsDictionaries.AnyAsync(
                x => x.DbmsName == command.DbmsName && x.Id != command.Id, ct))
        {
            return Result<DbmsDictionaryResponse>.Fail(DbmsDictionaryErrors.AlreadyExists(command.DbmsName));
        }

        var spec = new DbmsProbeSpec(
            command.DockerImage, command.DefaultPort,
            command.EnvUserKey, command.EnvPasswordKey, command.EnvDatabaseKey, command.ExtraEnvConfig,
            command.DefaultDatabase, command.DefaultUsername, command.DefaultPassword);

        var probeResult = await probe.ProbeAsync(spec, ct);
        if (!probeResult.IsSuccess)
        {
            return Result<DbmsDictionaryResponse>.Fail(probeResult.Error!);
        }

        entity.Update(
            command.DbmsName, command.DbmsSystemName, command.DockerImage, command.DefaultPort,
            command.EnvUserKey, command.EnvPasswordKey, command.EnvDatabaseKey, command.ExtraEnvConfig,
            command.DefaultDatabase, command.DefaultUsername, command.DefaultPassword);

        await db.SaveChangesAsync(ct);
        return DbmsDictionaryMappings.ToResponse(entity);
    }
}
