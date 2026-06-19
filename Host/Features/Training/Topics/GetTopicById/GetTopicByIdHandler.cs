using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.Topics;

internal record GetTopicByIdQuery(Guid Id) : IRequest<Result<TopicResponse>>;

internal sealed class GetTopicByIdHandler(AppDbContext db)
    : IRequestHandler<GetTopicByIdQuery, Result<TopicResponse>>
{
    public async Task<Result<TopicResponse>> Handle(GetTopicByIdQuery query, CancellationToken ct)
    {
        var entity = await db.Topics.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<TopicResponse>.Fail(Error.NotFound("Topic", query.Id));
        }

        return TopicMappings.ToResponse(entity);
    }
}
