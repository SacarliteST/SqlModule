using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Training.Topics;

public record GetAllTopicsQuery(int Offset, int Limit)
    : IRequest<Result<PageResponse<TopicResponse>>>;

public sealed class GetAllTopicsHandler(IApplicationDbContext db)
    : IRequestHandler<GetAllTopicsQuery, Result<PageResponse<TopicResponse>>>
{
    public async Task<Result<PageResponse<TopicResponse>>> Handle(
        GetAllTopicsQuery query, CancellationToken ct)
    {
        var total = await db.Topics.CountAsync(ct);

        var entities = await db.Topics
            .AsNoTracking()
            .OrderBy(t => t.TopicName)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return Result<PageResponse<TopicResponse>>.Success(new PageResponse<TopicResponse>
        {
            Items = entities.Select(TopicMappings.ToResponse).ToList(),
            Count = total
        });
    }
}
