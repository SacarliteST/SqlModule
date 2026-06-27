using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Data.Core;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal record GetAllSqlTasksQuery(int Offset, int Limit)
    : IRequest<Result<PageResponse<SqlTaskResponse>>>;

internal sealed class GetAllSqlTasksHandler(AppDbContext db)
    : IRequestHandler<GetAllSqlTasksQuery, Result<PageResponse<SqlTaskResponse>>>
{
    public async Task<Result<PageResponse<SqlTaskResponse>>> Handle(
        GetAllSqlTasksQuery query, CancellationToken ct)
    {
        var total = await db.SqlTasks.CountAsync(ct);

        var entities = await db.SqlTasks
            .AsNoTracking()
            .OrderBy(x => x.DifficultyLevel)
            .ThenBy(x => x.TaskName)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return Result<PageResponse<SqlTaskResponse>>.Success(new PageResponse<SqlTaskResponse>
        {
            Items = entities.Select(SqlTaskMappings.ToResponse).ToList(),
            Count = total
        });
    }
}
