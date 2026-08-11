namespace DevBeast.Mcp.Server.Models;

public sealed record DatabaseSchemaResult(
    string TableName,
    IReadOnlyList<ColumnInfo> Columns,
    IReadOnlyList<ForeignKeyInfo> ForeignKeys,
    IReadOnlyList<IndexInfo> Indexes);

public sealed record ColumnInfo(
    string Name,
    string DataType,
    bool IsNullable,
    bool IsPrimaryKey,
    string? DefaultValue);

public sealed record ForeignKeyInfo(
    string Name,
    string Column,
    string ReferencedTable,
    string ReferencedColumn);

public sealed record IndexInfo(
    string Name,
    bool IsUnique,
    bool IsPrimaryKey,
    IReadOnlyList<string> Columns);

public sealed record QueryResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    int RowCount);

public sealed record AggregatedError(
    string ExceptionType,
    string Message,
    string? StackTrace,
    int OccurrenceCount,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    string? Environment,
    IReadOnlyList<string> SourceFiles);
