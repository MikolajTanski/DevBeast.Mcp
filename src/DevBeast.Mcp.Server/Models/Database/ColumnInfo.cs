namespace DevBeast.Mcp.Server.Models;

public sealed record ColumnInfo(
    string Name,
    string DataType,
    bool IsNullable,
    bool IsPrimaryKey,
    string? DefaultValue);
