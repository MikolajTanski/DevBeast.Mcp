using System.Text.Json;
using DevBeast.Mcp.Server.Tests;

namespace DevBeast.Mcp.Server.Tests.Tools;

[Collection("DevBeast")]
public sealed class ArchitectureToolsTests(DevBeastTestFixture fixture)
{
    [Fact]
    public async Task ValidateArchitectureRules_FindsViolationsInReferenceApp()
    {
        var tools = fixture.GetTool<Server.Tools.ArchitectureTools>();
        var json = await tools.ValidateArchitectureRules(fixture.ReferenceAppPath);
        using var doc = DevBeastTestFixture.ParseJson(json);

        Assert.False(doc.RootElement.GetProperty("isCompliant").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("violationCount").GetInt32() > 0);
    }
}

[Collection("DevBeast")]
public sealed class IntegrationToolsTests(DevBeastTestFixture fixture)
{
    [Fact]
    public async Task GetTicketContext_ReturnsMockJiraTicket()
    {
        var tools = fixture.GetTool<Server.Tools.IntegrationTools>();
        var json = await tools.GetTicketContext("PROJ-142");
        using var doc = DevBeastTestFixture.ParseJson(json);

        Assert.Equal("PROJ-142", doc.RootElement.GetProperty("Id").GetString());
        Assert.True(doc.RootElement.GetProperty("isMock").GetBoolean());
        Assert.Contains("NullReferenceException", doc.RootElement.GetProperty("Description").GetString());
    }

    [Fact]
    public async Task CreatePullRequestWithImpact_ReturnsMockPrWithRiskAnalysis()
    {
        var tools = fixture.GetTool<Server.Tools.IntegrationTools>();
        var json = await tools.CreatePullRequestWithImpact(
            "Fix empty cart validation",
            "Adds guard clause in OrderService",
            fixture.ReferenceAppPath,
            "PROJ-142");

        using var doc = DevBeastTestFixture.ParseJson(json);

        Assert.True(doc.RootElement.GetProperty("IsMock").GetBoolean() || doc.RootElement.TryGetProperty("isMock", out var mock) && mock.GetBoolean());
        Assert.True(doc.RootElement.GetProperty("impact").GetProperty("recommendedTests").GetArrayLength() > 0);
    }
}

[Collection("DevBeast")]
public sealed class DataToolsTests(DevBeastTestFixture fixture)
{
    [Fact]
    public async Task DiffEnvironments_FindsDevTestProdDifferences()
    {
        var tools = fixture.GetTool<Server.Tools.DataTools>();
        var json = await tools.DiffEnvironments("appsettings", Path.Combine(fixture.MocksPath, "environments"));
        using var doc = DevBeastTestFixture.ParseJson(json);

        Assert.True(doc.RootElement.GetProperty("diffCount").GetInt32() > 0);
    }

    [Fact]
    public async Task GenerateTestFixtures_ProducesBogusCSharpCode()
    {
        if (!DevBeastTestFixture.IsMongoAvailable())
        {
            return; // skip gracefully when docker is down
        }

        var tools = fixture.GetTool<Server.Tools.DataTools>();
        var json = await tools.GenerateTestFixtures("products", 5);
        using var doc = DevBeastTestFixture.ParseJson(json);

        var code = doc.RootElement.GetProperty("code").GetString() ?? string.Empty;
        Assert.Contains("Bogus", code);
        Assert.Contains("Generate", code);
    }
}

[Collection("DevBeast")]
public sealed class InfrastructureToolsTests(DevBeastTestFixture fixture)
{
    [Fact]
    public async Task InspectRedisCache_ReturnsEntries()
    {
        var tools = fixture.GetTool<Server.Tools.InfrastructureTools>();
        var json = await tools.InspectRedisCache("cache:*");
        using var doc = DevBeastTestFixture.ParseJson(json);

        Assert.True(doc.RootElement.GetProperty("entryCount").GetInt32() >= 0);
    }

    [Fact]
    public async Task FlushKey_RemovesCacheEntryOrRunsAgainstRealRedis()
    {
        var tools = fixture.GetTool<Server.Tools.InfrastructureTools>();

        var inspectJson = await tools.InspectRedisCache("*");
        using var inspectDoc = DevBeastTestFixture.ParseJson(inspectJson);
        var isMock = inspectDoc.RootElement.GetProperty("isMockMode").GetBoolean();

        var key = isMock ? "cache:products:all" : "devbeast:test:flush";
        var flushJson = await tools.FlushKey(key);
        using var doc = DevBeastTestFixture.ParseJson(flushJson);

        Assert.Equal(key, doc.RootElement.GetProperty("key").GetString());
        if (isMock)
        {
            Assert.True(doc.RootElement.GetProperty("removed").GetBoolean());
        }
    }

    [Fact]
    public async Task PeekDeadLetterQueue_ReturnsMessages()
    {
        var tools = fixture.GetTool<Server.Tools.InfrastructureTools>();
        var json = await tools.PeekDeadLetterQueue(limit: 5);
        using var doc = DevBeastTestFixture.ParseJson(json);

        Assert.True(doc.RootElement.GetProperty("messageCount").GetInt32() > 0);
    }
}

[Collection("DevBeast")]
public sealed class SecurityToolsTests(DevBeastTestFixture fixture)
{
    [Fact]
    public async Task ScanSecretsAndPii_FindsHardcodedSecretsInReferenceApp()
    {
        var tools = fixture.GetTool<Server.Tools.SecurityTools>();
        var json = await tools.ScanSecretsAndPii(fixture.ReferenceAppPath);
        using var doc = DevBeastTestFixture.ParseJson(json);

        Assert.True(doc.RootElement.GetProperty("findingCount").GetInt32() > 0);
    }

    [Fact]
    public async Task CheckNugetVulnerabilities_ReturnsPackageList()
    {
        var tools = fixture.GetTool<Server.Tools.SecurityTools>();
        var json = await tools.CheckNugetVulnerabilities(fixture.ServerProjectPath);
        using var doc = DevBeastTestFixture.ParseJson(json);

        Assert.True(doc.RootElement.GetProperty("packages").GetArrayLength() > 0);
    }
}

[Collection("DevBeast")]
public sealed class DatabaseToolsTests(DevBeastTestFixture fixture)
{
    [Fact]
    public async Task GetDatabaseSchema_ReturnsProductsCollection()
    {
        if (!DevBeastTestFixture.IsMongoAvailable())
        {
            return;
        }

        var tools = fixture.GetTool<Server.Tools.DatabaseTools>();
        var json = await tools.GetDatabaseSchema("products");
        using var doc = DevBeastTestFixture.ParseJson(json);

        Assert.Equal("products", doc.RootElement.GetProperty("tableName").GetString());
        Assert.True(doc.RootElement.GetProperty("columns").GetArrayLength() > 0);
    }

    [Fact]
    public async Task ExecuteReadQuery_ReturnsPendingOrders()
    {
        if (!DevBeastTestFixture.IsMongoAvailable())
        {
            return;
        }

        var tools = fixture.GetTool<Server.Tools.DatabaseTools>();
        var query = """{"collection":"orders","filter":{"status":"Pending"},"limit":10}""";
        var json = await tools.ExecuteReadQuery(query);
        using var doc = DevBeastTestFixture.ParseJson(json);

        Assert.True(doc.RootElement.GetProperty("rowCount").GetInt32() >= 1);
    }
}

[Collection("DevBeast")]
public sealed class ScaffoldingToolsTests(DevBeastTestFixture fixture)
{
    [Fact]
    public async Task ScaffoldFeatureSlice_CreatesVerticalSliceFiles()
    {
        var featureName = $"TestFeature_{Guid.NewGuid():N}"[..20];
        var outputDir = Path.Combine(fixture.ScaffoldOutputPath, featureName);
        Directory.CreateDirectory(outputDir);

        var tools = fixture.GetTool<Server.Tools.ScaffoldingTools>();
        var json = await tools.ScaffoldFeatureSlice(featureName, outputDir);
        using var doc = DevBeastTestFixture.ParseJson(json);

        Assert.True(doc.RootElement.GetProperty("createdFileCount").GetInt32() >= 10);

        var createdFiles = doc.RootElement.GetProperty("createdFiles").EnumerateArray()
            .Select(e => e.GetString())
            .Where(f => f is not null)
            .ToList();

        Assert.Contains(createdFiles, f => f!.Contains("Controller"));
        Assert.Contains(createdFiles, f => f!.Contains("Handler"));
    }
}

[Collection("DevBeast")]
public sealed class ProjectStructureToolsTests(DevBeastTestFixture fixture)
{
    [Fact]
    public async Task EnsureProjectStructure_CompletesPartialReferenceApp()
    {
        var tools = fixture.GetTool<Server.Tools.ProjectStructureTools>();
        var json = await tools.EnsureProjectStructure(fixture.ReferenceAppPath, generateIfMissing: true);
        using var doc = DevBeastTestFixture.ParseJson(json);

        Assert.True(doc.RootElement.GetProperty("hasManifest").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("layers").GetArrayLength() >= 4);
        Assert.True(File.Exists(Path.Combine(fixture.ReferenceAppPath, ".devbeast", "project-structure.json")));
    }

    [Fact]
    public async Task EnsureProjectStructure_GeneratesSkeletonWhenMissing()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"devbeast-structure-{Guid.NewGuid():N}");
        try
        {
            var tools = fixture.GetTool<Server.Tools.ProjectStructureTools>();
            var json = await tools.EnsureProjectStructure(tempRoot, generateIfMissing: true, namespacePrefix: "Demo");
            using var doc = DevBeastTestFixture.ParseJson(json);

            Assert.True(doc.RootElement.GetProperty("wasGenerated").GetBoolean());
            Assert.True(File.Exists(Path.Combine(tempRoot, ".devbeast", "project-structure.json")));
            Assert.True(doc.RootElement.GetProperty("layers").GetArrayLength() >= 4);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task GetProjectStructure_ReturnsManifestAfterEnsure()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"devbeast-get-structure-{Guid.NewGuid():N}");
        try
        {
            var tools = fixture.GetTool<Server.Tools.ProjectStructureTools>();
            await tools.EnsureProjectStructure(tempRoot, generateIfMissing: true, namespacePrefix: "Shop");

            var json = await tools.GetProjectStructure(tempRoot);
            using var doc = DevBeastTestFixture.ParseJson(json);

            Assert.True(doc.RootElement.GetProperty("hasManifest").GetBoolean());
            Assert.Equal("Shop", doc.RootElement.GetProperty("namespacePrefix").GetString());
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}

[CollectionDefinition("DevBeast")]
public class DevBeastCollectionDefinition : ICollectionFixture<DevBeastTestFixture>;
