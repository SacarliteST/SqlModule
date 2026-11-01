using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;

namespace SQLModule.Application.Schema.AttributeParameterValues;

public record DeleteAttributeParameterValueCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteAttributeParameterValueHandler(IApplicationDbContext db)
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
