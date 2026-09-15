using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Data.Core;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;

namespace SQLModule.Web.Features.Schema.SchemaBuilder;

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

        var domainValidation = ValidateDraft(request, physicalTypesById);
        if (domainValidation is not null)
        {
            return Result<PreparedSchema>.Fail(domainValidation);
        }

        var syntax = syntaxFactory.For(dbms.DbmsSystemName);
        if (syntax is null)
        {
            return Result<PreparedSchema>.Fail(SchemaErrors.UnsupportedDbms(dbms.DbmsSystemName));
        }

        SchemaSpec spec;
        try
        {
            spec = SchemaRequestMapper.ToSchemaSpec(request, physicalTypesById);
        }
        catch (InvalidOperationException exception)
        {
            return Result<PreparedSchema>.Fail(SchemaErrors.InvalidTypeParameter(exception.Message));
        }
        var ddl = generator.GenerateDdl(syntax, spec);

        var sandboxResult = await executor.ValidateSetupAsync(dbms.ToSandboxSpec(), new SandboxSetup(ddl), ct);
        if (!sandboxResult.IsSuccess)
        {
            return Result<PreparedSchema>.Fail(sandboxResult.Error!);
        }

        return Result<PreparedSchema>.Success(new PreparedSchema(dbms, physicalTypesById, spec, ddl));
    }

    private static Error? ValidateDraft(
        CreateSchemaRequest request,
        IReadOnlyDictionary<Guid, Domain.DbmsCatalog.PhysicalType> physicalTypes)
    {
        if (request.Tables.Count > 100 || request.Tables.Any(x => x.Columns.Count > 200) || request.Relationships.Count > 500)
        {
            return SchemaErrors.SchemaLimitExceeded;
        }

        foreach (var table in request.Tables)
        {
            foreach (var column in table.Columns)
            {
                var type = physicalTypes[column.PhysicalTypeId];
                var definitions = type.ParameterDefinitions.ToDictionary(x => x.Id);
                if (column.Parameters.Select(x => x.ParameterDefinitionId).Distinct().Count() != column.Parameters.Count ||
                    column.Parameters.Any(x => !definitions.ContainsKey(x.ParameterDefinitionId)))
                {
                    return SchemaErrors.InvalidTypeParameter($"Колонка '{column.Name}' содержит лишний или повторяющийся параметр типа.");
                }

                foreach (var definition in definitions.Values.Where(x => x.IsRequired && String.IsNullOrWhiteSpace(x.DefaultValue)))
                {
                    if (!column.Parameters.Any(x => x.ParameterDefinitionId == definition.Id && !String.IsNullOrWhiteSpace(x.Value)))
                    {
                        return SchemaErrors.InvalidTypeParameter($"Параметр '{definition.DisplayName}' колонки '{column.Name}' обязателен.");
                    }
                }
            }
        }

        var allColumns = request.Tables.SelectMany(x => x.Columns).ToList();
        if (allColumns.GroupBy(x => x.TempId, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
        {
            return Error.Validation("DuplicateObjectName", "Идентификаторы колонок внутри снимка должны быть уникальны.");
        }

        var columns = allColumns.ToDictionary(x => x.TempId, StringComparer.OrdinalIgnoreCase);
        if (request.Relationships
            .GroupBy(relationship => relationship.SourceColumnTempId, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1))
        {
            return SchemaErrors.InvalidRelationship(
                "Одна исходная колонка может иметь только одну внешнюю связь.");
        }

        foreach (var relationship in request.Relationships)
        {
            if (!columns.TryGetValue(relationship.SourceColumnTempId, out var source) ||
                !columns.TryGetValue(relationship.TargetColumnTempId, out var target))
            {
                return SchemaErrors.InvalidRelationship("Связь ссылается на неизвестную колонку.");
            }

            if (source.PhysicalTypeId != target.PhysicalTypeId)
            {
                return SchemaErrors.InvalidRelationship($"Типы колонок связи '{relationship.Name}' несовместимы.");
            }

            if (!target.IsPrimaryKey)
            {
                return SchemaErrors.InvalidRelationship($"Целевая колонка связи '{relationship.Name}' должна входить в первичный ключ.");
            }

            if (String.Equals(relationship.DeleteRule, "SET NULL", StringComparison.OrdinalIgnoreCase) && source.IsRequired)
            {
                return SchemaErrors.InvalidRelationship($"SET NULL запрещён для обязательной колонки '{source.Name}'.");
            }
        }

        return null;
    }
}
