using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.CreateTargetDbFromDdl;

internal sealed class CreateTargetDbFromDdlEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.TargetDbs.FromDdl, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("CreateTargetDbFromDdl")
            .WithTags("Schema")
            .WithSummary("Атомарно создать учебную базу из пользовательского DDL")
            .Produces<CreateTargetDbFromDdlResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .AddEndpointFilter<ValidationFilter<CreateTargetDbFromDdlRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateTargetDbFromDdlRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        ISender sender,
        CancellationToken ct)
    {
        if (String.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Idempotency-Key"] = ["Заголовок Idempotency-Key обязателен."]
            }, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var result = await sender.Send<CreateTargetDbFromDdlCommand, Result<CreateTargetDbFromDdlResponse>>(
            new CreateTargetDbFromDdlCommand(idempotencyKey, request), ct);
        if (result is { IsSuccess: false, Error.Type: ErrorType.Validation })
        {
            return ApiProblemFactory.ToResult(
                StatusCodes.Status422UnprocessableEntity, "Ошибка проверки DDL", result.Error.Message,
                result.Error.Code, new Dictionary<string, string[]> { ["ddlScript"] = [result.Error.Message] });
        }

        return result.ToCreated(x => ApiRoutes.Schema.TargetDbs.ForSchema(x.TargetDbId));
    }
}
