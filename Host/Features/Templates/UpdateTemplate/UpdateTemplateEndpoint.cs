using Contracts;
using Domain;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using TemplateApi.Data.Core;
using TemplateApi.Host.Common;

namespace TemplateApi.Host.Features.Templates.UpdateTemplate;

public sealed class UpdateTemplateEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Template.ByIdRoute, Handle)
            .WithName("UpdateTemplate")
            .Produces<Response>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request(string? TemplateName);

    public record Response(Guid Id, string? TemplateName);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TemplateName).NotEmpty().MaximumLength(200);
        }
    }

    private static async Task<Ok<Response>> Handle(
        Guid id,
        Request request,
        TemplateDbContext db,
        ILogger<UpdateTemplateEndpoint> logger,
        CancellationToken ct)
    {
        var entity = await db.TemplateObjects
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(TemplateObject), id);

        entity.Update(request.TemplateName);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Шаблонный объект с Id: {Id} изменён", entity.Id);

        return TypedResults.Ok(new Response(entity.Id, entity.TemplateName));
    }
}
