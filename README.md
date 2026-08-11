# DevBeast MCP — Full Edition

Lokalny serwer MCP w .NET 9 dla agentów AI (Cursor, Claude Desktop, Claude Code). Automatyzuje pracę deweloperską: baza danych, logi, architektura, scaffolding, integracje (mock), Redis, DLQ, security.

## Stack

- .NET 9 / C# · MCP C# SDK (stdio)
- MongoDB + Redis (Docker) · MS SQL Server (opcjonalnie)
- Mocki: Jira, Azure DevOps, GitHub PR, RabbitMQ DLQ

## Szybki start

### 1. Uruchom infrastrukturę Docker

```bash
cd docker
docker compose up -d
```

To startuje:
- **MongoDB** `localhost:27018` — baza `devbeast` z przykładowymi kolekcjami (`products`, `orders`, `customers`, `deadLetterMessages`)
- **Redis** `localhost:6379` — cache (fallback na mock gdy niedostępny)

### 2. Skonfiguruj serwer MCP

```bash
cd src/DevBeast.Mcp.Server
cp appsettings.Local.json.example appsettings.Local.json
dotnet build
dotnet run
```

### 3. Podłącz w Cursor

Konfiguracja jest już gotowa w:
- `~/.cursor/mcp.json` — globalnie (działa we wszystkich projektach)
- `.cursor/mcp.json` — workspace DevBeast (używa `${workspaceFolder}`)

Po zmianie: przeładuj okno Cursor lub zrestartuj serwery MCP w Settings → MCP.

```json
{
  "mcpServers": {
    "devbeast": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/Users/mikolajtanski/RiderProjects/DevBeast.Mcp/src/DevBeast.Mcp.Server/DevBeast.Mcp.Server.csproj"
      ],
      "env": {
        "DEVBEAST__Mongo__ConnectionString": "mongodb://devbeast_app:devbeast_app@localhost:27018/devbeast?authSource=devbeast",
        "DEVBEAST__DefaultProjectPath": "/Users/mikolajtanski/RiderProjects/DevBeast.Mcp/samples/ReferenceApp"
      }
    }
  }
}
```

## Testy integracyjne

```bash
cd docker && docker compose up -d   # MongoDB + Redis
dotnet test                          # 15 testów (DI + MCP stdio)
```

Projekt `tests/DevBeast.Mcp.Server.Tests`:
- 13 testów narzędzi przez DI fixture
- 2 testy pełnej integracji MCP (spawn serwera + `ListTools` / `CallTool`)

## Narzędzia MCP (14)

### Baza danych i diagnostyka
| Narzędzie | Opis |
|-----------|------|
| `get_database_schema` | Schemat tabeli/kolekcji lub `*` |
| `execute_read_query` | SQL SELECT (SqlServer) lub JSON find (MongoDB) |
| `get_recent_errors` | Agregacja błędów z logów |

### Architektura i scaffolding
| Narzędzie | Opis |
|-----------|------|
| `validate_architecture_rules` | Clean Architecture / DDD — Domain bez EF, immutable DTO |
| `scaffold_feature_slice` | Vertical Slice: Entity, CQRS, Migration, Controller, Tests |

### Integracje zespołowe (Mock)
| Narzędzie | Opis |
|-----------|------|
| `get_ticket_context` | Ticket z Jiry/ADO — mock: `PROJ-142`, `ADO-891` |
| `create_pull_request_with_impact` | PR + analiza ryzyka (API, DB, testy) |

### Dane i środowiska
| Narzędzie | Opis |
|-----------|------|
| `generate_test_fixtures` | Seed data C# (Bogus) ze schematu bazy |
| `diff_environments` | Porównanie appsettings Dev/Test/Prod lub schematu DB |

### Infrastruktura
| Narzędzie | Opis |
|-----------|------|
| `inspect_redis_cache` | Podgląd kluczy Redis (JSON decode) |
| `flush_key` | Unieważnienie klucza cache |
| `peek_dead_letter_queue` | Wiadomości z DLQ (MongoDB / mock) |

### Security
| Narzędzie | Opis |
|-----------|------|
| `scan_secrets_and_pii` | Skan haseł, tokenów JWT, PII (RODO) |
| `check_nuget_vulnerabilities` | CVE w paczkach NuGet |

## Przykładowe wywołania w chacie

```
Pobierz kontekst ticketu PROJ-142 i napraw NullReferenceException
```

```
Zscaffolduj feature slice GetProductsByCategory w samples/Scaffolded
```

```
Sprawdź reguły Clean Architecture w samples/ReferenceApp
```

```
Wygeneruj fixtures dla kolekcji products (20 rekordów)
```

```
Porównaj appsettings między Dev, Test i Prod
```

```
Podejrzyj Redis cache:products:* i DLQ orders.processing
```

## Mocki

| Zasób | Lokalizacja |
|-------|-------------|
| Tickety Jira/ADO | `Mocks/tickets/PROJ-142.json`, `ADO-891.json` |
| appsettings Dev/Test/Prod | `Mocks/environments/` |
| Redis (fallback) | Wbudowany mock store w `RedisCacheService` |
| DLQ | Kolekcja MongoDB `deadLetterMessages` + hardcoded fallback |
| PR / Jira API | `MockPullRequestService`, `MockTicketService` |

## Przełączanie providera bazy

```json
"Database": { "Provider": "SqlServer", "ConnectionString": "..." }
// lub
"Database": { "Provider": "MongoDB" }
```

Mongo query example:
```json
{"collection":"orders","filter":{"status":"Pending"},"limit":10}
```

## Struktura projektu

```
DevBeast.Mcp/
├── docker/                  # MongoDB + Redis
├── tests/DevBeast.Mcp.Server.Tests/  # 15 testów integracyjnych
├── samples/
│   ├── ReferenceApp/        # Demo z naruszeniami architektury
│   └── Scaffolded/          # Output scaffold_feature_slice
└── src/DevBeast.Mcp.Server/
    ├── Mocks/               # Tickety, environments
    ├── Services/            # Logika biznesowa + mocki
    └── Tools/               # MCP tools (8 klas)
```

## Autor

Mikołaj Tański — .NET Developer / AI Integration Engineer
