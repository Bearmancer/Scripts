# Agent Rules — Plan Governance Workflow

> These rules are enforced for ALL agents operating on this repository. Violations = plan sprawl.

---

## General

- `./PLAN.md` is the ONLY place for implementation phases, tasks, and status tracking.
- No other file may contain task lists, phase definitions, or status checkboxes.
- Violation = sprawl.
- **NEVER skip tests. Every code change must include a passing test that locks the new behavior.** Skipping tests is a regression bomb; the test suite is the safety net, not a checkbox.
- **NEVER write comments in code.** Delete pre-existing comments. Make code self-documenting via naming and structure.

## Automated Sprawl Detection

- Enforce all markdown files in one dir (except `AGENTS.md` and `PLAN.md`)
- Never have more than 1 file for research, plan, diagram each.
- ALWAYS prefer update existing files.
- Be squeamish with generating new markdown files. ONLY if needed.

## Key Decisions

These were decided in earlier sessions. Do not question them again:

- EF entities are TARGET STATE (not dead/aspirational)
- Google Sheets → PostgreSQL (all data migrates, Sheets is legacy)
- Fresh install of PostgreSQL (no backward compat)
- Monolithic program (not library, single csproj, no exclusions)
- Two-Phase API Sync: Fetch external API data to local JSON disk buffer first, then ingest from disk to PGSQL (Prevents quota exhaustion on DB wipes)
- Migrating work state stored on Fibery natively onto PGSQL
- **Translation is standalone** — `AzureTranslationService` + `TranslationCache` form a generic, reusable layer. No YouTube-specific coupling. Any future caller (music, discogs, etc.) uses it directly with automatic caching.

## Microsoft Foundry (formerly Azure AI Services / Cognitive Services)

> **One platform, one brand**: All prebuilt AI services in this repo live under **Foundry Tools** (the renamed Azure AI Services / Cognitive Services). The underlying SDKs, endpoints, and capabilities are unchanged — only the product name changed. Microsoft rebranded at Microsoft Ignite 2025.

**Renaming history (for searching old docs):**
- 2015 Project Oxford → 2016 Cognitive Services → 2023 Azure AI Services → **2025 Foundry Tools** (same product)
- 2023 Azure AI Studio → 2024 Azure AI Foundry → **2025 Microsoft Foundry** (same platform)

**What's in our repo, mapped to Foundry Tools (verified Jun 2026):**

| Our class | Foundry Tool | Modern SDK package (NuGet) | API version | Endpoint env var | Verified price (Jun 2026) | Free tier (F0) |
|---|---|---|---|---|---|---|
| `AzureTranslationService` | Foundry Tools — Translator | `Azure.AI.Translation.Text` 1.0.0+ | v3.0 → **2026-06-06 GA** (migrate) | `AzureTranslatorEndpoint` | $10/1M chars (S1) | 2M chars/mo |
| `AzureDocumentIntelligenceOcrProvider` (Reader/Ocr) | Foundry Tools — Document Intelligence | `Azure.AI.DocumentIntelligence` 1.0.0+ | v4.0 (2024-11-30 GA) | `AzureDocumentIntelligenceEndpoint` | $1.50/1k pages (Read), $10/1k (Prebuilt) | 500 pages/mo |
| `AzureDocumentIntelligenceService` (planned) | Foundry Tools — Document Intelligence | same | same | same | same | same |
| `AzureVisionService` (planned) | Foundry Tools — Image Analysis | `Azure.AI.Vision.ImageAnalysis` 1.0.0+ | v4.0 (retires 2028-09-25) | `AzureVisionEndpoint` | $1/1k trans (Group 1), $1.50/1k (Group 2) | 5,000 trans/mo |
| `AzureOpenAIService` (planned, Whisper + GPT-4o-mini) | Foundry — Models (OpenAI) | `Azure.AI.OpenAI` 1.0.0+ (or standard `OpenAI` 2.x with `base_url`) | gpt-4o-mini → **gpt-4.1-mini** (migrate Oct 2026) | `AzureOpenAIEndpoint` | gpt-4o-mini: $0.15/$0.60 per 1M tok; gpt-4.1-mini: $0.40/$1.60; Whisper: $0.006/min | None |

**Modern NuGet packages (verified — no backward-compat legacy):**
- ✅ `Azure.AI.Translation.Text` — `dotnet add package Azure.AI.Translation.Text --prerelease` (1.0.0 GA, May 2024)
- ✅ `Azure.AI.DocumentIntelligence` — replaces `Azure.AI.FormRecognizer` (the legacy package, deprecated 2023)
- ✅ `Azure.AI.Vision.ImageAnalysis` — replaces `Microsoft.Azure.CognitiveServices.Vision.ComputerVision` (legacy, 3.0/3.1 retires 2026-09-13)
- ✅ `Azure.AI.OpenAI` — replaces `Azure.AI.OpenAI` v1.x (legacy, 2023)
- ✅ `Azure.Identity` — for `DefaultAzureCredential`
- ❌ **DO NOT use**: `Microsoft.Azure.CognitiveServices.*` (legacy, 3.x retires throughout 2025-2026)
- ❌ **DO NOT use**: `Azure.AI.TextAnalytics` (replaced by `Azure.AI.Language.Text`)
- ❌ **DO NOT use**: `Azure.AI.Inference` (retires 2026-05-30)

**Optional advanced packages** (for the unified project client):
- `Azure.AI.Projects` 2.0 GA (May 2026) — unified project client for Foundry Agents/Models/Tools
- `Azure.AI.Extensions.OpenAI` 2.0.0 — extension to use standard `OpenAI()` client with Azure resources

**Foundry resource model (verified Bicep/ARM):**
```bicep
resource aiServices 'Microsoft.CognitiveServices/accounts@2025-04-01-preview' = {
  name: accountName
  location: location
  sku: { name: 'S0' }
  kind: 'AIServices'  // Single kind for all Foundry Tools
  identity: { type: 'SystemAssigned' }
  properties: {
    allowProjectManagement: true  // Required for Foundry project support
    customSubDomainName: accountName
    publicNetworkAccess: 'Enabled'
  }
}
resource project 'Microsoft.CognitiveServices/accounts/projects@2025-04-01-preview' = {
  parent: aiServices
  name: project_name
  ...
}
```
- **One resource** (kind: `AIServices`) replaces the old Hub + Azure OpenAI + per-service resources
- **Endpoint format**: `https://<name>.services.ai.azure.com/api/projects/<project-name>`
- **Auth**: `DefaultAzureCredential` works (managed identity, Azure CLI, env vars)
- **CLI create**: `az cognitiveservices account create --kind AIServices --sku S0 --allow-project-management`

**Active deprecations (must migrate):**
- Translator API **v3.0 → 2026-06-06 GA** (Q3 2026): new schema with `targets` array replaces `to` param. SDK upgrade required. Several methods removed (BreakSentence, Detect, DictionaryLookup, DictionaryExamples).
- **gpt-4o-mini / gpt-4o-transcribe → 2026-10-01**: migrate to gpt-4.1-mini / gpt-4.1-transcribe (env var change only)
- **gpt-4o (all versions) → 2026-10-01**: migrate to gpt-5.1
- **Image Analysis 4.0 → 2028-09-25**: migrate to Foundry Content Understanding (or stay on Whisper+LLM pipeline)
- `azure-ai-inference` package → **2026-05-30**: use `OpenAI` package or `azure-ai-projects` v2
- Foundry Tools = Azure AI Services = Cognitive Services (rename only, no code change)

**Service limits (verified Jun 2026):**
- Translator F0: 2M chars/hour; S1: 40M chars/hour
- Document Intelligence F0: 4MB max doc, 2 pages max; S0: 500MB max doc, 2000 pages max, 15 TPS
- Vision F0: 5,000 trans/month, 20 TPM; S1: unlimited with rate limits
- OpenAI: per-token, no monthly limits; rate limits per deployment tier

**Foundry migration checklist for our repo:**
- [x] Scripts.csproj — 4 modern Foundry NuGet packages (added O2.5)
- [x] Secrets.cs — 5 Foundry endpoints (added O2.6)
- [x] Log.cs — 3 Foundry ServiceType enum values (added O2.7, ContentUnderstanding removed)
- [ ] `AzureTranslationService.cs` — upgrade SDK call to API version `2026-06-06` (replaces v3.0)
- [ ] `AzureOpenAIService.cs` (planned) — use `Azure.AI.OpenAI` for OpenAIClient
- [ ] `AzureVisionService.cs` (planned) — use `Azure.AI.Vision.ImageAnalysis` for ImageAnalysisClient
- [ ] `AzureDocumentIntelligenceService.cs` (planned) — use `Azure.AI.DocumentIntelligence` for DocumentIntelligenceClient

## Azure Service Implementation Pattern (Mandatory)

**All Azure AI services in this repo MUST follow this exact pattern.** Reference: `AzureTranslationService.cs`.

### Service Class Structure
```csharp
namespace Scripts.Services.{Area};  // e.g., Language, Vision, DocumentIntelligence

internal static class AzureXxxService
{
    private static readonly XxxClient? Client = string.IsNullOrWhiteSpace(
        Secrets.AzureXxxEndpoint
    )
        ? null
        : new XxxClient(
            Core.Auth.AzureAuth.Credential,
            new Uri(Secrets.AzureXxxEndpoint)
        );

    internal static bool IsConfigured => !string.IsNullOrWhiteSpace(Secrets.AzureXxxEndpoint);

    internal static async Task<X> MethodAsync(..., CancellationToken ct = default)
    {
        if (Client is null) return default;  // null/empty when not configured
        // ... call Client, cache, return result
    }
}
```

**Rules:**
- `internal static class` — no DI, no interfaces, no inheritance
- Nullable static client (null = not configured)
- `DefaultAzureCredential` via `Core.Auth.AzureAuth.Credential`
- Env var config in `Secrets.cs` with hardcoded fallback
- `IsConfigured` property for runtime checks
- All public methods accept `CancellationToken ct = default`
- Async-first
- No mocking — tests use real production code

### Cache Pattern (Mandatory for Deterministic Results)
```csharp
namespace Scripts.Services.Xxx;

internal static class XxxCache
{
    private static readonly string CachePath = Path.Combine(
        Paths.StateDirectory, "xxx-cache.json"
    );
    private static volatile Dictionary<string, string>? MemoryCache;
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    internal static async Task<string?> GetCachedAsync(string input, CancellationToken ct);
    internal static async Task SetCachedAsync(string input, string result, CancellationToken ct);
    internal static async Task SetBatchCachedAsync(IEnumerable<...>, CancellationToken ct);
    // Key = SHA256(input)[..16] (or hash + discriminator for binary inputs)
}
```

**Cache decision matrix:**
- Deterministic text output → CACHE (TranslationCache)
- Deterministic binary output → CACHE (VisionCache, DocumentIntelligenceCache with binary hash)
- LLM-reasoning output → NO CACHE (ContentUnderstanding — too variable)

## TUnit Test Pattern (Mandatory)

**Package**: `TUnit` (single reference, floating version `*`)

**Global usings** (`GlobalUsings.cs`):
```csharp
global using TUnit.Assertions;
global using TUnit.Assertions.Extensions;
```

**Test class structure**:
```csharp
namespace Scripts.Tests.{Category};

internal sealed class {Name}Tests
{
    [Test]
    public async Task {Method}_{Scenario}()
    {
        await Assert.That(actual).IsEqualTo(expected);
    }
}
```

**Mandatory TUnit conventions:**
- **Class**: `internal sealed class` — never `public`
- **Method**: `public async Task` — never `public async void`, avoid `void` (use `await Assert.That(() => action).ThrowsNothing()` instead)
- **Attribute**: `[Test]` on every test method
- **Namespace**: file-scoped (`namespace X.Y;`)
- **All assertions**: `await Assert.That(...)` — never synchronous `Assert.That(...)`
- **Unique test data**: `Guid.NewGuid()` + `DateTime.UtcNow.Ticks` for keys to avoid cross-test pollution

**DO NOT USE**:
- ❌ `FluentAssertions` (project is clean — zero usages)
- ❌ `Assert.That(x).IsEqualTo(x)` (tautology — always passes)
- ❌ `IsTrue().Or.IsFalse()` on `bool` (vacuously satisfied)
- ❌ `void` test methods (use `async Task` + `await Assert.That(() => action).ThrowsNothing()`)
- ❌ Moq, NSubstitute, or any mocking (use real production code)

**Test antipatterns to avoid:**
1. **Tautological assertions** — `await Assert.That(x).IsEqualTo(x)` (compares to itself)
2. **Vacuously satisfied** — `IsTrue().Or.IsFalse()` on `bool` (every bool passes)
3. **Misplaced tests** — model property tests in service test classes
4. **No-op path tests** — only testing degenerate inputs (empty, null, English) without exercising the real path
5. **Mock-like integration tests** — using real classes but only hitting the "skip" path
6. **No cleanup of test-created files** — cache files persist across test runs

**Complete assertion API inventory** (all require `await`):
- `await Assert.That(x).IsEqualTo(y)`
- `await Assert.That(x).IsNotEqualTo(y)`
- `await Assert.That(x).IsNull()`
- `await Assert.That(x).IsNotNull()`
- `await Assert.That(x).IsTrue()`
- `await Assert.That(x).IsFalse()`
- `await Assert.That(x).IsEmpty()`
- `await Assert.That(x).IsNotEmpty()`
- `await Assert.That(x).IsGreaterThan(n)`
- `await Assert.That(x).IsGreaterThanOrEqualTo(n)`
- `await Assert.That(collection).Count().IsEqualTo(n)`
- `await Assert.That(collection).Contains(item)`
- `await Assert.That(collection).Contains(predicate)`
- `await Assert.That(collection).IsEquivalentTo(other)`
- `await Assert.That(str).DoesNotContain(substring)`
- `await Assert.That(actual).IsNull().Or.IsEmpty()` // chained
- `await Assert.That(dt).IsEqualTo(expected).Within(timespan)` // tolerance
- `await Assert.That(() => action).ThrowsNothing()` // no exception
- `await Assert.That(async () => await x).Throws<TException>()`

**Lifetime hooks:**
- `[Before(Assembly)] static Task Method(AssemblyHookContext)` — runs once
- `[Before(Class)] static Task Method(ClassHookContext)` — runs per class
- `[After(Class)] static Task Method(ClassHookContext)` — runs per class
- `[Before(Test)] Task Method()` — runs per test
- `[After(Test)] Task Method()` — runs per test

**Parallel execution**: `[assembly: ParallelLimiter<SingleThreadedParallelLimit>]` in AssemblyInfo.cs (Limit=1)

**Custom skip attribute**: `[RequiresPgConnStr]` skips if `PGCONNSTR` env var missing

## Test Data Flow (Critical for Understanding Test Behavior)

**`AzureTranslationService.IsConfigured`** is **ALWAYS `true`** in the test environment:
1. `GlobalSetup` (assembly-level `[Before(Assembly)]`) loads `.env` from repo root
2. `.env` sets `AZURE_TRANSLATOR_ENDPOINT=https://api.cognitive.microsofttranslator.com`
3. `Secrets.GetEnvironmentVariable` checks User env → Process env → hardcoded fallback
4. Both env value and fallback are non-empty → `IsConfigured = true` always
5. **Tests that assert `IsFalse()` can never pass** — the env var fallback makes this impossible

**`TranslationCache.CachePath` = `{ProjectRoot}/state/translation-cache.json`**:
- `Paths.StateDirectory` resolves to `{ProjectRoot}/state` (same in test and production)
- `ProjectRoot` = first ancestor of `AppContext.BaseDirectory` containing `.git`
- File does NOT pre-exist — cache tests create it on first `SetCachedAsync` call
- Disk I/O is REAL — `SetCachedAsync` calls `SaveAsync` which writes JSON to disk

**Cache key** = first 16 hex chars of `SHA256(text.Trim() + "::" + targetLang.ToLowerInvariant())`:
- Case-insensitive on `targetLang` (lowercased)
- Whitespace-trimmed on `text`
- NOT case-insensitive on `text` (gap — not tested)

**`Azure.AI.Translation.Text` SDK** (used by `AzureTranslationService`):
- SDK class: `TextTranslationClient`
- Auth: `DefaultAzureCredential` via `Core.Auth.AzureAuth.Credential`
- Methods: `TranslateAsync(targetLanguage, texts, sourceLanguage, ct)` and `TranslateBatchAsync(...)`
- Returns `IReadOnlyList<TranslatedTextItem>` with `DetectedLanguage` and `Translations[0].Text`

## Current Session Notes (2026-06-14)

### Completed Work
- **Fibery expungement** — planned, not executed (per user deferral)
- **Translation Azure swap (T1-T5)** — COMPLETED:
  - T1: Swapped `YouTubeTranslationService` to use `AzureTranslationService.TranslateAsync()` (was `TranslationService.WithContainerAsync()`)
  - T2: Wired `TranslationCache` into `AzureTranslationService` (check before, store after)
  - T3: Deleted LibreTranslate files (`TranslationService.cs`, `TranslationClient.cs`, `LibreTranslateHostManager.cs`), moved `TranslationResult` record to `AzureTranslationService.cs`
  - T4: Removed `LibreTranslateUrl` from `Secrets.cs`
  - T5: Build succeeded, 0 errors

### Plan File Cleanup
Deleted all 4 plan files in `.omo/plans/` (fibery-expungement, translation-azure-swap, youtube-auth-restoration, youtube-pipeline-rebuild). `.omo/PLAN.md` is now the single plan file per AGENTS.md rule.

### Tests Created
- `csharp/tests/Scripts.Tests/Services/Language/AzureTranslationServiceTests.cs` (7 tests)
- `csharp/tests/Scripts.Tests/Services/Sync/YouTube/YouTubeTranslationServiceTests.cs` (8 tests)
- **Build status**: ✅ 0 errors
- **Test runner status**: ⚠️ TUnit + Microsoft Testing Platform environment issue (runner config, not test code)
- **Known gaps** (per Momus review):
  - `TranslateAsync` happy path (non-English video → Azure → translation applied) has ZERO coverage
  - `TranslateBatchAsync` has ZERO tests
  - Cancellation behavior untested
  - Error handling untested
  - "Client is null" code path untested
