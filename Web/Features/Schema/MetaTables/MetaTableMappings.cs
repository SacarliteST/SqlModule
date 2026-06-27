using SQLModule.Contracts.Schema.MetaTable;
using SQLModule.Domain.Schema;

namespace SQLModule.Web.Features.Schema.MetaTables;

internal static class MetaTableMappings
{
    internal static MetaTableResponse ToResponse(MetaTable e) => new(
        e.Id, e.TargetDbId, e.TableName, e.Description,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    internal static CreateMetaTableCommand ToCommand(CreateMetaTableRequest req) =>
        new(req.TargetDbId, req.TableName, req.Description);

    internal static UpdateMetaTableCommand ToCommand(Guid id, UpdateMetaTableRequest req) =>
        new(id, req.TableName, req.Description);
}
