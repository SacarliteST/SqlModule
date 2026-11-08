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

public sealed class UpdateSkillEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Training.Skills.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("UpdateSkill")
            .WithTags("Skills")
            .WithSummary("Изменить название и описание навыка")
            .Produces<SkillResponse>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter<ValidationFilter<UpdateSkillRequest>>();
    }

    private static async Task<IResult> Handle(
        Guid id,
        UpdateSkillRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new UpdateSkillCommand(id, request.Name!, request.Description);
        var result = await sender.Send<UpdateSkillCommand, Result<SkillResponse>>(command, ct);
        return result.ToOk();
    }
}
