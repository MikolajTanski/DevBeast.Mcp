using System.ComponentModel;
using System.Text.Json;
using DevBeast.Mcp.Server.Configuration;
using DevBeast.Mcp.Server.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace DevBeast.Mcp.Server.Tools;

[McpServerToolType]
public sealed class ArchitectureTools(
    IArchitectureValidationService validationService,
    IOptions<DevBeastOptions> options)
{
    [McpServerTool]
    [Description("Validates project files against Clean Architecture / DDD rules (Domain isolation, immutable DTOs).")]
    public async Task<string> ValidateArchitectureRules(
        [Description("Absolute path to the .NET project/solution root. Uses DefaultProjectPath if omitted.")] string? projectPath = null,
        CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(projectPath);
        var result = await validationService.ValidateAsync(path, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            projectPath = result.ProjectPath,
            isCompliant = result.IsCompliant,
            violationCount = result.Violations.Count,
            violations = result.Violations.Select(v => new
            {
                rule = v.Rule,
                severity = v.Severity,
                file = v.FilePath,
                line = v.LineNumber,
                message = v.Message
            })
        }, JsonOptions);
    }

    private string ResolvePath(string? projectPath) =>
        projectPath
        ?? options.Value.DefaultProjectPath
        ?? throw new InvalidOperationException("Provide projectPath or set DevBeast:DefaultProjectPath.");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
