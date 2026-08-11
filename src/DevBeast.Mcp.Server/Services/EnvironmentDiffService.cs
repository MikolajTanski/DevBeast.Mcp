using System.Text.Json;
using DevBeast.Mcp.Server.Configuration;
using DevBeast.Mcp.Server.Models;
using Microsoft.Extensions.Options;

namespace DevBeast.Mcp.Server.Services;

public interface IEnvironmentDiffService
{
    Task<IReadOnlyList<EnvironmentDiffEntry>> DiffAppSettingsAsync(
        string? environmentsPath = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvironmentDiffEntry>> DiffDatabaseSchemasAsync(
        CancellationToken cancellationToken = default);
}

public sealed class EnvironmentDiffService(
    IOptions<DevBeastOptions> options,
    IDatabaseService databaseService) : IEnvironmentDiffService
{
    public Task<IReadOnlyList<EnvironmentDiffEntry>> DiffAppSettingsAsync(
        string? environmentsPath = null,
        CancellationToken cancellationToken = default)
    {
        var path = environmentsPath ?? ResolveEnvironmentsPath(options.Value.Integrations.MockDataPath);

        var dev = LoadFlatJson(Path.Combine(path, "appsettings.Dev.json"));
        var test = LoadFlatJson(Path.Combine(path, "appsettings.Test.json"));
        var prod = LoadFlatJson(Path.Combine(path, "appsettings.Prod.json"));

        var allKeys = dev.Keys.Union(test.Keys).Union(prod.Keys).OrderBy(k => k);
        var diffs = new List<EnvironmentDiffEntry>();

        foreach (var key in allKeys)
        {
            dev.TryGetValue(key, out var devVal);
            test.TryGetValue(key, out var testVal);
            prod.TryGetValue(key, out var prodVal);

            if (devVal == testVal && testVal == prodVal) continue;

            var diffType = prodVal is not null && devVal != prodVal ? "ProdMismatch" :
                           testVal is not null && devVal != testVal ? "TestMismatch" : "Missing";

            diffs.Add(new EnvironmentDiffEntry(key, devVal, testVal, prodVal, diffType));
        }

        return Task.FromResult<IReadOnlyList<EnvironmentDiffEntry>>(diffs);
    }

    public async Task<IReadOnlyList<EnvironmentDiffEntry>> DiffDatabaseSchemasAsync(
        CancellationToken cancellationToken = default)
    {
        // Mock: in real scenario would connect to Dev/Test/Prod DBs
        var tables = await databaseService.GetTableNamesAsync(cancellationToken);
        var diffs = new List<EnvironmentDiffEntry>();

        foreach (var table in tables)
        {
            diffs.Add(new EnvironmentDiffEntry(
                $"schema:{table}",
                "present",
                tables.Count > 1 ? "present" : "missing",
                "unknown (mock — no Prod connection)",
                "MockDiff"));
        }

        if (diffs.Count == 0)
        {
            diffs.Add(new EnvironmentDiffEntry(
                "schema:*",
                "no connection",
                "no connection",
                "no connection",
                "Configure database to enable real diff"));
        }

        return diffs;
    }

    private static string ResolveEnvironmentsPath(string mockDataPath)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, mockDataPath, "environments"),
            Path.Combine(Directory.GetCurrentDirectory(), mockDataPath, "environments")
        };

        return candidates.First(Directory.Exists);
    }

    private static Dictionary<string, string?> LoadFlatJson(string filePath)
    {
        if (!File.Exists(filePath)) return [];

        var json = File.ReadAllText(filePath);
        using var doc = JsonDocument.Parse(json);
        return FlattenJson(doc.RootElement, string.Empty);
    }

    private static Dictionary<string, string?> FlattenJson(JsonElement element, string prefix)
    {
        var result = new Dictionary<string, string?>();

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var key = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}:{prop.Name}";
                    foreach (var kvp in FlattenJson(prop.Value, key))
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
                break;
            default:
                result[prefix] = element.ToString();
                break;
        }

        return result;
    }
}
