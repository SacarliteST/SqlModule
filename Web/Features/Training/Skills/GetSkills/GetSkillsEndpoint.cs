using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Training.Skills;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Skill;
using SQLModule.Web.Common;

namespace SQLModule.Web.Features.Training.Skills;

public sealed class GetSkillsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Training.Skills.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetSkills")
            .WithTags("Skills")
            .WithSummary("Получить каталог навыков")
            .Produces<PageResponse<SkillResponse>>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<GetSkillsRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetSkillsRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var query = new GetSkillsQuery(
            request.Search,
            request.Origin,
            request.IncludeDeleted,
            request.Offset,
            request.Limit);
        var result = await sender.Send<GetSkillsQuery, Result<PageResponse<SkillResponse>>>(query, ct);
        return result.ToOk();
    }
}
