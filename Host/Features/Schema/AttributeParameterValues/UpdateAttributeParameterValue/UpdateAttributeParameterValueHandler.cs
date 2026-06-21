using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.AttributeParameterValues;

internal record UpdateAttributeParameterValueCommand(Guid Id, string ParameterValue) : IRequest<Result>;

internal sealed class UpdateAttributeParameterValueHandler(AppDbContext db)
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
