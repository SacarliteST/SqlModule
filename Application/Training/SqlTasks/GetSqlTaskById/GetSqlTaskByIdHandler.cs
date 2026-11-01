using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Cqrs;
using SQLModule.Application.Persistence;
using SQLModule.Common.Results;
using SQLModule.Contracts.Training.SqlTask;

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
