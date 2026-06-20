using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Host.Common;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.DbmsCatalog.DbmsDictionary.UpdateDbmsDictionary;

internal sealed class UpdateDbmsDictionaryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.DbmsCatalog.DbmsDictionaries.ById, Handle)
            .WithName("UpdateDbmsDictionary")
            .WithTags("DbmsCatalog")
            .Produces<DbmsDictionaryResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<UpdateDbmsDictionaryRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id, UpdateDbmsDictionaryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<UpdateDbmsDictionaryCommand, Result<DbmsDictionaryResponse>>(
            new UpdateDbmsDictionaryCommand(
                id,
                request.DbmsName, request.DbmsSystemName, request.DockerImage, request.DefaultPort,
                request.EnvUserKey, request.EnvPasswordKey, request.EnvDatabaseKey, request.ExtraEnvConfig,
                request.DefaultDatabase, request.DefaultUsername, request.DefaultPassword), ct);
        return result.ToOk();
    }
}
