using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Sandbox;
using SQLModule.Sandbox;

namespace SQLModule.Host.Features.Schema.SchemaBuilder;

internal sealed class SchemaPreparer(
    AppDbContext db,
    ISandboxExecutor executor,
    ISqlSyntaxFactory syntaxFactory,
    ISchemaSqlGenerator generator)
    : ISchemaPreparer
{
    public async Task<Result<PreparedSchema>> PrepareAndValidateAsync(
        CreateSchemaRequest request, CancellationToken ct)
    {
        var dbms = await db.DbmsDictionaries
            .FirstOrDefaultAsync(x => x.Id == request.DbmsId, ct);
        if (dbms is null)
        {
            return Result<PreparedSchema>.Fail(SchemaErrors.DbmsNotFound(request.DbmsId));
        }

        var physicalTypeIds = request.Tables
            .SelectMany(t => t.Columns)
            .Select(c => c.PhysicalTypeId)
            .Distinct()
            .ToList();

        var physicalTypes = await db.PhysicalTypes
            .Include(pt => pt.ParameterDefinitions)
            .Where(pt => physicalTypeIds.Contains(pt.Id))
            .ToListAsync(ct);

        var physicalTypesById = physicalTypes.ToDictionary(pt => pt.Id);

        foreach (var id in physicalTypeIds)
        {
            if (!physicalTypesById.ContainsKey(id))
            {
                return Result<PreparedSchema>.Fail(SchemaErrors.PhysicalTypeNotFound(id));
            }
        }

        foreach (var pt in physicalTypes)
        {
            if (pt.DbmsId != request.DbmsId)
            {
                return Result<PreparedSchema>.Fail(SchemaErrors.PhysicalTypeMismatch(pt.Id, dbms.DbmsSystemName));
            }
        }

        var syntax = syntaxFactory.For(dbms.DbmsSystemName);
        if (syntax is null)
        {
            return Result<PreparedSchema>.Fail(SchemaErrors.UnsupportedDbms(dbms.DbmsSystemName));
        }

        var spec = SchemaRequestMapper.ToSchemaSpec(request, physicalTypesById);
        var ddl = generator.GenerateDdl(syntax, spec);

        var sandboxResult = await executor.ValidateSetupAsync(dbms.ToSandboxSpec(), new SandboxSetup(ddl), ct);
        if (!sandboxResult.IsSuccess)
        {
            return Result<PreparedSchema>.Fail(sandboxResult.Error!);
        }

        return Result<PreparedSchema>.Success(new PreparedSchema(dbms, physicalTypesById, spec, ddl));
    }
}
