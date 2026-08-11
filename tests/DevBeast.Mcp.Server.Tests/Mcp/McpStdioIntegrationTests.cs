using DevBeast.Mcp.Server.Tests;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace DevBeast.Mcp.Server.Tests.Mcp;

[Collection("DevBeast")]
public sealed class McpStdioIntegrationTests(DevBeastTestFixture fixture)
{
    [Fact]
    public async Task McpServer_ListsAllTools_OverStdio()
    {
        await using var client = await ConnectAsync();

        var tools = await client.ListToolsAsync();

        Assert.Contains(tools, t => t.Name == "get_database_schema");
        Assert.Contains(tools, t => t.Name == "validate_architecture_rules");
        Assert.Contains(tools, t => t.Name == "scaffold_feature_slice");
        Assert.Contains(tools, t => t.Name == "get_ticket_context");
        Assert.Contains(tools, t => t.Name == "create_pull_request_with_impact");
        Assert.Contains(tools, t => t.Name == "generate_test_fixtures");
        Assert.Contains(tools, t => t.Name == "diff_environments");
        Assert.Contains(tools, t => t.Name == "inspect_redis_cache");
        Assert.Contains(tools, t => t.Name == "flush_key");
        Assert.Contains(tools, t => t.Name == "peek_dead_letter_queue");
        Assert.Contains(tools, t => t.Name == "scan_secrets_and_pii");
        Assert.Contains(tools, t => t.Name == "check_nuget_vulnerabilities");
        Assert.Contains(tools, t => t.Name == "ensure_project_structure");
        Assert.Contains(tools, t => t.Name == "get_project_structure");
        Assert.True(tools.Count >= 16);
    }

    [Fact]
    public async Task McpServer_GetTicketContext_ReturnsValidJson()
    {
        await using var client = await ConnectAsync();

        var result = await client.CallToolAsync(
            "get_ticket_context",
            new Dictionary<string, object?> { ["ticketId"] = "ADO-891" });

        var text = result.Content.OfType<TextContentBlock>().First().Text;
        Assert.Contains("ADO-891", text);
        Assert.Contains("suggestedFeatureName", text);
    }

    private async Task<McpClient> ConnectAsync()
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Command = "dotnet",
            Arguments =
            [
                "run",
                "--project", fixture.ServerProjectPath,
                "--no-launch-profile"
            ],
            EnvironmentVariables = new Dictionary<string, string?>
            {
                ["DEVBEAST__Integrations__MockDataPath"] = fixture.MocksPath,
                ["DEVBEAST__DefaultProjectPath"] = fixture.ReferenceAppPath,
                ["DEVBEAST__Mongo__ConnectionString"] = "mongodb://devbeast_app:devbeast_app@localhost:27018/devbeast?authSource=devbeast",
                ["DEVBEAST__Database__Provider"] = "MongoDB"
            },
            Name = "DevBeast.Mcp.Server.Tests"
        });

        return await McpClient.CreateAsync(transport);
    }
}
