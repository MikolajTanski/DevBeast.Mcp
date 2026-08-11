# Zespół agentów DevBeast

Trzy wyspecjalizowane role + **orkiestrator** (główny agent w Cursor).

| Plik | Rola | Output |
|------|------|--------|
| [architect.md](architect.md) | Analiza kodu, projekt rozwiązania | Specyfikacja Markdown w `specs/` |
| [coder.md](coder.md) | Implementacja | Kod C# w `src/`, `tests/` |
| [tester.md](tester.md) | Weryfikacja | Testy xUnit + `dotnet test` |

Orkiestrator: [../AGENTS.md](../AGENTS.md)

## Pipeline

```
Użytkownik → Orkiestrator → Architect → Coder → Tester → Orkiestrator (raport)
```

Specyfikacje: `.agents/specs/{kebab-case-nazwa}.md`
