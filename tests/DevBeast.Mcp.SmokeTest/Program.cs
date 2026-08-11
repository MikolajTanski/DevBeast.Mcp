using System.Text.Json;
using DevBeast.Mcp.Server.Tests;

var fixture = new DevBeastTestFixture();
var results = new List<(string Tool, bool Ok, string Summary)>();

Console.WriteLine("DevBeast MCP — live smoke test\n");

try
{
    await Run("get_ticket_context (PROJ-142)", async () =>
    {
        var json = await fixture.GetTool<DevBeast.Mcp.Server.Tools.IntegrationTools>().GetTicketContext("PROJ-142");
        using var doc = JsonDocument.Parse(json);
        var title = doc.RootElement.GetProperty("Title").GetString();
        return title?[..Math.Min(60, title.Length)] ?? "ok";
    });

    await Run("validate_architecture_rules", async () =>
    {
        var json = await fixture.GetTool<DevBeast.Mcp.Server.Tools.ArchitectureTools>().ValidateArchitectureRules(fixture.ReferenceAppPath);
        using var doc = JsonDocument.Parse(json);
        return $"compliant={doc.RootElement.GetProperty("isCompliant").GetBoolean()}, violations={doc.RootElement.GetProperty("violationCount").GetInt32()}";
    });

    await Run("get_database_schema (products)", async () =>
    {
        if (!DevBeastTestFixture.IsMongoAvailable()) return "SKIP — MongoDB offline";
        var json = await fixture.GetTool<DevBeast.Mcp.Server.Tools.DatabaseTools>().GetDatabaseSchema("products");
        using var doc = JsonDocument.Parse(json);
        return $"columns={doc.RootElement.GetProperty("columns").GetArrayLength()}";
    });

    await Run("execute_read_query (pending orders)", async () =>
    {
        if (!DevBeastTestFixture.IsMongoAvailable()) return "SKIP — MongoDB offline";
        var json = await fixture.GetTool<DevBeast.Mcp.Server.Tools.DatabaseTools>().ExecuteReadQuery(
            """{"collection":"orders","filter":{"status":"Pending"},"limit":5}""");
        using var doc = JsonDocument.Parse(json);
        return $"rows={doc.RootElement.GetProperty("rowCount").GetInt32()}";
    });

    await Run("generate_test_fixtures (products)", async () =>
    {
        if (!DevBeastTestFixture.IsMongoAvailable()) return "SKIP — MongoDB offline";
        var json = await fixture.GetTool<DevBeast.Mcp.Server.Tools.DataTools>().GenerateTestFixtures("products", 3);
        using var doc = JsonDocument.Parse(json);
        var code = doc.RootElement.GetProperty("code").GetString() ?? "";
        return $"generated {code.Split('\n').Length} lines of C#";
    });

    await Run("inspect_redis_cache", async () =>
    {
        var json = await fixture.GetTool<DevBeast.Mcp.Server.Tools.InfrastructureTools>().InspectRedisCache("cache:*");
        using var doc = JsonDocument.Parse(json);
        return $"entries={doc.RootElement.GetProperty("entryCount").GetInt32()}, mock={doc.RootElement.GetProperty("isMockMode").GetBoolean()}";
    });

    await Run("peek_dead_letter_queue", async () =>
    {
        var json = await fixture.GetTool<DevBeast.Mcp.Server.Tools.InfrastructureTools>().PeekDeadLetterQueue(limit: 3);
        using var doc = JsonDocument.Parse(json);
        return $"messages={doc.RootElement.GetProperty("messageCount").GetInt32()}";
    });

    await Run("diff_environments", async () =>
    {
        var json = await fixture.GetTool<DevBeast.Mcp.Server.Tools.DataTools>().DiffEnvironments("appsettings", Path.Combine(fixture.MocksPath, "environments"));
        using var doc = JsonDocument.Parse(json);
        return $"diffs={doc.RootElement.GetProperty("diffCount").GetInt32()}";
    });

    await Run("scan_secrets_and_pii", async () =>
    {
        var json = await fixture.GetTool<DevBeast.Mcp.Server.Tools.SecurityTools>().ScanSecretsAndPii(fixture.ReferenceAppPath);
        using var doc = JsonDocument.Parse(json);
        return $"findings={doc.RootElement.GetProperty("findingCount").GetInt32()}";
    });

    await Run("create_pull_request_with_impact", async () =>
    {
        var json = await fixture.GetTool<DevBeast.Mcp.Server.Tools.IntegrationTools>().CreatePullRequestWithImpact(
            "Fix empty cart", "Guard clause in OrderService", fixture.ReferenceAppPath, "PROJ-142");
        using var doc = JsonDocument.Parse(json);
        return $"PR={doc.RootElement.GetProperty("PullRequestId").GetString()}, risk={doc.RootElement.GetProperty("impact").GetProperty("riskLevel").GetString()}";
    });
}
finally
{
    fixture.Dispose();
}

Console.WriteLine("\n--- Results ---");
foreach (var (tool, ok, summary) in results)
{
    Console.WriteLine($"  {(ok ? "OK" : "FAIL"),-4} {tool,-42} → {summary}");
}

var failed = results.Count(r => !r.Ok);
Console.WriteLine(failed == 0 ? "\nAll smoke tests passed." : $"\n{failed} failed.");
return failed;

async Task Run(string name, Func<Task<string>> action)
{
    try
    {
        var summary = await action();
        var ok = summary.StartsWith("SKIP", StringComparison.Ordinal) || !summary.Contains("FAIL");
        results.Add((name, ok, summary));
        Console.WriteLine($"  ... {name}");
    }
    catch (Exception ex)
    {
        results.Add((name, false, ex.Message));
        Console.WriteLine($"  ... {name} FAILED");
    }
}
