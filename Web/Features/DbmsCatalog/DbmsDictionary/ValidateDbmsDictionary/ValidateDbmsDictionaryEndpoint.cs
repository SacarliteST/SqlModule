using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.ValidateDbmsDictionary;

internal sealed class ValidateDbmsDictionaryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.DbmsCatalog.DbmsDictionaries.Validate, Handle)
            .WithName("ValidateDbmsDictionary")
            .WithTags("DbmsCatalog")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreateDbmsDictionaryRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateDbmsDictionaryRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<ValidateDbmsDictionaryCommand, Result>(
            new ValidateDbmsDictionaryCommand(
                request.DockerImage, request.DefaultPort,
                request.EnvUserKey, request.EnvPasswordKey, request.EnvDatabaseKey, request.ExtraEnvConfig,
                request.DefaultDatabase, request.DefaultUsername, request.DefaultPassword), ct);
        return result.ToNoContent();
    }
}
