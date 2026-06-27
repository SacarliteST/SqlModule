using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.Schema.TargetDb;
using SQLModule.Web.Common;
using SQLModule.Web.Common.Cqrs;

namespace SQLModule.Web.Features.Schema.TargetDbs;

public sealed class CreateTargetDbEndpoint : IDevEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Schema.TargetDbs.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("CreateTargetDb")
            .WithTags("Schema", "DevTools")
            .WithSummary("Создать целевую БД")
            .WithDescription(
                "dev-only: правка структуры схемы в обход CreateSchema. " +
                "Создаёт новую БД-песочницу для указанной СУБД. " +
                "Возвращает 201 Created с телом ответа. " +
                "400 — не прошла валидация входных данных. " +
                "409 — СУБД с указанным dbmsId не найдена в справочнике.")
            .Produces<TargetDbResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddEndpointFilter<ValidationFilter<CreateTargetDbRequest>>();
    }

    private static async Task<IResult> Handle(
        CreateTargetDbRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<CreateTargetDbCommand, Result<TargetDbResponse>>(
            TargetDbMappings.ToCommand(request), ct);
        return result.ToCreated(r => ApiRoutes.Schema.TargetDbs.ForId(r.Id));
    }
}
