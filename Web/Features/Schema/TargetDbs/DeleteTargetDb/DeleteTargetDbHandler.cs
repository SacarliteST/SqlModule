using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.TargetDbs;

internal record DeleteTargetDbCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteTargetDbHandler(AppDbContext db)
    : IRequestHandler<DeleteTargetDbCommand, Result>
{
    public async Task<Result> Handle(DeleteTargetDbCommand command, CancellationToken ct)
    {
        var entity = await db.TargetDbs.FirstOrDefaultAsync(x => x.Id == command.Id, ct);

        if (entity is null)
        {
            return Result.Fail(TargetDbErrors.NotFound(command.Id));
        }

        // SqlQuery -> TargetDb намеренно Restrict (эталонный запрос не должен молча остаться без базы),
        // поэтому проверяем это явно и отдаём понятный 409, а не падаем на FK-нарушении необработанным 500.
        var referencingQueries = await db.SqlQueries.AsNoTracking()
            .Where(x => x.TargetDbId == command.Id)
            .CountAsync(ct);
        if (referencingQueries > 0)
        {
            return Result.Fail(TargetDbErrors.InUse(command.Id, referencingQueries));
        }

        // MetaRelationship -> MetaAttribute тоже Restrict (см. MetaRelationshipConfiguration), а каскад
        // TargetDb -> MetaTable -> MetaAttribute его не подчищает: любая база хотя бы с одной связью
        // иначе падает на FK-нарушении. Удаляем связи явно, как это уже делает SchemaDiffApplier.
        var tableIds = await db.MetaTables.AsNoTracking()
            .Where(x => x.TargetDbId == command.Id)
            .Select(x => x.Id)
            .ToListAsync(ct);
        if (tableIds.Count > 0)
        {
            var columnIds = await db.MetaAttributes.AsNoTracking()
                .Where(x => tableIds.Contains(x.MetaTableId))
                .Select(x => x.Id)
                .ToListAsync(ct);
            if (columnIds.Count > 0)
            {
                var relationships = await db.MetaRelationships
                    .Where(x => columnIds.Contains(x.SourceAttributeId) || columnIds.Contains(x.TargetAttributeId))
                    .ToListAsync(ct);
                db.MetaRelationships.RemoveRange(relationships);
            }
        }

        db.TargetDbs.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
