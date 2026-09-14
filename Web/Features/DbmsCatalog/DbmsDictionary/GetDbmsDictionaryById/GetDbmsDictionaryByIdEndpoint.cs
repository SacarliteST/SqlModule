using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.GetDbmsDictionaryById;

internal sealed class GetDbmsDictionaryByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.DbmsCatalog.DbmsDictionaries.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
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
