# Architektura

Szczegółowy opis warstw kodu serwera DevBeast MCP, przepływów danych i punktów rozszerzenia.

## Cel systemu

DevBeast MCP to lokalny serwer [Model Context Protocol](https://modelcontextprotocol.io/) w .NET 9. Pełni rolę **kontrolowanej warstwy pośredniej** między agentem AI a infrastrukturą deweloperską — bez ujawniania agentowi bezpośredniego dostępu do shella, connection stringów w promptach czy nieograniczonego SQL.

## Diagram komponentów

```
┌──────────────────────────────────────────────────────────────────┐
│                     ORKIESTRATOR AI                              │
│           Cursor / Claude Desktop / Claude Code                  │
└────────────────────────────┬─────────────────────────────────────┘
                             │  JSON-RPC 2.0 / stdio
┌────────────────────────────▼─────────────────────────────────────┐
│                   DevBeast.Mcp.Server                            │
│                                                                  │
│  ┌──────────────┐    ┌───────────────┐    ┌──────────────────┐  │
│  │    Tools     │───►│   Services    │───►│  External I/O    │  │
│  │  (8 klas)    │    │  (15+ serw.)  │    │  Mongo/SQL/Redis │  │
│  │  16 narzędzi │    │               │    │  Files/Mocks     │  │
│  └──────────────┘    └───────┬───────┘    └──────────────────┘  │
│                            │                                     │
│                    ┌───────▼────────┐                            │
│                    │    Models      │                            │
│                    │  (7 folderów)  │                            │
│                    └────────────────┘                            │
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────────────┐  │
│  │ Configuration│  │  Security    │  │  Infrastructure (DI)  │  │
│  │ DevBeastOpts │  │ SqlValidator │  │  ServiceRegistration  │  │
│  └──────────────┘  └──────────────┘  └───────────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
```

## Warstwa Tools

Lokalizacja: `src/DevBeast.Mcp.Server/Tools/`

Cienka warstwa ekspozycji MCP. Każda klasa ma atrybut `[McpServerToolType]`, metody — `[McpServerTool]`.

| Klasa | Narzędzia | Deleguje do |
|-------|-----------|-------------|
| `DatabaseTools` | `get_database_schema`, `execute_read_query` | `IDatabaseService` |
| `DiagnosticsTools` | `get_recent_errors` | `ILogService` |
| `ArchitectureTools` | `validate_architecture_rules` | `IArchitectureValidationService` |
| `ProjectStructureTools` | `ensure_project_structure`, `get_project_structure` | `IProjectStructureService` |
| `ScaffoldingTools` | `scaffold_feature_slice` | `IFeatureSliceScaffolder` |
| `IntegrationTools` | `get_ticket_context`, `create_pull_request_with_impact` | `ITicketService`, `IPullRequestService` |
| `DataTools` | `generate_test_fixtures`, `diff_environments` | `IFixtureGeneratorService`, `IEnvironmentDiffService` |
| `InfrastructureTools` | `inspect_redis_cache`, `flush_key`, `peek_dead_letter_queue` | `ICacheService`, `IDeadLetterQueueService` |
| `SecurityTools` | `scan_secrets_and_pii`, `check_nuget_vulnerabilities` | `ISecretsScanner`, `INuGetVulnerabilityChecker` |

**Zasada:** Tools nie zawierają logiki biznesowej — tylko walidacja parametrów, wywołanie serwisu, serializacja JSON.

## Warstwa Services

Lokalizacja: `src/DevBeast.Mcp.Server/Services/`

| Serwis | Interfejs | Odpowiedzialność |
|--------|-----------|------------------|
| `SqlServerDatabaseService` | `IDatabaseService` | SQL: schema, SELECT |
| `MongoDatabaseService` | `IDatabaseService` | Mongo: collections, find JSON |
| `FileLogService` | `ILogService` | Parsowanie logów Serilog/JSON |
| `ArchitectureValidationService` | `IArchitectureValidationService` | Reguły CA/DDD |
| `ProjectStructureService` | `IProjectStructureService` | Skan/generacja struktury + manifest |
| `FeatureSliceScaffolder` | `IFeatureSliceScaffolder` | Vertical slice (11 plików) |
| `MockTicketService` | `ITicketService` | Tickety z JSON |
| `MockPullRequestService` | `IPullRequestService` | Mock PR + impact |
| `FixtureGeneratorService` | `IFixtureGeneratorService` | Bogus seed data |
| `EnvironmentDiffService` | `IEnvironmentDiffService` | Diff appsettings/DB |
| `RedisCacheService` | `ICacheService` | Redis + mock fallback |
| `MockDeadLetterQueueService` | `IDeadLetterQueueService` | DLQ Mongo + mock |
| `SecretsScanner` | `ISecretsScanner` | Regex scan secretów/PII |
| `NuGetVulnerabilityChecker` | `INuGetVulnerabilityChecker` | CVE audit |

### Factory providera bazy

`Infrastructure/ServiceRegistration.cs`:

```csharp
services.AddSingleton<IDatabaseService>(sp =>
{
    var options = sp.GetRequiredService<IOptions<DevBeastOptions>>().Value;
    return options.Database.Provider.Equals("MongoDB", StringComparison.OrdinalIgnoreCase)
        ? sp.GetRequiredService<MongoDatabaseService>()
        : sp.GetRequiredService<SqlServerDatabaseService>();
});
```

### Zależność scaffold → structure

`FeatureSliceScaffolder` wywołuje `IProjectStructureService.EnsureStructureAsync()` przed generowaniem plików. Dzięki temu ścieżki warstw pochodzą z manifestu, nie z hardcoded template.

## Warstwa Models

Lokalizacja: `src/DevBeast.Mcp.Server/Models/`

Jeden typ na plik, pogrupowany wg domeny:

```
Models/
├── Architecture/
│   ├── ArchitectureViolation.cs
│   └── ArchitectureValidationResult.cs
├── Database/
│   ├── ColumnInfo.cs
│   ├── DatabaseSchemaResult.cs
│   ├── ForeignKeyInfo.cs
│   ├── IndexInfo.cs
│   └── QueryResult.cs
├── Diagnostics/
│   └── AggregatedError.cs
├── Environments/
│   └── EnvironmentDiffEntry.cs
├── Infrastructure/
│   ├── CacheEntry.cs
│   └── DeadLetterMessage.cs
├── Integrations/
│   ├── PullRequestImpact.cs
│   ├── PullRequestResult.cs
│   └── TicketContext.cs
├── Project/
│   ├── ProjectLayerInfo.cs      (record w ProjectStructureResult.cs)
│   └── ProjectStructureResult.cs
└── Security/
    ├── NuGetVulnerability.cs
    └── SecretFinding.cs
```

Wszystkie typy w namespace `DevBeast.Mcp.Server.Models` — brak konieczności zmiany importów w serwisach po reorganizacji folderów.

## Konfiguracja

`Configuration/DevBeastOptions.cs` — wiązane z:

1. `appsettings.json` (domyślne)
2. `appsettings.Local.json` (gitignored, lokalne override)
3. Zmienne `DEVBEAST__*` (nadpisują pliki — używane w `mcp.json`)

## Bezpieczeństwo

### SqlQueryValidator

Lokalizacja: `Security/SqlQueryValidator.cs`

- Dozwolone: `SELECT`, `WITH ... SELECT`
- Zablokowane: `INSERT`, `UPDATE`, `DELETE`, `DROP`, `ALTER`, `TRUNCATE`, `CREATE`, `EXEC`, `MERGE`
- Walidacja słów kluczowych jako whole-word regex

### SecretsScanner

Skanuje pliki w projekcie — wynik zawiera `snippet` (obcięty), nigdy pełny secret.

## Transport MCP

```csharp
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<DatabaseTools>()
    // ... pozostałe Tools
```

- **stdout** — wyłącznie JSON-RPC MCP
- **stderr** — logi serwera (`LogToStandardErrorThreshold = Trace`)

## Manifest projektu

`.devbeast/project-structure.json` w repo docelowym:

- Commitowalny (nie gitignored)
- Generowany/aktualizowany przez `ensure_project_structure`
- Czytany przez `get_project_structure`, `FeatureSliceScaffolder`
- Invalidacja: niekompletny manifest jest ignorowany → regeneracja

## Struktura repozytorium DevBeast

```
DevBeast.Mcp/
├── docker/
│   ├── docker-compose.yml
│   └── mongo-init/01-init-devbeast.js
├── docs/                              ← dokumentacja
├── samples/
│   ├── ReferenceApp/                  ← demo + .devbeast/manifest
│   └── Scaffolded/                    ← gitignored output
├── src/DevBeast.Mcp.Server/
│   ├── Configuration/
│   ├── Infrastructure/ServiceRegistration.cs
│   ├── Mocks/                         ← tickety, environments
│   ├── Models/                        ← 7 subfolderów
│   ├── Security/
│   ├── Services/                      ← 15+ serwisów
│   ├── Tools/                         ← 8 klas MCP
│   ├── Program.cs
│   └── appsettings.json
└── tests/
    ├── DevBeast.Mcp.Server.Tests/     ← 18 testów integracyjnych
    └── DevBeast.Mcp.SmokeTest/        ← ręczny smoke test
```

## Rozszerzalność — dodanie nowego narzędzia

1. **Model** — `Models/{Domena}/NowyResult.cs`
2. **Serwis** — `Services/NowyService.cs` + interfejs `INowyService`
3. **Rejestracja DI** — `Infrastructure/ServiceRegistration.cs`
4. **Tool** — `Tools/NowyTools.cs` z `[McpServerTool]`
5. **Program.cs** — `.WithTools<NowyTools>()`
6. **Test** — `tests/.../McpToolsIntegrationTests.cs`
7. **Dokumentacja** — `docs/TOOLS.md`

### Podmiana mocka na prawdziwą integrację

Implementuj ten sam interfejs:

```csharp
// Było:
services.AddSingleton<ITicketService, MockTicketService>();

// Będzie:
services.AddSingleton<ITicketService, JiraTicketService>();
```

Tools i agent nie wymagają zmian — kontrakt JSON pozostaje ten sam.

## Testy

| Projekt | Testy | Zakres |
|---------|-------|--------|
| `DevBeast.Mcp.Server.Tests` | 18 | DI fixture + MCP stdio spawn |
| `DevBeast.Mcp.SmokeTest` | — | Ręczne wywołanie 10 narzędzi |

## Powiązane dokumenty

- [HOW_IT_WORKS.md](HOW_IT_WORKS.md) — flow agenta i scenariusze
- [SETUP.md](SETUP.md) — instalacja
- [TOOLS.md](TOOLS.md) — parametry narzędzi
