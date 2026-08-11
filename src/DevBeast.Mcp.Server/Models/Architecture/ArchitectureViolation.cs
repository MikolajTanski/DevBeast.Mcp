namespace DevBeast.Mcp.Server.Models;

public sealed record ArchitectureViolation(
    string Rule,
    string Severity,
    string FilePath,
    int? LineNumber,
    string Message);
