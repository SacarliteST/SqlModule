using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.SchemaBuilder.GetLookupValues;

internal sealed class GetLookupValuesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.TargetDbs.LookupValues, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetTargetDbTableLookupValues")
            .WithTags("Schema Data")
            .WithSummary("Получить страницу значений связанной таблицы")
            .Produces<LookupValuesResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<LookupValuesRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid targetDbId,
        Guid tableId,
        [AsParameters] LookupValuesRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send<GetLookupValuesQuery, Result<LookupValuesResponse>>(
            new GetLookupValuesQuery(
                targetDbId,
                tableId,
                request.ValueColumnId!.Value,
                request.LabelColumnId,
                request.Search?.Trim(),
                request.Offset ?? 0,
                request.Limit ?? 30),
            ct);
        return result.ToOk();
    }
}
