using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;

namespace SQLModule.Web.Common.Sandbox;

internal sealed class TargetDbReferentialIntegrityValidator(AppDbContext db)
    : ITargetDbReferentialIntegrityValidator
{
    public async Task<Result> ValidateAsync(Guid targetDbId, CancellationToken ct)
    {
        var relationships = await db.MetaRelationships
            .Where(relationship => relationship.SourceAttribute.MetaTable.TargetDbId == targetDbId)
            .Select(relationship => new RelationshipInfo(
                relationship.SourceAttributeId,
                relationship.SourceAttribute.AttributeName,
                relationship.SourceAttribute.PhysicalType.TypeName,
                relationship.TargetAttributeId,
                relationship.TargetAttribute.MetaTable.TableName))
            .ToListAsync(ct);
        if (relationships.Count == 0)
        {
            return Result.Success();
        }

        var attributeIds = relationships
            .SelectMany(relationship => new[] { relationship.SourceAttributeId, relationship.TargetAttributeId })
            .Distinct()
            .ToList();
        var cells = await db.CellValues
            .Where(cell => attributeIds.Contains(cell.MetaAttributeId))
            .Select(cell => new CellInfo(cell.MetaAttributeId, cell.TextValue))
            .ToListAsync(ct);
        var cellsByAttribute = cells.ToLookup(cell => cell.AttributeId);

        foreach (var relationship in relationships)
        {
            var targetValues = cellsByAttribute[relationship.TargetAttributeId]
                .Where(cell => cell.Value is not null)
                .Select(cell => Normalize(cell.Value!, relationship.SourceTypeName))
                .ToHashSet(StringComparer.Ordinal);

            foreach (var sourceCell in cellsByAttribute[relationship.SourceAttributeId]
                         .Where(cell => cell.Value is not null))
            {
                if (targetValues.Contains(Normalize(sourceCell.Value!, relationship.SourceTypeName)))
                {
                    continue;
                }

                return Result.Fail(TargetDbDataValidationErrors.ReferenceNotFound(
                    relationship.SourceColumnName,
                    relationship.TargetTableName,
                    sourceCell.Value!));
            }
        }

        return Result.Success();
    }

    private static string Normalize(string value, string typeName)
    {
        if (IsNumeric(typeName) &&
            Decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            return number.ToString("G29", CultureInfo.InvariantCulture);
        }

        return value;
    }

    private static bool IsNumeric(string typeName)
    {
        var normalized = typeName.ToLowerInvariant();
        return normalized.Contains("int", StringComparison.Ordinal) ||
               normalized.Contains("decimal", StringComparison.Ordinal) ||
               normalized.Contains("numeric", StringComparison.Ordinal) ||
               normalized.Contains("real", StringComparison.Ordinal) ||
               normalized.Contains("double", StringComparison.Ordinal) ||
               normalized.Contains("float", StringComparison.Ordinal);
    }

    private sealed record RelationshipInfo(
        Guid SourceAttributeId,
        string SourceColumnName,
        string SourceTypeName,
        Guid TargetAttributeId,
        string TargetTableName);

    private sealed record CellInfo(Guid AttributeId, string? Value);
}
