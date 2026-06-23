using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Sandbox;

namespace SQLModule.Host.Features.Schema.SchemaBuilder;

internal sealed record PreparedSchema(
    DbmsDictionary Dbms,
    IReadOnlyDictionary<Guid, PhysicalType> PhysicalTypes,
    SchemaSpec Spec,
    IReadOnlyList<string> Ddl);

internal interface ISchemaPreparer
{
    Task<Result<PreparedSchema>> PrepareAndValidateAsync(CreateSchemaRequest request, CancellationToken ct);
}
