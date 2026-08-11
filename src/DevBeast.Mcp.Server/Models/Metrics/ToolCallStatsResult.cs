namespace DevBeast.Mcp.Server.Models.Metrics;

public sealed class ToolCallStatsResult
{
    public DateTimeOffset SessionStartedAt { get; init; }

    public long TotalCalls { get; init; }

    public long TotalErrors { get; init; }

    public IReadOnlyList<ToolCallMetricEntry> Tools { get; init; } = [];
}

public sealed class ToolCallMetricEntry
{
    public required string Name { get; init; }

    public long Calls { get; init; }

    public long Errors { get; init; }

    public long TotalDurationMs { get; init; }

    public double AvgDurationMs { get; init; }
}
