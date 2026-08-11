using System.ComponentModel;
using System.Text.Json;
using DevBeast.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace DevBeast.Mcp.Server.Tools;

[McpServerToolType]
public sealed class DiagnosticsTools(ILogService logService)
{
    [McpServerTool]
    [Description("Returns aggregated recent application errors with stack traces from configured log sources.")]
    public async Task<string> GetRecentErrors(
        [Description("How many minutes back to search (e.g. 15).")] int timeWindowMinutes,
        [Description("Optional environment filter (e.g. 'Dev', 'Test'). Matches log file paths and JSON Environment field.")] string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var errors = await logService.GetRecentErrorsAsync(timeWindowMinutes, environment, cancellationToken);

        var payload = errors.Select(error => new
        {
            exceptionType = error.ExceptionType,
            message = error.Message,
            stackTrace = error.StackTrace,
            occurrenceCount = error.OccurrenceCount,
            firstSeen = error.FirstSeen,
            lastSeen = error.LastSeen,
            environment = error.Environment,
            sourceFiles = error.SourceFiles
        });

        return JsonSerializer.Serialize(new { errors = payload, count = errors.Count }, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
