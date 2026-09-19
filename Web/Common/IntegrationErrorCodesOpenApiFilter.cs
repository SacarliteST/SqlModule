using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SQLModule.Web.Common;

/// <summary>Закрепляет стабильные machine-коды интеграционных ошибок рядом с HTTP-ответами.</summary>
internal sealed class IntegrationErrorCodesOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        AddTargetDbConflictResponse(operation, context);

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
        else if (endpointName == "PublishSqlTask")
        {
            SetDescription(
                operation,
                StatusCodes.Status409Conflict,
                "Задание не готово к публикации или уже опубликовано; статус задания не меняется. " +
                "code и статус определяет первая причина, все причины — в errors. Возможные code: " +
                "SqlTask.ValidationVersionNotPublished (оценка решения не опубликована), " +
                "SqlTask.TrainingDatabaseUnavailable, SqlTask.HasAttemptsOnPublish, SqlTask.AlreadyPublished, " +
                "SqlTask.ArchivedCannotBePublished.");
            SetDescription(
                operation,
                StatusCodes.Status422UnprocessableEntity,
                "Эталон не задан, не проверен или его результат устарел относительно схемы и данных. " +
                "Возможные code: SqlTask.ReferenceQueryMissing, SqlTask.ReferenceQueryNotValidated, " +
                "ReferenceResultExceedsComparisonLimit.");
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

    private static readonly string[] TargetDbMutationPrefixes =
    [
        "api/v1/meta-tables", "api/v1/meta-attributes", "api/v1/meta-relationships",
        "api/v1/data-records", "api/v1/cell-values", "api/v1/attribute-parameter-values"
    ];

    // Схема и данные базы, используемой опубликованным заданием, менять нельзя (перехватчик сохранения).
    private static void AddTargetDbConflictResponse(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod;
        var path = context.ApiDescription.RelativePath?.Trim('/') ?? String.Empty;
        if (method is null || method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var mutatesTargetDb =
            TargetDbMutationPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) ||
            path.StartsWith("api/v1/target-dbs/", StringComparison.OrdinalIgnoreCase) &&
            (path.EndsWith("/schema", StringComparison.OrdinalIgnoreCase) ||
             path.Contains("/rows", StringComparison.OrdinalIgnoreCase) ||
             method.Equals("DELETE", StringComparison.OrdinalIgnoreCase) && path.Count(c => c == '/') == 3);
        if (!mutatesTargetDb)
        {
            return;
        }

        const string description =
            "Учебная база используется опубликованными заданиями — изменение схемы или данных запрещено. " +
            "code: TargetDb.PublishedTaskReferenceExists, идентификаторы заданий — в errors.taskIds. " +
            "Сначала архивируйте эти задания.";
        operation.Responses ??= [];
        if (operation.Responses.TryGetValue("409", out var existing) && existing is OpenApiResponse existingResponse)
        {
            existingResponse.Description = existingResponse.Description is { Length: > 0 } text && text != "Conflict"
                ? text + " " + description
                : description;
            return;
        }

        var schema = context.SchemaGenerator.GenerateSchema(
            typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), context.SchemaRepository);
        operation.Responses["409"] = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/problem+json"] = new OpenApiMediaType { Schema = schema }
            }
        };
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
