using System.Text.Json;
using DevBeast.Mcp.Server.Configuration;
using DevBeast.Mcp.Server.Infrastructure;
using DevBeast.Mcp.Server.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace DevBeast.Mcp.Server.Tests;

public sealed class DevBeastTestFixture : IDisposable
{
    public DevBeastTestFixture()
    {
        var repoRoot = FindRepoRoot();
        ReferenceAppPath = Path.Combine(repoRoot, "samples", "ReferenceApp");
        ScaffoldOutputPath = Path.Combine(repoRoot, "samples", "Scaffolded");
        MocksPath = Path.Combine(repoRoot, "src", "DevBeast.Mcp.Server", "Mocks");
        ServerProjectPath = Path.Combine(repoRoot, "src", "DevBeast.Mcp.Server", "DevBeast.Mcp.Server.csproj");

        Directory.CreateDirectory(ScaffoldOutputPath);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{DevBeastOptions.SectionName}:DefaultProjectPath"] = ReferenceAppPath,
                [$"{DevBeastOptions.SectionName}:Database:Provider"] = "MongoDB",
                [$"{DevBeastOptions.SectionName}:Mongo:ConnectionString"] = "mongodb://devbeast_app:devbeast_app@localhost:27018/devbeast?authSource=devbeast",
                [$"{DevBeastOptions.SectionName}:Mongo:DatabaseName"] = "devbeast",
                [$"{DevBeastOptions.SectionName}:Redis:ConnectionString"] = "localhost:6379",
                [$"{DevBeastOptions.SectionName}:Redis:UseMockWhenUnavailable"] = "true",
                [$"{DevBeastOptions.SectionName}:Integrations:Mode"] = "Mock",
                [$"{DevBeastOptions.SectionName}:Integrations:MockDataPath"] = MocksPath,
                [$"{DevBeastOptions.SectionName}:Scaffolding:OutputRoot"] = ScaffoldOutputPath,
                [$"{DevBeastOptions.SectionName}:Scaffolding:NamespacePrefix"] = "App",
                [$"{DevBeastOptions.SectionName}:Logs:Directory"] = Path.GetTempPath()
            })
            .Build();

        Services = new ServiceCollection();
        Services.Configure<DevBeastOptions>(config.GetSection(DevBeastOptions.SectionName));
        Services.AddDevBeastServices();
        Services.AddSingleton<ArchitectureTools>();
        Services.AddSingleton<ScaffoldingTools>();
        Services.AddSingleton<ProjectStructureTools>();
        Services.AddSingleton<IntegrationTools>();
        Services.AddSingleton<DataTools>();
        Services.AddSingleton<InfrastructureTools>();
        Services.AddSingleton<SecurityTools>();
        Services.AddSingleton<DatabaseTools>();
        Services.AddSingleton<DiagnosticsTools>();

        Provider = Services.BuildServiceProvider();
    }

    public string ReferenceAppPath { get; }
    public string ScaffoldOutputPath { get; }
    public string MocksPath { get; }
    public string ServerProjectPath { get; }
    public ServiceProvider Provider { get; }
    private ServiceCollection Services { get; }

    public T GetTool<T>() where T : notnull => Provider.GetRequiredService<T>();

    public static bool IsMongoAvailable()
    {
        try
        {
            var client = new MongoClient("mongodb://devbeast_app:devbeast_app@localhost:27018/devbeast?authSource=devbeast");
            client.GetDatabase("devbeast").RunCommand<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static JsonDocument ParseJson(string json) => JsonDocument.Parse(json);

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DevBeast.Mcp.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate DevBeast.Mcp.sln from test output directory.");
    }

    public void Dispose() => Provider.Dispose();
}