using DevBeast.Mcp.Server.Models;

namespace DevBeast.Mcp.Server.Services;

public interface IPullRequestService
{
    Task<PullRequestResult> CreateWithImpactAnalysisAsync(
        string title,
        string description,
        string? projectPath = null,
        string? ticketId = null,
        CancellationToken cancellationToken = default);
}

public sealed class MockPullRequestService : IPullRequestService
{
    public Task<PullRequestResult> CreateWithImpactAnalysisAsync(
        string title,
        string description,
        string? projectPath = null,
        string? ticketId = null,
        CancellationToken cancellationToken = default)
    {
        var impact = AnalyzeImpact(projectPath);
        var prId = $"PR-{Random.Shared.Next(1000, 9999)}";

        var result = new PullRequestResult(
            $"https://github.com/mock-org/mock-repo/pull/{prId.Replace("PR-", "")}",
            prId,
            title,
            impact,
            IsMock: true);

        if (!string.IsNullOrWhiteSpace(ticketId))
        {
            // Mock: ticket comment would be posted here in real integration
        }

        return Task.FromResult(result);
    }

    private static PullRequestImpact AnalyzeImpact(string? projectPath)
    {
        var changedApis = new List<string> { "POST /api/orders", "GET /api/products" };
        var databases = new List<string> { "ShopDb (Orders, Products tables)" };
        var tests = new List<string>
        {
            "dotnet test --filter Category=Integration",
            "dotnet test --filter FullyQualifiedName~Orders",
            "dotnet test --filter FullyQualifiedName~Products"
        };

        if (!string.IsNullOrWhiteSpace(projectPath) && Directory.Exists(projectPath))
        {
            var controllers = Directory.GetFiles(projectPath, "*Controller.cs", SearchOption.AllDirectories);
            changedApis = controllers
                .Take(5)
                .Select(f => $"Detected controller: {Path.GetFileNameWithoutExtension(f)}")
                .ToList();

            if (Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories).Any(f => File.ReadAllText(f).Contains("EntityFrameworkCore")))
            {
                databases.Add("EF Core DbContext detected — verify migrations");
            }
        }

        var risk = changedApis.Count > 3 ? "High" : changedApis.Count > 1 ? "Medium" : "Low";

        return new PullRequestImpact(changedApis, databases, tests, risk);
    }
}
