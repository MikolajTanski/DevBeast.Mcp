# Agent workflows — gotowe prompty

Ten dokument zawiera **copy-paste prompty** do Cursor Agent (lub Claude Code) z serwerem DevBeast MCP. Każdy prompt opisuje oczekiwany flow narzędzi — agent sam je wywoła, jeśli MCP jest podpięty.

> **Wymagania:** Docker (Mongo + Redis), zbudowany serwer MCP, zielony status `devbeast` w **Settings → MCP**.  
> Szczegóły → [SETUP.md](SETUP.md).

## Jak używać

1. Otwórz **Agent mode** w Cursor (nie zwykły Chat).
2. Skopiuj prompt z sekcji poniżej.
3. Dostosuj ścieżki (`samples/ReferenceApp`) do swojego projektu, jeśli pracujesz nad inną aplikacją.
4. Wyślij — agent powinien najpierw zebrać kontekst przez DevBeast, potem edytować kod.

**Tip:** Im bardziej konkretny prompt (ticket ID, nazwa kolekcji, okno czasowe), tym mniej agent zgaduje.

---

## Szybki start (demo na ReferenceApp)

### 1. Pełny przegląd projektu

```
Użyj DevBeast MCP i zrób szybki health-check projektu samples/ReferenceApp:
1. Odczytaj manifest projektu (get_project_structure)
2. Waliduj reguły Clean Architecture (validate_architecture_rules)
3. Pokaż schemat wszystkich kolekcji MongoDB (get_database_schema tableName="*")
4. Przeskanuj sekrety i CVE NuGet (scan_secrets_and_pii, check_nuget_vulnerabilities)

Na końcu: krótki raport markdown — co jest OK, co wymaga uwagi. Nie zmieniaj kodu.
```

**Oczekiwany flow:** `get_project_structure` → `validate_architecture_rules` → `get_database_schema` → `scan_secrets_and_pii` → `check_nuget_vulnerabilities`

---

## Bugfix z ticketa

### 2. Bug z Jiry — pełny cykl (PROJ-142)

```
Pobierz kontekst ticketa PROJ-142 przez DevBeast MCP.

Następnie:
1. Przeanalizuj opis buga i acceptance criteria
2. Upewnij się, że znasz strukturę projektu (ensure_project_structure na samples/ReferenceApp)
3. Zlokalizuj problem w kodzie — ticket wskazuje pliki w linkedFiles; w ReferenceApp szukaj analogicznej logiki Orders
4. Napraw NullReferenceException tak, aby pusty koszyk/zamówienie zwracało walidację zamiast 500
5. Dodaj test jednostkowy zgodny z AC z ticketa
6. Uruchom dotnet test
7. Przygotuj mock PR z analizą impact (create_pull_request_with_impact) — tytuł: "fix(PROJ-142): empty cart validation"

Podsumuj: co było przyczyną, co zmieniłeś, wynik testów.
```

**Oczekiwany flow:** `get_ticket_context` → `ensure_project_structure` → [edycja kodu] → `dotnet test` → `create_pull_request_with_impact`

### 3. Bug bez ticketa — diagnostyka z logów

```
Coś się wywaliło w ostatnich 30 minutach. Użyj DevBeast MCP:
1. get_recent_errors timeWindowMinutes=30
2. Jeśli w stack trace widać problem z kolejką — peek_dead_letter_queue limit=10
3. Sprawdź cache Redis pod cache:* (inspect_redis_cache)

Na podstawie wyników: wskaż najbardziej prawdopodobną przyczynę i zaproponuj fix w kodzie (jeszcze nie commituj).
```

**Oczekiwany flow:** `get_recent_errors` → `peek_dead_letter_queue` → `inspect_redis_cache`

### 4. Bug „działa u mnie” — diff środowisk

```
Prod zachowuje się inaczej niż Dev. Użyj DevBeast:
1. diff_environments mode=appsettings — porównaj Dev/Test/Prod
2. diff_environments mode=database — różnice schematu (jeśli dostępne)

Wypisz różnice, które mogą tłumaczyć bug w produkcji (connection stringi, feature flagi, timeouty). Nie zmieniaj plików.
```

**Oczekiwany flow:** `diff_environments` (×2)

---

## Nowy feature (Vertical Slice)

### 5. User story z ADO — od ticketa do kodu (ADO-891)

```
Zrealizuj user story z mock ticketa ADO-891 używając DevBeast MCP:

1. get_ticket_context ADO-891
2. ensure_project_structure na samples/ReferenceApp (generateIfMissing=true)
3. scaffold_feature_slice featureName=GetProductsByCategory projectPath=samples/ReferenceApp
4. Dopasuj wygenerowany kod do istniejącego projektu (Products już istnieją — nie duplikuj Entity)
5. validate_architecture_rules — napraw ewentualne naruszenia
6. dotnet build && dotnet test

Na końcu: lista utworzonych/zmienionych plików + checklist AC z ticketa.
```

**Oczekiwany flow:** `get_ticket_context` → `ensure_project_structure` → `scaffold_feature_slice` → `validate_architecture_rules`

### 6. Nowy feature od zera (bez ticketa)

```
Zscaffolduj nowy vertical slice "Payment" w samples/Scaffolded:

1. ensure_project_structure projectPath=samples/Scaffolded namespacePrefix=App generateIfMissing=true
2. scaffold_feature_slice featureName=Payment projectPath=samples/Scaffolded
3. validate_architecture_rules projectPath=samples/Scaffolded
4. Wygeneruj 5 fixture'ów testowych dla kolekcji payments (generate_test_fixtures tableName=payments count=5)

Pokaż strukturę plików i wygenerowany kod fixture'ów. Nie commituj.
```

**Oczekiwany flow:** `ensure_project_structure` → `scaffold_feature_slice` → `validate_architecture_rules` → `generate_test_fixtures`

### 7. Feature + PR z impact analysis

```
Dodałem obsługę kategorii produktów. Użyj DevBeast:
1. validate_architecture_rules na samples/ReferenceApp
2. execute_read_query {"collection":"products","filter":{"category":"Electronics"},"limit":5} — potwierdź dane testowe
3. create_pull_request_with_impact:
   - title: "feat: filter products by category"
   - description: krótkie podsumowanie zmian
   - ticketId: ADO-891

Wypisz riskLevel, changedApis i recommendedTests z impact analysis.
```

**Oczekiwany flow:** `validate_architecture_rules` → `execute_read_query` → `create_pull_request_with_impact`

---

## Baza danych i dane

### 8. Eksploracja MongoDB

```
Pokaż mi stan bazy DevBeast (MongoDB):

1. get_database_schema tableName="*" — wszystkie kolekcje
2. execute_read_query {"collection":"orders","filter":{},"limit":5}
3. execute_read_query {"collection":"orders","filter":{"status":"Pending"},"limit":10}
4. execute_read_query {"collection":"products","filter":{},"limit":3}

Sformatuj wynik jako czytelne tabele markdown. Nie modyfikuj danych.
```

**Oczekiwany flow:** `get_database_schema` → `execute_read_query` (×3)

### 9. Fixture'y pod testy integracyjne

```
Potrzebuję seed data do testów integracyjnych orders:

1. get_database_schema tableName=orders — zrozum strukturę i FK
2. generate_test_fixtures tableName=orders count=20
3. generate_test_fixtures tableName=customers count=5

Wklej wygenerowany kod C# (Bogus) do nowego pliku tests/.../OrderTestFixtures.cs w samples/ReferenceApp.
Upewnij się, że respektuje relacje FK między orders a customers.
```

**Oczekiwany flow:** `get_database_schema` → `generate_test_fixtures` (×2) → [edycja pliku]

### 10. SQL Server (gdy Provider=SqlServer)

```
Projekt używa SQL Server (DevBeast Database:Provider=SqlServer).

1. get_database_schema tableName=Orders
2. execute_read_query: SELECT TOP 10 o.Id, o.Status, o.CreatedAt FROM Orders o WHERE o.Status = 'Pending' ORDER BY o.CreatedAt DESC

Tylko SELECT — bez modyfikacji danych. Wynik w tabeli markdown.
```

---

## Infrastruktura i runtime

### 11. Debug cache — „stare dane w API”

```
Użytkownicy widzą nieaktualne produkty. Zdiagnozuj cache:

1. inspect_redis_cache keyPattern="cache:*"
2. inspect_redis_cache keyPattern="*product*"
3. Jeśli znajdziesz klucz cache:products:all (lub podobny) — flush_key na ten klucz
4. inspect_redis_cache ponownie — potwierdź usunięcie

Opisz co było w cache i czy TTL wygląda poprawnie. Nie restartuj Dockera.
```

**Oczekiwany flow:** `inspect_redis_cache` → `flush_key` → `inspect_redis_cache`

### 12. Dead Letter Queue — failed messages

```
Sprawdź co wpada do DLQ:

1. peek_dead_letter_queue limit=20
2. peek_dead_letter_queue queueName=orders.processing limit=10
3. get_recent_errors timeWindowMinutes=60 — skoreluj błędy z wiadomościami DLQ

Zaproponuj fix handlera dla najczęstszego typu błędu. Nie usuwaj wiadomości z DLQ.
```

**Oczekiwany flow:** `peek_dead_letter_queue` (×2) → `get_recent_errors`

### 13. Pełna diagnostyka incydentu (runbook)

```
Incydent produkcyjny — pełny runbook DevBeast (read-only, bez zmian w kodzie):

Kolejność:
1. get_recent_errors timeWindowMinutes=15 environment=Prod
2. peek_dead_letter_queue limit=10
3. inspect_redis_cache keyPattern="*"
4. execute_read_query {"collection":"orders","filter":{"status":"Failed"},"limit":10}
5. diff_environments mode=appsettings

Na końcu: timeline incydentu, hipoteza root cause, suggested next steps (max 1 strona).
```

**Oczekiwany flow:** 5 narzędzi diagnostycznych → raport

---

## Security i compliance

### 14. Security audit przed release

```
Przed releasem zrób security audit samples/ReferenceApp przez DevBeast:

1. scan_secrets_and_pii — wypisz findings z severity
2. check_nuget_vulnerabilities — lista CVE z rekomendacją upgrade
3. validate_architecture_rules — upewnij się, że Domain nie importuje Infrastructure

Raport: Critical / High / Medium / Info. Dla Critical i High — konkretne kroki naprawy.
```

**Oczekiwany flow:** `scan_secrets_and_pii` → `check_nuget_vulnerabilities` → `validate_architecture_rules`

### 15. Audyt po merge (tylko skan)

```
Ktoś właśnie zmergował PR. Szybki skan:
- scan_secrets_and_pii na całym repo DevBeast.Mcp
- check_nuget_vulnerabilities

Jeśli coś znajdziesz — pokaż plik i linię, nie naprawiaj automatycznie.
```

---

## Onboarding nowego projektu

### 16. Bootstrap Clean Architecture w istniejącym repo

```
Mam pusty folder aplikacji. Użyj DevBeast, żeby przygotować strukturę:

1. ensure_project_structure projectPath=/ABSOLUTNA/SCIEZKA/MojaApp namespacePrefix=MojaApp generateIfMissing=true
2. get_project_structure — pokaż manifest .devbeast/project-structure.json
3. validate_architecture_rules

Wyjaśnij co zostało wygenerowane i co powinienem commitować.
```

> Zamień `/ABSOLUTNA/SCIEZKA/MojaApp` na realną ścieżkę.

### 17. Podpięcie DevBeast pod istniejący projekt .NET

```
Pracuję nad projektem w /path/to/my-api. DevBeast ma DefaultProjectPath wskazujący gdzie indziej.

W tym prompcie używaj projectPath=/path/to/my-api we wszystkich narzędziach DevBeast:
1. ensure_project_structure + get_project_structure
2. validate_architecture_rules
3. get_database_schema tableName="*"

Na końcu podaj przykładowy wpis do .cursor/mcp.json z DEVBEAST__DefaultProjectPath dla tego projektu.
```

---

## Kombinacje wieloetapowe (realistyczne dni pracy)

### 18. „Sprint task” — ticket → kod → test → PR

```
Dzień pracy na ADO-891. Workflow end-to-end z DevBeast MCP:

Faza discovery:
- get_ticket_context ADO-891
- get_project_structure samples/ReferenceApp
- get_database_schema products

Faza implementacji:
- scaffold_feature_slice GetProductsByCategory (lub ręczna implementacja jeśli slice już częściowo istnieje)
- validate_architecture_rules po każdej większej zmianie

Faza weryfikacji:
- execute_read_query z filtrem category
- dotnet test

Faza zamknięcia:
- create_pull_request_with_impact z ticketId=ADO-891

Checkpointy: po każdej fazie krótkie podsumowanie zanim przejdziesz dalej.
```

### 19. „On-call” — 15 minut do hipotezy

```
Jestem on-call. Mam 15 minut. Użyj DevBeast read-only:

1. Błędy: get_recent_errors 15
2. Kolejka: peek_dead_letter_queue 5
3. Cache: inspect_redis_cache cache:*
4. DB: pending/failed orders (execute_read_query)

Jedna hipoteza, jeden recommended action. Bez refactoru.
```

### 20. „Code review assist” — PR przed merge

```
Review brancha przed merge (nie zmieniaj kodu bez mojej zgody):

1. validate_architecture_rules
2. scan_secrets_and_pii
3. check_nuget_vulnerabilities
4. diff_environments appsettings — czy przypadkiem nie wchodzi config Prod-only

Format: ✅ / ⚠️ / ❌ per kategoria + lista must-fix przed merge.
```

---

## Dostosowanie promptów

| Placeholder | Kiedy zmienić | Przykład |
|-------------|---------------|----------|
| `samples/ReferenceApp` | Praca nad własną aplikacją | `/Users/me/Projects/ShopApi` |
| `PROJ-142` / `ADO-891` | Własne mocki w `Mocks/tickets/` | `SHOP-99.json` |
| `timeWindowMinutes` | Inny horyzont logów | `5`, `120` |
| `samples/Scaffolded` | Inny output scaffold | `../MyApp` |
| Mongo JSON query | Inna kolekcja/filtr | `{"collection":"customers",...}` |

### Per-projekt w Cursor

W `.cursor/mcp.json` **aplikacji docelowej** (nie globalnie):

```json
{
  "mcpServers": {
    "devbeast": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/DevBeast.Mcp/src/DevBeast.Mcp.Server/DevBeast.Mcp.Server.csproj", "--no-build"],
      "env": {
        "DEVBEAST__DefaultProjectPath": "/path/to/MojaAplikacja",
        "DEVBEAST__Mongo__ConnectionString": "mongodb://...",
        "DEVBEAST__Logs__Directory": "/path/to/logs"
      }
    }
  }
}
```

Wtedy prompty możesz skracać — agent domyślnie użyje `DefaultProjectPath`.

---

## Troubleshooting promptów

| Problem | Co zrobić |
|---------|-----------|
| Agent nie woła MCP | Settings → MCP → `devbeast` zielony; Reload Window |
| Agent edytuje kod bez diagnostyki | Dopisz: „**Najpierw** użyj DevBeast MCP, potem kod” |
| Puste logi w `get_recent_errors` | Ustaw `DEVBEAST__Logs__Directory` na katalog z `.log` |
| Mongo auth failed | Port **27018**, nie 27017 |
| Agent halucynuje schemat DB | Wymuś: `get_database_schema` przed `execute_read_query` |

---

## Powiązane dokumenty

- [HOW_IT_WORKS.md](HOW_IT_WORKS.md) — mechanika MCP i best practice kolejności narzędzi
- [TOOLS.md](TOOLS.md) — parametry wszystkich 16 narzędzi
- [SETUP.md](SETUP.md) — instalacja i konfiguracja Cursor
