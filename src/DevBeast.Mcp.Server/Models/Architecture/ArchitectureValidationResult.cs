namespace DevBeast.Mcp.Server.Models;

public sealed record ArchitectureValidationResult(
    string ProjectPath,
    bool IsCompliant,
    IReadOnlyList<ArchitectureViolation> Violations);
