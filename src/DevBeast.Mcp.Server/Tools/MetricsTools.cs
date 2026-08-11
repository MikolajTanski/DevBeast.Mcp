using System.ComponentModel;
using System.Text.Json;
using DevBeast.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace DevBeast.Mcp.Server.Tools;

[McpServerToolType]
public sealed class MetricsTools(IToolCallMetrics metrics)
{
    [McpServerTool]
    [Description("Returns MCP tool call counters for the current server session: total calls, errors, per-tool counts and average duration.")]
    public Task<string> GetToolCallStats(
        [Description("When true, resets counters after returning current stats.")] bool reset = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stats = metrics.GetStats();
        var payload = new
        {
            sessionStartedAt = stats.SessionStartedAt,
            totalCalls = stats.TotalCalls,
            totalErrors = stats.TotalErrors,
            tools = stats.Tools.Select(tool => new
            {
                name = tool.Name,
                calls = tool.Calls,
                errors = tool.Errors,
                totalDurationMs = tool.TotalDurationMs,
                avgDurationMs = tool.AvgDurationMs
            })
        };

        if (reset)
        {
            metrics.Reset();
        }

        return Task.FromResult(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
