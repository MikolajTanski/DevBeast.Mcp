namespace DevBeast.Mcp.Server.Models;

public sealed record AggregatedError(
    string ExceptionType,
    string Message,
    string? StackTrace,
    int OccurrenceCount,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    string? Environment,
    IReadOnlyList<string> SourceFiles);
