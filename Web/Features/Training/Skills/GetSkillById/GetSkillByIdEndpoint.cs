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

public sealed class GetSkillByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Skills.ById, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetSkillById")
            .WithTags("Skills")
            .WithSummary("Получить навык по идентификатору")
            .Produces<SkillResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetSkillByIdQuery, Result<SkillResponse>>(
            new GetSkillByIdQuery(id), ct);
        return result.ToOk();
    }
}
