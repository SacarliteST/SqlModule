using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Application.Persistence;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.Training.SqlTasks;

public record GetSqlTaskByIdQuery(Guid Id) : IRequest<Result<SqlTaskResponse>>;

public sealed class GetSqlTaskByIdHandler(IApplicationDbContext db)
    : IRequestHandler<GetSqlTaskByIdQuery, Result<SqlTaskResponse>>
{
    public async Task<Result<SqlTaskResponse>> Handle(GetSqlTaskByIdQuery query, CancellationToken ct)
    {
        var entity = await db.SqlTasks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.Id, ct);

        if (entity is null)
        {
            return Result<SqlTaskResponse>.Fail(SqlTaskErrors.NotFound(query.Id));
        }

        return SqlTaskMappings.ToResponse(entity);
    }
}
