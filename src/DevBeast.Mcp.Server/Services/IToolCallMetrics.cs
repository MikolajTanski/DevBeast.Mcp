using DevBeast.Mcp.Server.Models.Metrics;

namespace DevBeast.Mcp.Server.Services;

public interface IToolCallMetrics
{
    void RecordCall(string toolName, TimeSpan duration, bool isError);

    ToolCallStatsResult GetStats();

    void Reset();
}
