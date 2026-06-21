using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

internal record DeleteAttributeParameterValueCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteAttributeParameterValueHandler(AppDbContext db)
    : IRequestHandler<DeleteAttributeParameterValueCommand, Result>
{
    public async Task<Result> Handle(DeleteAttributeParameterValueCommand command, CancellationToken ct)
    {
        var entity = await db.AttributeParameterValues
            .FirstOrDefaultAsync(x => x.Id == command.Id, ct);

        if (entity is null)
        {
            return Result.Fail(AttributeParameterValueErrors.NotFound(command.Id));
        }

        db.AttributeParameterValues.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
