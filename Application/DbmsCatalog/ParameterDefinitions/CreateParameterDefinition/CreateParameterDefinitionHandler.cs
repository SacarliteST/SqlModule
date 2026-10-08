using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.DbmsCatalog.ParameterDefinition;
using SQLModule.Application.Persistence;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.DbmsCatalog.ParameterDefinitions;

public record CreateParameterDefinitionCommand(
    Guid PhysicalTypeId,
    string ParameterKey,
    string DisplayName,
    string InputType,
    string? DefaultValue,
    short SortOrder,
    string SqlFragment,
    bool IsRequired,
    string? ValuePrefix,
    string? ValueSuffix,
    string? Separator) : IRequest<Result<ParameterDefinitionResponse>>;

public sealed class CreateParameterDefinitionHandler(IApplicationDbContext db)
    : IRequestHandler<CreateParameterDefinitionCommand, Result<ParameterDefinitionResponse>>
{
    public async Task<Result<ParameterDefinitionResponse>> Handle(
        CreateParameterDefinitionCommand command, CancellationToken ct)
    {
        if (!await db.PhysicalTypes.AnyAsync(x => x.Id == command.PhysicalTypeId, ct))
        {
            return Result<ParameterDefinitionResponse>.Fail(
                ParameterDefinitionErrors.PhysicalTypeNotFound(command.PhysicalTypeId));
        }

        if (await db.ParameterDefinitions.AnyAsync(
                x => x.PhysicalTypeId == command.PhysicalTypeId &&
                     x.ParameterKey == command.ParameterKey, ct))
        {
            return Result<ParameterDefinitionResponse>.Fail(ParameterDefinitionErrors.AlreadyExists);
        }

        var entity = ParameterDefinition.Create(
            command.PhysicalTypeId, command.ParameterKey, command.DisplayName, command.InputType,
            command.DefaultValue, command.SortOrder, command.SqlFragment, command.IsRequired,
            command.ValuePrefix, command.ValueSuffix, command.Separator);

        db.ParameterDefinitions.Add(entity);
        await db.SaveChangesAsync(ct);
        return ParameterDefinitionMappings.ToResponse(entity);
    }
}
