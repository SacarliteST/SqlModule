using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Training.Skills;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Web.Common;

namespace SQLModule.Web.Features.Training.Skills;

public sealed class DeleteSkillEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiRoutes.Training.Skills.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("DeleteSkill")
            .WithTags("Skills")
            .WithSummary("Мягко удалить навык")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<DeleteSkillCommand, Result>(new DeleteSkillCommand(id), ct);
        return result.ToNoContent();
    }
}
