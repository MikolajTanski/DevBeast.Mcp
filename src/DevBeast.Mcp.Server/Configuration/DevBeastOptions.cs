namespace DevBeast.Mcp.Server.Configuration;

public sealed class DevBeastOptions
{
    public const string SectionName = "DevBeast";

    public string DefaultProjectPath { get; set; } = string.Empty;
    public DatabaseOptions Database { get; set; } = new();
    public MongoOptions Mongo { get; set; } = new();
    public LogsOptions Logs { get; set; } = new();
    public RedisOptions Redis { get; set; } = new();
    public IntegrationsOptions Integrations { get; set; } = new();
    public ScaffoldingOptions Scaffolding { get; set; } = new();
    public MetricsOptions Metrics { get; set; } = new();
}

public sealed class MetricsOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Log each tool call to stderr (visible in Cursor MCP logs).
    /// </summary>
    public bool LogEachCall { get; set; }
}

public sealed class DatabaseOptions
{
    public string Provider { get; set; } = "SqlServer";
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class MongoOptions
{
    public string ConnectionString { get; set; } = "mongodb://devbeast_app:devbeast_app@localhost:27018/devbeast?authSource=devbeast";
    public string DatabaseName { get; set; } = "devbeast";
}

public sealed class LogsOptions
{
    public string Provider { get; set; } = "File";
    public string Directory { get; set; } = string.Empty;
    public string FilePattern { get; set; } = "*.log";
    public string EnvironmentFilter { get; set; } = string.Empty;
}

public sealed class RedisOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public bool UseMockWhenUnavailable { get; set; } = true;
}

public sealed class IntegrationsOptions
{
    public string Mode { get; set; } = "Mock";
    public string MockDataPath { get; set; } = "Mocks";
}

public sealed class ScaffoldingOptions
{
    public string OutputRoot { get; set; } = string.Empty;
    public string NamespacePrefix { get; set; } = "App";
}
