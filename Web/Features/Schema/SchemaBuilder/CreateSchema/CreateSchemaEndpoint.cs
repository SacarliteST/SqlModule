using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.CreateSchema;

internal sealed class CreateSchemaEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.SchemaBuilder.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("CreateSchema")
            .WithTags("Schema")
            .WithSummary("Создать схему")
            .WithDescription("Валидирует DDL в реальном Docker-контейнере и атомарно сохраняет мету схемы.")
            .Produces<CreateSchemaResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<CreateSchemaRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateSchemaRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<CreateSchemaCommand, Result<CreateSchemaResponse>>(
            new CreateSchemaCommand(request), ct);
        return result.ToCreated(r => ApiRoutes.Schema.TargetDbs.ForId(r.TargetDbId));
    }
}
