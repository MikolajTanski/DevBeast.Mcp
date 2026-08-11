namespace DevBeast.Mcp.Server.Models;

public sealed record SecretFinding(
    string FilePath,
    int LineNumber,
    string Category,
    string Snippet,
    string Severity);
