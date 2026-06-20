using Microsoft.EntityFrameworkCore;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.DbmsCatalog.ParameterDefinitions;

internal record DeleteParameterDefinitionCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteParameterDefinitionHandler(AppDbContext db)
    : IRequestHandler<DeleteParameterDefinitionCommand, Result>
{
    public async Task<Result> Handle(DeleteParameterDefinitionCommand command, CancellationToken ct)
    {
        var entity = await db.ParameterDefinitions.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null)
        {
            return Result.Fail(ParameterDefinitionErrors.NotFound(command.Id));
        }

        db.ParameterDefinitions.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
