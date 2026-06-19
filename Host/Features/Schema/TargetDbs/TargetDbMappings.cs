using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Domain.Schema;

namespace SQLModule.Host.Features.Schema.TargetDbs;

internal static class TargetDbMappings
{
    internal static TargetDbResponse ToResponse(TargetDb e) => new(
        e.Id, e.DbmsId, e.DbName, e.Description, e.IsReadOnly,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    internal static CreateTargetDbCommand ToCommand(CreateTargetDbRequest req) =>
        new(req.DbmsId, req.DbName, req.Description, req.IsReadOnly);

    internal static UpdateTargetDbCommand ToCommand(Guid id, UpdateTargetDbRequest req) =>
        new(id, req.DbName, req.Description, req.IsReadOnly);
}
