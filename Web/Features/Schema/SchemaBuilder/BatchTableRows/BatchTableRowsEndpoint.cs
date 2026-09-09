using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.BatchTableRows;

internal sealed class BatchTableRowsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPatch(ApiRoutes.Schema.TargetDbs.TableRows, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("BatchTargetDbTableRows")
            .WithTags("Schema Data")
            .WithSummary("Атомарно сохранить пакет изменений учебных строк")
            .Produces<BatchTableRowsResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .AddEndpointFilter<ValidationFilter<BatchTableRowsRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id,
        Guid tableId,
        BatchTableRowsRequest request,
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

        var result = await sender.Send<BatchTableRowsCommand, Result<BatchTableRowsResponse>>(
            new BatchTableRowsCommand(id, tableId, idempotencyKey, request), ct);
        return result.ToOk();
    }
}
