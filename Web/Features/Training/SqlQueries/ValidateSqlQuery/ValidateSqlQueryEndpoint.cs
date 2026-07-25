using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Training.SqlQuery;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Training.SqlQueries;

public sealed class ValidateSqlQueryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Training.SqlQueries.Validate, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("ValidateSqlQuery")
            .WithTags("Training")
            .WithSummary("Проверить SQL-запрос без сохранения")
            .WithDescription(
                "Выполняет SQL-запрос в read-only песочнице на выбранной учебной базе, " +
                "применяет настроенные timeout и лимит строк и возвращает preview результата. " +
                "Не создаёт и не изменяет SqlQuery.")
            .Produces<ValidateSqlQueryResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<ValidateSqlQueryRequest>>();
    }

    private static async Task<IResult> Handle(
        ValidateSqlQueryRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send<ValidateSqlQueryCommand, Result<ValidateSqlQueryResponse>>(
            new ValidateSqlQueryCommand(request.TargetDbId, request.QueryText),
            ct);
        return result.ToOk();
    }
}
