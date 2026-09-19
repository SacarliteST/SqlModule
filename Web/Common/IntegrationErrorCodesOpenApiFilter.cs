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
                "Токен с session_id при выключенной интеграции. code: PlatformSession.ScopeRestricted.");
            SetDescription(
                operation,
                StatusCodes.Status404NotFound,
                "Платформенная сессия отсутствует или чужая (code: ModuleSession.NotFound), " +
                "либо задание не совпадает с заданием сессии (code: PlatformSession.TaskNotFound). " +
                "Проверка выполняется до резервации попытки и запуска sandbox.");
            SetDescription(
                operation,
                StatusCodes.Status409Conflict,
                "Конфликт состояния платформенной сессии, прохождения или идемпотентного запроса. " +
                "Возможные code: ModuleSession.Expired, ModuleSession.Closed, Progress.Closed, " +
                "Progress.AttemptsExhausted, IdempotencyKeyPayloadMismatch, IdempotencyRequestInProgress. " +
                "Повтор с тем же Idempotency-Key после завершения сессии возвращает исходный результат.");
        }
        else if (endpointName is "GetStudentTopics" or "GetStudentTasks" or "GetStudentAttempts" or "GetStudentAttemptById")
        {
            SetDescription(
                operation,
                StatusCodes.Status403Forbidden,
                "Роль не Student либо токен платформенной сессии: платформенный студент видит только " +
                "назначенное задание. code: PlatformSession.ScopeRestricted.");
        }
        else if (endpointName is "GetStudentTaskById" or "GetStudentTaskSchema")
        {
            SetDescription(
                operation,
                StatusCodes.Status403Forbidden,
                "Роль не Student либо токен с session_id при выключенной интеграции. " +
                "code: PlatformSession.ScopeRestricted.");
            SetDescription(
                operation,
                StatusCodes.Status404NotFound,
                "Задание не найдено или закрыто. Для платформенного токена — также чужое задание " +
                "(code: PlatformSession.TaskNotFound) или недоступная сессия (code: ModuleSession.NotFound). " +
                "Чтение доступно и для завершённой или истёкшей сессии.");
        }
        else if (endpointName is "StartStudentTaskProgress" or "RestartStudentTaskProgress" or "FinalizeStudentTaskProgress")
        {
            SetDescription(
                operation,
                StatusCodes.Status403Forbidden,
                "Standalone-операция недоступна платформенному токену. " +
                "code: PlatformSession.StandaloneOperationForbidden (или PlatformSession.ScopeRestricted).");
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
