using SQLModule.Contracts;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.DeleteDbmsDictionary;

internal sealed class DeleteDbmsDictionaryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.DbmsCatalog.DbmsDictionaries.ById, Handle)
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
