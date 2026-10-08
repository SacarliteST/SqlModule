using SQLModule.Contracts.Schema.AttributeParameterValue;
using SQLModule.Domain.Schema;

namespace SQLModule.Application.Schema.AttributeParameterValues;

public static class AttributeParameterValueMappings
{
    public static AttributeParameterValueResponse ToResponse(AttributeParameterValue e) => new(
        e.Id, e.MetaAttributeId, e.ParameterDefinitionId, e.ParameterValue,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);

    public static CreateAttributeParameterValueCommand ToCommand(
        CreateAttributeParameterValueRequest req) =>
        new(req.MetaAttributeId, req.ParameterDefinitionId, req.ParameterValue);

    public static UpdateAttributeParameterValueCommand ToCommand(
        Guid id, UpdateAttributeParameterValueRequest req) =>
        new(id, req.ParameterValue);
}
