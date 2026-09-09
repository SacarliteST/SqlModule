using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.GetTableRows;

internal sealed class GetTableRowsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.TargetDbs.TableRows, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetTargetDbTableRows")
            .WithTags("Schema Data")
            .WithSummary("Получить страницу учебных строк в табличном представлении")
            .Produces<TableRowsResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id, Guid tableId, int offset, int limit, ISender sender, CancellationToken ct)
    {
        if (offset < 0 || limit is < 1 or > 100)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [offset < 0 ? "Offset" : "Limit"] = ["offset должен быть >= 0, limit — от 1 до 100."]
            }, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var result = await sender.Send<GetTableRowsQuery, Result<TableRowsResponse>>(
            new GetTableRowsQuery(id, tableId, offset, limit), ct);
        return result.ToOk();
    }
}
