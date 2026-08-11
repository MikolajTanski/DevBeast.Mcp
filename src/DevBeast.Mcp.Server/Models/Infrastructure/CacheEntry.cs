namespace DevBeast.Mcp.Server.Models;

public sealed record CacheEntry(
    string Key,
    string? Value,
    string? ValueType,
    TimeSpan? Ttl);
