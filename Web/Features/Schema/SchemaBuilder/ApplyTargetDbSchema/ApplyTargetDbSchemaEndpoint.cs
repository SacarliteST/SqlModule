using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.ApplyTargetDbSchema;

internal sealed class ApplyTargetDbSchemaEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Schema.TargetDbs.Schema, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("ApplyTargetDbSchema")
            .WithTags("Schema")
            .WithSummary("Проверить и атомарно применить полный снимок схемы")
            .Produces<TargetDbSchemaResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .AddEndpointFilter<ValidationFilter<SchemaUpsertRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id,
        SchemaUpsertRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        HttpContext httpContext,
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

        var result = await sender.Send<ApplyTargetDbSchemaCommand, Result<TargetDbSchemaResponse>>(
            new ApplyTargetDbSchemaCommand(id, ifMatch, idempotencyKey, request), ct);
        if (result.IsSuccess)
        {
            httpContext.Response.Headers.ETag = $"\"{result.Value!.Version}\"";
        }

        return result.ToOk();
    }
}
