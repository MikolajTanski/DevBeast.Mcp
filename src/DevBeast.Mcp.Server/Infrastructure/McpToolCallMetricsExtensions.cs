using System.Diagnostics;
using DevBeast.Mcp.Server.Configuration;
using DevBeast.Mcp.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace DevBeast.Mcp.Server.Infrastructure;

public static class McpToolCallMetricsExtensions
{
    public static IMcpServerBuilder WithToolCallMetrics(this IMcpServerBuilder builder)
    {
        return builder.WithRequestFilters(filters =>
        {
            filters.AddCallToolFilter(next => async (context, cancellationToken) =>
            {
                var services = context.Services
                    ?? throw new InvalidOperationException("CallTool filter requires a service provider.");

                var metricsOptions = services.GetService<IOptions<DevBeastOptions>>()?.Value.Metrics;
                if (metricsOptions is { Enabled: false })
                {
                    return await next(context, cancellationToken);
                }

                var metrics = services.GetRequiredService<IToolCallMetrics>();
                var logger = services.GetService<ILoggerFactory>()?.CreateLogger("DevBeast.Mcp.Metrics");
                var toolName = context.Params.Name;
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    var result = await next(context, cancellationToken);
                    stopwatch.Stop();

                    var isError = result.IsError.GetValueOrDefault();
                    metrics.RecordCall(toolName, stopwatch.Elapsed, isError);

                    if (metricsOptions?.LogEachCall == true)
                    {
                        logger?.LogInformation(
                            "MCP tool {ToolName} completed in {ElapsedMs}ms (error={IsError})",
                            toolName,
                            stopwatch.ElapsedMilliseconds,
                            isError);
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    metrics.RecordCall(toolName, stopwatch.Elapsed, isError: true);

                    if (metricsOptions?.LogEachCall == true)
                    {
                        logger?.LogError(
                            ex,
                            "MCP tool {ToolName} failed after {ElapsedMs}ms",
                            toolName,
                            stopwatch.ElapsedMilliseconds);
                    }

                    throw;
                }
            });
        });
    }
}
