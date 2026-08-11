using DevBeast.Mcp.Server.Configuration;
using Microsoft.Extensions.Options;

namespace DevBeast.Mcp.Server.Services;

public interface IFeatureSliceScaffolder
{
    Task<IReadOnlyList<string>> ScaffoldAsync(
        string featureName,
        string? projectPath = null,
        CancellationToken cancellationToken = default);
}

public sealed class FeatureSliceScaffolder(
    IOptions<DevBeastOptions> options,
    IProjectStructureService projectStructureService) : IFeatureSliceScaffolder
{
    public async Task<IReadOnlyList<string>> ScaffoldAsync(
        string featureName,
        string? projectPath = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(featureName))
        {
            throw new ArgumentException("Feature name is required.", nameof(featureName));
        }

        var root = projectPath
            ?? options.Value.Scaffolding.OutputRoot
            ?? options.Value.DefaultProjectPath;

        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException("Output path not configured. Provide projectPath or set DevBeast:Scaffolding:OutputRoot.");
        }

        var structure = await projectStructureService.EnsureStructureAsync(
            root, generateIfMissing: true, options.Value.Scaffolding.NamespacePrefix, cancellationToken);

        var ns = structure.NamespacePrefix;
        var feature = SanitizeName(featureName);
        var createdFiles = new List<string>();

        string Layer(string layer, string suffix) =>
            $"{ResolveLayerPath(structure, layer, ns)}/{suffix}";

        var files = new Dictionary<string, string>
        {
            [Layer("Domain", $"{feature}/{feature}.cs")] = DomainEntityTemplate(ns, feature),
            [Layer("Application", $"{feature}/Commands/Create{feature}Command.cs")] = CommandTemplate(ns, feature),
            [Layer("Application", $"{feature}/Commands/Create{feature}Handler.cs")] = HandlerTemplate(ns, feature),
            [Layer("Application", $"{feature}/Queries/Get{feature}ByIdQuery.cs")] = QueryTemplate(ns, feature),
            [Layer("Application", $"{feature}/Queries/Get{feature}ByIdHandler.cs")] = QueryHandlerTemplate(ns, feature),
            [Layer("Application", $"{feature}/Dtos/{feature}Dto.cs")] = DtoTemplate(ns, feature),
            [Layer("Application", $"{feature}/Mapping/{feature}Profile.cs")] = MapperTemplate(ns, feature),
            [Layer("Infrastructure", $"Persistence/Configurations/{feature}Configuration.cs")] = EfConfigTemplate(ns, feature),
            [Layer("Infrastructure", $"Persistence/Migrations/Add{feature}Migration.cs")] = MigrationTemplate(ns, feature),
            [Layer("Api", $"Controllers/{feature}Controller.cs")] = ControllerTemplate(ns, feature),
            [$"{ResolveLayerPath(structure, "Tests", ns)}/{feature}/Create{feature}HandlerTests.cs"] = TestTemplate(ns, feature)
        };

        foreach (var (relativePath, content) in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullPath = Path.Combine(root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            if (!File.Exists(fullPath))
            {
                await File.WriteAllTextAsync(fullPath, content, cancellationToken);
                createdFiles.Add(fullPath);
            }
        }

        return createdFiles;
    }

    private static string ResolveLayerPath(Models.ProjectStructureResult structure, string layer, string ns) =>
        ProjectStructureService.GetLayerPath(structure, layer)
        ?? layer switch
        {
            "Tests" => $"tests/{ns}.Application.Tests",
            _ => $"src/{ns}.{layer}"
        };

    private static string SanitizeName(string name) =>
        string.Concat(name.Where(char.IsLetterOrDigit));

    private static string DomainEntityTemplate(string ns, string feature) => $$"""
        namespace {{ns}}.Domain.{{feature}};

        public sealed class {{feature}}
        {
            public Guid Id { get; private set; }
            public string Name { get; private set; } = string.Empty;
            public DateTimeOffset CreatedAt { get; private set; }

            private {{feature}}() { }

            public static {{feature}} Create(string name) =>
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    CreatedAt = DateTimeOffset.UtcNow
                };
        }
        """;

    private static string CommandTemplate(string ns, string feature) => $$"""
        using MediatR;

        namespace {{ns}}.Application.{{feature}}.Commands;

        public sealed record Create{{feature}}Command(string Name) : IRequest<Guid>;
        """;

    private static string HandlerTemplate(string ns, string feature) => $$"""
        using MediatR;
        using {{ns}}.Domain.{{feature}};

        namespace {{ns}}.Application.{{feature}}.Commands;

        public sealed class Create{{feature}}Handler : IRequestHandler<Create{{feature}}Command, Guid>
        {
            public Task<Guid> Handle(Create{{feature}}Command request, CancellationToken cancellationToken)
            {
                var entity = {{feature}}.Create(request.Name);
                // TODO: persist via I{{feature}}Repository
                return Task.FromResult(entity.Id);
            }
        }
        """;

    private static string QueryTemplate(string ns, string feature) => $$"""
        using MediatR;
        using {{ns}}.Application.{{feature}}.Dtos;

        namespace {{ns}}.Application.{{feature}}.Queries;

        public sealed record Get{{feature}}ByIdQuery(Guid Id) : IRequest<{{feature}}Dto?>;
        """;

    private static string QueryHandlerTemplate(string ns, string feature) => $$"""
        using MediatR;
        using {{ns}}.Application.{{feature}}.Dtos;

        namespace {{ns}}.Application.{{feature}}.Queries;

        public sealed class Get{{feature}}ByIdHandler : IRequestHandler<Get{{feature}}ByIdQuery, {{feature}}Dto?>
        {
            public Task<{{feature}}Dto?> Handle(Get{{feature}}ByIdQuery request, CancellationToken cancellationToken)
            {
                // TODO: load from repository
                return Task.FromResult<{{feature}}Dto?>(null);
            }
        }
        """;

    private static string DtoTemplate(string ns, string feature) => $$"""
        namespace {{ns}}.Application.{{feature}}.Dtos;

        public sealed record {{feature}}Dto(Guid Id, string Name, DateTimeOffset CreatedAt);
        """;

    private static string MapperTemplate(string ns, string feature) => $$"""
        using AutoMapper;
        using {{ns}}.Application.{{feature}}.Dtos;
        using {{ns}}.Domain.{{feature}};

        namespace {{ns}}.Application.{{feature}}.Mapping;

        public sealed class {{feature}}Profile : Profile
        {
            public {{feature}}Profile()
            {
                CreateMap<{{feature}}, {{feature}}Dto>();
            }
        }
        """;

    private static string EfConfigTemplate(string ns, string feature) => $$"""
        using Microsoft.EntityFrameworkCore;
        using Microsoft.EntityFrameworkCore.Metadata.Builders;
        using {{ns}}.Domain.{{feature}};

        namespace {{ns}}.Infrastructure.Persistence.Configurations;

        public sealed class {{feature}}Configuration : IEntityTypeConfiguration<{{feature}}>
        {
            public void Configure(EntityTypeBuilder<{{feature}}> builder)
            {
                builder.ToTable("{{feature}}s");
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
            }
        }
        """;

    private static string MigrationTemplate(string ns, string feature) => $$"""
        using Microsoft.EntityFrameworkCore.Migrations;

        namespace {{ns}}.Infrastructure.Persistence.Migrations;

        public partial class Add{{feature}}Migration : Migration
        {
            protected override void Up(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.CreateTable(
                    name: "{{feature}}s",
                    columns: table => new
                    {
                        Id = table.Column<Guid>(nullable: false),
                        Name = table.Column<string>(maxLength: 200, nullable: false),
                        CreatedAt = table.Column<DateTimeOffset>(nullable: false)
                    },
                    constraints: table => table.PrimaryKey("PK_{{feature}}s", x => x.Id));
            }

            protected override void Down(MigrationBuilder migrationBuilder)
            {
                migrationBuilder.DropTable(name: "{{feature}}s");
            }
        }
        """;

    private static string ControllerTemplate(string ns, string feature) => $$"""
        using MediatR;
        using Microsoft.AspNetCore.Mvc;
        using {{ns}}.Application.{{feature}}.Commands;
        using {{ns}}.Application.{{feature}}.Queries;

        namespace {{ns}}.Api.Controllers;

        [ApiController]
        [Route("api/[controller]")]
        public sealed class {{feature}}Controller(IMediator mediator) : ControllerBase
        {
            [HttpPost]
            public async Task<IActionResult> Create([FromBody] Create{{feature}}Command command, CancellationToken ct) =>
                Ok(await mediator.Send(command, ct));

            [HttpGet("{id:guid}")]
            public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
                Ok(await mediator.Send(new Get{{feature}}ByIdQuery(id), ct));
        }
        """;

    private static string TestTemplate(string ns, string feature) => $$"""
        using {{ns}}.Application.{{feature}}.Commands;
        using Xunit;

        namespace {{ns}}.Application.Tests.{{feature}};

        public sealed class Create{{feature}}HandlerTests
        {
            [Fact]
            public async Task Handle_ValidCommand_ReturnsGuid()
            {
                var handler = new Create{{feature}}Handler();
                var result = await handler.Handle(new Create{{feature}}Command("Test"), CancellationToken.None);
                Assert.NotEqual(Guid.Empty, result);
            }
        }
        """;
}
