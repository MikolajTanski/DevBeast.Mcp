using DevBeast.Mcp.Server.Models;

namespace DevBeast.Mcp.Server.Services;

public interface ILogService
{
    Task<IReadOnlyList<AggregatedError>> GetRecentErrorsAsync(
        int timeWindowMinutes,
        string? environment = null,
        CancellationToken cancellationToken = default);
}
