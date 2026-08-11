# Assets dokumentacji

Infografiki i (opcjonalnie) zrzuty ekranu używane w `docs/`.

## Infografiki (wygenerowane)

| Plik | Używany w |
|------|-----------|
| `devbeast-architecture.png` | `docs/README.md`, `ARCHITECTURE.md` |
| `devbeast-setup.png` | `docs/README.md`, `SETUP.md` |
| `devbeast-tools-map.png` | `docs/README.md`, `TOOLS.md` |
| `devbeast-agent-flow.png` | `docs/README.md`, `HOW_IT_WORKS.md` |
| `devbeast-agent-pipeline.png` | `docs/README.md`, `AGENTS.md` |

## Zrzuty ekranu (do uzupełnienia)

Opcjonalny folder `screenshots/` — realne capture z Cursor:

| Sugerowana nazwa | Co nagrać |
|------------------|-----------|
| `cursor-mcp-green.png` | Settings → MCP → serwer `devbeast` (zielony status) |
| `cursor-agent-tool-call.png` | Agent chat z widocznym wywołaniem `get_database_schema` |
| `cursor-agent-workflow.png` | Pełny prompt z AGENT_WORKFLOWS + odpowiedź agenta |

Format: PNG, szerokość 1200–1600 px. W markdown:

```markdown
![Opis](screenshots/cursor-mcp-green.png)
```
