namespace DevBeast.Mcp.Server.Models;

public sealed record EnvironmentDiffEntry(
    string KeyPath,
    string? DevValue,
    string? TestValue,
    string? ProdValue,
    string DiffType);
