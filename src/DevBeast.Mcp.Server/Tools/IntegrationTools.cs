using System.ComponentModel;
using System.Text.Json;
using DevBeast.Mcp.Server.Services;
using ModelContextProtocol.Server;

namespace DevBeast.Mcp.Server.Tools;

[McpServerToolType]
public sealed class IntegrationTools(
    ITicketService ticketService,
    IPullRequestService pullRequestService)
{
    [McpServerTool]
    [Description("Fetches ticket context from Jira/Azure DevOps (Mock mode: reads from Mocks/tickets/{ticketId}.json).")]
    public async Task<string> GetTicketContext(
        [Description("Ticket ID, e.g. 'PROJ-142' or 'ADO-891'.")] string ticketId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await ticketService.GetTicketContextAsync(ticketId, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            ticket.Id,
            ticket.Source,
            ticket.Type,
            ticket.Title,
            ticket.Description,
            ticket.Priority,
            ticket.Status,
            acceptanceCriteria = ticket.AcceptanceCriteria,
            linkedFiles = ticket.LinkedFiles,
            labels = ticket.Labels,
            suggestedFeatureName = ticket.SuggestedFeatureName,
            isMock = ticket.Source.StartsWith("Mock", StringComparison.OrdinalIgnoreCase)
        }, JsonOptions);
    }

    [McpServerTool]
    [Description("Creates a Pull Request with automatic impact analysis (changed APIs, affected DBs, recommended tests). Mock mode returns simulated PR.")]
    public async Task<string> CreatePullRequestWithImpact(
        [Description("PR title.")] string title,
        [Description("PR description / summary of changes.")] string description,
        [Description("Project path for impact analysis (optional).")] string? projectPath = null,
        [Description("Linked ticket ID — mock adds comment with PR link.")] string? ticketId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await pullRequestService.CreateWithImpactAnalysisAsync(
            title, description, projectPath, ticketId, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            result.PullRequestId,
            result.PullRequestUrl,
            result.Title,
            result.IsMock,
            ticketComment = ticketId is not null
                ? $"Mock comment added to {ticketId}: PR {result.PullRequestUrl}"
                : null,
            impact = new
            {
                riskLevel = result.Impact.RiskLevel,
                changedApis = result.Impact.ChangedApis,
                affectedDatabases = result.Impact.AffectedDatabases,
                recommendedTests = result.Impact.RecommendedTests
            }
        }, JsonOptions);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
