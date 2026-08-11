using DevBeast.Mcp.Server.Configuration;
using DevBeast.Mcp.Server.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DevBeast.Mcp.Server.Services;

public sealed class MongoDatabaseService(IOptions<DevBeastOptions> options) : IDatabaseService
{
    private IMongoDatabase GetDatabase()
    {
        var mongo = options.Value.Mongo;
        var client = new MongoClient(mongo.ConnectionString);
        return client.GetDatabase(mongo.DatabaseName);
    }

    public async Task<IReadOnlyList<string>> GetTableNamesAsync(CancellationToken cancellationToken = default)
    {
        var db = GetDatabase();
        var collections = await db.ListCollectionNamesAsync(cancellationToken: cancellationToken);
        return await collections.ToListAsync(cancellationToken);
    }

    public async Task<DatabaseSchemaResult> GetTableSchemaAsync(string tableName, CancellationToken cancellationToken = default)
    {
        var db = GetDatabase();
        var collection = db.GetCollection<BsonDocument>(tableName);

        var sample = await collection.Find(FilterDefinition<BsonDocument>.Empty)
            .Limit(5)
            .ToListAsync(cancellationToken);

        var columns = InferColumnsFromSamples(sample);
        return new DatabaseSchemaResult(tableName, columns, [], []);
    }

    public async Task<QueryResult> ExecuteReadQueryAsync(string sqlQuery, CancellationToken cancellationToken = default)
    {
        // Mongo uses JSON find syntax: {"collection":"orders","filter":{"status":"Pending"},"limit":10}
        using var doc = System.Text.Json.JsonDocument.Parse(sqlQuery);
        var root = doc.RootElement;

        var collectionName = root.GetProperty("collection").GetString()
            ?? throw new InvalidOperationException("Mongo query requires 'collection' field.");

        var limit = root.TryGetProperty("limit", out var limitProp) ? limitProp.GetInt32() : 100;
        var filterJson = root.TryGetProperty("filter", out var filterProp)
            ? filterProp.GetRawText()
            : "{}";

        var filter = BsonDocument.Parse(filterJson);
        var db = GetDatabase();
        var collection = db.GetCollection<BsonDocument>(collectionName);

        var results = await collection.Find(filter).Limit(limit).ToListAsync(cancellationToken);

        if (results.Count == 0)
        {
            return new QueryResult([], [], 0);
        }

        var columns = results.SelectMany(r => r.Names).Distinct().OrderBy(n => n).ToList();
        var rows = results.Select(doc =>
        {
            var row = new object?[columns.Count];
            for (var i = 0; i < columns.Count; i++)
            {
                row[i] = doc.Contains(columns[i]) ? doc[columns[i]].ToString() : null;
            }
            return (IReadOnlyList<object?>)row;
        }).ToList();

        return new QueryResult(columns, rows, rows.Count);
    }

    private static IReadOnlyList<ColumnInfo> InferColumnsFromSamples(IReadOnlyList<BsonDocument> samples)
    {
        var fieldTypes = new Dictionary<string, HashSet<string>>();

        foreach (var doc in samples)
        {
            foreach (var element in doc.Elements)
            {
                if (!fieldTypes.TryGetValue(element.Name, out var types))
                {
                    types = [];
                    fieldTypes[element.Name] = types;
                }

                types.Add(element.Value.BsonType.ToString());
            }
        }

        return fieldTypes
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new ColumnInfo(
                kvp.Key,
                string.Join("|", kvp.Value),
                true,
                kvp.Key == "_id",
                null))
            .ToList();
    }
}
