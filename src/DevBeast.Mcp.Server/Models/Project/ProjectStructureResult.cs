namespace DevBeast.Mcp.Server.Models;

public sealed record ProjectLayerInfo(
    string Name,
    string RelativePath,
    string? ProjectFile,
    IReadOnlyList<string> FeatureFolders);

public sealed record ProjectStructureResult(
    string RootPath,
    string NamespacePrefix,
    bool WasGenerated,
    bool HasManifest,
    string? ManifestPath,
    IReadOnlyList<ProjectLayerInfo> Layers,
    IReadOnlyList<string> SolutionFiles,
    IReadOnlyList<string> AllProjects,
    IReadOnlyList<string> DetectedFeatures);
