namespace DevBeast.Mcp.Server.Models;

public sealed record IndexInfo(
    string Name,
    bool IsUnique,
    bool IsPrimaryKey,
    IReadOnlyList<string> Columns);
