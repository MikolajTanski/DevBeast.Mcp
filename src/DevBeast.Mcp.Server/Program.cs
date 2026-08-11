using DevBeast.Mcp.Server.Configuration;
using DevBeast.Mcp.Server.Infrastructure;
using DevBeast.Mcp.Server.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables(prefix: "DEVBEAST_");

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.Configure<DevBeastOptions>(builder.Configuration.GetSection(DevBeastOptions.SectionName));
builder.Services.AddDevBeastServices();

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolCallMetrics()
    .WithTools<DatabaseTools>()
    .WithTools<DiagnosticsTools>()
    .WithTools<MetricsTools>()
    .WithTools<ArchitectureTools>()
    .WithTools<ScaffoldingTools>()
    .WithTools<ProjectStructureTools>()
    .WithTools<IntegrationTools>()
    .WithTools<DataTools>()
    .WithTools<InfrastructureTools>()
    .WithTools<SecurityTools>();

await builder.Build().RunAsync();
