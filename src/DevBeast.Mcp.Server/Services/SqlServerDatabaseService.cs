using DevBeast.Mcp.Server.Configuration;
using DevBeast.Mcp.Server.Models;
using DevBeast.Mcp.Server.Security;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace DevBeast.Mcp.Server.Services;

public sealed class SqlServerDatabaseService(IOptions<DevBeastOptions> options) : IDatabaseService
{
    private string ConnectionString =>
        options.Value.Database.ConnectionString
        ?? throw new InvalidOperationException("Database connection string is not configured.");

    public async Task<IReadOnlyList<string>> GetTableNamesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TABLE_SCHEMA + '.' + TABLE_NAME AS FullName
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_SCHEMA, TABLE_NAME
            """;

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var tables = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    public async Task<DatabaseSchemaResult> GetTableSchemaAsync(string tableName, CancellationToken cancellationToken = default)
    {
        var (schema, table) = ParseTableName(tableName);

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var columns = await GetColumnsAsync(connection, schema, table, cancellationToken);
        var foreignKeys = await GetForeignKeysAsync(connection, schema, table, cancellationToken);
        var indexes = await GetIndexesAsync(connection, schema, table, cancellationToken);

        return new DatabaseSchemaResult($"{schema}.{table}", columns, foreignKeys, indexes);
    }

    public async Task<QueryResult> ExecuteReadQueryAsync(string sqlQuery, CancellationToken cancellationToken = default)
    {
        SqlQueryValidator.EnsureReadOnly(sqlQuery);

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sqlQuery, connection)
        {
            CommandTimeout = 30
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var columns = Enumerable.Range(0, reader.FieldCount)
            .Select(reader.GetName)
            .ToList();

        var rows = new List<IReadOnlyList<object?>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new object?[reader.FieldCount];
            reader.GetValues(row);
            rows.Add(row);
        }

        return new QueryResult(columns, rows, rows.Count);
    }

    private static (string Schema, string Table) ParseTableName(string tableName)
    {
        var parts = tableName.Split('.', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return parts.Length switch
        {
            2 => (parts[0], parts[1]),
            1 => ("dbo", parts[0]),
            _ => throw new ArgumentException($"Invalid table name: '{tableName}'", nameof(tableName))
        };
    }

    private static async Task<IReadOnlyList<ColumnInfo>> GetColumnsAsync(
        SqlConnection connection,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.IS_NULLABLE,
                c.COLUMN_DEFAULT,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                    ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                    AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA
                    AND tc.TABLE_NAME = ku.TABLE_NAME
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            ) pk ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA
                AND c.TABLE_NAME = pk.TABLE_NAME
                AND c.COLUMN_NAME = pk.COLUMN_NAME
            WHERE c.TABLE_SCHEMA = @Schema AND c.TABLE_NAME = @Table
            ORDER BY c.ORDINAL_POSITION
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Schema", schema);
        command.Parameters.AddWithValue("@Table", table);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var columns = new List<ColumnInfo>();

        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnInfo(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2).Equals("YES", StringComparison.OrdinalIgnoreCase),
                reader.GetInt32(4) == 1,
                reader.IsDBNull(3) ? null : reader.GetString(3)));
        }

        return columns;
    }

    private static async Task<IReadOnlyList<ForeignKeyInfo>> GetForeignKeysAsync(
        SqlConnection connection,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                fk.name AS ForeignKeyName,
                cp.name AS ColumnName,
                rs.name + '.' + rt.name AS ReferencedTable,
                cr.name AS ReferencedColumn
            FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            JOIN sys.tables t ON fkc.parent_object_id = t.object_id
            JOIN sys.schemas s ON t.schema_id = s.schema_id
            JOIN sys.columns cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
            JOIN sys.columns cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
            JOIN sys.tables rt ON fkc.referenced_object_id = rt.object_id
            JOIN sys.schemas rs ON rt.schema_id = rs.schema_id
            WHERE s.name = @Schema AND t.name = @Table
            ORDER BY fk.name, fkc.constraint_column_id
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Schema", schema);
        command.Parameters.AddWithValue("@Table", table);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var foreignKeys = new List<ForeignKeyInfo>();

        while (await reader.ReadAsync(cancellationToken))
        {
            foreignKeys.Add(new ForeignKeyInfo(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3)));
        }

        return foreignKeys;
    }

    private static async Task<IReadOnlyList<IndexInfo>> GetIndexesAsync(
        SqlConnection connection,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                i.name AS IndexName,
                i.is_unique AS IsUnique,
                i.is_primary_key AS IsPrimaryKey,
                STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS Columns
            FROM sys.indexes i
            JOIN sys.tables t ON i.object_id = t.object_id
            JOIN sys.schemas s ON t.schema_id = s.schema_id
            JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
            WHERE s.name = @Schema AND t.name = @Table AND i.name IS NOT NULL
            GROUP BY i.name, i.is_unique, i.is_primary_key
            ORDER BY i.is_primary_key DESC, i.name
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Schema", schema);
        command.Parameters.AddWithValue("@Table", table);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var indexes = new List<IndexInfo>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var columnList = reader.GetString(3)
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            indexes.Add(new IndexInfo(
                reader.GetString(0),
                reader.GetBoolean(1),
                reader.GetBoolean(2),
                columnList));
        }

        return indexes;
    }
}
