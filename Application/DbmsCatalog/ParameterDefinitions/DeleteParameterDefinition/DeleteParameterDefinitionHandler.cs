using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.DbmsCatalog.ParameterDefinitions;

public record DeleteParameterDefinitionCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteParameterDefinitionHandler(IApplicationDbContext db)
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
