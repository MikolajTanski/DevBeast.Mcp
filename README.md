# DevBeast MCP — Full Edition

Lokalny serwer MCP w .NET 9, który daje agentom AI (Cursor, Claude Desktop, Claude Code) bezpieczny dostęp do bazy danych, logów, architektury kodu i procesów deweloperskich.

**Repozytorium:** https://github.com/MikolajTanski/DevBeast.Mcp

## Dokumentacja

| Dokument | Opis |
|----------|------|
| [Jak to działa](docs/HOW_IT_WORKS.md) | Flow agenta, transport MCP, manifest projektu, typowe scenariusze |
| [Agent workflows](docs/AGENT_WORKFLOWS.md) | **20 gotowych promptów** — bugfix, feature, on-call, security audit |
| [Instalacja i konfiguracja](docs/SETUP.md) | Docker, appsettings, Cursor MCP, zmienne env, troubleshooting |
| [Architektura](docs/ARCHITECTURE.md) | Warstwy serwera, modele, serwisy, rozszerzalność |
| [Narzędzia MCP (16)](docs/TOOLS.md) | Pełna referencja: parametry, przykłady, ograniczenia |
| [Template / starter kit](docs/TEMPLATE.md) | Jak użyć repo jako bazy pod własny serwer MCP |

## Szybki start (3 kroki)

```bash
# 1. Infrastruktura
cd docker && docker compose up -d

# 2. Serwer
cd src/DevBeast.Mcp.Server
cp appsettings.Local.json.example appsettings.Local.json
dotnet run

# 3. Testy
dotnet test   # z katalogu głównego repo
```

Cursor MCP jest skonfigurowany w `~/.cursor/mcp.json` i `.cursor/mcp.json`. Szczegóły → [SETUP.md](docs/SETUP.md).

## Stack

- .NET 9 / C# · [MCP C# SDK](https://csharp.sdk.modelcontextprotocol.io/) (stdio)
- MongoDB `:27018` + Redis `:6379` (Docker)
- MS SQL Server (opcjonalnie)
- Mocki: Jira, Azure DevOps, GitHub PR, DLQ

## Struktura repozytorium

```
DevBeast.Mcp/
├── docs/                         # Dokumentacja
├── docker/                       # MongoDB + Redis
├── samples/
│   ├── ReferenceApp/             # Przykładowa app + manifest .devbeast/
│   └── Scaffolded/               # Output scaffold_feature_slice (gitignored)
├── src/DevBeast.Mcp.Server/      # Serwer MCP
└── tests/                        # Testy integracyjne + smoke test
```

## Autor

Mikołaj Tański — .NET Developer / AI Integration Engineer
