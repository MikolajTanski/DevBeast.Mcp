# Instalacja i konfiguracja

Przewodnik krok po kroku: od sklonowania repo do działającego serwera MCP w Cursor.

![DevBeast — szybka instalacja](assets/devbeast-setup.png)

## Wymagania

| Wymaganie | Wersja | Uwagi |
|-----------|--------|-------|
| .NET SDK | 9.0+ | `dotnet --version` |
| Docker | 20+ | MongoDB + Redis |
| Cursor | dowolna | lub Claude Desktop / Claude Code |
| (opcja) mongosh | 2.x | do ręcznej weryfikacji MongoDB |

## 1. Klonowanie

```bash
git clone https://github.com/MikolajTanski/DevBeast.Mcp.git
cd DevBeast.Mcp
```

## 2. Infrastruktura Docker

```bash
cd docker
docker compose up -d
docker compose ps   # oba kontenery powinny być healthy
```

### Co startuje

| Usługa | Port hosta | Credentials | Baza / rola |
|--------|------------|-------------|-------------|
| MongoDB | **27018** | `devbeast_app` / `devbeast_app` | `devbeast` — products, orders, customers, deadLetterMessages |
| Redis | **6379** | brak hasła | cache |

> **Uwaga:** Port MongoDB to **27018**, nie 27017. Na macOS często działa lokalny `mongod` na 27017 — Docker mapuje na 27018, żeby uniknąć konfliktu.

### Weryfikacja MongoDB

```bash
mongosh "mongodb://devbeast_app:devbeast_app@localhost:27018/devbeast?authSource=devbeast" \
  --eval 'db.products.countDocuments()'
# Oczekiwany wynik: 3
```

### Weryfikacja Redis

```bash
redis-cli ping
# Oczekiwany wynik: PONG
```

### Zatrzymanie

```bash
cd docker
docker compose down        # zatrzymaj
docker compose down -v     # zatrzymaj + usuń volumes (reset danych)
```

## 3. Konfiguracja serwera MCP

```bash
cd src/DevBeast.Mcp.Server
cp appsettings.Local.json.example appsettings.Local.json
```

### appsettings.Local.json — minimalna konfiguracja

```json
{
  "DevBeast": {
    "DefaultProjectPath": "/ABSOLUTNA/SCIEZKA/do/samples/ReferenceApp",
    "Mongo": {
      "ConnectionString": "mongodb://devbeast_app:devbeast_app@localhost:27018/devbeast?authSource=devbeast",
      "DatabaseName": "devbeast"
    },
    "Logs": {
      "Directory": "/tmp/devbeast-logs"
    },
    "Scaffolding": {
      "OutputRoot": "/ABSOLUTNA/SCIEZKA/do/samples/Scaffolded"
    }
  }
}
```

### Wszystkie opcje konfiguracji

| Klucz | Opis | Domyślnie |
|-------|------|-----------|
| `DefaultProjectPath` | Projekt docelowy dla architektury, security scan | `""` |
| `Database:Provider` | `MongoDB` lub `SqlServer` | `MongoDB` |
| `Database:ConnectionString` | Connection string SQL (gdy SqlServer) | `""` |
| `Mongo:ConnectionString` | Connection string MongoDB | `localhost:27018` |
| `Mongo:DatabaseName` | Nazwa bazy | `devbeast` |
| `Logs:Directory` | Katalog plików logów | `""` |
| `Logs:FilePattern` | Glob logów | `*.log` |
| `Redis:ConnectionString` | Redis | `localhost:6379` |
| `Redis:UseMockWhenUnavailable` | Fallback mock gdy Redis down | `true` |
| `Integrations:Mode` | `Mock` (Jira/ADO/PR) | `Mock` |
| `Integrations:MockDataPath` | Folder mocków | `Mocks` |
| `Scaffolding:OutputRoot` | Domyślny output scaffold | `""` |
| `Scaffolding:NamespacePrefix` | Prefix namespace (np. `App`) | `App` |

### Zmienne środowiskowe

Prefiks: `DEVBEAST__` (podwójny underscore = zagnieżdżenie).

```bash
export DEVBEAST__Mongo__ConnectionString="mongodb://devbeast_app:devbeast_app@localhost:27018/devbeast?authSource=devbeast"
export DEVBEAST__DefaultProjectPath="/path/to/your/app"
```

W `mcp.json` Cursor używa tego samego formatu w sekcji `env`.

## 4. Build i uruchomienie

```bash
# Z katalogu głównego repo
dotnet build

# Uruchom serwer (stdio — czeka na input MCP, nie kończy się od razu)
dotnet run --project src/DevBeast.Mcp.Server
```

Serwer MCP nie jest przeznaczony do ręcznego użytkowania w terminalu — Cursor go spawnuje automatycznie.

## 5. Konfiguracja Cursor MCP

### Globalnie (`~/.cursor/mcp.json`)

Działa we wszystkich projektach:

```json
{
  "mcpServers": {
    "devbeast": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/ABSOLUTNA/SCIEZKA/DevBeast.Mcp/src/DevBeast.Mcp.Server/DevBeast.Mcp.Server.csproj",
        "--no-build"
      ],
      "env": {
        "DEVBEAST__Mongo__ConnectionString": "mongodb://devbeast_app:devbeast_app@localhost:27018/devbeast?authSource=devbeast",
        "DEVBEAST__Mongo__DatabaseName": "devbeast",
        "DEVBEAST__Database__Provider": "MongoDB",
        "DEVBEAST__DefaultProjectPath": "/ABSOLUTNA/SCIEZKA/DevBeast.Mcp/samples/ReferenceApp",
        "DEVBEAST__Scaffolding__OutputRoot": "/ABSOLUTNA/SCIEZKA/DevBeast.Mcp/samples/Scaffolded"
      }
    }
  }
}
```

### Workspace (`.cursor/mcp.json` w repo)

Używa `${workspaceFolder}` — lepsze gdy pracujesz w samym repo DevBeast:

```json
{
  "mcpServers": {
    "devbeast": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "${workspaceFolder}/src/DevBeast.Mcp.Server/DevBeast.Mcp.Server.csproj"
      ],
      "env": {
        "DEVBEAST__Mongo__ConnectionString": "mongodb://devbeast_app:devbeast_app@localhost:27018/devbeast?authSource=devbeast",
        "DEVBEAST__DefaultProjectPath": "${workspaceFolder}/samples/ReferenceApp"
      }
    }
  }
}
```

### Per aplikacja docelowa

Gdy pracujesz nad **inną** aplikacją, dodaj wpis MCP w `.cursor/mcp.json` **tego projektu** z własnym:

- `DEVBEAST__DefaultProjectPath` → ścieżka do tej aplikacji
- `DEVBEAST__Database__ConnectionString` → baza tej aplikacji
- `DEVBEAST__Logs__Directory` → logi tej aplikacji

Ten sam binarny serwer DevBeast, inna konfiguracja `env`.

### Aktywacja

```mermaid
flowchart LR
    A[Zapisz mcp.json] --> B[Settings → MCP]
    B --> C{Status devbeast?}
    C -->|Zielony| D[Gotowe — użyj Agent mode]
    C -->|Czerwony| E[dotnet build + Reload Window]
    E --> B
```

1. Zapisz `mcp.json`
2. Cursor → **Settings → MCP** — serwer `devbeast` powinien być zielony
3. Jeśli nie: przeładuj okno (`Cmd+Shift+P` → Reload Window)

> **Tip:** opcjonalny zrzut ekranu zielonego statusu MCP możesz dodać do `docs/assets/screenshots/cursor-mcp-green.png` — patrz [assets/README.md](assets/README.md).

## 6. Testy

```bash
# Pełna suite (19 testów)
dotnet test

# Smoke test (ręczne wywołanie wszystkich narzędzi)
dotnet run --project tests/DevBeast.Mcp.SmokeTest

# Tylko testy MCP stdio
dotnet test --filter "McpStdio"
```

## 7. Troubleshooting

### MongoDB: Authentication failed na porcie 27017

**Przyczyna:** Łączysz się z lokalnym `mongod`, nie z Dockerem.

**Fix:** Użyj portu **27018** w connection string.

### MCP serwer czerwony w Cursor

| Symptom | Fix |
|---------|-----|
| `dotnet not found` | Upewnij się, że `dotnet` jest w PATH Cursora |
| Build error | Uruchom `dotnet build` ręcznie i napraw błędy |
| Brak narzędzi | Sprawdź logi MCP w Settings → MCP → devbeast |
| Stary config | Przeładuj okno po zmianie `mcp.json` |

### Redis: entries=0, mock=false

Normalne — Docker Redis startuje pusty. DevBeast używa mock store gdy Redis niedostępny (`UseMockWhenUnavailable: true`).

### Logi: brak błędów w get_recent_errors

Ustaw `DevBeast:Logs:Directory` na katalog z plikami `.log` (Serilog JSON lub text). Utwórz katalog jeśli nie istnieje.

### ensure_project_structure: pusty manifest

Usuń `.devbeast/project-structure.json` i wywołaj ponownie z `generateIfMissing: true`. Stary pusty manifest blokował regenerację (naprawione w aktualnej wersji).

## 8. SQL Server (opcjonalnie)

```json
{
  "DevBeast": {
    "Database": {
      "Provider": "SqlServer",
      "ConnectionString": "Server=localhost;Database=ShopDb;Trusted_Connection=True;TrustServerCertificate=True"
    }
  }
}
```

`execute_read_query` przyjmuje wtedy SQL SELECT zamiast JSON Mongo.

## Następne kroki

- [HOW_IT_WORKS.md](HOW_IT_WORKS.md) — jak agent korzysta z narzędzi
- [TOOLS.md](TOOLS.md) — referencja wszystkich 17 narzędzi
- [TEMPLATE.md](TEMPLATE.md) — fork pod własny projekt MCP
