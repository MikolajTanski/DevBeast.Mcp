namespace DevBeast.Mcp.Server.Models;

public sealed record DatabaseSchemaResult(
    string TableName,
    IReadOnlyList<ColumnInfo> Columns,
    IReadOnlyList<ForeignKeyInfo> ForeignKeys,
    IReadOnlyList<IndexInfo> Indexes);
