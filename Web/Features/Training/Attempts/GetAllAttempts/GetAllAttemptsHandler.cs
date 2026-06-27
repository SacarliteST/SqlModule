using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.Attempt;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.Attempts;

internal record GetAllAttemptsQuery(int Offset, int Limit, Guid? TaskId, Guid? UserId)
    : IRequest<Result<PageResponse<AttemptResponse>>>;

internal sealed class GetAllAttemptsHandler(AppDbContext db)
    : IRequestHandler<GetAllAttemptsQuery, Result<PageResponse<AttemptResponse>>>
{
    public async Task<Result<PageResponse<AttemptResponse>>> Handle(
        GetAllAttemptsQuery query, CancellationToken ct)
    {
        var q = db.Attempts.AsNoTracking();

        if (query.TaskId.HasValue)
        {
            q = q.Where(a => a.TaskId == query.TaskId.Value);
        }

        if (query.UserId.HasValue)
        {
            q = q.Where(a => a.UserId == query.UserId.Value);
        }

        var total = await q.CountAsync(ct);

        var entities = await q
            .OrderByDescending(a => a.StartedAt)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return Result<PageResponse<AttemptResponse>>.Success(new PageResponse<AttemptResponse>
        {
            Items = entities.Select(AttemptMappings.ToResponse).ToList(),
            Count = total
        });
    }
}
