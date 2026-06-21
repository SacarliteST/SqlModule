using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.GetAllDbmsDictionaries;

internal sealed class GetAllDbmsDictionariesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.DbmsCatalog.DbmsDictionaries.Collection, Handle)
            .WithName("GetAllDbmsDictionaries")
            .WithTags("DbmsCatalog")
            .Produces<PageResponse<DbmsDictionaryResponse>>()
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<GetAllDbmsDictionariesRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllDbmsDictionariesRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAllDbmsDictionariesQuery, Result<PageResponse<DbmsDictionaryResponse>>>(
            new GetAllDbmsDictionariesQuery(request.Offset, request.Limit), ct);
        return result.ToOk();
    }
}
