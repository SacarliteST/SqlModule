using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;
using SQLModule.Sandbox.DbmsProbing;

namespace SQLModule.Application.DbmsCatalog.DbmsDictionary.UpdateDbmsDictionary;

public record UpdateDbmsDictionaryCommand(
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

public sealed class UpdateDbmsDictionaryHandler(IApplicationDbContext db, IDbmsProbe probe)
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
            command.DbmsSystemName, command.DockerImage, command.DefaultPort,
            command.EnvUserKey, command.EnvPasswordKey, command.EnvDatabaseKey, command.ExtraEnvConfig,
            command.DefaultDatabase, command.DefaultUsername, command.DefaultPassword);

        var probeResult = await probe.ProbeAsync(spec, ct);
        if (!probeResult.IsSuccess)
        {
            return Result<DbmsDictionaryResponse>.Fail(DbmsDictionaryErrors.ProbeFailed(probeResult.ErrorMessage!));
        }

        entity.Update(
            command.DbmsName, command.DbmsSystemName, command.DockerImage, command.DefaultPort,
            command.EnvUserKey, command.EnvPasswordKey, command.EnvDatabaseKey, command.ExtraEnvConfig,
            command.DefaultDatabase, command.DefaultUsername, command.DefaultPassword);

        await db.SaveChangesAsync(ct);
        return DbmsDictionaryMappings.ToResponse(entity);
    }
}
