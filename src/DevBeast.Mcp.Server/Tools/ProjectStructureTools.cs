using System.ComponentModel;
using System.Text.Json;
using DevBeast.Mcp.Server.Configuration;
using DevBeast.Mcp.Server.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace DevBeast.Mcp.Server.Tools;

[McpServerToolType]
public sealed class ProjectStructureTools(
    IProjectStructureService projectStructureService,
    IOptions<DevBeastOptions> options)
{
    [McpServerTool]
    [Description("Reads project structure from repo (.devbeast/project-structure.json) or scans existing layout. Generates Clean Architecture skeleton if missing.")]
    public async Task<string> EnsureProjectStructure(
        [Description("Project root path. Uses DefaultProjectPath if omitted.")] string? projectPath = null,
        [Description("When true (default), generates Domain/Application/Infrastructure/Api/Tests skeleton if structure is missing.")] bool generateIfMissing = true,
        [Description("Namespace prefix for generation, e.g. 'App' or 'Shop'. Uses config default if omitted.")] string? namespacePrefix = null,
        CancellationToken cancellationToken = default)
    {
        var result = await projectStructureService.EnsureStructureAsync(
            projectPath, generateIfMissing, namespacePrefix, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            rootPath = result.RootPath,
            namespacePrefix = result.NamespacePrefix,
            wasGenerated = result.WasGenerated,
            hasManifest = result.HasManifest,
            manifestPath = result.ManifestPath,
            layers = result.Layers.Select(l => new
            {
                name = l.Name,
                path = l.RelativePath,
                project = l.ProjectFile,
                features = l.FeatureFolders
            }),
            solutionFiles = result.SolutionFiles,
            projects = result.AllProjects,
            detectedFeatures = result.DetectedFeatures,
            hint = result.WasGenerated
                ? "Structure was generated. Use manifest paths for scaffold_feature_slice and validate_architecture_rules."
                : "Use layer paths from manifest when creating or modifying files."
        }, JsonOptions);
    }

    [McpServerTool]
    [Description("Returns only the saved project structure manifest without generating files.")]
    public async Task<string> GetProjectStructure(
        [Description("Project root path. Uses DefaultProjectPath if omitted.")] string? projectPath = null,
        CancellationToken cancellationToken = default)
    {
        var path = projectPath
            ?? options.Value.DefaultProjectPath
            ?? throw new InvalidOperationException("Provide projectPath or set DevBeast:DefaultProjectPath.");

        var result = await projectStructureService.EnsureStructureAsync(
            path, generateIfMissing: false, cancellationToken: cancellationToken);

        return JsonSerializer.Serialize(new
        {
            rootPath = result.RootPath,
            namespacePrefix = result.NamespacePrefix,
            hasManifest = result.HasManifest,
            manifestPath = result.ManifestPath,
            layers = result.Layers,
            projects = result.AllProjects,
            detectedFeatures = result.DetectedFeatures
        }, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
