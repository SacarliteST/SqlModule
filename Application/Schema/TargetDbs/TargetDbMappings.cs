using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.TargetDbs;

public static class TargetDbMappings
{
    public static TargetDbResponse ToResponse(TargetDb e) => new(
        e.Id, e.DbmsId, e.DbName, e.Description, e.IsReadOnly,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt,
        e.CreatedByName, e.UpdatedByName);

    public static CreateTargetDbCommand ToCommand(CreateTargetDbRequest req) =>
        new(req.DbmsId, req.DbName, req.Description, req.IsReadOnly);

    public static UpdateTargetDbCommand ToCommand(Guid id, UpdateTargetDbRequest req) =>
        new(id, req.DbName, req.Description, req.IsReadOnly);
}
