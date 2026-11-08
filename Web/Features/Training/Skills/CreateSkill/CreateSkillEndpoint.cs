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

public sealed class CreateSkillEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.Skills.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("CreateSkill")
            .WithTags("Skills")
            .WithSummary("Создать преподавательский навык")
            .Produces<SkillResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreateSkillRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateSkillRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreateSkillCommand(request.SkillId!, request.Name!, request.Description);
        var result = await sender.Send<CreateSkillCommand, Result<SkillResponse>>(command, ct);
        return result.ToCreated(skill => ApiRoutes.Training.Skills.ForId(skill.Id));
    }
}
