namespace DevBeast.Mcp.Server.Models;

public sealed record ArchitectureViolation(
    string Rule,
    string Severity,
    string FilePath,
    int? LineNumber,
    string Message);

public sealed record ArchitectureValidationResult(
    string ProjectPath,
    bool IsCompliant,
    IReadOnlyList<ArchitectureViolation> Violations);

public sealed record TicketContext(
    string Id,
    string Source,
    string Type,
    string Title,
    string Description,
    string Priority,
    string Status,
    IReadOnlyList<string> AcceptanceCriteria,
    IReadOnlyList<string> LinkedFiles,
    IReadOnlyList<string> Labels,
    string? SuggestedFeatureName = null);

public sealed record PullRequestImpact(
    IReadOnlyList<string> ChangedApis,
    IReadOnlyList<string> AffectedDatabases,
    IReadOnlyList<string> RecommendedTests,
    string RiskLevel);

public sealed record PullRequestResult(
    string PullRequestUrl,
    string PullRequestId,
    string Title,
    PullRequestImpact Impact,
    bool IsMock);

public sealed record EnvironmentDiffEntry(
    string KeyPath,
    string? DevValue,
    string? TestValue,
    string? ProdValue,
    string DiffType);

public sealed record CacheEntry(
    string Key,
    string? Value,
    string? ValueType,
    TimeSpan? Ttl);

public sealed record DeadLetterMessage(
    string MessageId,
    string Queue,
    string Reason,
    string Payload,
    string Error,
    DateTimeOffset FailedAt,
    int RetryCount);

public sealed record SecretFinding(
    string FilePath,
    int LineNumber,
    string Category,
    string Snippet,
    string Severity);

public sealed record NuGetVulnerability(
    string PackageId,
    string Version,
    string Severity,
    string AdvisoryUrl,
    string? RecommendedVersion);
