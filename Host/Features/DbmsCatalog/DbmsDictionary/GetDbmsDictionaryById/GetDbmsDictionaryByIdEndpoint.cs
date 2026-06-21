using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;

namespace SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.GetDbmsDictionaryById;

internal sealed class GetDbmsDictionaryByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.DbmsCatalog.DbmsDictionaries.ById, Handle)
            .WithName("GetDbmsDictionaryById")
            .WithTags("DbmsCatalog")
            .Produces<DbmsDictionaryResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetDbmsDictionaryByIdQuery, Result<DbmsDictionaryResponse>>(
            new GetDbmsDictionaryByIdQuery(id), ct);
        return result.ToOk();
    }
}
