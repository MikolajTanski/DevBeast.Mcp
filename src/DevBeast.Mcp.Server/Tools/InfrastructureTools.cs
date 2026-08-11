using System.ComponentModel;
using System.Text.Json;
using DevBeast.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace DevBeast.Mcp.Server.Tools;

[McpServerToolType]
public sealed class InfrastructureTools(
    ICacheService cacheService,
    IDeadLetterQueueService deadLetterQueue)
{
    [McpServerTool]
    [Description("Inspects Redis cache keys matching a pattern. Decodes JSON values. Falls back to mock store when Redis is unavailable.")]
    public async Task<string> InspectRedisCache(
        [Description("Key pattern, e.g. 'cache:*' or '*' (default).")] string? keyPattern = "*",
        CancellationToken cancellationToken = default)
    {
        var entries = await cacheService.InspectAsync(keyPattern, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            isMockMode = cacheService.IsMockMode,
            keyPattern,
            entryCount = entries.Count,
            entries = entries.Select(e => new
            {
                key = e.Key,
                value = e.Value,
                valueType = e.ValueType,
                ttlSeconds = e.Ttl?.TotalSeconds
            })
        }, JsonOptions);
    }

    [McpServerTool]
    [Description("Invalidates (flushes) a specific Redis cache key. Useful during debugging.")]
    public async Task<string> FlushKey(
        [Description("Exact cache key to delete, e.g. 'cache:products:all'.")] string key,
        CancellationToken cancellationToken = default)
    {
        var removed = await cacheService.FlushKeyAsync(key, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            key,
            removed,
            isMockMode = cacheService.IsMockMode
        }, JsonOptions);
    }

    [McpServerTool]
    [Description("Peeks messages from Dead Letter Queue (RabbitMQ/Service Bus mock or MongoDB deadLetterMessages collection).")]
    public async Task<string> PeekDeadLetterQueue(
        [Description("Optional queue name filter, e.g. 'orders.processing'.")] string? queueName = null,
        [Description("Max messages to return (default 20).")] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var messages = await deadLetterQueue.PeekAsync(queueName, limit, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            queueName,
            messageCount = messages.Count,
            messages = messages.Select(m => new
            {
                m.MessageId,
                m.Queue,
                m.Reason,
                payload = m.Payload,
                m.Error,
                m.FailedAt,
                m.RetryCount
            })
        }, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
