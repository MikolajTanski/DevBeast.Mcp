using System.ComponentModel;
using System.Text.Json;
using DevBeast.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace DevBeast.Mcp.Server.Tools;

[McpServerToolType]
public sealed class DataTools(
    IFixtureGeneratorService fixtureGenerator,
    IEnvironmentDiffService environmentDiff)
{
    [McpServerTool]
    [Description("Generates realistic C# test fixtures (Bogus) based on database schema, respecting column types and FK relationships.")]
    public async Task<string> GenerateTestFixtures(
        [Description("Table or collection name.")] string tableName,
        [Description("Number of fixtures to generate (1-500, default 10).")] int count = 10,
        CancellationToken cancellationToken = default)
    {
        var code = await fixtureGenerator.GenerateAsync(tableName, count, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            tableName,
            count,
            language = "csharp",
            generator = "Bogus",
            code
        }, JsonOptions);
    }

    [McpServerTool]
    [Description("Compares appsettings.json or database schema between Dev, Test, and Prod environments.")]
    public async Task<string> DiffEnvironments(
        [Description("Diff mode: 'appsettings' or 'database'.")] string mode = "appsettings",
        [Description("Path to environments folder with appsettings.{Dev,Test,Prod}.json files.")] string? environmentsPath = null,
        CancellationToken cancellationToken = default)
    {
        var diffs = mode.Equals("database", StringComparison.OrdinalIgnoreCase)
            ? await environmentDiff.DiffDatabaseSchemasAsync(cancellationToken)
            : await environmentDiff.DiffAppSettingsAsync(environmentsPath, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            mode,
            diffCount = diffs.Count,
            diffs = diffs.Select(d => new
            {
                key = d.KeyPath,
                dev = d.DevValue,
                test = d.TestValue,
                prod = d.ProdValue,
                diffType = d.DiffType
            })
        }, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
