using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Training.Topics;

public record GetTopicByIdQuery(Guid Id) : IRequest<Result<TopicResponse>>;

public sealed class GetTopicByIdHandler(IApplicationDbContext db)
    : IRequestHandler<GetTopicByIdQuery, Result<TopicResponse>>
{
    public async Task<Result<TopicResponse>> Handle(GetTopicByIdQuery query, CancellationToken ct)
    {
        var entity = await db.Topics.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<TopicResponse>.Fail(TopicErrors.NotFound(query.Id));
        }

        return TopicMappings.ToResponse(entity);
    }
}
