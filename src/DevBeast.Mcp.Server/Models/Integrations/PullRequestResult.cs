namespace DevBeast.Mcp.Server.Models;

public sealed record PullRequestResult(
    string PullRequestUrl,
    string PullRequestId,
    string Title,
    PullRequestImpact Impact,
    bool IsMock);
