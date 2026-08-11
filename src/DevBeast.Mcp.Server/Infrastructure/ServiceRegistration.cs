using DevBeast.Mcp.Server.Configuration;
using DevBeast.Mcp.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DevBeast.Mcp.Server.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddDevBeastServices(this IServiceCollection services)
    {
        services.AddSingleton<IDatabaseService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DevBeastOptions>>().Value;
            return options.Database.Provider.Equals("MongoDB", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<MongoDatabaseService>()
                : sp.GetRequiredService<SqlServerDatabaseService>();
        });

        services.AddSingleton<SqlServerDatabaseService>();
        services.AddSingleton<MongoDatabaseService>();
        services.AddSingleton<ILogService, FileLogService>();
        services.AddSingleton<IArchitectureValidationService, ArchitectureValidationService>();
        services.AddSingleton<IProjectStructureService, ProjectStructureService>();
        services.AddSingleton<IFeatureSliceScaffolder, FeatureSliceScaffolder>();
        services.AddSingleton<ITicketService, MockTicketService>();
        services.AddSingleton<IPullRequestService, MockPullRequestService>();
        services.AddSingleton<IFixtureGeneratorService, FixtureGeneratorService>();
        services.AddSingleton<IEnvironmentDiffService, EnvironmentDiffService>();
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddSingleton<IDeadLetterQueueService, MockDeadLetterQueueService>();
        services.AddSingleton<ISecretsScanner, SecretsScanner>();
        services.AddSingleton<INuGetVulnerabilityChecker, NuGetVulnerabilityChecker>();

        return services;
    }
}
