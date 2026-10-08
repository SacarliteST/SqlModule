using SQLModule.Application.Training.Attempts;
﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Training.Attempts.GetAttemptFilterOptions;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Web.Common;

namespace SQLModule.Web.Features.Training.Attempts;

/// <summary>Справочники значений, реально встречающихся в журнале попыток.</summary>
public sealed class GetAttemptFilterOptionsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        MapStudents(app);
        MapTopics(app);
        MapTasks(app);
    }

    private static void MapStudents(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Attempts.StudentFilterOptions, GetStudents)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetAttemptStudentFilterOptions")
            .WithTags("Training")
            .WithSummary("Студенты для фильтра журнала попыток")
            .WithDescription("Возвращает уникальных студентов, встречающихся в доступном журнале. Поддерживает поиск по имени/email, восстановление по id и серверную пагинацию.")
            .Produces<PageResponse<AttemptStudentFilterOptionResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<AttemptFilterOptionsRequest>>();
    }

    private static void MapTopics(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Attempts.TopicFilterOptions, GetTopics)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetAttemptTopicFilterOptions")
            .WithTags("Training")
            .WithSummary("Темы для фильтра журнала попыток")
            .WithDescription("Возвращает уникальные темы заданий, встречающихся в доступном журнале, включая архивные данные с попытками.")
            .Produces<PageResponse<AttemptTopicFilterOptionResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<AttemptFilterOptionsRequest>>();
    }

    private static void MapTasks(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Attempts.TaskFilterOptions, GetTasks)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetAttemptTaskFilterOptions")
            .WithTags("Training")
            .WithSummary("Задания для фильтра журнала попыток")
            .WithDescription("Возвращает уникальные задания, встречающиеся в доступном журнале. Поддерживает поиск, фильтр по теме, восстановление по id и пагинацию.")
            .Produces<PageResponse<AttemptTaskFilterOptionResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .AddEndpointFilter<ValidationFilter<AttemptTaskFilterOptionsRequest>>();
    }

    private static async Task<IResult> GetStudents(
        [AsParameters] AttemptFilterOptionsRequest request, ISender sender, CancellationToken ct) =>
        (await sender.Send<GetAttemptStudentFilterOptionsQuery, Result<PageResponse<AttemptStudentFilterOptionResponse>>>(
            new GetAttemptStudentFilterOptionsQuery(request), ct)).ToOk();

    private static async Task<IResult> GetTopics(
        [AsParameters] AttemptFilterOptionsRequest request, ISender sender, CancellationToken ct) =>
        (await sender.Send<GetAttemptTopicFilterOptionsQuery, Result<PageResponse<AttemptTopicFilterOptionResponse>>>(
            new GetAttemptTopicFilterOptionsQuery(request), ct)).ToOk();

    private static async Task<IResult> GetTasks(
        [AsParameters] AttemptTaskFilterOptionsRequest request, ISender sender, CancellationToken ct) =>
        (await sender.Send<GetAttemptTaskFilterOptionsQuery, Result<PageResponse<AttemptTaskFilterOptionResponse>>>(
            new GetAttemptTaskFilterOptionsQuery(request), ct)).ToOk();
}
