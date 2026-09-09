using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SQLModule.Web.Common;

/// <summary>Закрепляет стабильные machine-коды интеграционных ошибок рядом с HTTP-ответами.</summary>
internal sealed class IntegrationErrorCodesOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointName = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<EndpointNameMetadata>()
            .Select(metadata => metadata.EndpointName)
            .FirstOrDefault();

        if (endpointName == "SubmitAttempt")
        {
            SetDescription(
                operation,
                StatusCodes.Status403Forbidden,
                "Сессия принадлежит другому пользователю. code: ModuleSessionForbidden.");
            SetDescription(
                operation,
                StatusCodes.Status409Conflict,
                "Конфликт состояния платформенной сессии или идемпотентного запроса. " +
                "Возможные code: ModuleSessionRequired, SessionTaskMismatch, ModuleSessionClosed, " +
                "IdempotencyKeyPayloadMismatch, IdempotencyRequestInProgress.");
        }
        else if (endpointName == "UpsertModuleSession")
        {
            SetDescription(
                operation,
                StatusCodes.Status401Unauthorized,
                "Неверный X-Service-Key. code: InvalidServiceKey.");
            SetDescription(
                operation,
                StatusCodes.Status409Conflict,
                "Неактивную сессию нельзя обновить. code: ModuleSession.Completed.");
            SetDescription(
                operation,
                StatusCodes.Status422UnprocessableEntity,
                "Ошибка полей запроса: sessionId и userId должны быть UUID; sessionKey и taskRef " +
                "обязательны и ограничены по длине; returnUrl обязателен и должен быть абсолютным " +
                "HTTP/HTTPS URL; expiresAt, если передан, должен иметь корректный date-time формат.");
        }
    }

    private static void SetDescription(OpenApiOperation operation, int statusCode, string description)
    {
        if (operation.Responses?.TryGetValue(statusCode.ToString(), out var response) == true &&
            response is OpenApiResponse openApiResponse)
        {
            openApiResponse.Description = description;
        }
    }
}
