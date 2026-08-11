using System.ComponentModel;
using System.Text.Json;
using DevBeast.Mcp.Server.Configuration;
using DevBeast.Mcp.Server.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace DevBeast.Mcp.Server.Tools;

[McpServerToolType]
public sealed class SecurityTools(
    ISecretsScanner secretsScanner,
    INuGetVulnerabilityChecker nuGetChecker,
    IOptions<DevBeastOptions> options)
{
    [McpServerTool]
    [Description("Scans project source code for leaked secrets (API keys, JWT, passwords) and PII (email, PESEL, phone numbers).")]
    public async Task<string> ScanSecretsAndPii(
        [Description("Project root path to scan. Uses DefaultProjectPath if omitted.")] string? projectPath = null,
        CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(projectPath);
        var findings = await secretsScanner.ScanAsync(path, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            projectPath = path,
            findingCount = findings.Count,
            criticalCount = findings.Count(f => f.Severity == "Critical"),
            findings = findings.Select(f => new
            {
                file = f.FilePath,
                line = f.LineNumber,
                category = f.Category,
                severity = f.Severity,
                snippet = f.Snippet
            })
        }, JsonOptions);
    }

    [McpServerTool]
    [Description("Checks NuGet packages in .csproj for known CVE vulnerabilities using 'dotnet list package --vulnerable'.")]
    public async Task<string> CheckNugetVulnerabilities(
        [Description("Path to project directory or .csproj file. Uses DefaultProjectPath if omitted.")] string? projectPath = null,
        CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(projectPath);
        var vulnerabilities = await nuGetChecker.CheckAsync(path, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            projectPath = path,
            vulnerabilityCount = vulnerabilities.Count(v => v.Severity is not "Info"),
            packages = vulnerabilities.Select(v => new
            {
                packageId = v.PackageId,
                version = v.Version,
                severity = v.Severity,
                advisoryUrl = v.AdvisoryUrl,
                recommendedVersion = v.RecommendedVersion
            })
        }, JsonOptions);
    }

    private string ResolvePath(string? projectPath) =>
        projectPath
        ?? options.Value.DefaultProjectPath
        ?? throw new InvalidOperationException("Provide projectPath or set DevBeast:DefaultProjectPath.");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
