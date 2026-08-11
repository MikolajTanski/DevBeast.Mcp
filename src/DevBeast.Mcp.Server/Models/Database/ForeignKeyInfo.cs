namespace DevBeast.Mcp.Server.Models;

public sealed record ForeignKeyInfo(
    string Name,
    string Column,
    string ReferencedTable,
    string ReferencedColumn);
