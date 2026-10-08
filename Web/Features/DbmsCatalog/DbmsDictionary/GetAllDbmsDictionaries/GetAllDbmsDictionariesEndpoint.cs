using SQLModule.Application.DbmsCatalog.DbmsDictionary;
using SQLModule.Application.DbmsCatalog.DbmsDictionary.GetAllDbmsDictionaries;
﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using SQLModule.Domain.Common;
using SQLModule.Common.Results;
using SQLModule.Contracts;
using SQLModule.Contracts.DbmsCatalog.DbmsDictionary;
using SQLModule.Web.Common;
using SQLModule.Application.Cqrs;

namespace SQLModule.Web.Features.DbmsCatalog.DbmsDictionary.GetAllDbmsDictionaries;

internal sealed class GetAllDbmsDictionariesEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.DbmsCatalog.DbmsDictionaries.Collection, Handle)
            .RequireAuthorization(Policies.ContentAuthor)
            .WithName("GetAllDbmsDictionaries")
            .WithTags("DbmsCatalog")
            .Produces<PageResponse<DbmsDictionaryResponse>>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter<ValidationFilter<GetAllDbmsDictionariesRequest>>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] GetAllDbmsDictionariesRequest request, ClaimsPrincipal user, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send<GetAllDbmsDictionariesQuery, Result<PageResponse<DbmsDictionaryResponse>>>(
            new GetAllDbmsDictionariesQuery(request.Offset, request.Limit, user.IsInRole(Roles.Admin)), ct);
        return result.ToOk();
    }
}
