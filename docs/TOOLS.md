# Referencja narzędzi MCP

DevBeast wystawia **17 narzędzi** pogrupowanych w 7 modułów. Wszystkie zwracają JSON.

![Mapa 17 narzędzi MCP](assets/devbeast-tools-map.png)

## Spis

- [Baza danych i diagnostyka](#baza-danych-i-diagnostyka)
- [Architektura i scaffolding](#architektura-i-scaffolding)
- [Integracje zespołowe (Mock)](#integracje-zespołowe-mock)
- [Dane i środowiska](#dane-i-środowiska)
- [Infrastruktura](#infrastruktura)
- [Security](#security)

---

## Baza danych i diagnostyka

### `get_database_schema`

Zwraca metadane tabeli/kolekcji: kolumny, typy, klucze obce, indeksy.

| Parametr | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `tableName` | string | tak | Nazwa tabeli/kolekcji lub `*` dla wszystkich |

**Przykład:** `tableName: "products"`

**MongoDB — wynik:** typy pól inferowane z sample documents.

**SQL Server — wynik:** `INFORMATION_SCHEMA` + `sys.foreign_keys`.

---

### `execute_read_query`

Wykonuje zapytanie **read-only**.

| Parametr | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `sqlQuery` | string | tak | SQL SELECT **lub** JSON find (MongoDB) |

**MongoDB przykład:**
```json
{"collection":"orders","filter":{"status":"Pending"},"limit":10}
```

**SQL Server przykład:**
```sql
SELECT TOP 10 * FROM Orders WHERE Status = 'Pending'
```

**Zabezpieczenia:** odrzuca INSERT, UPDATE, DELETE, DROP, ALTER, TRUNCATE, EXEC.

---

### `get_recent_errors`

Agreguje wyjątki z plików logów w oknie czasowym.

| Parametr | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `timeWindowMinutes` | int | tak | Ile minut wstecz (np. `15`) |
| `environment` | string | nie | Filtr env (np. `Dev`) — ścieżka pliku lub pole JSON |

**Obsługiwane formaty logów:** Serilog JSON (`@l`, `@m`, `@t`), Serilog text, plain text z `Exception`.

**Wynik:** lista zagregowanych błędów z `occurrenceCount`, `stackTrace`, `firstSeen`, `lastSeen`.

---

### `get_tool_call_stats`

Licznik wywołań narzędzi MCP w bieżącej sesji serwera (od startu procesu).

| Parametr | Typ | Wymagany | Domyślnie | Opis |
|----------|-----|----------|-----------|------|
| `reset` | bool | nie | `false` | Po zwróceniu statystyk zeruje liczniki |

**Wynik:** `sessionStartedAt`, `totalCalls`, `totalErrors`, `tools[]` z `calls`, `errors`, `avgDurationMs` per narzędzie.

**Mechanizm:** filtr `AddCallToolFilter` w pipeline MCP — thread-safe, in-memory. Logi per-call opcjonalnie przez `DevBeast:Metrics:LogEachCall`.

---

## Architektura i scaffolding

### `ensure_project_structure`

Skanuje repo, generuje brakującą strukturę Clean Architecture, zapisuje manifest.

| Parametr | Typ | Wymagany | Domyślnie | Opis |
|----------|-----|----------|-----------|------|
| `projectPath` | string | nie | `DefaultProjectPath` | Korzeń projektu |
| `generateIfMissing` | bool | nie | `true` | Generuj szkielet gdy brak |
| `namespacePrefix` | string | nie | `App` | Prefix namespace (np. `Shop`) |

**Generuje gdy brak:**
```
src/{Ns}.Domain/
src/{Ns}.Application/
src/{Ns}.Infrastructure/
src/{Ns}.Api/
tests/{Ns}.Application.Tests/
{Ns}.sln
.devbeast/project-structure.json
```

**Wynik:** `layers`, `projects`, `detectedFeatures`, `wasGenerated`, `manifestPath`.

---

### `get_project_structure`

Zwraca manifest **bez generowania** plików.

| Parametr | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `projectPath` | string | nie | Korzeń projektu |

---

### `validate_architecture_rules`

Skanuje pliki `.cs` pod kątem reguł Clean Architecture / DDD.

| Parametr | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `projectPath` | string | nie | Korzeń projektu do skanowania |

**Reguły:**

| ID | Severity | Opis |
|----|----------|------|
| CA-DOM-001 | Error | Domain nie może importować EF, ASP.NET, MediatR, MongoDB |
| CA-DOM-002 | Error | Domain nie może referencować Infrastructure/Web |
| CA-DTO-001 | Warning | DTO z mutable `set` zamiast `init` |
| CA-DTO-002 | Info | Rozważ `record` zamiast `class` dla DTO |

---

### `scaffold_feature_slice`

Generuje kompletny Vertical Slice.

| Parametr | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `featureName` | string | tak | np. `Product`, `GetOrdersByStatus` |
| `projectPath` | string | nie | Gdzie tworzyć pliki |

**Tworzy (11 plików):** Entity, Command, Handler, Query, DTO, AutoMapper Profile, EF Config, Migration, Controller, Test.

**Wymaga/wykonuje:** `ensure_project_structure` — używa ścieżek z manifestu.

---

## Integracje zespołowe (Mock)

### `get_ticket_context`

Pobiera kontekst ticketa Jira / Azure DevOps.

| Parametr | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `ticketId` | string | tak | np. `PROJ-142`, `ADO-891` |

**Mocki dostępne:**

| ID | Typ | Opis |
|----|-----|------|
| `PROJ-142` | Bug | NullReferenceException w OrderService |
| `ADO-891` | User Story | GET /api/products z filtrowaniem |

**Wynik:** title, description, acceptanceCriteria, linkedFiles, labels, suggestedFeatureName.

---

### `create_pull_request_with_impact`

Tworzy mock PR z analizą ryzyka.

| Parametr | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `title` | string | tak | Tytuł PR |
| `description` | string | tak | Opis zmian |
| `projectPath` | string | nie | Do analizy impact |
| `ticketId` | string | nie | Link mock komentarza do ticketa |

**Wynik:** `pullRequestUrl`, `impact.riskLevel`, `changedApis`, `affectedDatabases`, `recommendedTests`.

---

## Dane i środowiska

### `generate_test_fixtures`

Generuje kod C# seed data (Bogus) ze schematu bazy.

| Parametr | Typ | Wymagany | Domyślnie | Opis |
|----------|-----|----------|-----------|------|
| `tableName` | string | tak | — | Tabela / kolekcja |
| `count` | int | nie | `10` | Liczba rekordów (1–500) |

**Wynik:** gotowy kod C# z `Bogus.Faker<T>` i regułami per kolumna.

---

### `diff_environments`

Porównuje konfigurację między środowiskami.

| Parametr | Typ | Wymagany | Domyślnie | Opis |
|----------|-----|----------|-----------|------|
| `mode` | string | nie | `appsettings` | `appsettings` lub `database` |
| `environmentsPath` | string | nie | `Mocks/environments/` | Folder z plikami env |

**appsettings:** porównuje `appsettings.Dev.json`, `.Test.json`, `.Prod.json`.

**database:** porównuje schemat DB (mock diff gdy brak połączeń Prod).

---

## Infrastruktura

### `inspect_redis_cache`

Podgląda klucze Redis z dekodowaniem JSON.

| Parametr | Typ | Wymagany | Domyślnie | Opis |
|----------|-----|----------|-----------|------|
| `keyPattern` | string | nie | `*` | Glob, np. `cache:*` |

**Wynik:** `entries[]` z `key`, `value`, `valueType`, `ttlSeconds`, `isMockMode`.

---

### `flush_key`

Usuwa klucz z Redis (lub mock store).

| Parametr | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `key` | string | tak | Dokładny klucz, np. `cache:products:all` |

---

### `peek_dead_letter_queue`

Podgląda wiadomości z Dead Letter Queue.

| Parametr | Typ | Wymagany | Domyślnie | Opis |
|----------|-----|----------|-----------|------|
| `queueName` | string | nie | wszystkie | Filtr kolejki, np. `orders.processing` |
| `limit` | int | nie | `20` | Max wiadomości |

**Źródło:** kolekcja MongoDB `deadLetterMessages` → fallback hardcoded mock.

---

## Security

### `scan_secrets_and_pii`

Skanuje kod pod kątem wycieków i danych osobowych.

| Parametr | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `projectPath` | string | nie | Korzeń projektu |

**Wykrywa:** API keys, JWT, hardcoded passwords, connection string secrets, emaile, PESEL, telefony, karty kredytowe.

**Skanuje:** `.cs`, `.json`, `.env`, `.yaml`, `.config`, `.ts`, `.js`.

---

### `check_nuget_vulnerabilities`

Audyt paczek NuGet pod kątem CVE.

| Parametr | Typ | Wymagany | Opis |
|----------|-----|----------|------|
| `projectPath` | string | nie | Katalog projektu lub ścieżka `.csproj` |

**Mechanizm:** `dotnet list package --vulnerable --include-transitive`.

---

## Szybka ściągawka (chat)

```
ensure_project_structure → scaffold_feature_slice Payment
get_ticket_context PROJ-142
get_database_schema products
execute_read_query {"collection":"orders","filter":{"status":"Pending"},"limit":5}
validate_architecture_rules
scan_secrets_and_pii
diff_environments appsettings
inspect_redis_cache cache:*
peek_dead_letter_queue orders.processing
```
