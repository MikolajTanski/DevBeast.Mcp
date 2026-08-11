namespace DevBeast.Mcp.Server.Models;

public sealed record PullRequestImpact(
    IReadOnlyList<string> ChangedApis,
    IReadOnlyList<string> AffectedDatabases,
    IReadOnlyList<string> RecommendedTests,
    string RiskLevel);
