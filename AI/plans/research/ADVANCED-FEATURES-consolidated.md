# Advanced Features — Consolidated Research

**Consolidated from:** 20260522-t1-10-ef10-queries-research.md, 20260522-t1-11-compiled-model-research.md, 20260522-t1-12-logging-research.md, 20260522-t1-13-lingua-research.md, 20260522-t1-14-resilience-research.md

---

## 1. EF10 Query Patterns

### 1.1 EF11-Only Patterns: Audit Results

**Search pattern:** `MaxByAsync|MinByAsync|JsonPathExists` across `csharp/src/**/*.cs`

**Result:** **ZERO instances found.** ✅

No files in the codebase use these EF11-only LINQ operators.

### 1.2 EF10 Replacement Patterns

| EF11-Only (Do NOT use)        | EF10 Replacement                                            |
| ----------------------------- | ----------------------------------------------------------- |
| `MaxByAsync` / `MinByAsync`   | `OrderByDescending(x => x.Timestamp).FirstOrDefaultAsync()` |
| `EF.Functions.JsonPathExists` | `EF.Functions.JsonContains()` / `@>` Npgsql operator        |

### 1.3 JSONB Column Inventory

Four entities use `JsonDocument?` properties mapped to PostgreSQL `jsonb`:

| Entity         | Property   | Type                         | Config File                       |
| -------------- | ---------- | ---------------------------- | --------------------------------- |
| `Artist`       | `Metadata` | `JsonDocument?`              | `ArtistConfiguration.cs:14`       |
| `Video`        | `Metadata` | `Dictionary<string, string>` | `VideoConfiguration.cs:16`        |
| `ExecutionLog` | `Payload`  | `JsonDocument?`              | `ExecutionLogConfiguration.cs:17` |
| `FiberyEntity` | `RawData`  | `JsonDocument?`              | `FiberyEntityConfiguration.cs:13` |

---

## 2. Compiled Models

### 2.1 Current State

**Not present** in any `.csproj` file. No `CompiledModels/` directory exists.

### 2.2 Required `.csproj` Changes

Target: `csharp/CSharpScripts.csproj`

```xml
<PropertyGroup>
  <EFOptimizeContext>true</EFOptimizeContext>
  <EFScaffoldModelStage>build</EFScaffoldModelStage>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.Tasks" Version="*" />
</ItemGroup>
```

### 2.3 Generation Command

```powershell
dotnet ef dbcontext optimize `
  --project csharp/CSharpScripts.csproj `
  --output-dir CompiledModels `
  --namespace CSharpScripts.Data.Compiled
```

### 2.4 Auto-Detection (EF9+)

**No `.UseModel()` call needed.** EF9+ auto-detects the compiled model when the `DbContext` and compiled model types are in the same assembly.

---

## 3. Logging

### 3.1 Current Log Path

**Current:** `<project_root>/logs/` (e.g., `C:\Users\Lance\Dev\Scripts\logs\`)

**Target per AGENTS.md:**
```
- Log directory: `%USERPROFILE%\.cache\logs\scripts\`
- File format: `yyyy-MM-dd_HH-mm-ss.json` (Serilog CompactJsonFormatter)
- Console output: human-readable Serilog template
- Stack traces: demystified via `Ben.Demystifier`
```

### 3.2 Required Changes

#### Paths.cs — Change `LogDirectory`

**File:** `csharp/src/Core/Paths.cs:9`

**Current:**
```csharp
public static readonly string LogDirectory = Path.Combine(path1: ProjectRoot, path2: "logs");
```

**Target:**
```csharp
public static readonly string LogDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".cache", "logs", "scripts"
);
```

#### Add Ben.Demystifier

1. **Add NuGet package:** `<PackageReference Include="Ben.Demystifier" Version="*" />`
2. **Modify `Log.Error()` and `Log.Fatal()`:**
   ```csharp
   public static void Error(Exception ex, string messageTemplate, params object?[] args) =>
       ActiveLogger.Error(exception: ex.Demystify(), messageTemplate: messageTemplate, propertyValues: args);
   ```

#### Ensure Directory Creation

Add to `Log` static constructor or before logger creation:
```csharp
Directory.CreateDirectory(Paths.LogDirectory);
```

---

## 4. Lingua Language Detection

### 4.1 Current State: NTextCat Setup

**File:** `csharp/src/Services/Language/LanguageIdentifier.cs`

Uses NTextCat with `Core14.profile.xml` file (which does NOT exist in repo).

### 4.2 Target: SearchPioneer.Lingua v1.0.5

| Property          | Value                                                                 |
| ----------------- | --------------------------------------------------------------------- |
| **Package**       | `SearchPioneer.Lingua`                                                |
| **Version**       | `1.0.5`                                                               |
| **Languages**     | 79 (vs NTextCat's 15)                                                 |
| **Model Loading** | Embedded in NuGet package — no file-based profile distribution needed |
| **Dependencies**  | **Zero** — fully self-contained                                       |

### 4.3 Updated LanguageIdentifier.cs

```csharp
using Lingua;
using static Lingua.Language;

namespace CSharpScripts.Services.Language;

internal static class LanguageIdentifier
{
    private static readonly ILanguageDetector Detector = LanguageDetectorBuilder
        .FromAllLanguages()
        .WithPreloadedLanguageModels()
        .Build();

    public static string? Detect(string text)
    {
        if (IsNullOrWhiteSpace(value: text) || text.Length < 15)
            return null;

        var result = Detector.DetectLanguageOf(text);
        return result == Unknown ? null : result.IsoCode6393();
    }

    public static bool IsEnglish(string text) =>
        Detect(text: text)?.EqualsIgnoreCase(other: "eng") == true;

    public static bool RequiresTranslation(string text)
    {
        var lang = Detect(text: text);
        return lang is { } && !lang.EqualsIgnoreCase(other: "eng");
    }
}
```

### 4.4 Changes Summary

| #   | Change                                                                                                  | Location                |
| --- | ------------------------------------------------------------------------------------------------------- | ----------------------- |
| 1   | Add `using Lingua;` and `using static Lingua.Language;`                                                 | `LanguageIdentifier.cs` |
| 2   | Replace `Lazy<RankedLanguageIdentifier?>` with `ILanguageDetector` field                                | `LanguageIdentifier.cs` |
| 3   | Replace builder pattern                                                                                 | `LanguageIdentifier.cs` |
| 4   | Replace `.Identify(text).FirstOrDefault()?.Item1.Iso639_3` with `.DetectLanguageOf(text).IsoCode6393()` | `LanguageIdentifier.cs` |
| 5   | Add `<PackageReference Include="SearchPioneer.Lingua" Version="1.0.5" />`                               | `CSharpScripts.csproj`  |

---

## 5. Resilience & Retry Policies

### 5.1 Current State: Polly v8 Already Implemented

**File:** `csharp/src/Core/Resilience.cs` (271 lines)

Uses the complete Polly v8 API surface:
- ✅ Circuit breaker (50% failure ratio, 3-min window, 30-sec break)
- ✅ Rate limiter (Last.fm only, 1 permit/sec)
- ✅ Retry (10 attempts, exponential backoff, jitter)
- ✅ Timeout (per-service: 30s-120s)

### 5.2 Gap: DB Retry Policy Missing

**No EF Core retry strategy is configured.** Both entry points for DbContext creation lack `EnableRetryOnFailure`.

#### DbContextRegistration.cs

```csharp
services.AddDbContext<ScriptsDbContext>(opts => opts.UseNpgsql(connectionString: connStr,
    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
        maxRetryCount: 3,
        maxRetryDelay: TimeSpan.FromSeconds(30),
        errorCodesToAdd: null
    )));
```

#### ScriptsDbContextFactory.cs

```csharp
optionsBuilder.UseNpgsql(connectionString: connStr,
    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
        maxRetryCount: 3,
        maxRetryDelay: TimeSpan.FromSeconds(30)
    ));
```

### 5.3 Infrastructure Resilience — Recommended Action

**Delete `Infrastructure/Resilience.cs`** — legacy duplicate that:
- Lacks circuit breaker, timeout, and rate limiter (present in Core)
- Uses Console logging instead of Serilog
- Creates new pipelines per call instead of caching
- No callers import its namespace

---

## 6. Summary of Required Changes

| Priority | Task                                                         | File(s)                                                  |
| -------- | ------------------------------------------------------------ | -------------------------------------------------------- |
| **P0**   | Add `EnableRetryOnFailure` to DbContext registration         | `DbContextRegistration.cs`, `ScriptsDbContextFactory.cs` |
| **P1**   | Change `LogDirectory` to `%USERPROFILE%\.cache\logs\scripts` | `Paths.cs`                                               |
| **P1**   | Add Ben.Demystifier integration                              | `Log.cs`, `CSharpScripts.csproj`                         |
| **P1**   | Migrate from NTextCat to Lingua                              | `LanguageIdentifier.cs`, `CSharpScripts.csproj`          |
| **P1**   | Add compiled model generation                                | `CSharpScripts.csproj`                                   |
| **P2**   | Delete `Infrastructure/Resilience.cs`                        | `Infrastructure/Resilience.cs`                           |

---

## 7. File Paths

```
Logging:
  C:\Users\Lance\Dev\Scripts\csharp\src\Core\Paths.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Core\Log.cs

Language Detection:
  C:\Users\Lance\Dev\Scripts\csharp\src\Services\Language\LanguageIdentifier.cs

Resilience:
  C:\Users\Lance\Dev\Scripts\csharp\src\Core\Resilience.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\DbContextRegistration.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContextFactory.cs

Project:
  C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj
```
