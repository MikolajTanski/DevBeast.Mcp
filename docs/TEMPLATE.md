# DevBeast MCP — Template / Starter Kit

> **Status:** Template referencyjny (Core/Full Edition) — baza pod przyszłe projekty MCP w ekosystemie .NET.

## Czym jest ten projekt?

DevBeast MCP to **gotowy szablon lokalnego serwera MCP** dla agentów AI. Nie jest jeszcze produkcyjną integracją z Jirą, Azure DevOps ani GitHubem — te moduły działają w trybie **Mock**, aby można było od razu testować flow agenta bez konfiguracji zewnętrznych systemów.

Projekt powstał jako odpowiedź na pytanie:

> *„Jak dać AI bezpieczny, kontrolowany dostęp do bazy, logów i procesów deweloperskich w .NET?”*

## Co robi dziś (out of the box)

| Obszar | Działanie |
|--------|-----------|
| **Baza danych** | Odczyt schematu i SELECT (SQL Server) lub find (MongoDB) |
| **Logi** | Agregacja ostatnich błędów z plików Serilog/JSON |
| **Architektura** | Walidacja Clean Architecture / DDD w kodzie C# |
| **Scaffolding** | Generowanie Vertical Slice (Entity, CQRS, Controller, testy) |
| **Integracje** | Mock tickety (Jira/ADO) i mock PR z analizą ryzyka |
| **Dane testowe** | Generowanie fixture'ów C# (Bogus) ze schematu DB |
| **Środowiska** | Diff appsettings Dev/Test/Prod |
| **Infrastruktura** | Redis cache inspect/flush, peek DLQ |
| **Security** | Skan secretów/PII, audyt CVE w NuGet |

## Co to jest „template na kiedyś”

Ten repozytorium jest **punktem startowym**, nie finalnym produktem. Przeznaczone jest do:

1. **Klonowania** jako baza pod nowy serwer MCP dla konkretnej aplikacji / zespołu
2. **Podmiany mocków** na prawdziwe integracje (Jira REST, Azure DevOps API, GitHub CLI)
3. **Rozszerzenia** o kolejne narzędzia specyficzne dla danej domeny biznesowej
4. **Podłączenia** pod Cursor / Claude Desktop przez `mcp.json`

### Roadmap (planowane rozszerzenia)

- [ ] Prawdziwa integracja Jira / Azure DevOps / GitHub
- [ ] Provider ELK / Elasticsearch dla logów
- [ ] Multi-tenant konfiguracja (wiele aplikacji, jeden serwer)
- [ ] HTTP transport (Streamable HTTP) obok stdio
- [ ] Pakiet NuGet do dystrybucji serwera

## Jak użyć jako template

```bash
# 1. Sklonuj
git clone git@github.com:MikolajTanski/DevBeast.Mcp.git moj-projekt-mcp
cd moj-projekt-mcp

# 2. Uruchom infrastrukturę
cd docker && docker compose up -d

# 3. Skonfiguruj
cp src/DevBeast.Mcp.Server/appsettings.Local.json.example \
   src/DevBeast.Mcp.Server/appsettings.Local.json
# → ustaw connection string, ścieżki logów, DefaultProjectPath

# 4. Podłącz w Cursor (~/.cursor/mcp.json lub .cursor/mcp.json)
# 5. Dostosuj mocki w src/DevBeast.Mcp.Server/Mocks/
# 6. Zamień Mock*Service na prawdziwe implementacje gdy będziesz gotowy
```

## Struktura kluczowych folderów

```
DevBeast.Mcp/
├── docs/
│   ├── ARCHITECTURE.md    ← szczegółowa architektura
│   └── TEMPLATE.md        ← ten plik
├── docker/                ← MongoDB (:27018) + Redis
├── samples/
│   └── ReferenceApp/      ← przykładowa app z naruszeniami architektury (do testów)
├── src/DevBeast.Mcp.Server/
│   ├── Tools/             ← warstwa MCP (API dla agenta)
│   ├── Services/          ← logika biznesowa
│   ├── Models/            ← kontrakty (1 typ = 1 plik, pogrupowane wg domeny)
│   ├── Mocks/             ← dane testowe (tickety, environments)
│   └── Configuration/     ← DevBeastOptions
└── tests/                 ← testy integracyjne + smoke test
```

## Dla kogo?

- **.NET developerzy** integrujący AI z codzienną pracą
- **Zespoły** budujące własne serwery MCP na bazie sprawdzonego wzorca
- **Eksperymenty** z agentami w Cursor / Claude bez ręcznego kopiowania schematu DB i logów

## Autor

Mikołaj Tański — .NET Developer / AI Integration Engineer

## Licencja

Użyj swobodnie jako template w projektach wewnętrznych. Dostosuj mocki i konfigurację do własnych potrzeb.
