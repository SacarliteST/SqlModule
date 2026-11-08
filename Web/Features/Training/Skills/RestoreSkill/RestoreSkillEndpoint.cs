using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Training.Skills;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Skill;
using SQLModule.Web.Common;

namespace SQLModule.Web.Features.Training.Skills;

public sealed class RestoreSkillEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.Skills.Restore, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("RestoreSkill")
            .WithTags("Skills")
            .WithSummary("Восстановить мягко удалённый навык")
            .Produces<SkillResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<RestoreSkillCommand, Result<SkillResponse>>(
            new RestoreSkillCommand(id), ct);
        return result.ToOk();
    }
}
