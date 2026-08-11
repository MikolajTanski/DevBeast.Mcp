using System.Text.RegularExpressions;
using DevBeast.Mcp.Server.Models;

namespace DevBeast.Mcp.Server.Services;

public interface ISecretsScanner
{
    Task<IReadOnlyList<SecretFinding>> ScanAsync(string projectPath, CancellationToken cancellationToken = default);
}

public sealed partial class SecretsScanner : ISecretsScanner
{
    private static readonly (Regex Pattern, string Category, string Severity)[] Rules =
    [
        (ApiKeyPattern(), "API Key / Secret", "Critical"),
        (JwtPattern(), "JWT Token", "Critical"),
        (PasswordAssignmentPattern(), "Hardcoded Password", "Critical"),
        (ConnectionStringPasswordPattern(), "Connection String Secret", "High"),
        (EmailPattern(), "PII: Email", "Medium"),
        (PeselPattern(), "PII: PESEL", "High"),
        (PhonePattern(), "PII: Phone Number", "Medium"),
        (CreditCardPattern(), "PII: Credit Card", "Critical")
    ];

    public Task<IReadOnlyList<SecretFinding>> ScanAsync(string projectPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(projectPath))
        {
            throw new DirectoryNotFoundException($"Project path not found: {projectPath}");
        }

        var findings = new List<SecretFinding>();
        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".cs", ".json", ".env", ".yaml", ".yml", ".config", ".xml", ".ts", ".js" };

        var files = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
            .Where(f => extensions.Contains(Path.GetExtension(f)))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.Contains("node_modules", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(projectPath, file);
            var lines = File.ReadAllLines(file);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (IsLikelyFalsePositive(line)) continue;

                foreach (var (pattern, category, severity) in Rules)
                {
                    if (pattern.IsMatch(line))
                    {
                        findings.Add(new SecretFinding(
                            relativePath,
                            i + 1,
                            category,
                            Truncate(line.Trim(), 120),
                            severity));
                    }
                }
            }
        }

        return Task.FromResult<IReadOnlyList<SecretFinding>>(findings);
    }

    private static bool IsLikelyFalsePositive(string line)
    {
        var trimmed = line.Trim();
        return trimmed.StartsWith("//", StringComparison.Ordinal)
               || trimmed.StartsWith("*", StringComparison.Ordinal)
               || trimmed.Contains("example.com", StringComparison.OrdinalIgnoreCase)
               || trimmed.Contains("your-api-key", StringComparison.OrdinalIgnoreCase)
               || trimmed.Contains("***", StringComparison.Ordinal);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";

    [GeneratedRegex(@"(?i)(api[_-]?key|secret[_-]?key|access[_-]?token)\s*[:=]\s*['""]?[A-Za-z0-9_\-]{16,}", RegexOptions.CultureInvariant)]
    private static partial Regex ApiKeyPattern();

    [GeneratedRegex(@"eyJ[A-Za-z0-9_-]+\.eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", RegexOptions.CultureInvariant)]
    private static partial Regex JwtPattern();

    [GeneratedRegex(@"(?i)(password|passwd|pwd)\s*[:=]\s*['""][^'""]{4,}['""]", RegexOptions.CultureInvariant)]
    private static partial Regex PasswordAssignmentPattern();

    [GeneratedRegex(@"(?i)(Password|Pwd)\s*=\s*[^;""'\s]{4,}", RegexOptions.CultureInvariant)]
    private static partial Regex ConnectionStringPasswordPattern();

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"\b\d{11}\b", RegexOptions.CultureInvariant)]
    private static partial Regex PeselPattern();

    [GeneratedRegex(@"\b(?:\+48\s?)?\d{3}[\s-]?\d{3}[\s-]?\d{3}\b", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"\b(?:\d{4}[\s-]?){3}\d{4}\b", RegexOptions.CultureInvariant)]
    private static partial Regex CreditCardPattern();
}
