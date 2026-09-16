using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SQLModule.Web.Common;

/// <summary>
/// Уточняет контракт строкового заголовка: строковый тип нужен endpoint для безопасной
/// собственной ошибки 400, а OpenAPI должен представить значение как обязательный UUID.
/// </summary>
internal sealed class SubmitAttemptIdempotencyOpenApiFilter : IOperationFilter
{
    private static readonly HashSet<string> IdempotentOperations =
    [
        "SubmitAttempt",
        "StartStudentTaskProgress",
        "RestartStudentTaskProgress",
        "FinalizeStudentTaskProgress",
        "FinalizeCurrentModuleSession",
        "PublishTaskValidation"
    ];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointName = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<EndpointNameMetadata>()
            .Select(metadata => metadata.EndpointName)
            .FirstOrDefault();
        if (endpointName is null || !IdempotentOperations.Contains(endpointName))
        {
            return;
        }

        var parameter = operation.Parameters?.OfType<OpenApiParameter>()
            .SingleOrDefault(item => item.Name == "Idempotency-Key");
        if (parameter?.Schema is not OpenApiSchema schema)
        {
            return;
        }

        parameter.Required = true;
        parameter.Description =
            "Уникальный UUID операции в формате xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx.";
        schema.Format = "uuid";
        schema.MinLength = 36;
        schema.MaxLength = 36;
    }
}
