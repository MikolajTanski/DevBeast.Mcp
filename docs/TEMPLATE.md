# Template / Starter Kit

> **Status:** Template referencyjny (Full Edition) — baza pod własne serwery MCP w ekosystemie .NET.

## Czym jest ten projekt?

DevBeast MCP to **gotowy szablon lokalnego serwera MCP** — nie produkcyjna integracja z Jirą, Azure DevOps ani GitHubem. Moduły zespołowe działają w trybie **Mock**, żeby od razu testować flow agenta bez konfiguracji zewnętrznych systemów.

## Dla kogo?

- **.NET developerzy** integrujący AI z codzienną pracą
- **Zespoły** budujące własne serwery MCP na bazie sprawdzonego wzorca
- **Eksperymenty** z agentami w Cursor bez ręcznego kopiowania schematu DB i logów

## Co dostajesz out of the box

| Obszar | Narzędzia MCP |
|--------|---------------|
| Baza danych | `get_database_schema`, `execute_read_query` |
| Logi | `get_recent_errors` |
| Architektura | `ensure_project_structure`, `validate_architecture_rules` |
| Scaffolding | `scaffold_feature_slice` |
| Integracje (mock) | `get_ticket_context`, `create_pull_request_with_impact` |
| Dane testowe | `generate_test_fixtures`, `diff_environments` |
| Infrastruktura | `inspect_redis_cache`, `flush_key`, `peek_dead_letter_queue` |
| Security | `scan_secrets_and_pii`, `check_nuget_vulnerabilities` |

Szczegóły → [TOOLS.md](TOOLS.md)

## Jak użyć jako template

### 1. Sklonuj i dostosuj

```bash
git clone https://github.com/MikolajTanski/DevBeast.Mcp.git moj-projekt-mcp
cd moj-projekt-mcp
```

### 2. Postaw środowisko

Pełna instrukcja → [SETUP.md](SETUP.md)

```bash
cd docker && docker compose up -d
cp src/DevBeast.Mcp.Server/appsettings.Local.json.example \
   src/DevBeast.Mcp.Server/appsettings.Local.json
```

### 3. Podłącz pod swoją aplikację

W `.cursor/mcp.json` **projektu docelowego**:

```json
{
  "mcpServers": {
    "devbeast": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/DevBeast.Mcp.Server.csproj"],
      "env": {
        "DEVBEAST__DefaultProjectPath": "/path/to/MOJA.APLIKACJA",
        "DEVBEAST__Mongo__ConnectionString": "...",
        "DEVBEAST__Logs__Directory": "/path/to/logs"
      }
    }
  }
}
```

### 4. Wygeneruj strukturę w swoim projekcie

W chacie Cursor:

```
ensure_project_structure projectPath=/path/to/MOJA.APLIKACJA namespacePrefix=MojaApp
```

### 5. Dostosuj mocki

Edytuj pliki w `src/DevBeast.Mcp.Server/Mocks/`:

- `tickets/` — tickety Jira/ADO przypominające te z Twojego zespołu
- `environments/` — appsettings Dev/Test/Prod Twojej aplikacji

### 6. Zamień mocki na prawdziwe integracje

Gdy będziesz gotowy — implementuj interfejsy:

| Interfejs | Mock | Docelowa implementacja |
|-----------|------|------------------------|
| `ITicketService` | `MockTicketService` | `JiraTicketService` |
| `IPullRequestService` | `MockPullRequestService` | `GitHubPullRequestService` |
| `ILogService` | `FileLogService` | `ElkLogService` |
| `IDeadLetterQueueService` | `MockDeadLetterQueueService` | `ServiceBusDlqService` |

Rejestracja w `Infrastructure/ServiceRegistration.cs` — Tools i agent bez zmian.

## Roadmap

- [ ] Prawdziwa integracja Jira / Azure DevOps / GitHub
- [ ] Provider ELK / Elasticsearch dla logów
- [ ] Multi-tenant konfiguracja (wiele aplikacji, jeden serwer)
- [ ] HTTP transport (Streamable HTTP) obok stdio
- [ ] Pakiet NuGet do dystrybucji serwera

## Dokumentacja

| Dokument | Kiedy czytać |
|----------|--------------|
| [README.md](../README.md) | Start — linki do wszystkiego |
| [HOW_IT_WORKS.md](HOW_IT_WORKS.md) | Jak agent korzysta z narzędzi |
| [SETUP.md](SETUP.md) | Instalacja krok po kroku |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Warstwy kodu, rozszerzalność |
| [TOOLS.md](TOOLS.md) | Referencja 16 narzędzi |

## Autor

Mikołaj Tański — .NET Developer / AI Integration Engineer

## Licencja

Użyj swobodnie jako template w projektach wewnętrznych.
