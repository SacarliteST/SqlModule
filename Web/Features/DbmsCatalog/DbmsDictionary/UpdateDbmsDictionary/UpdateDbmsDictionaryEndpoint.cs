using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.UpdateDbmsDictionary;

internal sealed class UpdateDbmsDictionaryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.DbmsCatalog.DbmsDictionaries.ById, Handle)
            .RequireAuthorization(Policies.Admin)
            .WithName("UpdateDbmsDictionary")
            .WithTags("DbmsCatalog")
            .Produces<DbmsDictionaryResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
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
