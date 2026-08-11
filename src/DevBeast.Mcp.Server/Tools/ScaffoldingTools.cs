using System.ComponentModel;
using System.Text.Json;
using DevBeast.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace DevBeast.Mcp.Server.Tools;

[McpServerToolType]
public sealed class ScaffoldingTools(IFeatureSliceScaffolder scaffolder)
{
    [McpServerTool]
    [Description("Scaffolds a complete Vertical Slice: Domain entity, CQRS (MediatR), EF migration, API controller, AutoMapper profile, and unit tests.")]
    public async Task<string> ScaffoldFeatureSlice(
        [Description("Feature name, e.g. 'Product' or 'GetProductsByCategory'.")] string featureName,
        [Description("Target project root path where src/ and tests/ folders will be created.")] string? projectPath = null,
        CancellationToken cancellationToken = default)
    {
        var files = await scaffolder.ScaffoldAsync(featureName, projectPath, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            featureName,
            projectPath = projectPath ?? "(from config)",
            createdFileCount = files.Count,
            createdFiles = files
        }, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
