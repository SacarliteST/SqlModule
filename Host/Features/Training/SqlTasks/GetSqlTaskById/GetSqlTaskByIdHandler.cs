using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Data.Core;
using SQLModule.Host.Common.Cqrs;
using SQLModule.Host.Common.Results;

namespace SQLModule.Host.Features.Training.SqlTasks;

internal record GetSqlTaskByIdQuery(Guid Id) : IRequest<Result<SqlTaskResponse>>;

internal sealed class GetSqlTaskByIdHandler(AppDbContext db)
    : IRequestHandler<GetSqlTaskByIdQuery, Result<SqlTaskResponse>>
{
    public async Task<Result<SqlTaskResponse>> Handle(GetSqlTaskByIdQuery query, CancellationToken ct)
    {
        var entity = await db.SqlTasks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<SqlTaskResponse>.Fail(Error.NotFound("SqlTask", query.Id));
        }

        return SqlTaskMappings.ToResponse(entity);
    }
}
