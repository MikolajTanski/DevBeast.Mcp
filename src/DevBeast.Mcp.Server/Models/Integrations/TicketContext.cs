namespace DevBeast.Mcp.Server.Models;

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
