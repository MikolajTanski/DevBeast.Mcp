using System.Collections.Concurrent;
using DevBeast.Mcp.Server.Models.Metrics;

namespace DevBeast.Mcp.Server.Services;

public sealed class ToolCallMetrics : IToolCallMetrics
{
    private readonly DateTimeOffset _sessionStartedAt = DateTimeOffset.UtcNow;
    private readonly ConcurrentDictionary<string, ToolCounter> _counters = new(StringComparer.OrdinalIgnoreCase);
    private long _totalCalls;
    private long _totalErrors;

    public void RecordCall(string toolName, TimeSpan duration, bool isError)
    {
        Interlocked.Increment(ref _totalCalls);
        if (isError)
        {
            Interlocked.Increment(ref _totalErrors);
        }

        var counter = _counters.GetOrAdd(toolName, static _ => new ToolCounter());
        counter.Record(duration, isError);
    }

    public ToolCallStatsResult GetStats()
    {
        var tools = _counters
            .OrderByDescending(pair => pair.Value.Calls)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new ToolCallMetricEntry
            {
                Name = pair.Key,
                Calls = pair.Value.Calls,
                Errors = pair.Value.Errors,
                TotalDurationMs = pair.Value.TotalDurationMs,
                AvgDurationMs = pair.Value.Calls == 0
                    ? 0
                    : Math.Round((double)pair.Value.TotalDurationMs / pair.Value.Calls, 2)
            })
            .ToList();

        return new ToolCallStatsResult
        {
            SessionStartedAt = _sessionStartedAt,
            TotalCalls = Interlocked.Read(ref _totalCalls),
            TotalErrors = Interlocked.Read(ref _totalErrors),
            Tools = tools
        };
    }

    public void Reset()
    {
        _counters.Clear();
        Interlocked.Exchange(ref _totalCalls, 0);
        Interlocked.Exchange(ref _totalErrors, 0);
    }

    private sealed class ToolCounter
    {
        private long _calls;
        private long _errors;
        private long _totalDurationMs;

        public long Calls => Interlocked.Read(ref _calls);

        public long Errors => Interlocked.Read(ref _errors);

        public long TotalDurationMs => Interlocked.Read(ref _totalDurationMs);

        public void Record(TimeSpan duration, bool isError)
        {
            Interlocked.Increment(ref _calls);
            Interlocked.Add(ref _totalDurationMs, (long)duration.TotalMilliseconds);
            if (isError)
            {
                Interlocked.Increment(ref _errors);
            }
        }
    }
}
