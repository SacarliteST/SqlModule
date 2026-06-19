using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SQLModule.Contracts;
using SQLModule.Data.Core;
using SQLModule.Domain.Schema;
using SQLModule.Host.Common;

namespace SQLModule.Host.Features.Schema.DataRecords;

public sealed class GetAllDataRecordsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Schema.DataRecords.Collection, Handle)
            .WithName("GetAllDataRecords")
            .WithTags("Schema")
            .Produces<PageResponse<CreateDataRecordEndpoint.Response>>()
            .ProducesValidationProblem()
            .AddEndpointFilter<ValidationFilter<Request>>();
    }

    public record Request([FromQuery] int Offset = 0, [FromQuery] int Limit = 20);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Limit).GreaterThan(0).LessThanOrEqualTo(100);
            RuleFor(x => x.Offset).GreaterThanOrEqualTo(0);
        }
    }

    private static async Task<Ok<PageResponse<CreateDataRecordEndpoint.Response>>> Handle(
        [AsParameters] Request request, AppDbContext db, CancellationToken ct)
    {
        var total = await db.DataRecords.CountAsync(ct);

        var entities = await db.DataRecords
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(ct);

        var items = entities.Select(CreateDataRecordEndpoint.ToResponse).ToList();

        return TypedResults.Ok(new PageResponse<CreateDataRecordEndpoint.Response> { Items = items, Count = total });
    }
}
