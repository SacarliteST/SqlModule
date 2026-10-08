using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;
using SQLModule.Domain.DbmsCatalog;

namespace SQLModule.Application.DbmsCatalog.ParameterDefinitions;

public static class ParameterDefinitionMappings
{
    public static ParameterDefinitionResponse ToResponse(ParameterDefinition e) => new(
        e.Id, e.PhysicalTypeId, e.ParameterKey, e.DisplayName, e.InputType,
        e.DefaultValue, e.SortOrder, e.SqlFragment, e.IsRequired,
        e.ValuePrefix, e.ValueSuffix, e.Separator,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    public static CreateParameterDefinitionCommand ToCommand(CreateParameterDefinitionRequest req) =>
        new(req.PhysicalTypeId, req.ParameterKey, req.DisplayName, req.InputType,
            req.DefaultValue, req.SortOrder, req.SqlFragment, req.IsRequired,
            req.ValuePrefix, req.ValueSuffix, req.Separator);

    public static UpdateParameterDefinitionCommand ToCommand(Guid id, UpdateParameterDefinitionRequest req) =>
        new(id, req.ParameterKey, req.DisplayName, req.InputType,
            req.DefaultValue, req.SortOrder, req.SqlFragment, req.IsRequired,
            req.ValuePrefix, req.ValueSuffix, req.Separator);
}
