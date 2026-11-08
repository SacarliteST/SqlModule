using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Skill;

namespace SQLModule.Application.Training.Skills;

public sealed record GetSkillsQuery(
    string? Search,
    SkillOriginResponse? Origin,
    bool IncludeDeleted,
    int Offset,
    int Limit) : IRequest<Result<PageResponse<SkillResponse>>>;

public sealed class GetSkillsHandler(IApplicationDbContext db, ITextSearch textSearch)
    : IRequestHandler<GetSkillsQuery, Result<PageResponse<SkillResponse>>>
{
    public async Task<Result<PageResponse<SkillResponse>>> Handle(GetSkillsQuery query, CancellationToken ct)
    {
        var skills = db.Skills.AsNoTracking();
        if (!query.IncludeDeleted)
        {
            skills = skills.Where(skill => !skill.IsDeleted);
        }

        if (query.Origin.HasValue)
        {
            var origin = (SQLModule.Domain.Training.SkillOrigin)query.Origin.Value;
            skills = skills.Where(skill => skill.Origin == origin);
        }

        if (!String.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLikePattern(query.Search.Trim())}%";
            skills = textSearch.WhereAnyMatchesIgnoreCase(
                skills,
                pattern,
                skill => skill.SkillId,
                skill => skill.Name);
        }

        var count = await skills.CountAsync(ct);
        var items = await skills
            .OrderBy(skill => skill.Name)
            .ThenBy(skill => skill.SkillId)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return new PageResponse<SkillResponse>
        {
            Count = count,
            Items = items.Select(SkillMappings.ToResponse).ToList()
        };
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}
