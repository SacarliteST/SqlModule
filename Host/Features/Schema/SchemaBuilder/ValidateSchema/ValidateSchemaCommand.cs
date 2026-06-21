using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Sandbox;
using SQLModule.Sandbox;

namespace SQLModule.Host.Features.Schema.SchemaBuilder.ValidateSchema;

internal record ValidateSchemaCommand(CreateSchemaRequest Request) : IRequest<Result>;

internal sealed class ValidateSchemaHandler(
    AppDbContext db,
    ISandboxExecutor executor,
    ISqlSyntaxFactory syntaxFactory,
    ISchemaSqlGenerator generator)
    : IRequestHandler<ValidateSchemaCommand, Result>
{
    public async Task<Result> Handle(ValidateSchemaCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var dbms = await db.DbmsDictionaries
            .FirstOrDefaultAsync(x => x.Id == request.DbmsId, ct);
        if (dbms is null)
        {
            return Result.Fail(SchemaErrors.DbmsNotFound(request.DbmsId));
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
                return Result.Fail(SchemaErrors.PhysicalTypeNotFound(id));
            }
        }

        foreach (var pt in physicalTypes)
        {
            if (pt.DbmsId != request.DbmsId)
            {
                return Result.Fail(SchemaErrors.PhysicalTypeMismatch(pt.Id, dbms.DbmsSystemName));
            }
        }

        var syntax = syntaxFactory.For(dbms.DbmsSystemName);
        if (syntax is null)
        {
            return Result.Fail(SchemaErrors.UnsupportedDbms(dbms.DbmsSystemName));
        }

        var schemaSpec = SchemaRequestMapper.ToSchemaSpec(request, physicalTypesById);
        var ddl = generator.GenerateDdl(syntax, schemaSpec);

        return await executor.ValidateSetupAsync(dbms.ToSandboxSpec(), new SandboxSetup(ddl), ct);
    }
}
