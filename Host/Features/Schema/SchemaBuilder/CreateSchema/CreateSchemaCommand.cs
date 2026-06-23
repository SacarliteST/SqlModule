using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.Schema.SchemaBuilder.CreateSchema;

internal record CreateSchemaCommand(CreateSchemaRequest Request) : IRequest<Result<CreateSchemaResponse>>;

internal sealed class CreateSchemaHandler(ISchemaPreparer preparer, AppDbContext db)
    : IRequestHandler<CreateSchemaCommand, Result<CreateSchemaResponse>>
{
    public async Task<Result<CreateSchemaResponse>> Handle(CreateSchemaCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var prepResult = await preparer.PrepareAndValidateAsync(request, ct);
        if (!prepResult.IsSuccess)
        {
            return Result<CreateSchemaResponse>.Fail(prepResult.Error!);
        }

        var exists = await db.TargetDbs
            .AnyAsync(x => x.DbmsId == request.DbmsId && x.DbName == request.SchemaName, ct);
        if (exists)
        {
            return Result<CreateSchemaResponse>.Fail(SchemaErrors.AlreadyExists(request.SchemaName));
        }

        var targetDb = TargetDb.Create(request.DbmsId, request.SchemaName, null, isReadOnly: false);
        db.TargetDbs.Add(targetDb);

        var attrIdByTempId = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var tableMaps = new List<SchemaTableMap>();

        foreach (var t in request.Tables)
        {
            var metaTable = MetaTable.Create(targetDb.Id, t.Name, null);
            db.MetaTables.Add(metaTable);

            var colMaps = new List<SchemaColumnMap>();
            foreach (var c in t.Columns)
            {
                var attr = MetaAttribute.Create(metaTable.Id, c.PhysicalTypeId, c.Name, c.IsPrimaryKey, c.IsRequired, c.SortOrder);
                db.MetaAttributes.Add(attr);
                attrIdByTempId[c.TempId] = attr.Id;
                colMaps.Add(new SchemaColumnMap(c.TempId, attr.Id));

                foreach (var p in c.Parameters)
                {
                    db.AttributeParameterValues.Add(
                        AttributeParameterValue.Create(attr.Id, p.ParameterDefinitionId, p.Value));
                }
            }

            tableMaps.Add(new SchemaTableMap(t.TempId, metaTable.Id, colMaps));
        }

        foreach (var r in request.Relationships)
        {
            db.MetaRelationships.Add(MetaRelationship.Create(
                r.Name,
                attrIdByTempId[r.SourceColumnTempId],
                attrIdByTempId[r.TargetColumnTempId],
                r.DeleteRule,
                r.UpdateRule));
        }

        // TODO: схема иммутабельна после создания; переименование — через отдельный эндпоинт.
        await db.SaveChangesAsync(ct);

        return Result<CreateSchemaResponse>.Success(new CreateSchemaResponse(targetDb.Id, tableMaps));
    }
}
