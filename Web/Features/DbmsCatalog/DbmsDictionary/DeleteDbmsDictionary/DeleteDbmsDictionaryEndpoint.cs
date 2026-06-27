using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.DeleteDbmsDictionary;

internal sealed class DeleteDbmsDictionaryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.DbmsCatalog.DbmsDictionaries.ById, Handle)
            .RequireAuthorization(Policies.Admin)
            .WithName("DeleteDbmsDictionary")
            .WithTags("DbmsCatalog")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<DeleteDbmsDictionaryCommand, Result>(
            new DeleteDbmsDictionaryCommand(id), ct);
        return result.ToNoContent();
    }
}
