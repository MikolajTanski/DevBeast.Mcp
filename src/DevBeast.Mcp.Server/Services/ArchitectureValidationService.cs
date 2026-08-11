using DevBeast.Mcp.Server.Models;

namespace DevBeast.Mcp.Server.Services;

public interface IArchitectureValidationService
{
    Task<ArchitectureValidationResult> ValidateAsync(string projectPath, CancellationToken cancellationToken = default);
}

public sealed class ArchitectureValidationService : IArchitectureValidationService
{
    private static readonly string[] DomainForbiddenNamespaces =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "MediatR",
        "AutoMapper",
        "System.Data.SqlClient",
        "Microsoft.Data.SqlClient",
        "MongoDB.Driver"
    ];

    private static readonly string[] ApplicationForbiddenInDomain =
    [
        "Infrastructure",
        "Persistence",
        "Web",
        "Api"
    ];

    public Task<ArchitectureValidationResult> ValidateAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(projectPath))
        {
            throw new DirectoryNotFoundException($"Project path not found: {projectPath}");
        }

        var violations = new List<ArchitectureViolation>();
        var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var file in csFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(projectPath, file).Replace('\\', '/');
            var lines = File.ReadAllLines(file);
            var layer = DetectLayer(relativePath);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var lineNumber = i + 1;

                if (layer == "Domain")
                {
                    CheckDomainRules(relativePath, lineNumber, line, violations);
                }

                if (layer is "Application" or "Domain" && IsDtoFile(relativePath))
                {
                    CheckDtoImmutability(relativePath, lineNumber, line, violations);
                }

                CheckCrossLayerReferences(relativePath, lineNumber, line, layer, violations);
            }
        }

        return Task.FromResult(new ArchitectureValidationResult(
            projectPath,
            violations.Count == 0,
            violations));
    }

    private static string DetectLayer(string relativePath)
    {
        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (segment.Contains("Domain", StringComparison.OrdinalIgnoreCase)) return "Domain";
            if (segment.Contains("Application", StringComparison.OrdinalIgnoreCase)) return "Application";
            if (segment.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase)) return "Infrastructure";
            if (segment.Contains("Api", StringComparison.OrdinalIgnoreCase) || segment.Contains("Web", StringComparison.OrdinalIgnoreCase)) return "Presentation";
        }

        return "Unknown";
    }

    private static bool IsDtoFile(string relativePath) =>
        relativePath.Contains("Dto", StringComparison.OrdinalIgnoreCase)
        || relativePath.Contains("DTO", StringComparison.OrdinalIgnoreCase)
        || relativePath.Contains("Response", StringComparison.OrdinalIgnoreCase)
        || relativePath.Contains("Request", StringComparison.OrdinalIgnoreCase);

    private static void CheckDomainRules(string file, int line, string content, List<ArchitectureViolation> violations)
    {
        foreach (var ns in DomainForbiddenNamespaces)
        {
            if (content.Contains($"using {ns}", StringComparison.Ordinal))
            {
                violations.Add(new ArchitectureViolation(
                    "CA-DOM-001",
                    "Error",
                    file,
                    line,
                    $"Domain layer must not depend on '{ns}' (Clean Architecture violation)."));
            }
        }
    }

    private static void CheckDtoImmutability(string file, int line, string content, List<ArchitectureViolation> violations)
    {
        if (content.Contains(" set; }", StringComparison.Ordinal) && !content.Contains(" init; }", StringComparison.Ordinal))
        {
            violations.Add(new ArchitectureViolation(
                "CA-DTO-001",
                "Warning",
                file,
                line,
                "DTO property uses mutable 'set' — prefer 'init' or convert to 'record' for immutability."));
        }

        if (content.Contains("class ", StringComparison.Ordinal) && IsDtoFile(file) && content.Contains("Dto", StringComparison.OrdinalIgnoreCase))
        {
            violations.Add(new ArchitectureViolation(
                "CA-DTO-002",
                "Info",
                file,
                line,
                "Consider using 'record' instead of 'class' for DTO types."));
        }
    }

    private static void CheckCrossLayerReferences(string file, int line, string content, string layer, List<ArchitectureViolation> violations)
    {
        if (layer != "Domain") return;

        foreach (var forbidden in ApplicationForbiddenInDomain)
        {
            if (content.Contains($"using {forbidden}", StringComparison.OrdinalIgnoreCase)
                || content.Contains($".{forbidden}.", StringComparison.OrdinalIgnoreCase))
            {
                violations.Add(new ArchitectureViolation(
                    "CA-DOM-002",
                    "Error",
                    file,
                    line,
                    $"Domain layer must not reference '{forbidden}' layer."));
            }
        }
    }
}
