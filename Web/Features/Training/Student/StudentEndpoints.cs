using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Training.Episodes;
using SQLModule.Application.Training.Student;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Contracts.Training.Episode;
using SQLModule.Contracts.Training.Student;
using SQLModule.Contracts.Training.Validation;
using SQLModule.Domain.Common;
using SQLModule.Web.Common;
using SQLModule.Web.Features.ModuleIntegration;

namespace SQLModule.Web.Features.Training.Student;

public sealed class StudentEndpoints : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Student.Topics, GetStudentTopics)
            .RequireAuthorization(Policies.Student).AddEndpointFilter<StandaloneOnlyReadFilter>()
            .WithName(nameof(GetStudentTopics)).WithTags("Student")
            .Produces<PageResponse<StudentTopicResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
        app.MapGet(ApiRoutes.Training.Student.Tasks, GetStudentTasks)
            .RequireAuthorization(Policies.Student).AddEndpointFilter<StandaloneOnlyReadFilter>()
            .WithName(nameof(GetStudentTasks)).WithTags("Student")
            .Produces<PageResponse<StudentTaskResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
        app.MapGet(ApiRoutes.Training.Student.TaskById, GetStudentTaskById)
            .RequireAuthorization(Policies.Student).AddEndpointFilter<PlatformTaskScopeFilter>()
            .WithName(nameof(GetStudentTaskById)).WithTags("Student")
            .Produces<StudentTaskDetailsResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        app.MapGet(ApiRoutes.Training.Student.TaskByBankNumber, GetStudentTaskByBankNumber)
            .RequireAuthorization(Policies.Student).AddEndpointFilter<StandaloneOnlyReadFilter>()
            .WithName(nameof(GetStudentTaskByBankNumber)).WithTags("Student")
            .WithSummary("Получить задание открытого банка по номеру")
            .Produces<StudentTaskDetailsResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
        app.MapPost(ApiRoutes.Training.Student.TaskHint, GetBankHint)
            .RequireAuthorization(Policies.Student).AddEndpointFilter<StandaloneOnlyReadFilter>()
            .WithName(nameof(GetBankHint)).WithTags("Student")
            .WithSummary("Получить подсказку к заданию открытого банка")
            .Produces<BankHintResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        app.MapPost(ApiRoutes.Training.Student.TaskSolution, GetBankSolution)
            .RequireAuthorization(Policies.Student).AddEndpointFilter<StandaloneOnlyReadFilter>()
            .WithName(nameof(GetBankSolution)).WithTags("Student")
            .WithSummary("Получить разбор решения задания открытого банка")
            .Produces<BankSolutionResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        app.MapGet(ApiRoutes.Training.Student.TaskSchema, GetStudentTaskSchema)
            .RequireAuthorization(Policies.Student).AddEndpointFilter<PlatformTaskScopeFilter>()
            .WithName(nameof(GetStudentTaskSchema)).WithTags("Student")
            .WithSummary("Получить безопасную схему учебной базы задания")
            .WithDescription("Возвращает только структуру базы опубликованного задания: таблицы, колонки и внешние ключи. Закрытые задания скрываются ответом 404.")
            .Produces<StudentTaskSchemaResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        app.MapGet(ApiRoutes.Training.Student.TaskTableRows, GetStudentTaskTableRows)
            .RequireAuthorization(Policies.Student).AddEndpointFilter<PlatformTaskScopeFilter>()
            .WithName(nameof(GetStudentTaskTableRows)).WithTags("Student")
            .WithSummary("Получить страницу учебных данных таблицы задания")
            .WithDescription("Возвращает только данные таблицы опубликованного задания. Доступ ограничен taskId текущей платформенной сессии.")
            .Produces<TableRowsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
        app.MapGet(ApiRoutes.Training.Student.Attempts, GetStudentAttempts)
            .RequireAuthorization(Policies.Student).AddEndpointFilter<StandaloneOnlyReadFilter>()
            .WithName(nameof(GetStudentAttempts)).WithTags("Student")
            .Produces<PageResponse<StudentAttemptListItemResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
        app.MapGet(ApiRoutes.Training.Student.AttemptById, GetStudentAttemptById)
            .RequireAuthorization(Policies.Student).AddEndpointFilter<StandaloneOnlyReadFilter>()
            .WithName(nameof(GetStudentAttemptById)).WithTags("Student")
            .Produces<StudentAttemptResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        app.MapPost(ApiRoutes.Training.Student.TaskEpisodes, StartLearningEpisode)
            .RequireAuthorization(Policies.Student).AddEndpointFilter<PlatformTaskScopeFilter>()
            .WithName(nameof(StartLearningEpisode)).WithTags("Student Episodes")
            .WithSummary("Начать или продолжить учебный эпизод")
            .Produces<LearningEpisodeResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        app.MapPost(ApiRoutes.Training.Student.EpisodeHeartbeat, HeartbeatLearningEpisode)
            .RequireAuthorization(Policies.Student)
            .WithName(nameof(HeartbeatLearningEpisode)).WithTags("Student Episodes")
            .WithSummary("Подтвердить активность текущего учебного эпизода")
            .Produces<LearningEpisodeResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        app.MapPost(ApiRoutes.Training.Student.EpisodeFinalize, FinalizeLearningEpisode)
            .RequireAuthorization(Policies.Student).AddEndpointFilter<StandaloneOnlyReadFilter>()
            .WithName(nameof(FinalizeLearningEpisode)).WithTags("Student Episodes")
            .WithSummary("Окончательно закрыть учебный эпизод")
            .Produces<LearningEpisodeResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static bool InvalidPage(int offset, int limit) => offset < 0 || limit is < 1 or > 100;
    private static IResult InvalidPagination() => Results.ValidationProblem(
        new Dictionary<string, string[]> { ["Pagination"] = ["offset должен быть >= 0, limit — от 1 до 100."] },
        statusCode: StatusCodes.Status422UnprocessableEntity);

    private static IResult ToStudentResult<T>(Result<T> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value) : TypedResults.NotFound();

    private static async Task<IResult> GetStudentTopics(
        [AsParameters] StudentTopicsRequest request,
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (InvalidPage(request.Offset, request.Limit))
        {
            return InvalidPagination();
        }

        return ToStudentResult(await sender.Send<GetStudentTopicsQuery, Result<PageResponse<StudentTopicResponse>>>(
            new GetStudentTopicsQuery(request.Offset, request.Limit, currentUser.UserId!.Value), ct));
    }

    private static async Task<IResult> GetStudentTasks(
        [AsParameters] StudentTasksRequest request,
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        if (request.Offset < 0 || request.Limit is < 1 or > 50)
        {
            return InvalidPagination();
        }

        if (request.Search?.Length > 200 || request.BankNumber is <= 0)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [request.Search?.Length > 200 ? "Search" : "BankNumber"] =
                        [request.Search?.Length > 200
                            ? "Строка поиска не должна превышать 200 символов."
                            : "Номер задания должен быть положительным."]
                },
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        return ToStudentResult(await sender.Send<GetStudentTasksQuery, Result<PageResponse<StudentTaskResponse>>>(
            new GetStudentTasksQuery(request, currentUser.UserId!.Value), ct));
    }

    private static async Task<IResult> GetStudentTaskById(
        Guid taskId, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        ToStudentResult(await sender.Send<GetStudentTaskByIdQuery, Result<StudentTaskDetailsResponse>>(
            new GetStudentTaskByIdQuery(taskId, currentUser.UserId!.Value, currentUser.ModuleSessionId), ct));

    private static async Task<IResult> GetStudentTaskByBankNumber(
        long bankNumber,
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct) =>
        ToStudentResult(await sender.Send<GetStudentTaskByBankNumberQuery, Result<StudentTaskDetailsResponse>>(
            new GetStudentTaskByBankNumberQuery(bankNumber, currentUser.UserId!.Value), ct));

    private static async Task<IResult> GetBankHint(
        Guid taskId,
        HttpContext httpContext,
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        httpContext.Response.Headers.CacheControl = "no-store";
        return (await sender.Send<GetBankHintQuery, Result<BankHintResponse>>(
            new GetBankHintQuery(taskId, currentUser.UserId!.Value), ct)).ToOk();
    }

    private static async Task<IResult> GetBankSolution(
        Guid taskId,
        HttpContext httpContext,
        ICurrentUser currentUser,
        ISender sender,
        CancellationToken ct)
    {
        httpContext.Response.Headers.CacheControl = "no-store";
        return (await sender.Send<GetBankSolutionQuery, Result<BankSolutionResponse>>(
            new GetBankSolutionQuery(taskId, currentUser.UserId!.Value), ct)).ToOk();
    }

    private static async Task<IResult> GetStudentTaskSchema(
        Guid taskId, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        ToStudentResult(await sender.Send<GetStudentTaskSchemaQuery, Result<StudentTaskSchemaResponse>>(
            new GetStudentTaskSchemaQuery(
                taskId, currentUser.UserId!.Value, currentUser.ModuleSessionId), ct));

    private static async Task<IResult> GetStudentTaskTableRows(
        Guid taskId, Guid tableId, int offset, int limit,
        ICurrentUser currentUser, ISender sender, CancellationToken ct)
    {
        if (InvalidPage(offset, limit))
        {
            return InvalidPagination();
        }

        var result = await sender.Send<GetStudentTaskTableRowsQuery, Result<TableRowsResponse>>(
            new GetStudentTaskTableRowsQuery(
                taskId, tableId, offset, limit,
                currentUser.UserId!.Value, currentUser.ModuleSessionId), ct);
        return result.Error == StudentErrors.NotFound ? TypedResults.NotFound() : result.ToOk();
    }

    private static async Task<IResult> GetStudentAttempts(
        [AsParameters] StudentAttemptsRequest request, ICurrentUser currentUser, ISender sender, CancellationToken ct)
    {
        if (InvalidPage(request.Offset, request.Limit))
        {
            return InvalidPagination();
        }

        return ToStudentResult(
            await sender.Send<GetStudentAttemptsQuery, Result<PageResponse<StudentAttemptListItemResponse>>>(
                new GetStudentAttemptsQuery(request, currentUser.UserId!.Value), ct));
    }

    private static async Task<IResult> GetStudentAttemptById(
        Guid attemptId, ICurrentUser currentUser, ISender sender, CancellationToken ct) =>
        ToStudentResult(await sender.Send<GetStudentAttemptByIdQuery, Result<StudentAttemptResponse>>(
            new GetStudentAttemptByIdQuery(attemptId, currentUser.UserId!.Value), ct));

    private static async Task<IResult> StartLearningEpisode(
        Guid taskId,
        ICurrentUser currentUser,
        ILearningEpisodeService service,
        CancellationToken ct) =>
        (await service.StartAsync(
            currentUser.UserId!.Value,
            taskId,
            currentUser.ModuleSessionId,
            ct)).ToOk();

    private static async Task<IResult> HeartbeatLearningEpisode(
        Guid episodeId,
        ICurrentUser currentUser,
        ILearningEpisodeService service,
        CancellationToken ct) =>
        (await service.HeartbeatAsync(currentUser.UserId!.Value, episodeId, ct)).ToOk();

    private static async Task<IResult> FinalizeLearningEpisode(
        Guid episodeId,
        FinalizeLearningEpisodeRequest request,
        HttpContext httpContext,
        ICurrentUser currentUser,
        ILearningEpisodeService service,
        CancellationToken ct)
    {
        if (!request.Mode.HasValue)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { ["Mode"] = ["Укажите способ завершения эпизода."] },
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].ToString();
        if (!Guid.TryParseExact(idempotencyKey, "D", out var key))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["Idempotency-Key"] = ["Укажите UUID в формате D."]
                },
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        return (await service.FinalizeAsync(
            currentUser.UserId!.Value,
            episodeId,
            request.Mode.Value,
            key,
            ct)).ToOk();
    }
}
