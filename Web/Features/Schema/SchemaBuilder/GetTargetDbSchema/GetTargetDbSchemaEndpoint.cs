using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.GetTargetDbSchema;

internal sealed class GetTargetDbSchemaEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.TargetDbs.Schema, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetTargetDbSchema")
            .WithTags("Schema")
            .WithSummary("Получить согласованный снимок схемы учебной базы")
            .Produces<TargetDbSchemaResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id,
        HttpContext httpContext,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send<GetTargetDbSchemaQuery, Result<TargetDbSchemaResponse>>(
            new GetTargetDbSchemaQuery(id), ct);
        if (result.IsSuccess)
        {
            httpContext.Response.Headers.ETag = $"\"{result.Value!.Version}\"";
        }

        return result.ToOk();
    }
}
