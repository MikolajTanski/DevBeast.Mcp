using DevBeast.Mcp.Server.Configuration;
using DevBeast.Mcp.Server.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DevBeast.Mcp.Server.Services;

public interface IDeadLetterQueueService
{
    Task<IReadOnlyList<DeadLetterMessage>> PeekAsync(
        string? queueName = null,
        int limit = 20,
        CancellationToken cancellationToken = default);
}

public sealed class MockDeadLetterQueueService(IOptions<DevBeastOptions> options) : IDeadLetterQueueService
{
    public async Task<IReadOnlyList<DeadLetterMessage>> PeekAsync(
        string? queueName = null,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        // Try MongoDB deadLetterMessages collection first (from docker init)
        try
        {
            var mongo = options.Value.Mongo;
            var client = new MongoClient(mongo.ConnectionString);
            var db = client.GetDatabase(mongo.DatabaseName);
            var collection = db.GetCollection<BsonDocument>("deadLetterMessages");

            var filter = string.IsNullOrWhiteSpace(queueName)
                ? FilterDefinition<BsonDocument>.Empty
                : Builders<BsonDocument>.Filter.Eq("queue", queueName);

            var docs = await collection.Find(filter).Limit(limit).ToListAsync(cancellationToken);

            if (docs.Count > 0)
            {
                return docs.Select(MapFromBson).ToList();
            }
        }
        catch
        {
            // Fall through to hardcoded mock
        }

        return GetHardcodedMock(queueName, limit);
    }

    private static DeadLetterMessage MapFromBson(BsonDocument doc) => new(
        doc.GetValue("messageId", "unknown").ToString()!,
        doc.GetValue("queue", "unknown").ToString()!,
        doc.GetValue("reason", "unknown").ToString()!,
        doc.GetValue("payload", new BsonDocument()).ToJson(),
        doc.GetValue("error", "unknown").ToString()!,
        doc.Contains("failedAt") && doc["failedAt"].BsonType == BsonType.DateTime
            ? doc["failedAt"].ToUniversalTime()
            : DateTimeOffset.UtcNow,
        doc.Contains("retryCount") ? doc["retryCount"].AsInt32 : 0);

    private static IReadOnlyList<DeadLetterMessage> GetHardcodedMock(string? queueName, int limit)
    {
        var messages = new List<DeadLetterMessage>
        {
            new("msg-mock-001", "orders.processing", "InvalidOrderState",
                """{"orderNumber":"ORD-2025-002","action":"ProcessPayment"}""",
                "Order is in Pending state, expected Confirmed",
                DateTimeOffset.UtcNow.AddMinutes(-30), 3),
            new("msg-mock-002", "inventory.sync", "DeserializationError",
                """{"sku":"SKU-999","delta":"not-a-number"}""",
                "JSON deserialization failed for field delta",
                DateTimeOffset.UtcNow.AddHours(-2), 5)
        };

        if (!string.IsNullOrWhiteSpace(queueName))
        {
            messages = messages.Where(m => m.Queue.Equals(queueName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return messages.Take(limit).ToList();
    }
}
