using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlTask;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlTasks;

internal sealed class ArchiveSqlTaskEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.SqlTasks.Archive, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("ArchiveSqlTask")
            .WithTags("Training")
            .WithSummary("Архивировать SQL-задание")
            .Produces<SqlTaskResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> Handle(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<ArchiveSqlTaskCommand, Result<SqlTaskResponse>>(
            new ArchiveSqlTaskCommand(id), ct);
        return result.ToOk();
    }
}
