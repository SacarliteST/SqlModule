using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.AttributeParameterValue;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

internal record CreateAttributeParameterValueCommand(
    Guid MetaAttributeId,
    Guid ParameterDefinitionId,
    string ParameterValue) : IRequest<Result<AttributeParameterValueResponse>>;

internal sealed class CreateAttributeParameterValueHandler(AppDbContext db)
    : IRequestHandler<CreateAttributeParameterValueCommand, Result<AttributeParameterValueResponse>>
{
    public async Task<Result<AttributeParameterValueResponse>> Handle(
        CreateAttributeParameterValueCommand command, CancellationToken ct)
    {
        if (!await db.MetaAttributes.AnyAsync(x => x.Id == command.MetaAttributeId, ct))
        {
            return Result<AttributeParameterValueResponse>.Fail(
                AttributeParameterValueErrors.MetaAttributeNotFound(command.MetaAttributeId));
        }

        if (!await db.ParameterDefinitions.AnyAsync(x => x.Id == command.ParameterDefinitionId, ct))
        {
            return Result<AttributeParameterValueResponse>.Fail(
                AttributeParameterValueErrors.ParameterDefinitionNotFound(command.ParameterDefinitionId));
        }

        if (await db.AttributeParameterValues.AnyAsync(
                x => x.MetaAttributeId == command.MetaAttributeId &&
                     x.ParameterDefinitionId == command.ParameterDefinitionId, ct))
        {
            return Result<AttributeParameterValueResponse>.Fail(
                AttributeParameterValueErrors.AlreadyExists);
        }

        var entity = AttributeParameterValue.Create(
            command.MetaAttributeId, command.ParameterDefinitionId, command.ParameterValue);

        db.AttributeParameterValues.Add(entity);
        await db.SaveChangesAsync(ct);
        return AttributeParameterValueMappings.ToResponse(entity);
    }
}
