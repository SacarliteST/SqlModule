using SQLModule.Domain.DbmsCatalog;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Domain.Exceptions;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.DbmsCatalog.DbmsDictionary;

public sealed class UpdateDbmsDictionaryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.DbmsCatalog.DbmsDictionaries.ById, Handle)
            .WithName("UpdateDbmsDictionary")
            .WithTags("DbmsCatalog")
            .Produces<CreateDbmsDictionaryEndpoint.Response>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(
        string DbmsName,
        string DbmsSystemName,
        string DockerImage,
        int DefaultPort,
        string EnvUserKey,
        string EnvPasswordKey,
        string EnvDatabaseKey,
        string? ExtraEnvConfig,
        string DefaultDatabase,
        string DefaultUsername,
        string DefaultPassword);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.DbmsName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.DbmsSystemName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.DockerImage).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DefaultPort).InclusiveBetween(1, 65535);
            RuleFor(x => x.EnvUserKey).NotEmpty().MaximumLength(100);
            RuleFor(x => x.EnvPasswordKey).NotEmpty().MaximumLength(100);
            RuleFor(x => x.EnvDatabaseKey).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ExtraEnvConfig).MaximumLength(500);
            RuleFor(x => x.DefaultDatabase).NotEmpty().MaximumLength(100);
            RuleFor(x => x.DefaultUsername).NotEmpty().MaximumLength(100);
            RuleFor(x => x.DefaultPassword).NotEmpty().MaximumLength(100);
        }
    }

    private static async Task<Ok<CreateDbmsDictionaryEndpoint.Response>> Handle(
        Guid id, Request request, AppDbContext db, CancellationToken ct)
    {
        var entity = await db.DbmsDictionaries
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("DbmsDictionary", id);

        entity.Update(
            request.DbmsName, request.DbmsSystemName, request.DockerImage, request.DefaultPort,
            request.EnvUserKey, request.EnvPasswordKey, request.EnvDatabaseKey, request.ExtraEnvConfig,
            request.DefaultDatabase, request.DefaultUsername, request.DefaultPassword);

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(CreateDbmsDictionaryEndpoint.ToResponse(entity));
    }
}
