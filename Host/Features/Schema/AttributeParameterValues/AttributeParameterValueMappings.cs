using SQLModule.Contracts.Schema.AttributeParameterValue;
using SQLModule.Domain.Schema;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

internal static class AttributeParameterValueMappings
{
    internal static AttributeParameterValueResponse ToResponse(AttributeParameterValue e) => new(
        e.Id, e.MetaAttributeId, e.ParameterDefinitionId, e.ParameterValue,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    internal static CreateAttributeParameterValueCommand ToCommand(
        CreateAttributeParameterValueRequest req) =>
        new(req.MetaAttributeId, req.ParameterDefinitionId, req.ParameterValue);

    internal static UpdateAttributeParameterValueCommand ToCommand(
        Guid id, UpdateAttributeParameterValueRequest req) =>
        new(id, req.ParameterValue);
}
