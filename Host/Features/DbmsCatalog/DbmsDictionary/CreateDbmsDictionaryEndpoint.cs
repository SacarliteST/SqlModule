using SQLModule.Domain.DbmsCatalog;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.DbmsCatalog.DbmsDictionary;

public sealed class CreateDbmsDictionaryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.DbmsCatalog.DbmsDictionaries.Collection, Handle)
            .WithName("CreateDbmsDictionary")
            .WithTags("DbmsCatalog")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
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

    public record Response(
        Guid Id,
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
        string DefaultPassword,
        Guid CreatedById,
        DateTimeOffset CreatedAt,
        Guid UpdatedById,
        DateTimeOffset UpdatedAt);

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

    private static async Task<Created<Response>> Handle(
        Request request,
        TemplateDbContext db,
        CancellationToken ct)
    {
        var entity = global::SQLModule.Domain.DbmsCatalog.DbmsDictionary.Create(
            request.DbmsName, request.DbmsSystemName, request.DockerImage, request.DefaultPort,
            request.EnvUserKey, request.EnvPasswordKey, request.EnvDatabaseKey, request.ExtraEnvConfig,
            request.DefaultDatabase, request.DefaultUsername, request.DefaultPassword);

        db.DbmsDictionaries.Add(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(ApiRoutes.DbmsCatalog.DbmsDictionaries.ForId(entity.Id), ToResponse(entity));
    }

    internal static Response ToResponse(global::SQLModule.Domain.DbmsCatalog.DbmsDictionary e) => new(
        e.Id, e.DbmsName, e.DbmsSystemName, e.DockerImage, e.DefaultPort,
        e.EnvUserKey, e.EnvPasswordKey, e.EnvDatabaseKey, e.ExtraEnvConfig,
        e.DefaultDatabase, e.DefaultUsername, e.DefaultPassword,
        e.CreatedById, e.CreatedAt, e.UpdatedById, e.UpdatedAt);
}
