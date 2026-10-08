using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.MetaTables;

public static class MetaTableMappings
{
    public static MetaTableResponse ToResponse(MetaTable e) => new(
        e.Id, e.TargetDbId, e.TableName, e.Description,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    public static CreateMetaTableCommand ToCommand(CreateMetaTableRequest req) =>
        new(req.TargetDbId, req.TableName, req.Description);

    public static UpdateMetaTableCommand ToCommand(Guid id, UpdateMetaTableRequest req) =>
        new(id, req.TableName, req.Description);
}
