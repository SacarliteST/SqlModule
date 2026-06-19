using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.Topics;

internal record GetAllTopicsQuery(int Offset, int Limit)
    : IRequest<Result<PageResponse<TopicResponse>>>;

internal sealed class GetAllTopicsHandler(AppDbContext db)
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
