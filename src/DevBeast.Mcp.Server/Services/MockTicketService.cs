using System.Text.Json;
using DevBeast.Mcp.Server.Configuration;
using DevBeast.Mcp.Server.Models;
using Microsoft.Extensions.Options;

namespace DevBeast.Mcp.Server.Services;

public interface ITicketService
{
    Task<TicketContext> GetTicketContextAsync(string ticketId, CancellationToken cancellationToken = default);
}

public sealed class MockTicketService(IOptions<DevBeastOptions> options) : ITicketService
{
    public async Task<TicketContext> GetTicketContextAsync(string ticketId, CancellationToken cancellationToken = default)
    {
        var mockPath = ResolveMockPath(options.Value.Integrations.MockDataPath, "tickets", $"{ticketId}.json");

        if (!File.Exists(mockPath))
        {
            throw new FileNotFoundException($"Mock ticket '{ticketId}' not found. Available mocks are in Mocks/tickets/.");
        }

        await using var stream = File.OpenRead(mockPath);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = doc.RootElement;

        return new TicketContext(
            root.GetProperty("id").GetString() ?? ticketId,
            root.GetProperty("source").GetString() ?? "Mock",
            root.GetProperty("type").GetString() ?? "Task",
            root.GetProperty("title").GetString() ?? string.Empty,
            root.GetProperty("description").GetString() ?? string.Empty,
            root.GetProperty("priority").GetString() ?? "Medium",
            root.GetProperty("status").GetString() ?? "Open",
            ReadStringArray(root, "acceptanceCriteria"),
            ReadStringArray(root, "linkedFiles"),
            ReadStringArray(root, "labels"),
            root.TryGetProperty("suggestedFeatureName", out var feature) ? feature.GetString() : null);
    }

    private static string ResolveMockPath(string mockDataPath, params string[] segments)
    {
        var relative = Path.Combine(segments);
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, mockDataPath, relative),
            Path.Combine(Directory.GetCurrentDirectory(), mockDataPath, relative)
        };

        return candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException($"Mock file not found: {relative}. Expected in Mocks/ folder.");
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Select(e => e.GetString() ?? string.Empty)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }
}
