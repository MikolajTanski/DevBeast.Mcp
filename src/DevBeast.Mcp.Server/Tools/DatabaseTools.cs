using System.ComponentModel;
using System.Text.Json;
using DevBeast.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace DevBeast.Mcp.Server.Tools;

[McpServerToolType]
public sealed class DatabaseTools(IDatabaseService databaseService)
{
    [McpServerTool]
    [Description("Returns database schema metadata: columns, data types, foreign keys, and indexes. Use tableName='*' for all tables.")]
    public async Task<string> GetDatabaseSchema(
        [Description("Table name (e.g. 'Users' or 'dbo.Orders') or '*' for all tables.")] string tableName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("tableName is required.", nameof(tableName));
        }

        if (tableName.Trim() == "*")
        {
            var tables = await databaseService.GetTableNamesAsync(cancellationToken);
            var schemas = new List<object>();

            foreach (var table in tables)
            {
                var schema = await databaseService.GetTableSchemaAsync(table, cancellationToken);
                schemas.Add(ToSchemaObject(schema));
            }

            return JsonSerializer.Serialize(new { tables = schemas }, JsonOptions);
        }

        var singleSchema = await databaseService.GetTableSchemaAsync(tableName.Trim(), cancellationToken);
        return JsonSerializer.Serialize(ToSchemaObject(singleSchema), JsonOptions);
    }

    [McpServerTool]
    [Description("Executes a read-only query. SQL Server: SELECT/WITH SQL. MongoDB: JSON {\"collection\":\"orders\",\"filter\":{},\"limit\":10}.")]
    public async Task<string> ExecuteReadQuery(
        [Description("A SELECT (or WITH ... SELECT) SQL query.")] string sqlQuery,
        CancellationToken cancellationToken = default)
    {
        var result = await databaseService.ExecuteReadQueryAsync(sqlQuery, cancellationToken);

        var payload = new
        {
            columns = result.Columns,
            rowCount = result.RowCount,
            rows = result.Rows.Select(row => result.Columns.Zip(row, (column, value) => new KeyValuePair<string, object?>(column, value))
                .ToDictionary(pair => pair.Key, pair => pair.Value))
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static object ToSchemaObject(Models.DatabaseSchemaResult schema) => new
    {
        tableName = schema.TableName,
        columns = schema.Columns.Select(c => new
        {
            name = c.Name,
            dataType = c.DataType,
            isNullable = c.IsNullable,
            isPrimaryKey = c.IsPrimaryKey,
            defaultValue = c.DefaultValue
        }),
        foreignKeys = schema.ForeignKeys.Select(fk => new
        {
            name = fk.Name,
            column = fk.Column,
            referencedTable = fk.ReferencedTable,
            referencedColumn = fk.ReferencedColumn
        }),
        indexes = schema.Indexes.Select(i => new
        {
            name = i.Name,
            isUnique = i.IsUnique,
            isPrimaryKey = i.IsPrimaryKey,
            columns = i.Columns
        })
    };

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
