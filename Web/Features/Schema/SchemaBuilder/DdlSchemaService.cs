using Microsoft.EntityFrameworkCore;
using SQLModule.Common.Results;
using SQLModule.Data.Core;
using SQLModule.Domain.DbmsCatalog;
using SQLModule.Sandbox;
using SQLModule.Web.Common.Sandbox;

namespace SQLModule.Web.Features.Schema.SchemaBuilder;

internal sealed record PreparedDdlSchema(
    DbmsDictionary Dbms,
    InspectedSchema Schema,
    IReadOnlyDictionary<string, PhysicalType> PhysicalTypes);

internal interface IDdlSchemaService
{
    Task<Result<PreparedDdlSchema>> ValidateAndInspectAsync(Guid dbmsId, string ddlScript, CancellationToken ct);
}

internal sealed class DdlSchemaService(AppDbContext db, ISandboxExecutor sandbox) : IDdlSchemaService
{
    public async Task<Result<PreparedDdlSchema>> ValidateAndInspectAsync(
        Guid dbmsId, string ddlScript, CancellationToken ct)
    {
        var safetyError = DdlSafetyPolicy.Validate(ddlScript);
        if (safetyError is not null)
        {
            return Result<PreparedDdlSchema>.Fail(safetyError);
        }

        var dbms = await db.DbmsDictionaries.AsNoTracking().SingleOrDefaultAsync(x => x.Id == dbmsId, ct);
        if (dbms is null)
        {
            return Result<PreparedDdlSchema>.Fail(SchemaErrors.DbmsNotFound(dbmsId));
        }

        var inspection = await sandbox.InspectDdlAsync(dbms.ToSandboxSpec(), ddlScript, ct);
        if (!inspection.IsSuccess)
        {
            return Result<PreparedDdlSchema>.Fail(inspection.Error!.Code == "Sandbox.ContainerFailed"
                ? Error.Unavailable("DbmsUnavailable", "Движок выбранной СУБД временно недоступен.")
                : Error.Validation("SchemaValidationFailed", "DDL-скрипт отклонён выбранной СУБД."));
        }

        if (inspection.Value!.Tables.Count == 0)
        {
            return Result<PreparedDdlSchema>.Fail(
                Error.Validation("SchemaValidationFailed", "DDL должен создать хотя бы одну таблицу."));
        }

        if (inspection.Value.Tables.Count > 100 ||
            inspection.Value.Tables.Any(x => x.Columns.Count > 200) ||
            inspection.Value.Relationships.Count > 500)
        {
            return Result<PreparedDdlSchema>.Fail(SchemaErrors.SchemaLimitExceeded);
        }

        var types = await db.PhysicalTypes.AsNoTracking().Include(x => x.ParameterDefinitions)
            .Where(x => x.DbmsId == dbmsId).ToListAsync(ct);
        var mapped = new Dictionary<string, PhysicalType>(StringComparer.OrdinalIgnoreCase);
        foreach (var storeType in inspection.Value.Tables.SelectMany(x => x.Columns).Select(x => x.StoreType).Distinct())
        {
            var physicalType = types.FirstOrDefault(x => TypeMatches(x.TypeName, storeType));
            if (physicalType is null)
            {
                return Result<PreparedDdlSchema>.Fail(Error.Validation(
                    "PhysicalTypeNotAllowed", $"Тип '{storeType}' отсутствует в каталоге выбранной СУБД."));
            }

            mapped[storeType] = physicalType;
        }

        return Result<PreparedDdlSchema>.Success(new PreparedDdlSchema(dbms, inspection.Value, mapped));
    }

    private static bool TypeMatches(string catalogType, string inspectedType)
    {
        static string Normalize(string value)
        {
            var bracket = value.IndexOf('(');
            value = bracket < 0 ? value : value[..bracket];
            return value.Trim().ToLowerInvariant() switch
            {
                "varchar" => "character varying",
                "char" => "character",
                "int" or "int4" => "integer",
                "int8" => "bigint",
                "int2" => "smallint",
                "bool" => "boolean",
                "float8" => "double precision",
                "float4" => "real",
                var normalized => normalized
            };
        }

        return Normalize(catalogType) == Normalize(inspectedType);
    }
}

internal static class DdlSafetyPolicy
{
    private static readonly HashSet<string> Forbidden = new(StringComparer.OrdinalIgnoreCase)
    {
        "INSERT", "MERGE", "SELECT", "CALL", "GRANT", "REVOKE",
        "DROP", "TRUNCATE", "COPY", "DO", "EXEC", "EXECUTE", "USER", "ROLE",
        "DATABASE", "SCHEMA", "EXTENSION", "FUNCTION", "PROCEDURE", "TRIGGER", "OWNER"
    };

    internal static Error? Validate(string sql)
    {
        var statements = Tokenize(sql).ChunkByStatement();
        if (statements.Count == 0)
        {
            return Error.Validation("SchemaValidationFailed", "DDL-скрипт пуст.");
        }

        foreach (var statement in statements)
        {
            if (statement.Any(Forbidden.Contains))
            {
                return Error.Validation("DdlCommandForbidden", "DDL содержит запрещённую команду.");
            }

            var allowed = statement.Count >= 2 && statement[0].Equals("CREATE", StringComparison.OrdinalIgnoreCase) &&
                          (statement[1].Equals("TABLE", StringComparison.OrdinalIgnoreCase) ||
                           statement[1].Equals("INDEX", StringComparison.OrdinalIgnoreCase) ||
                           statement.Count >= 3 && statement[1].Equals("UNIQUE", StringComparison.OrdinalIgnoreCase) &&
                           statement[2].Equals("INDEX", StringComparison.OrdinalIgnoreCase)) ||
                          statement.Count >= 4 && statement[0].Equals("ALTER", StringComparison.OrdinalIgnoreCase) &&
                          statement[1].Equals("TABLE", StringComparison.OrdinalIgnoreCase) &&
                          statement.Contains("FOREIGN", StringComparer.OrdinalIgnoreCase) &&
                          statement.Contains("KEY", StringComparer.OrdinalIgnoreCase);
            if (!allowed)
            {
                return Error.Validation("DdlCommandForbidden", "Разрешены только CREATE TABLE, CREATE INDEX и добавление FOREIGN KEY.");
            }
        }

        return null;
    }

    private static List<string> Tokenize(string sql)
    {
        var result = new List<string>();
        var token = new System.Text.StringBuilder();
        var quote = '\0';
        var lineComment = false;
        var blockComment = false;
        for (var i = 0; i < sql.Length; i++)
        {
            var c = sql[i];
            var next = i + 1 < sql.Length ? sql[i + 1] : '\0';
            if (lineComment)
            { if (c == '\n') { lineComment = false; } continue; }
            if (blockComment)
            { if (c == '*' && next == '/') { blockComment = false; i++; } continue; }
            if (quote != '\0')
            {
                if (c == quote && next == quote)
                { i++; continue; }
                if (c == quote)
                {
                    quote = '\0';
                }

                continue;
            }
            if (c == '-' && next == '-')
            { Flush(); lineComment = true; i++; continue; }
            if (c == '/' && next == '*')
            { Flush(); blockComment = true; i++; continue; }
            if (c is '\'' or '"' or '`')
            { Flush(); quote = c; continue; }
            if (Char.IsLetterOrDigit(c) || c == '_')
            {
                token.Append(c);
            }
            else
            {
                Flush();
                if (c == ';')
                {
                    result.Add(";");
                }
            }
        }
        Flush();
        return result;

        void Flush()
        {
            if (token.Length == 0)
            {
                return;
            }

            result.Add(token.ToString());
            token.Clear();
        }
    }

    private static List<List<string>> ChunkByStatement(this List<string> tokens)
    {
        var result = new List<List<string>>();
        var current = new List<string>();
        foreach (var token in tokens)
        {
            if (token == ";")
            { if (current.Count > 0) { result.Add(current); } current = []; }
            else
            {
                current.Add(token);
            }
        }
        if (current.Count > 0)
        {
            result.Add(current);
        }

        return result;
    }
}
