using Microsoft.OpenApi;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Contracts.Training.Student;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SQLModule.Web.Common;

/// <summary>
/// Явно сохраняет nullable-семантику ячейки двумерного результата SQL.
/// Swashbuckle не переносит nullability из string? через оба IReadOnlyList.
/// </summary>
internal sealed class NullableResultRowsSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var isDetailsResponse = context.Type == typeof(SubmitAttemptResponse) ||
            context.Type == typeof(AttemptResponse) ||
            context.Type == typeof(StudentAttemptResponse);
        var hasAttemptRowCount = isDetailsResponse ||
            context.Type == typeof(AttemptListItemResponse) ||
            context.Type == typeof(StudentAttemptListItemResponse);

        if (isDetailsResponse && schema is OpenApiSchema detailsSchema)
        {
            detailsSchema.Required ??= new HashSet<string>();
            detailsSchema.Required.Add("resultSnapshotState");
        }

        if (hasAttemptRowCount &&
            schema.Properties?.TryGetValue("rowCount", out var rowCountSchema) == true)
        {
            rowCountSchema.Description =
                "Количество строк, прочитанных для сравнения в рамках внутреннего comparison-лимита. " +
                "Это не общее количество строк полного неограниченного результата; " +
                "returnedRowCount содержит число строк публичного snapshot, а resultRowLimit — его отдельный лимит.";
        }

        if (!isDetailsResponse)
        {
            return;
        }

        if (
            schema.Properties is null ||
            !schema.Properties.TryGetValue("actualRows", out var rowsSchema) ||
            rowsSchema is not OpenApiSchema rows ||
            rows.Items is not OpenApiSchema row ||
            row.Items is not OpenApiSchema cell)
        {
            return;
        }

        cell.Type = JsonSchemaType.String | JsonSchemaType.Null;
    }
}
