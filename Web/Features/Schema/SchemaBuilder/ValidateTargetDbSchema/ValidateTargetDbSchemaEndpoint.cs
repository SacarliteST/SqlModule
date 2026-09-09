using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.ValidateTargetDbSchema;

internal sealed class ValidateTargetDbSchemaEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.TargetDbs.ValidateSchema, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("ValidateTargetDbSchema")
            .WithTags("Schema")
            .WithSummary("Проверить желаемую схему существующей учебной базы")
            .Produces<SchemaValidationResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<SchemaUpsertRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id,
        SchemaUpsertRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send<ValidateTargetDbSchemaCommand, Result<SchemaValidationResponse>>(
            new ValidateTargetDbSchemaCommand(id, request), ct);
        return result.ToOk();
    }
}
