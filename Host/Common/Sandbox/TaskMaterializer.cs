using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Sandbox;

namespace SQLModule.Host.Common.Sandbox;

internal sealed class TaskMaterializer(
    AppDbContext db,
    ISqlSyntaxFactory syntaxFactory,
    ISchemaSqlGenerator generator)
    : ITaskMaterializer
{
    public async Task<Result<MaterializedTask>> MaterializeAsync(Guid targetDbId, CancellationToken ct)
    {
        var targetDb = await db.TargetDbs
            .Include(t => t.Dbms)
            .Include(t => t.MetaTables)
                .ThenInclude(mt => mt.Attributes)
                    .ThenInclude(a => a.PhysicalType)
                        .ThenInclude(pt => pt.ParameterDefinitions)
            .Include(t => t.MetaTables)
                .ThenInclude(mt => mt.DataRecords)
                    .ThenInclude(dr => dr.CellValues)
            .FirstOrDefaultAsync(t => t.Id == targetDbId, ct);

        if (targetDb is null)
        {
            return Result<MaterializedTask>.Fail(TaskMaterializeErrors.TargetDbNotFound(targetDbId));
        }

        var syntax = syntaxFactory.For(targetDb.Dbms.DbmsSystemName);
        if (syntax is null)
        {
            return Result<MaterializedTask>.Fail(TaskMaterializeErrors.UnsupportedDbms(targetDb.Dbms.DbmsSystemName));
        }

        var tables = targetDb.MetaTables.ToList();
        var attributeIds = tables.SelectMany(t => t.Attributes).Select(a => a.Id).ToList();

        var relationships = await db.MetaRelationships
            .Where(r => attributeIds.Contains(r.SourceAttributeId))
            .ToListAsync(ct);

        var parameterValues = await db.AttributeParameterValues
            .Where(pv => attributeIds.Contains(pv.MetaAttributeId))
            .ToListAsync(ct);

        var paramLookup = parameterValues.ToLookup(pv => pv.MetaAttributeId);

        var schemaSpec = MetaSchemaMapper.ToSchemaSpec(tables, relationships, paramLookup);
        var dataSpec = MetaSchemaMapper.ToDataSpec(tables);

        var ddl = generator.GenerateDdl(syntax, schemaSpec);
        var inserts = generator.GenerateInserts(syntax, schemaSpec, dataSpec);

        return Result<MaterializedTask>.Success(
            new MaterializedTask(targetDb.Dbms, new SandboxSetup([.. ddl, .. inserts])));
    }
}
