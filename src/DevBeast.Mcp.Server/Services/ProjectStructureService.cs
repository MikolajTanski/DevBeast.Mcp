using System.Text.Json;
using DevBeast.Mcp.Server.Configuration;
using DevBeast.Mcp.Server.Models;
using Microsoft.Extensions.Options;

namespace DevBeast.Mcp.Server.Services;

public interface IProjectStructureService
{
    Task<ProjectStructureResult> EnsureStructureAsync(
        string? projectPath = null,
        bool generateIfMissing = true,
        string? namespacePrefix = null,
        CancellationToken cancellationToken = default);
}

public sealed class ProjectStructureService(IOptions<DevBeastOptions> options) : IProjectStructureService
{
    private const string ManifestFolder = ".devbeast";
    private const string ManifestFileName = "project-structure.json";

    private static readonly string[] LayerSuffixes =
        ["Domain", "Application", "Infrastructure", "Api", "Web"];

    public async Task<ProjectStructureResult> EnsureStructureAsync(
        string? projectPath = null,
        bool generateIfMissing = true,
        string? namespacePrefix = null,
        CancellationToken cancellationToken = default)
    {
        var root = ResolveRoot(projectPath);
        var ns = namespacePrefix
            ?? options.Value.Scaffolding.NamespacePrefix
            ?? InferNamespaceFromPath(root);

        Directory.CreateDirectory(root);

        var manifestPath = Path.Combine(root, ManifestFolder, ManifestFileName);
        var wasGenerated = false;

        if (File.Exists(manifestPath))
        {
            var fromManifest = await LoadFromManifestAsync(manifestPath, root, cancellationToken);
            if (fromManifest is not null && IsStructureComplete(fromManifest))
            {
                return fromManifest with { HasManifest = true, ManifestPath = manifestPath };
            }
        }

        var scanned = ScanExistingStructure(root, ns);

        if (IsStructureComplete(scanned))
        {
            await SaveManifestAsync(manifestPath, scanned, cancellationToken);
            return scanned with { HasManifest = true, ManifestPath = manifestPath };
        }

        if (!generateIfMissing)
        {
            return scanned with
            {
                HasManifest = File.Exists(manifestPath),
                ManifestPath = File.Exists(manifestPath) ? manifestPath : null
            };
        }

        wasGenerated = true;
        await GenerateCleanArchitectureSkeletonAsync(root, ns, cancellationToken);
        scanned = ScanExistingStructure(root, ns);
        await SaveManifestAsync(manifestPath, scanned, cancellationToken);

        return scanned with
        {
            WasGenerated = wasGenerated,
            HasManifest = true,
            ManifestPath = manifestPath
        };
    }

    public static string? GetLayerPath(ProjectStructureResult structure, string layerName) =>
        structure.Layers.FirstOrDefault(l =>
            l.Name.Equals(layerName, StringComparison.OrdinalIgnoreCase))?.RelativePath;

    private string ResolveRoot(string? projectPath)
    {
        var root = projectPath
            ?? options.Value.DefaultProjectPath;

        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException("Provide projectPath or set DevBeast:DefaultProjectPath.");
        }

        return Path.GetFullPath(root);
    }

    private static string InferNamespaceFromPath(string root) =>
        new DirectoryInfo(root).Name.Replace(".", "").Replace("-", "");

    private static bool IsStructureComplete(ProjectStructureResult structure) =>
        structure.Layers.Any(l => l.Name == "Domain")
        && structure.Layers.Any(l => l.Name == "Application")
        && structure.AllProjects.Count > 0;

    private ProjectStructureResult ScanExistingStructure(string root, string ns)
    {
        var csprojFiles = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .OrderBy(f => f)
            .ToList();

        var solutionFiles = Directory.GetFiles(root, "*.sln", SearchOption.TopDirectoryOnly)
            .Select(f => Path.GetFileName(f)!)
            .ToList();

        var layers = new List<ProjectLayerInfo>();

        foreach (var suffix in LayerSuffixes)
        {
            var match = csprojFiles.FirstOrDefault(p =>
                p.Contains($".{suffix}/", StringComparison.OrdinalIgnoreCase)
                || p.EndsWith($".{suffix}.csproj", StringComparison.OrdinalIgnoreCase));

            if (match is null) continue;

            var layerDir = Path.GetDirectoryName(match)!.Replace('\\', '/');
            var features = DetectFeatureFolders(root, layerDir, suffix);
            layers.Add(new ProjectLayerInfo(suffix, layerDir, match, features));
        }

        var testProjects = csprojFiles
            .Where(p => p.Contains(".Tests/", StringComparison.OrdinalIgnoreCase)
                        || p.Contains("tests/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (testProjects.Count > 0)
        {
            var testPath = Path.GetDirectoryName(testProjects[0])!.Replace('\\', '/');
            layers.Add(new ProjectLayerInfo("Tests", testPath, testProjects[0], []));
        }

        var allFeatures = layers
            .Where(l => l.Name == "Application")
            .SelectMany(l => l.FeatureFolders)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(f => f)
            .ToList();

        return new ProjectStructureResult(
            root, ns, false, false, null, layers, solutionFiles, csprojFiles, allFeatures);
    }

    private static IReadOnlyList<string> DetectFeatureFolders(string root, string layerRelativePath, string layer)
    {
        if (!layer.Equals("Application", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var applicationDir = Path.Combine(root, layerRelativePath);
        if (!Directory.Exists(applicationDir))
        {
            return [];
        }

        return Directory.GetDirectories(applicationDir)
            .Select(Path.GetFileName)
            .Where(name => name is not null
                           && !name.Equals("Common", StringComparison.OrdinalIgnoreCase)
                           && !name.Equals("Dtos", StringComparison.OrdinalIgnoreCase)
                           && !name.Equals("Mapping", StringComparison.OrdinalIgnoreCase)
                           && !name.EndsWith("Dto", StringComparison.OrdinalIgnoreCase))
            .Select(name => name!)
            .OrderBy(n => n)
            .ToList();
    }

    private async Task GenerateCleanArchitectureSkeletonAsync(
        string root,
        string ns,
        CancellationToken cancellationToken)
    {
        var projects = new (string RelativeDir, string ProjectName, string Template)[]
        {
            ($"src/{ns}.Domain", $"{ns}.Domain", DomainCsprojTemplate(ns)),
            ($"src/{ns}.Application", $"{ns}.Application", ApplicationCsprojTemplate(ns)),
            ($"src/{ns}.Infrastructure", $"{ns}.Infrastructure", InfrastructureCsprojTemplate(ns)),
            ($"src/{ns}.Api", $"{ns}.Api", ApiCsprojTemplate(ns)),
            ($"tests/{ns}.Application.Tests", $"{ns}.Application.Tests", TestCsprojTemplate(ns))
        };

        foreach (var (relativeDir, projectName, template) in projects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dir = Path.Combine(root, relativeDir);
            Directory.CreateDirectory(dir);

            var csprojPath = Path.Combine(dir, $"{projectName}.csproj");
            if (!File.Exists(csprojPath))
            {
                await File.WriteAllTextAsync(csprojPath, template, cancellationToken);
            }
        }

        var slnPath = Path.Combine(root, $"{ns}.sln");
        if (!File.Exists(slnPath))
        {
            await File.WriteAllTextAsync(slnPath, SolutionTemplate(ns), cancellationToken);
        }

        var gitkeepDirs = new[]
        {
            $"src/{ns}.Domain",
            $"src/{ns}.Application",
            $"src/{ns}.Infrastructure/Persistence",
            $"src/{ns}.Api/Controllers",
            $"tests/{ns}.Application.Tests"
        };

        foreach (var dir in gitkeepDirs)
        {
            Directory.CreateDirectory(Path.Combine(root, dir));
        }
    }

    private static async Task SaveManifestAsync(
        string manifestPath,
        ProjectStructureResult structure,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);

        var manifest = new
        {
            version = 1,
            namespacePrefix = structure.NamespacePrefix,
            rootPath = structure.RootPath,
            generatedAt = DateTimeOffset.UtcNow,
            layers = structure.Layers.ToDictionary(
                l => l.Name,
                l => new { path = l.RelativePath, project = l.ProjectFile, features = l.FeatureFolders }),
            solutionFiles = structure.SolutionFiles,
            projects = structure.AllProjects,
            features = structure.DetectedFeatures
        };

        await File.WriteAllTextAsync(
            manifestPath,
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken);
    }

    private static async Task<ProjectStructureResult?> LoadFromManifestAsync(
        string manifestPath,
        string root,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(manifestPath);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var rootElement = doc.RootElement;

            var ns = rootElement.GetProperty("namespacePrefix").GetString() ?? "App";
            var layers = new List<ProjectLayerInfo>();

            if (rootElement.TryGetProperty("layers", out var layersElement))
            {
                foreach (var layerProp in layersElement.EnumerateObject())
                {
                    var path = layerProp.Value.GetProperty("path").GetString() ?? string.Empty;
                    var project = layerProp.Value.TryGetProperty("project", out var proj) ? proj.GetString() : null;
                    var layerFeatures = layerProp.Value.TryGetProperty("features", out var feat)
                        ? feat.EnumerateArray().Select(e => e.GetString() ?? string.Empty).Where(s => s.Length > 0).ToList()
                        : [];

                    layers.Add(new ProjectLayerInfo(layerProp.Name, path, project, layerFeatures));
                }
            }

            var solutions = rootElement.TryGetProperty("solutionFiles", out var sol)
                ? sol.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList()
                : [];

            var projects = rootElement.TryGetProperty("projects", out var projs)
                ? projs.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList()
                : [];

            var features = rootElement.TryGetProperty("features", out var feats)
                ? feats.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList()
                : [];

            return new ProjectStructureResult(root, ns, false, true, manifestPath, layers, solutions, projects, features);
        }
        catch
        {
            return null;
        }
    }

    private static string DomainCsprojTemplate(string ns) => $$"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net9.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
            <RootNamespace>{{ns}}.Domain</RootNamespace>
          </PropertyGroup>
        </Project>
        """;

    private static string ApplicationCsprojTemplate(string ns) => $$"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net9.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
          <ItemGroup>
            <ProjectReference Include="..\{{ns}}.Domain\{{ns}}.Domain.csproj" />
          </ItemGroup>
        </Project>
        """;

    private static string InfrastructureCsprojTemplate(string ns) => $$"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net9.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
          <ItemGroup>
            <ProjectReference Include="..\{{ns}}.Application\{{ns}}.Application.csproj" />
          </ItemGroup>
        </Project>
        """;

    private static string ApiCsprojTemplate(string ns) => $$"""
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            <TargetFramework>net9.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
          <ItemGroup>
            <ProjectReference Include="..\{{ns}}.Application\{{ns}}.Application.csproj" />
            <ProjectReference Include="..\{{ns}}.Infrastructure\{{ns}}.Infrastructure.csproj" />
          </ItemGroup>
        </Project>
        """;

    private static string TestCsprojTemplate(string ns) => $$"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net9.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
            <IsPackable>false</IsPackable>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
            <PackageReference Include="xunit" Version="2.9.2" />
            <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
          </ItemGroup>
          <ItemGroup>
            <Using Include="Xunit" />
          </ItemGroup>
          <ItemGroup>
            <ProjectReference Include="..\..\src\{{ns}}.Application\{{ns}}.Application.csproj" />
          </ItemGroup>
        </Project>
        """;

    private static string SolutionTemplate(string ns) => $$"""
        Microsoft Visual Studio Solution File, Format Version 12.00
        Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "{{ns}}.Domain", "src\{{ns}}.Domain\{{ns}}.Domain.csproj", "{11111111-1111-1111-1111-111111111111}"
        EndProject
        Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "{{ns}}.Application", "src\{{ns}}.Application\{{ns}}.Application.csproj", "{22222222-2222-2222-2222-222222222222}"
        EndProject
        Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "{{ns}}.Infrastructure", "src\{{ns}}.Infrastructure\{{ns}}.Infrastructure.csproj", "{33333333-3333-3333-3333-333333333333}"
        EndProject
        Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "{{ns}}.Api", "src\{{ns}}.Api\{{ns}}.Api.csproj", "{44444444-4444-4444-4444-444444444444}"
        EndProject
        Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "{{ns}}.Application.Tests", "tests\{{ns}}.Application.Tests\{{ns}}.Application.Tests.csproj", "{55555555-5555-5555-5555-555555555555}"
        EndProject
        Global
          GlobalSection(SolutionConfigurationPlatforms) = preSolution
            Debug|Any CPU = Debug|Any CPU
          EndGlobalSection
        EndGlobal
        """;
}
