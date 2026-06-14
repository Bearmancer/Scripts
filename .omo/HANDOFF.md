# Handoff Document — Scripts Azure Services Integration

> **Purpose**: Standalone knowledge-transfer document for the Azure services layer in the Scripts repo. Read this first if you're new to the project. Complements (does not duplicate) `.omo/PLAN.md` (unfinished work) and `changelog.md` (done work).

---

## TL;DR

- **Repo**: Scripts — monolithic .NET 10 CLI app, single csproj, Exe, no library
- **Platform**: **Microsoft Foundry** (formerly Azure AI)
- **Active Azure services**: 1 (`AzureTranslationService` + `TranslationCache`)
- **Planned Azure services**: 3 (`AzureOpenAIService`, `AzureVisionService`, `AzureDocumentIntelligenceService`)
- **Planned CLI command**: 1 (`SubtitleCommand` for video→SRT pipeline) // Overhaul current existing subtitle functions

---

## 1. Project Context

### What is Scripts?

- YouTube playlist sync (via Google API)
- Music metadata enrichment (Last.fm, Discogs, MusicBrainz)
- PDF/EPUB/image reading pipeline
- Cloud usage reporting (Azure Cost Management)
- EF Core persistence to PostgreSQL
- CLI commands via Spectre.Console

### What is the Azure services layer?

The wrapper around Microsoft Foundry Tools (prebuilt AI services) that the Scripts app uses for:
- Text translation (auto-translating YouTube video titles/descriptions, music metadata)
- OCR (extracting text from PDFs, images, ebooks)
- AI-powered analysis (planned: image captioning, structured extraction, transcription)
- Video→SRT subtitle generation (planned)

---

## 2. Final Architecture (Directory Structure)

```
csharp/src/Services/
+-- Language/                                                [Scripts.Services.Language]
|   +-- AzureTranslationService.cs                           [ACTIVE]  R1 Translator
|   +-- TranslationCache.cs                                  [ACTIVE]  SHA256 key, SemaphoreSlim
|   +-- AzureOpenAIService.cs                                [NEW]     R5 Whisper + GPT-4o-mini
|   +-- AzureVisionService.cs                                [NEW]     R3 Image Analysis
|   +-- AzureDocumentIntelligenceService.cs                  [NEW]     R2 Structured extraction
|   +-- (no new caches; only TranslationCache)
+-- Sync/                                                   [existing, untouched]
+-- Music/                                                  [existing, untouched]
+-- Cloud/                                                  [CloudUsageService — existing]
+-- Reader/Ocr/                                              [Scripts.Services.Read.Ocr]
|   +-- IOcrProvider.cs                                      [ACTIVE]  interface
|   +-- IStructuredImageOcrProvider.cs                       [ACTIVE]  interface
|   +-- AzureDocumentIntelligenceOcrProvider.cs              [DELETE]  deduped → Services/Language
|   +-- GoogleVisionOcrProvider.cs                           [ACTIVE]
|   +-- TesseractOcrProvider.cs                              [ACTIVE]
|   +-- DocumentAiOcrProvider.cs                             [ACTIVE]
|   +-- OcrTextCleanup.cs                                    [ACTIVE]

csharp/src/CLI/Subtitle/                                    [Scripts.CLI.Subtitle]   <-- NEW
+-- SubtitleCommand.cs                                       [NEW]   video/audio → SRT

csharp/tests/Scripts.Tests/Services/Language/                 [Scripts.Tests.Services.Language]
+-- AzureTranslationServiceTests.cs                         [existing]
+-- AzureOpenAIServiceTests.cs                              [NEW]
+-- AzureVisionServiceTests.cs                              [NEW]
+-- AzureDocumentIntelligenceServiceTests.cs                [NEW]

csharp/tests/Scripts.Tests/CLI/Subtitle/                      [NEW]
+-- SubtitleCommandTests.cs                                  [NEW]
```

---

## 3. Service Catalog (Foundry Tools mapping)

| Our class                                                    | Foundry Tool          | Modern SDK package                     | API version                                       | Endpoint env var                    |
| ------------------------------------------------------------ | --------------------- | -------------------------------------- | ------------------------------------------------- | ----------------------------------- |
| `AzureTranslationService`                                    | Translator            | `Azure.AI.Translation.Text` 1.0.0+     | v3.0 → **2026-06-06 GA** (migrate)                | `AzureTranslatorEndpoint`           |
| `AzureDocumentIntelligenceOcrProvider` (Reader/Ocr) → DELETE | Document Intelligence | `Azure.AI.DocumentIntelligence` 1.0.0+ | v4.0 (2024-11-30 GA)                              | `AzureDocumentIntelligenceEndpoint` |
| `AzureDocumentIntelligenceService` (NEW)                     | Document Intelligence | same                                   | same                                              | same                                |
| `AzureVisionService` (NEW)                                   | Image Analysis        | `Azure.AI.Vision.ImageAnalysis` 1.0.0+ | v4.0 (retires 2028-09-25)                         | `AzureVisionEndpoint`               |
| `AzureOpenAIService` (NEW)                                   | Models (OpenAI)       | `Azure.AI.OpenAI` 1.0.0+               | gpt-4o-mini → **gpt-4.1-mini** (migrate Oct 2026) | `AzureOpenAIEndpoint`               |

### Cost Reference (per Microsoft pricing, Jun 2026)

| Service                           | Free tier (F0)     | Standard (S1)                       |
| --------------------------------- | ------------------ | ----------------------------------- |
| Translator                        | **2M chars/mo**    | $10/1M chars                        |
| Document Intelligence             | **500 pages/mo**   | $1.50/1k (Read), $10/1k (Prebuilt)  |
| Vision ImageAnalysis              | **5,000 trans/mo** | $1/1k (Group 1), $1.50/1k (Group 2) |
| OpenAI Whisper                    | None               | $0.006/min ($0.36/hr)               |
| OpenAI GPT-4o-mini                | None               | $0.15/$0.60 per 1M tok              |
| OpenAI gpt-4.1-mini (replacement) | None               | $0.40/$1.60 per 1M tok (2.7× more)  |

### Pipeline cost (2hr Japanese video → English SRT)

| Pipeline                                              | Cost (per video)                                 | Quality                                      |
| ----------------------------------------------------- | ------------------------------------------------ | -------------------------------------------- |
| **Whisper + GPT-4o-mini** (default `SubtitleCommand`) | **$0.73**                                        | ~5-8% JA WER, slight translation variability |
| Whisper + Azure Translator                            | $0.72 (first 55/mo, free tier covers Translator) | ~5-8% JA WER, deterministic                  |
| Local Whisper + Azure Translator                      | $0.40 (if you have GPU)                          | ~5-8% JA WER, deterministic                  |

---

## 4. Foundry Tools — Modern NuGet Packages

### ✅ Use these (modern, forward-compatible)

```xml
<PackageReference Include="Azure.AI.Translation.Text" Version="*" />        <!-- Translator -->
<PackageReference Include="Azure.AI.DocumentIntelligence" Version="*" />   <!-- Document Intelligence -->
<PackageReference Include="Azure.AI.Vision.ImageAnalysis" Version="*" />   <!-- Image Analysis -->
<PackageReference Include="Azure.AI.OpenAI" Version="*" />                 <!-- OpenAI inference -->
<PackageReference Include="Azure.Identity" Version="*" />                  <!-- DefaultAzureCredential -->
```

### ✅ Optional advanced packages

```xml
<PackageReference Include="Azure.AI.Projects" Version="2.*" />              <!-- Unified project client (Foundry Agents/Models/Tools) -->
<PackageReference Include="Azure.AI.Extensions.OpenAI" Version="2.*" />     <!-- Use standard OpenAI() client with Azure resources -->
```

### ❌ DO NOT use (legacy, will break)

- ❌ `Microsoft.Azure.CognitiveServices.*` (3.x retires 2025-2026)
- ❌ `Azure.AI.TextAnalytics` (replaced by `Azure.AI.Language.Text`)
- ❌ `Azure.AI.Inference` (retires 2026-05-30)
- ❌ `Azure.AI.FormRecognizer` (renamed to `Azure.AI.DocumentIntelligence` in 2023)

---

## 5. Patterns (Mandatory)

### Service class structure (copy `AzureTranslationService.cs`)

```csharp
namespace Scripts.Services.Language;

internal static class AzureTranslationService
{
    private static readonly TextTranslationClient? Client = string.IsNullOrWhiteSpace(
        Secrets.AzureTranslatorEndpoint
    )
        ? throw new InvalidOperationException(
            "AZURE_TRANSLATOR_ENDPOINT not set. " +
            "Set the env var or add a hardcoded fallback to Secrets.cs.")
        : new TextTranslationClient(
            Core.Auth.AzureAuth.Credential,
            new Uri(Secrets.AzureTranslatorEndpoint));

    public static async Task<TranslationResult?> TranslateAsync(
        string text,
        string? sourceLanguage = null,
        CancellationToken ct = default)
    {
        // 1. Check cache
        var cached = await TranslationCache.GetCachedAsync(text, "en", ct);
        if (cached is { }) return new TranslationResult(cached, sourceLanguage ?? "unknown");

        // 2. Call SDK
        try
        {
            var response = await Client.TranslateAsync("en", [text], sourceLanguage, ct);
            if (response.Value is not { Count: > 0 } items) return null;
            var translated = items[0].Translations?[0].Text;
            if (translated is null) return null;

            // 3. Store cache
            await TranslationCache.SetCachedAsync(text, "en", translated, ct);
            return new TranslationResult(translated, items[0].DetectedLanguage?.Language ?? sourceLanguage ?? "unknown");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Warning("Azure translation failed: {Error}", ex.Message);
            return null;
        }
    }
}
```

**Rules:**
- `internal static class` for stateless services
- `internal sealed class` only when implementing an interface (e.g., `IOcrProvider`)
- Nullable static `Client?` field initialized at class level
- **Auto-terminate**: throw `InvalidOperationException` in the field initializer if endpoint missing
- `Core.Auth.AzureAuth.Credential` (DefaultAzureCredential) for auth
- `try/catch (Exception ex) when (ex is not OperationCanceledException)` with `Log.Warning`
- `CancellationToken ct = default` on every public method
- `internal record XxxResult(...)` co-located in same file (record is a public DTO returned to callers)
- NO `void` methods — use `Task<bool>` for success/failure, `Task<T?>` for nullable results

### Cache pattern (only `TranslationCache` exists; no new caches)

```csharp
internal static class TranslationCache
{
    private static readonly string CachePath = Path.Combine(Paths.StateDirectory, "translation-cache.json");
    private static volatile Dictionary<string, string>? MemoryCache;
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    public static async Task<string?> GetCachedAsync(string text, string targetLang, CancellationToken ct)
    {
        var key = ComputeKey(text, targetLang);
        return (await GetCacheAsync(ct)).GetValueOrDefault(key);
    }

    public static async Task SetCachedAsync(string text, string targetLang, string translation, CancellationToken ct)
    {
        await FileLock.WaitAsync(ct);
        try
        {
            var cache = await GetCacheUnsafeAsync(ct);
            cache[ComputeKey(text, targetLang)] = translation;
            await SaveAsync(cache, ct);
            MemoryCache = cache;
        }
        finally { FileLock.Release(); }
    }
    // ...
}
```

**Why only 1 cache:**
- Translation: cheap deterministic text → cache (TranslationCache)
- Whisper audio: large binary, low reuse, expensive to cache → no cache
- Vision images: same → no cache
- DI documents: same → no cache
- LLM output (GPT-4o-mini): non-deterministic → no cache

### Test pattern (TUnit, real production code, no mocking)

```csharp
namespace Scripts.Tests.Services.Language;

internal sealed class AzureTranslationServiceTests
{
    [After(Test)]
    public void CleanupTranslateDelegate() => AzureTranslationService.TranslateDelegate = null;

    [After(Test)]
    public void CleanupCacheFile() => DeleteCacheFileIfExists();

    [Test]
    public async Task IsConfigured_ReturnsTrue_WhenEndpointIsConfigured() =>
        await Assert.That(AzureTranslationService.IsConfigured).IsTrue();

    [Test]
    public async Task TranslateAsync_ReturnsTranslationResult_WhenDelegateIsSet()
    {
        AzureTranslationService.TranslateDelegate = (text, sourceLang, ct) =>
            Task.FromResult<TranslationResult?>(
                new TranslationResult(Translation: $"translated_{text}", DetectedLanguage: sourceLang ?? "fr"));
        var result = await AzureTranslationService.TranslateAsync(text: "Bonjour", sourceLanguage: "fr");
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Translation).IsEqualTo("translated_Bonjour");
    }
}
```

**Rules (per AGENTS.md):**
- `internal sealed class` (never `public`)
- `public async Task` (never `void`)
- `[Test]` attribute on every test
- `await Assert.That(...)` (never synchronous)
- Unique test data: `Guid.NewGuid()` + `DateTime.UtcNow.Ticks`
- No mocking (use real production code)
- `[After(Test)]` for cleanup (cache file, delegate state)
- Custom skip: `[RequiresAzureXxxEndpoint]` for tests requiring real Azure

### Access modifiers

| Element                  | Modifier                              | Why                                                             |
| ------------------------ | ------------------------------------- | --------------------------------------------------------------- |
| Top-level classes        | `internal`                            | This is a CLI app, not a library. `public` has no consumer.     |
| Non-static classes       | `internal sealed`                     | Documents design intent (not for inheritance), slight JIT perf. |
| Static classes           | `internal static` (implicitly sealed) | Standard.                                                       |
| Members                  | `private` (default)                   | Smallest scope.                                                 |
| Fields that don't change | `readonly`                            | Immutability, thread-safety (especially `static readonly`).     |
| Records (DTOs)           | `public sealed record`                | Cross-boundary data types.                                      |
| `protected`              | **AVOID**                             | No inheritance planned.                                         |
| `public` for top-level   | **AVOID**                             | False signal of public API.                                     |
| File-scoped helpers      | `internal file class` (C# 11+)        | Tight encapsulation.                                            |
| Test-mock interfaces     | **NEVER**                             | Tests use real production code.                                 |

---

## 6. Active Deprecations Affecting This Repo

| What                          | When       | Action                                                                                                                                                                                                           | Effort                      |
| ----------------------------- | ---------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------- |
| **Translator API v3.0**       | Q3 2026    | Upgrade `AzureTranslationService.cs` to API version `2026-06-06` (new schema: `targets` array replaces `to` param; several methods removed: `BreakSentence`, `Detect`, `DictionaryLookup`, `DictionaryExamples`) | 1 file, ~10 lines           |
| **gpt-4o-mini**               | 2026-10-01 | Change `Secrets.AzureOpenAIDeploymentName` env var to `gpt-4.1-mini` (env var only, no code change)                                                                                                              | 1 env var                   |
| **gpt-4o-transcribe**         | 2026-10-01 | Same as above for the Whisper deployment                                                                                                                                                                         | 1 env var                   |
| **gpt-4o (all versions)**     | 2026-10-01 | Change to `gpt-5.1`                                                                                                                                                                                              | 1 env var                   |
| **Image Analysis 4.0**        | 2028-09-25 | Migrate `AzureVisionService` to Foundry Content Understanding (or Whisper+LLM pipeline). 2.5 years out.                                                                                                          | Migration later             |
| `azure-ai-inference` package  | 2026-05-30 | Use `OpenAI` package or `azure-ai-projects` v2                                                                                                                                                                   | No action (we don't use it) |
| Computer Vision API v1.0-v3.1 | 2026-09-13 | No action (we use v4.0)                                                                                                                                                                                          | None                        |
| Document Intelligence v2.0    | 2026-08-31 | No action (we use v4.0)                                                                                                                                                                                          | None                        |
| Document Intelligence v2.1    | 2027-09-15 | No action (we use v4.0)                                                                                                                                                                                          | None                        |
| Document Intelligence v3.0    | 2029-03-30 | No action (we use v4.0)                                                                                                                                                                                          | None                        |

---

## 7. Secrets Configuration (`.omo/PLAN.md` → `Secrets.cs`)

| Property                            | Env var                                | Fallback (hardcoded)                                               |
| ----------------------------------- | -------------------------------------- | ------------------------------------------------------------------ |
| `GoogleClientId`                    | `GOOGLE_CLIENT_ID`                     | (required, throws if missing)                                      |
| `GoogleClientSecret`                | `GOOGLE_CLIENT_SECRET`                 | (required)                                                         |
| `YouTubeSpreadsheetId`              | `YOUTUBE_SPREADSHEET_ID`               | (required)                                                         |
| `LastFmApiKey`                      | `LAST_FM_API_KEY`                      | (required)                                                         |
| `LastFmApiSecret`                   | `LAST_FM_API_SECRET`                   | (required)                                                         |
| `LastFmSpreadsheetId`               | `LAST_FM_SPREADSHEET_ID`               | (required)                                                         |
| `DiscogsToken`                      | `DISCOGS_USER_TOKEN`                   | (required)                                                         |
| `GoogleDocumentAiProcessorName`     | `GOOGLE_DOCUMENTAI_PROCESSOR_NAME`     | (required)                                                         |
| `AzureTranslatorEndpoint`           | `AZURE_TRANSLATOR_ENDPOINT`            | `https://translator-lance.cognitiveservices.azure.com/`            |
| `AzureDocumentIntelligenceEndpoint` | `AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT` | `https://document-intelligence-lance.cognitiveservices.azure.com/` |
| `AzureDocumentIntelligenceModelId`  | `AZURE_DOCUMENT_INTELLIGENCE_MODEL_ID` | `prebuilt-layout`                                                  |
| `AzureVisionEndpoint`               | `AZURE_VISION_ENDPOINT`                | `https://vision-lance.cognitiveservices.azure.com/`                |
| `AzureOpenAIEndpoint`               | `AZURE_OPENAI_ENDPOINT`                | `https://openai-lance.openai.azure.com/`                           |
| `AzureOpenAIDeploymentName`         | `AZURE_OPENAI_DEPLOYMENT_NAME`         | `gpt-4o-mini` (migrate to `gpt-4.1-mini` by Oct 2026)              |

---

## 8. Foundry Resource Setup (Bicep)

For production deployment, the recommended pattern is a single Foundry resource with child projects:

```bicep
resource aiServices 'Microsoft.CognitiveServices/accounts@2025-04-01-preview' = {
  name: accountName
  location: location
  sku: { name: 'S0' }
  kind: 'AIServices'  // Single kind for all Foundry Tools
  identity: { type: 'SystemAssigned' }
  properties: {
    allowProjectManagement: true  // Required for project support
    customSubDomainName: accountName
    publicNetworkAccess: 'Enabled'
  }
}

resource project 'Microsoft.CognitiveServices/accounts/projects@2025-04-01-preview' = {
  parent: aiServices
  name: 'scripts-prod'
  location: location
  identity: { type: 'SystemAssigned' }
  properties: {
    description: 'Scripts monolithic CLI app'
    displayName: 'Scripts Production'
  }
}
```

**CLI equivalent**:
```bash
az cognitiveservices account create \
  --name my-foundry-resource \
  --resource-group scripts-rg \
  --kind AIServices \
  --sku S0 \
  --location eastus \
  --allow-project-management
```

**Auth**: `DefaultAzureCredential` (managed identity in production, env vars in dev) — already wired via `Core.Auth.AzureAuth`.

---

## 9. Quick-Start for New Developers

### Build and test

```bash
cd csharp
dotnet build Scripts.slnx                    # Build (must show 0 errors, 0 warnings)
dotnet test                                # Run all tests (TUnit + Microsoft Testing Platform)
```

### Add a new Azure service (3-step recipe)

**Step 1**: Add the package to `csharp/Scripts.csproj`:
```xml
<PackageReference Include="Azure.AI.NewService" Version="*" />
```

**Step 2**: Add endpoint to `csharp/src/Core/Auth/Secrets.cs`:
```csharp
public static string AzureNewServiceEndpoint =>
    GetEnvironmentVariable("AZURE_NEW_SERVICE_ENDPOINT") ?? "https://new-service-lance.cognitiveservices.azure.com/";
```

**Step 3**: Create the service class at `csharp/src/Services/{Area}/AzureNewServiceService.cs`:
```csharp
using Azure.AI.NewService;

namespace Scripts.Services.{Area};

internal static class AzureNewServiceService
{
    private static readonly NewServiceClient? Client = string.IsNullOrWhiteSpace(
        Secrets.AzureNewServiceEndpoint
    )
        ? throw new InvalidOperationException("AZURE_NEW_SERVICE_ENDPOINT not set.")
        : new NewServiceClient(
            new Uri(Secrets.AzureNewServiceEndpoint),
            Core.Auth.AzureAuth.Credential);

    public static bool IsConfigured => Client is not null;

    public static async Task<NewServiceResult?> DoSomethingAsync(
        string input, CancellationToken ct = default)
    {
        if (Client is null) return null;
        try
        {
            var response = await Client.DoSomethingAsync(input, ct);
            return new NewServiceResult(/* map response */);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Warning("Azure NewService failed: {Error}", ex.Message);
            return null;
        }
    }
}

internal record NewServiceResult(/* fields */);
```

**Step 4**: Add tests at `csharp/tests/Scripts.Tests/Services/{Area}/AzureNewServiceServiceTests.cs`:
```csharp
namespace Scripts.Tests.Services.{Area};

internal sealed class AzureNewServiceServiceTests
{
    [Test]
    public async Task IsConfigured_ReturnsTrue_WhenEndpointIsConfigured() =>
        await Assert.That(AzureNewServiceService.IsConfigured).IsTrue();

    [Test]
    public async Task DoSomethingAsync_ReturnsResult_OnSuccess() =>
        await Assert.That(
            await AzureNewServiceService.DoSomethingAsync("test", CancellationToken.None)
        ).IsNotNull();
}
```

**Step 5**: Build, test, commit. Update `changelog.md` with a done entry. Update `.omo/PLAN.md` checkboxes.

### Common pitfalls

- **Forgetting auto-terminate**: If your service returns `null` for "not configured", change it to throw in the static initializer. Per AGENTS.md.
- **Using `void` for async methods**: Use `Task<T>` always. If you don't have a return value, use `Task<bool>` for success/failure.
- **Adding `public` to top-level types**: Use `internal`. This is a CLI app, not a library.
- **Adding test-mock interfaces**: Don't. Tests use real production code via the `IsConfigured` gate.
- **Adding a cache for non-deterministic output**: Don't. Per AGENTS.md cache decision matrix.
- **Writing comments in code**: Don't. Per AGENTS.md.
- **Calling `new XxxClient(...)` per method call**: No. Make it a `static readonly` field. SDK clients hold connection pools.

---

## 10. Open Work (per `.omo/PLAN.md`)

| Task                               | Description                                                                                        | Effort            |
| ---------------------------------- | -------------------------------------------------------------------------------------------------- | ----------------- |
| O2.0                               | Upgrade `AzureTranslationService.cs` to Translator API version `2026-06-06` (v3.0 retires Q3 2026) | 1 file, ~10 lines |
| O2.1                               | Build `AzureOpenAIService` (Whisper + GPT-4o-mini)                                                 | ~110 lines        |
| O2.2                               | Build `AzureVisionService` (image OCR + caption + tags)                                            | ~70 lines         |
| O2.3a                              | Build `AzureDocumentIntelligenceService` (dedupes OcrProvider)                                     | ~100 lines        |
| O2.3b                              | Delete `Reader/Ocr/AzureDocumentIntelligenceOcrProvider.cs`                                        | -184 lines        |
| O2.3c                              | Refactor 3 Local extractors to call O2.3a                                                          | ~30 lines         |
| O2.12                              | Build `SubtitleCommand` (uses O2.1)                                                                | ~150 lines        |
| O2.8-O2.10, O2.13                  | Tests for new services + command                                                                   | ~490 lines        |
| Build                              | `dotnet build` — 0 errors, 0 warnings                                                              | n/a               |
| Cleanup                            | Flip checkboxes, update changelog                                                                  | n/a               |
| YouTube Pipeline Rebuild Tasks 2-9 | Unfocused but unfinished                                                                           | n/a               |
| Fibery Expungement                 | Deferred per user                                                                                  | n/a               |
| Phase 2 TDD Gates                  | Low priority                                                                                       | n/a               |

---

## 11. Glossary

| Term                          | Meaning                                                                                                                                                                                                                                                      |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Foundry Tools**             | Microsoft's brand for all prebuilt AI services (formerly Cognitive Services → Azure AI Services). Includes Translator, Document Intelligence, Vision, Speech, Language, Content Understanding, Content Safety.                                               |
| **Microsoft Foundry**         | The unified platform (formerly Azure AI Foundry). Includes Foundry Tools + Foundry Models (LLM hosting) + Foundry Agent Service (agent runtime) + Foundry IQ (knowledge retrieval) + Foundry Control Plane (governance).                                     |
| **Cognitive Services**        | Old name for what is now Foundry Tools. SDK package names still use "Azure" prefix for backward compat.                                                                                                                                                      |
| **DefaultAzureCredential**    | Azure SDK credential class that auto-selects the best credential based on environment (managed identity, env vars, Azure CLI, etc.). Used by `Core.Auth.AzureAuth.Credential`.                                                                               |
| **R1, R2, R3, R4, R5**        | Cost ranks used internally. R1 = cheapest (Translator). R5 = most expensive (OpenAI).                                                                                                                                                                        |
| **Auto-terminate**            | The pattern where a service throws `InvalidOperationException` at static initializer if the endpoint is not configured. Avoids silent `IsConfigured` checks at every method call.                                                                            |
| **Deduped**                   | Refactoring to remove a duplicate. In Phase 0.2, the existing `Reader/Ocr/AzureDocumentIntelligenceOcrProvider` is being deduped into the new `Language/AzureDocumentIntelligenceService` (same SDK, same endpoint, same client class — just different job). |
| **Prefixed env var fallback** | `Secrets.cs` pattern: `GetEnvironmentVariable("FOO") ?? "hardcoded-default"`. Falls back to a hardcoded value if env var missing. Use for non-sensitive endpoints only.                                                                                      |
| **`#if DEBUG` test seam**     | The pattern where test-only code (e.g., `TranslateDelegate` field) is wrapped in `#if DEBUG` to exclude from production builds. Used in `AzureTranslationService.cs`.                                                                                        |

---

## 12. Source-of-Truth Documents

| Doc                           | What's in it                                                                                                                                                             |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `.omo/PLAN.md`                | **Unfinished work** (current implementation order, future phases, handoff with ASCII tree + cost table + Foundry SDK packages). The ONLY plan file.                      |
| `changelog.md`                | **Done work** (per-session entries, completed tasks, recent discoveries, Foundry verification, governance cleanup). Updated as work completes.                           |
| `AGENTS.md`                   | **Rules** (TUnit conventions, Azure service pattern, cache pattern, Foundry Tools reference, deprecation watch list, access modifiers policy). Read this for governance. |
| `.omo/HANDOFF.md` (this file) | **Knowledge transfer** (architecture, service catalog, patterns, deprecations, quick-start). Read this for onboarding.                                                   |
| `.omo/EF_SCHEMA_AUTHORITY.md` | EF database schema reference.                                                                                                                                            |
| `.omo/drafts/`                | Design records (e.g., music-work-schema.md).                                                                                                                             |
| `.omo/evidence/`              | TDD gate evidence.                                                                                                                                                       |
| `.omo/notepads/`              | Active debugging knowledge (YouTube pipeline rebuild).                                                                                                                   |
| `.omo/diagrams/final/`        | Architecture diagrams (csharp, powershell, python).                                                                                                                      |

**Read order for a new dev**: `AGENTS.md` → this file (HANDOFF.md) → `.omo/PLAN.md` (current unfinished work) → `changelog.md` (history)
