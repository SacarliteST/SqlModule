using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;

namespace SQLModule.Application.Schema.AttributeParameterValues;

public record UpdateAttributeParameterValueCommand(Guid Id, string ParameterValue) : IRequest<Result>;

public sealed class UpdateAttributeParameterValueHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateAttributeParameterValueCommand, Result>
{
    public async Task<Result> Handle(UpdateAttributeParameterValueCommand command, CancellationToken ct)
    {
        var entity = await db.AttributeParameterValues
            .FirstOrDefaultAsync(x => x.Id == command.Id, ct);

        if (entity is null)
        {
            return Result.Fail(AttributeParameterValueErrors.NotFound(command.Id));
        }

        entity.Update(command.ParameterValue);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
