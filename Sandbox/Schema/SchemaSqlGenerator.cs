using System.Text;

namespace SQLModule.Sandbox;

internal sealed class SchemaSqlGenerator : ISchemaSqlGenerator
{
    public IReadOnlyList<string> GenerateDdl(ISqlSyntax syntax, SchemaSpec schema)
    {
        var columnIndex = BuildColumnIndex(schema);
        var result = new List<string>(schema.Tables.Count + schema.Relationships.Count);

        foreach (var table in schema.Tables)
        {
            result.Add(BuildCreateTable(syntax, table));
        }

        foreach (var rel in schema.Relationships)
        {
            if (columnIndex.TryGetValue(rel.SourceColumnKey, out var src) &&
                columnIndex.TryGetValue(rel.TargetColumnKey, out var tgt))
            {
                result.Add(BuildAlterTableFk(syntax, rel, src, tgt));
            }
        }

        return result;
    }

    public IReadOnlyList<string> GenerateInserts(ISqlSyntax syntax, SchemaSpec schema, DataSpec data)
    {
        var result = new List<string>();
        var tableIndex = schema.Tables.ToDictionary(t => t.Key);
        var dataIndex = data.Tables.ToDictionary(t => t.TableKey);

        foreach (var tableKey in BuildTopologicalOrder(schema))
        {
            if (!tableIndex.TryGetValue(tableKey, out var table))
            {
                continue;
            }

            if (!dataIndex.TryGetValue(tableKey, out var tableRows))
            {
                continue;
            }

            var columns = table.Columns.OrderBy(c => c.SortOrder).ToList();
            var quotedTable = syntax.QuoteIdentifier(table.Name);
            var quotedCols = String.Join(", ", columns.Select(c => syntax.QuoteIdentifier(c.Name)));

            foreach (var row in tableRows.Rows)
            {
                var cellIndex = row.Cells.ToDictionary(c => c.ColumnKey, c => c.Value);
                var values = String.Join(", ", columns.Select(c =>
                    syntax.FormatValue(cellIndex.GetValueOrDefault(c.Key))));
                result.Add($"INSERT INTO {quotedTable} ({quotedCols}) VALUES ({values});");
            }
        }

        return result;
    }

    private static string BuildCreateTable(ISqlSyntax syntax, TableSpec table)
    {
        var sb = new StringBuilder();
        var columns = table.Columns.OrderBy(c => c.SortOrder).ToList();
        var pkCols = columns.Where(c => c.IsPrimaryKey).ToList();

        sb.Append($"CREATE TABLE {syntax.QuoteIdentifier(table.Name)} (");

        var parts = new List<string>();
        foreach (var col in columns)
        {
            var nullPart = col.IsNullable ? String.Empty : " NOT NULL";
            parts.Add($"{syntax.QuoteIdentifier(col.Name)} {col.SqlType}{nullPart}");
        }

        if (pkCols.Count > 0)
        {
            var pkNames = String.Join(", ", pkCols.Select(c => syntax.QuoteIdentifier(c.Name)));
            parts.Add($"PRIMARY KEY ({pkNames})");
        }

        sb.Append(String.Join(", ", parts));
        sb.Append(");");
        return sb.ToString();
    }

    private static string BuildAlterTableFk(ISqlSyntax syntax, RelationshipSpec rel,
        ColumnIndexEntry src, ColumnIndexEntry tgt)
    {
        var constraintName = $"fk_{src.TableName}_{src.ColumnName}".ToLowerInvariant()
            .Replace(' ', '_');
        var sb = new StringBuilder();

        sb.Append($"ALTER TABLE {syntax.QuoteIdentifier(src.TableName)}");
        sb.Append($" ADD CONSTRAINT {syntax.QuoteIdentifier(constraintName)}");
        sb.Append($" FOREIGN KEY ({syntax.QuoteIdentifier(src.ColumnName)})");
        sb.Append($" REFERENCES {syntax.QuoteIdentifier(tgt.TableName)} ({syntax.QuoteIdentifier(tgt.ColumnName)})");

        if (!String.IsNullOrWhiteSpace(rel.DeleteRule))
        {
            sb.Append($" ON DELETE {rel.DeleteRule}");
        }

        if (!String.IsNullOrWhiteSpace(rel.UpdateRule))
        {
            sb.Append($" ON UPDATE {rel.UpdateRule}");
        }

        sb.Append(';');
        return sb.ToString();
    }

    private static Dictionary<string, ColumnIndexEntry> BuildColumnIndex(SchemaSpec schema)
    {
        var index = new Dictionary<string, ColumnIndexEntry>(StringComparer.Ordinal);
        foreach (var table in schema.Tables)
        {
            foreach (var col in table.Columns)
            {
                index[col.Key] = new ColumnIndexEntry(table.Key, table.Name, col.Name);
            }
        }
        return index;
    }

    private static List<string> BuildTopologicalOrder(SchemaSpec schema)
    {
        var columnToTable = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var table in schema.Tables)
        {
            foreach (var col in table.Columns)
            {
                columnToTable[col.Key] = table.Key;
            }
        }

        var adjacency = schema.Tables.ToDictionary(t => t.Key, _ => new HashSet<string>(StringComparer.Ordinal));
        var inDegree = schema.Tables.ToDictionary(t => t.Key, _ => 0);

        foreach (var rel in schema.Relationships)
        {
            if (!columnToTable.TryGetValue(rel.SourceColumnKey, out var srcKey) ||
                !columnToTable.TryGetValue(rel.TargetColumnKey, out var tgtKey) ||
                srcKey == tgtKey)
            {
                continue;
            }

            if (adjacency[tgtKey].Add(srcKey))
            {
                inDegree[srcKey]++;
            }
        }

        var queue = new Queue<string>(
            schema.Tables.Where(t => inDegree[t.Key] == 0).Select(t => t.Key));
        var result = new List<string>(schema.Tables.Count);
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);
            visited.Add(current);

            foreach (var successor in adjacency[current])
            {
                if (--inDegree[successor] == 0)
                {
                    queue.Enqueue(successor);
                }
            }
        }

        foreach (var table in schema.Tables)
        {
            if (!visited.Contains(table.Key))
            {
                result.Add(table.Key);
            }
        }

        return result;
    }

    private readonly record struct ColumnIndexEntry(string TableKey, string TableName, string ColumnName);
}
