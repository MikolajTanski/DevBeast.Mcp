# Architektura DevBeast MCP

## Cel

DevBeast MCP to lokalny serwer [Model Context Protocol](https://modelcontextprotocol.io/) napisany w .NET 9. Działa jako most między agentem AI (Cursor, Claude Desktop, Claude Code) a infrastrukturą deweloperską: bazami danych, logami, cache, kolejkami i procesami zespołowymi.

## Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    ORKIESTRATOR AI                          │
│              (Cursor / Claude Desktop / Claude Code)        │
└───────────────────────────┬─────────────────────────────────┘
                            │  JSON-RPC 2.0 / stdio
┌───────────────────────────▼─────────────────────────────────┐
│                 DevBeast.Mcp.Server (.NET 9)                  │
│                                                             │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │    Tools    │  │   Services   │  │  Infrastructure  │  │
│  │  (MCP API)  │──│  (logika)    │──│  (DI, config)    │  │
│  └─────────────┘  └──────────────┘  └──────────────────┘  │
│         │                 │                                 │
│         │          ┌──────┴──────┐                          │
│         │          │   Models    │                          │
│         │          │ (kontrakty) │                          │
│         │          └─────────────┘                          │
└─────────┼─────────────────┼─────────────────────────────────┘
          │                 │
          ▼                 ▼
   [ MongoDB / SQL ]  [ Redis / Logi / Mocki Jira·ADO·PR ]
```

## Warstwy

### 1. Tools (`src/DevBeast.Mcp.Server/Tools/`)

Warstwa ekspozycji MCP. Każda klasa oznaczona `[McpServerToolType]` mapuje metody C# na narzędzia JSON-RPC widoczne dla agenta.

| Klasa | Narzędzia |
|-------|-----------|
| `DatabaseTools` | `get_database_schema`, `execute_read_query` |
| `DiagnosticsTools` | `get_recent_errors` |
| `ArchitectureTools` | `validate_architecture_rules` |
| `ScaffoldingTools` | `scaffold_feature_slice` |
| `IntegrationTools` | `get_ticket_context`, `create_pull_request_with_impact` |
| `DataTools` | `generate_test_fixtures`, `diff_environments` |
| `InfrastructureTools` | `inspect_redis_cache`, `flush_key`, `peek_dead_letter_queue` |
| `SecurityTools` | `scan_secrets_and_pii`, `check_nuget_vulnerabilities` |

Tools są cienkie — delegują do serwisów i serializują wynik do JSON.

### 2. Services (`src/DevBeast.Mcp.Server/Services/`)

Logika biznesowa i integracje. Serwisy implementują interfejsy (`IDatabaseService`, `ITicketService`, …) i są rejestrowane w DI.

| Serwis | Odpowiedzialność |
|--------|------------------|
| `SqlServerDatabaseService` / `MongoDatabaseService` | Odczyt schematu i zapytań (read-only) |
| `FileLogService` | Parsowanie logów Serilog/JSON |
| `ArchitectureValidationService` | Reguły Clean Architecture / DDD |
| `FeatureSliceScaffolder` | Generowanie Vertical Slice |
| `MockTicketService` / `MockPullRequestService` | Mocki Jira, Azure DevOps, GitHub |
| `FixtureGeneratorService` | Seed data (Bogus) ze schematu DB |
| `EnvironmentDiffService` | Porównanie appsettings / schematu DB |
| `RedisCacheService` | Redis + fallback mock |
| `MockDeadLetterQueueService` | DLQ z MongoDB / hardcoded mock |
| `SecretsScanner` | Wykrywanie secretów i PII |
| `NuGetVulnerabilityChecker` | Audyt paczek NuGet (CVE) |

Factory providera bazy: `Infrastructure/ServiceRegistration.cs` wybiera SQL lub Mongo na podstawie `DevBeast:Database:Provider`.

### 3. Models (`src/DevBeast.Mcp.Server/Models/`)

Kontrakty danych — jeden typ na plik, pogrupowane wg domeny:

```
Models/
├── Architecture/     ArchitectureViolation, ArchitectureValidationResult
├── Database/         ColumnInfo, DatabaseSchemaResult, QueryResult, …
├── Diagnostics/      AggregatedError
├── Environments/     EnvironmentDiffEntry
├── Infrastructure/   CacheEntry, DeadLetterMessage
├── Integrations/     TicketContext, PullRequestImpact, PullRequestResult
└── Security/         SecretFinding, NuGetVulnerability
```

### 4. Configuration (`Configuration/DevBeastOptions.cs`)

Opcje wiązane z `appsettings.json`, `appsettings.Local.json` i zmiennymi `DEVBEAST_*`.

### 5. Security (`Security/SqlQueryValidator.cs`)

Walidacja zapytań SQL — tylko SELECT/WITH, blokada INSERT/UPDATE/DELETE/DROP/ALTER.

## Transport

Serwer komunikuje się po **stdio** (JSON-RPC 2.0). Logi idą na stderr, stdout jest zarezerwowany dla MCP.

```csharp
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<DatabaseTools>()
    // …
```

## Infrastruktura lokalna (Docker)

| Usługa | Port | Rola |
|--------|------|------|
| MongoDB | `27018` | Przykładowa baza `devbeast` (products, orders, DLQ) |
| Redis | `6379` | Cache (fallback mock gdy niedostępny) |

> Port MongoDB: **27018** (nie 27017) — unika konfliktu z lokalnym `mongod`.

## Testy

| Projekt | Zakres |
|---------|--------|
| `tests/DevBeast.Mcp.Server.Tests` | 15 testów integracyjnych (DI + MCP stdio) |
| `tests/DevBeast.Mcp.SmokeTest` | Ręczny smoke test wszystkich narzędzi |

## Rozszerzalność

Nowe narzędzie = nowy serwis + klasa Tool + rejestracja w `ServiceRegistration` i `Program.cs`. Mocki można podmienić na prawdziwe integracje implementując ten sam interfejs (np. `ITicketService` → `JiraTicketService`).
