using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.Topic;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Topics;

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
            return Result<TopicResponse>.Fail(TopicErrors.NotFound(query.Id));
        }

        return TopicMappings.ToResponse(entity);
    }
}
