using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.DbmsCatalog.ParameterDefinitions;

public record UpdateParameterDefinitionCommand(
    Guid Id,
    string ParameterKey,
    string DisplayName,
    string InputType,
    string? DefaultValue,
    short SortOrder,
    string SqlFragment,
    bool IsRequired,
    string? ValuePrefix,
    string? ValueSuffix,
    string? Separator) : IRequest<Result>;

public sealed class UpdateParameterDefinitionHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateParameterDefinitionCommand, Result>
{
    public async Task<Result> Handle(UpdateParameterDefinitionCommand command, CancellationToken ct)
    {
        var entity = await db.ParameterDefinitions.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(ParameterDefinitionErrors.NotFound(command.Id));
        }

        entity.Update(command.ParameterKey, command.DisplayName, command.InputType,
            command.DefaultValue, command.SortOrder, command.SqlFragment, command.IsRequired,
            command.ValuePrefix, command.ValueSuffix, command.Separator);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
