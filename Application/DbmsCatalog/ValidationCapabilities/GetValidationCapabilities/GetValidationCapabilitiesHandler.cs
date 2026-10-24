using Microsoft.EntityFrameworkCore;
using SQLModule.Application.Training.Validation;
using Microsoft.Extensions.Logging;
using SQLModule.Common.Results;
using SQLModule.Contracts.DbmsCatalog.Validation;
using SQLModule.Application.Persistence;
using SQLModule.Domain.Training;
using SQLModule.Domain.Training.Validation;
using SQLModule.Application.Cqrs;

namespace SQLModule.Application.DbmsCatalog.ValidationCapabilities.GetValidationCapabilities;

public sealed record GetValidationCapabilitiesQuery(Guid DbmsId)
    : IRequest<Result<DbmsValidationCapabilitiesResponse>>;

public sealed class GetValidationCapabilitiesHandler(
    IApplicationDbContext db,
    ISqlSyntaxAnalyzerResolver analyzerResolver,
    ILogger<GetValidationCapabilitiesHandler> logger)
    : IRequestHandler<GetValidationCapabilitiesQuery, Result<DbmsValidationCapabilitiesResponse>>
{
    private static readonly IReadOnlyList<ValidationCheckKind> AstCheckKinds =
    [
        ValidationCheckKind.RequiredConstruct,
        ValidationCheckKind.ForbiddenConstruct,
        ValidationCheckKind.RequiredTable,
        ValidationCheckKind.ForbiddenTable
    ];

    private static readonly IReadOnlyList<HintGroup> AstHintGroups =
    [
        HintGroup.RequiredConstructs,
        HintGroup.ForbiddenConstructs,
        HintGroup.RequiredTables,
        HintGroup.ForbiddenTables
    ];

    public async Task<Result<DbmsValidationCapabilitiesResponse>> Handle(
        GetValidationCapabilitiesQuery query,
        CancellationToken ct)
    {
        var dbmsSystemName = await db.DbmsDictionaries
            .AsNoTracking()
            .Where(dbms => dbms.Id == query.DbmsId)
            .Select(dbms => dbms.DbmsSystemName)
            .SingleOrDefaultAsync(ct);

        if (dbmsSystemName is null)
        {
            return Result<DbmsValidationCapabilitiesResponse>.Fail(
                ValidationCapabilitiesErrors.DbmsNotFound(query.DbmsId));
        }

        var analyzer = analyzerResolver.Resolve(dbmsSystemName);
        if (analyzer is null)
        {
            logger.LogWarning(
                "Для СУБД {DbmsId} с системным именем {DbmsSystemName} не зарегистрирован AST-анализатор",
                query.DbmsId,
                dbmsSystemName);
            return Result<DbmsValidationCapabilitiesResponse>.Fail(
                ValidationCapabilitiesErrors.AnalyzerNotSupported(dbmsSystemName));
        }

        var supportedConstructs = Enum.GetValues<SqlConstruct>()
            .Where(analyzer.SupportedConstructs.Contains)
            .ToArray();

        return new DbmsValidationCapabilitiesResponse(
            query.DbmsId,
            [ValidationCheckKind.MainDatasetResult.ToContract(), .. AstCheckKinds.Select(value => value.ToContract())],
            supportedConstructs.Select(value => value.ToContract()).ToArray(),
            [HintGroup.Result.ToContract(), .. AstHintGroups.Select(value => value.ToContract())],
            analyzer.AnalyzerVersion);
    }
}
