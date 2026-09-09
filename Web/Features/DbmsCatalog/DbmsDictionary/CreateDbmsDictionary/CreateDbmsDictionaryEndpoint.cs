using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.CreateDbmsDictionary;

internal sealed class CreateDbmsDictionaryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.DbmsCatalog.DbmsDictionaries.Collection, Handle)
            .RequireAuthorization(Policies.Admin)
            .WithName("CreateDbmsDictionary")
            .WithTags("DbmsCatalog")
            .Produces<DbmsDictionaryResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreateDbmsDictionaryRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateDbmsDictionaryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<CreateDbmsDictionaryCommand, Result<DbmsDictionaryResponse>>(
            new CreateDbmsDictionaryCommand(
                request.DbmsName, request.DbmsSystemName, request.DockerImage, request.DefaultPort,
                request.EnvUserKey, request.EnvPasswordKey, request.EnvDatabaseKey, request.ExtraEnvConfig,
                request.DefaultDatabase, request.DefaultUsername, request.DefaultPassword), ct);
        return result.ToCreated(r => ApiRoutes.DbmsCatalog.DbmsDictionaries.ForId(r.Id));
    }
}
