# AI Coding Instructions

## Forbidden Practices

- Summarizing to-dos - make task list for all points individually retaining ordering.
- Pausing for confirmations, confirming action plan, printing status instead of executing, or only partially completing instructions.
- Using UNIX commands (`grep`, `echo`, etc.) or specifying `pwsh` — pwsh is default; use PowerShell-native commands only.
- Using `-ErrorAction SilentlyContinue` or aliases for commandlets.
- Catching critical failures — throw and abort the program, including in tests - no skips for whatever reason.
- Deleting build artifacts manually — use `dotnet clean`.
- Making parallel external API calls — sequential only to avoid rate limits.
- Performing destructive in-place edits (`Set-Content`, regex) — replace text manually to ensure it can be undone.
- Limiting refactor scope for backward compatibility — execute the full action plan regardless of scale.
- Creating markdown files outside the repo-level `Markdown` directory.
- Modifying user's `To-Do.md` - maintain your own distinct markdown file.
- Checking for null manually when null-conditional operators suffice:
  - `string name = GetName() ?? "Default";`
  - `cache ??= [];`
  - `result?.Value?.ToString()`
  - `config?.Settings?.RetryPolicy = new ExponentialBackoffRetryPolicy();` (C# 14 null-conditional assignment)
- Adding unsolicited docs, markdown files, or verbose comments — comment only when code is non-obvious.

## General Principles

- Semantically meaningful variable names.
- Minimize nesting with early returns.
- Exhaustive, color-coded, vertically-aligned logging using 4-letter aliases.
- Treat all keys/secrets as environment variables.
- Date-time format: `YYYY/mm/dd HH:mm:ss`.

---

## Resiliency

- Exponential backoff: 5s base delay, 10 attempts max.
- Detect by exception type/message/code (e.g., `HttpRequestException`, `TimeoutException`, SQL error -2).
- Keep operations idempotent to tolerate retries.
- Fail immediately non-transient errors with full context.

---

## Testing

- Ensure creation of both unit and integration tests.
- Run unit tests first, then integration.
- External dependencies belong in integration tests; isolate with fakes/mocks for unit.
- Semantically meaningful test names reflecting behavior.
- Rich formatted output showing expected vs actual.
- Explicitly disable test parallelization.

---

## C#

### Modeling & Types

- Using `class` for data models — use `record`.
- Creating interfaces for services implemented only once.
- Declaring `{ get; set; }` when `{ get; }` suffices.
- Using `private` fields or underscore-prefixed names.
- Ignoring positional records: `record Product(string Id, decimal Price);`
- Ignoring primary constructors for dependency injection.

### Initialization

- Using `var` when target-typed `new()` would be clearer:
  - Prefer: `Person person = new() { Name = "Alice", Age = 30 };`
  - Exception: `var result = CreateComplexThing();` when return type is clear.
- Ignoring collection expressions and spread operator:
  - `List<int> numbers = [];`
  - `int[] combined = [..first, ..second];`

### Project Setup

- Specifying package versions — always use `*`.
- Using file-scoped usings — declare all usings as global in `GlobalUsings.cs` with aliases to prevent ambiguity.
- Targeting anything other than `net10.0` for SDK-style projects.
- Omitting `.gitignore` — create via `dotnet new gitignore`.

### Naming & Access

- Misordering fields — use: `const` → `static readonly` → instance.
- Using wrong case — `SCREAMING_SNAKE_CASE` for `const`; `camelCase` for locals/parameters; `PascalCase` otherwise.

---

## Python

- Use `basedpyright` in strict mode.
- Always use type hints.
- Suppress type errors for untyped external libraries.

---

## Formatting

- C#: `csharpier format .`
- Python: `black .`

---

## Libraries

| Purpose          | C#                       | Python                     |
| ---------------- | ------------------------ | -------------------------- |
| CLI              | `Spectre.Console.CLI`    | `argparser`                |
| Logging          | `Spectre.Console`        | `Rich`                     |
| Resiliency       | `Polly`                  | `Tenacity`                 |
| Test Framework   | `MSTest`                 | `pytest`                   |
| Assertions       | `Shouldly`               | `pytest`                   |
| Mocking          | `FakeItEasy`             | `pytest.mock`              |
| Decompilation    | `ILSpy`                  | —                          |
| JSON             | `System.Text.Json`       | `json`                     |
| CSV              | `CsvHelper`              | `Polars`                   |
| Audio Metadata   | `z440.atl.core`          | `mutagen`                  |
| FFmpeg           | `Xabe.FFmpeg`            | `ffmpeg-python`            |
| Static HTML      | `AngleSharp`             | `Scrapy`                   |
| JS-Rendered HTML | `Playwright`             | `Scrapling` / `Playwright` |
| Web Scraping     | `IronWebScraper`         | `Scrapy`                   |
| Discogs          | `ParkSquare.Discogs`     | `python3-discogs-client`   |
| MusicBrainz      | `MetaBrainz.MusicBrainz` | `musicbrainzngs`           |
| Google API       | `Google.Apis`            | `google-api-python-client` |
| Google Auth      | `Google.Apis.Auth`       | `google-auth`              |
| Google Sheets    | `Google.Apis.Sheets.v4`  | `gspread`                  |
| YouTube          | `Google.Apis.YouTube.v3` | `python-youtube`           |
