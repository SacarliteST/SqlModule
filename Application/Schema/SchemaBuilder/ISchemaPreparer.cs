using SQLModule.Common.Results;
using SQLModule.Contracts.Schema.SchemaBuilder;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Sandbox;

namespace SQLModule.Application.Schema.SchemaBuilder;

public sealed record PreparedSchema(
    DbmsDictionary Dbms,
    IReadOnlyDictionary<Guid, PhysicalType> PhysicalTypes,
    SchemaSpec Spec,
    IReadOnlyList<string> Ddl);

public interface ISchemaPreparer
{
    Task<Result<PreparedSchema>> PrepareAndValidateAsync(CreateSchemaRequest request, CancellationToken ct);
}
