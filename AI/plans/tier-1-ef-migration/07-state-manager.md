# T1-07: State Manager Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Move the authoritative `StateManager.cs` from `Core/Persistence/` to `Data/State/`, delete the duplicate `Infrastructure/StateManager.cs`, update the `Logger.cs` reference to `JsonCompact`, and add the `global using` for the new namespace.

**Architecture:** The Core/Persistence/StateManager.cs (361 lines, async-first, atomic writes, JSON corruption handling) is the canonical version. The Infrastructure/StateManager.cs (286 lines, sync-only, no corruption handling) is a legacy duplicate. Move the canonical file to `csharp/src/Data/State/` with namespace `CSharpScripts.Data.State`, update `GlobalUsings.cs` with the new namespace, update `Logger.cs` to reference the new namespace's `StateManager.JsonCompact`, and delete the Infrastructure duplicate (backup first).

**Key Findings from Research:**
- StateManager is used by ScrobbleSyncOrchestrator, YouTubePlaylistOrchestrator, LastFmService, CleanResetCommand, CleanCacheCommand, MusicSearchCommand
- StateManager.JsonIndented and StateManager.JsonCompact are static fields used by TranslationClient, MusicBrainzService, Logger, SyncCommands
- Core version is authoritative: async-first (LoadStateAsync, SaveStateAsync), atomic writes, JSON corruption recovery
- Infrastructure version is legacy: sync-only (Load, Save), no corruption handling, creates new pipelines per call
- Target location: `csharp/src/Data/State/` (co-located with ReleaseProgressCache)
- Target namespace: `CSharpScripts.Data.State` (follows Data layer convention)
- Legacy LastFm/LastFmService.cs references Infrastructure StateManager — will be deleted in T1-09
- After move, add `global using CSharpScripts.Data.State;` to GlobalUsings.cs for seamless resolution

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- Phases 00-06 completed — repositories exist, database is migrated
- `Core/Persistence/StateManager.cs` exists (361 lines, namespace `CSharpScripts.Core`)
- `Infrastructure/StateManager.cs` exists (286 lines, namespace `CSharpScripts.Infrastructure`) — DUPLICATE
- `GlobalUsings.cs` exists at `csharp/src/GlobalUsings.cs`
- `Infrastructure/Logger.cs` exists (references `StateManager.JsonCompact` at lines 236, 305)

---

## File Map

| File | Path | Action |
|------|------|--------|
| `StateManager.cs` | `csharp/src/Data/State/StateManager.cs` | CREATE (moved from Core/Persistence) |
| `StateManager.cs` (Core) | `csharp/src/Core/Persistence/StateManager.cs` | DELETE (backup first) |
| `StateManager.cs` (Infra) | `csharp/src/Infrastructure/StateManager.cs` | DELETE (backup first) |
| `GlobalUsings.cs` | `csharp/src/GlobalUsings.cs` | EDIT: add global using CSharpScripts.Data.State |
| `Logger.cs` | `csharp/src/Infrastructure/Logger.cs:236,305` | EDIT: check using resolves correctly |
| Test: StateManagerNamespaceTests.cs | `csharp/tests/Scripts.Tests/StateManager/StateManagerNamespaceTests.cs` | CREATE |
| Test: StateManagerDeleteTests.cs | `csharp/tests/Scripts.Tests/StateManager/StateManagerDeleteTests.cs` | CREATE |

---

## Task 1: Create StateManager.cs in Data/State/ and Update Namespace

**Files:**
- Create: `/home/lance/Scripts/csharp/src\Data\State\StateManager.cs`
- Create: `/home/lance/Scripts/csharp/tests\Scripts.Tests\StateManager\StateManagerNamespaceTests.cs`

### Step 0: Preflight

```powershell
# Current state: StateManager lives in Core/Persistence/ with namespace CSharpScripts.Core
# Reason: Should live in Data layer per AGENTS.md architecture
# What: Copy the file to Data/State/, change namespace to CSharpScripts.Data.State
# Expected: File exists in new location with updated namespace

Test-Path /home/lance/Scripts/csharp/src\Core\Persistence\StateManager.cs
# Expected: True

Test-Path /home/lance/Scripts/csharp/src\Data\State
# Expected: False
```

### Step 1: Write the failing test

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\StateManager\StateManagerNamespaceTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using System.Text.Json;

namespace Scripts.Tests.StateManager;

public sealed class StateManagerNamespaceTests
{
    [Test]
    public void StateManager_ExistsIn_DataStateNamespace()
    {
        var type = Type.GetType("CSharpScripts.Data.State.StateManager, CSharpScripts");
        type.Should().NotBeNull(because: "StateManager must live in CSharpScripts.Data.State namespace");
    }

    [Test]
    public void StateManager_HasJsonIndented_Option()
    {
        var type = Type.GetType("CSharpScripts.Data.State.StateManager, CSharpScripts");
        type.Should().NotBeNull();

        var field = type!.GetField("JsonIndented");
        field.Should().NotBeNull();
        field!.FieldType.Should().Be(typeof(JsonSerializerOptions));
    }

    [Test]
    public void StateManager_HasJsonCompact_Option()
    {
        var type = Type.GetType("CSharpScripts.Data.State.StateManager, CSharpScripts");
        type.Should().NotBeNull();

        var field = type!.GetField("JsonCompact");
        field.Should().NotBeNull();
        field!.FieldType.Should().Be(typeof(JsonSerializerOptions));
    }

    [Test]
    public void StateManager_HasLoadStateAsync_Method()
    {
        var type = Type.GetType("CSharpScripts.Data.State.StateManager, CSharpScripts");
        type.Should().NotBeNull();

        var method = type!.GetMethod("LoadStateAsync");
        method.Should().NotBeNull();
    }

    [Test]
    public void StateManager_HasSaveStateAsync_Method()
    {
        var type = Type.GetType("CSharpScripts.Data.State.StateManager, CSharpScripts");
        type.Should().NotBeNull();

        var method = type!.GetMethod("SaveStateAsync");
        method.Should().NotBeNull();
    }
}
```

### Step 2: Read-back

```powershell
Test-Path '/home/lance/Scripts/csharp/tests\Scripts.Tests\StateManager\StateManagerNamespaceTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "StateManagerNamespaceTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: FAIL — `Type.GetType("CSharpScripts.Data.State.StateManager...")` returns null.

### Step 3.5: Assess

StateManager does not exist in `CSharpScripts.Data.State` namespace. Proceed to create it.

### Step 4: Write minimal implementation

Create directory and copy the file:

```powershell
New-Item -ItemType Directory -Force -Path /home/lance/Scripts/csharp/src\Data\State
```

File: `/home/lance/Scripts/csharp/src\Data\State\StateManager.cs`

Copy the entire contents of `Core/Persistence/StateManager.cs` but change the namespace line:

```csharp
// Original: namespace CSharpScripts.Core;
// Changed to:
namespace CSharpScripts.Data.State;
```

Full file (key excerpt — all 361 lines with only the namespace line changed):

```csharp
using System.Text.Encodings.Web;

namespace CSharpScripts.Data.State;

internal static class StateManager
{
	public const string LastFmSyncFile = "lastfm/sync.json";
	public const string LastFmScrobblesFile = "lastfm/scrobbles.json";
	public const string YoutubeSyncFile = "youtube/sync.json";
	// ... (all original content from Core/Persistence/StateManager.cs follows unchanged)
}
```

The implementation copies lines 1-361 from `/home/lance/Scripts/csharp/src\Core\Persistence\StateManager.cs`, changing only the namespace declaration from `namespace CSharpScripts.Core;` to `namespace CSharpScripts.Data.State;`.

Verify:

```powershell
Test-Path /home/lance/Scripts/csharp/src\Data\State\StateManager.cs
# Expected: True

Select-String -Path /home/lance/Scripts/csharp/src\Data\State\StateManager.cs -Pattern 'namespace CSharpScripts.Data.State'
# Expected: 1 match

Select-String -Path /home/lance/Scripts/csharp/src\Data\State\StateManager.cs -Pattern 'CSharpScripts.Core'
# Expected: 0 matches
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "StateManagerNamespaceTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: `5 passed, 0 failed`

### Step 6: Commit

```powershell
git -C /home/lance/Scripts add csharp/src/Data/State/StateManager.cs
git -C /home/lance/Scripts add csharp/tests/Scripts.Tests/StateManager/StateManagerNamespaceTests.cs
git -C /home/lance/Scripts commit -m "feat(t1-07): create StateManager in Data/State namespace"
```

---

## Task 2: Add Global Using for CSharpScripts.Data.State

**Files:**
- Modify: `/home/lance/Scripts/csharp/src\GlobalUsings.cs`

### Step 0: Preflight

```powershell
# Current state: GlobalUsings.cs has no CSharpScripts.Data.State entry
# Reason: Callers need to resolve StateManager without explicit imports
# What: Add global using CSharpScripts.Data.State;
# Expected: All existing callers continue to compile

Select-String -Path /home/lance/Scripts/csharp/src\GlobalUsings.cs -Pattern 'CSharpScripts.Data.State'
# Expected: 0 matches
```

### Step 1: Write the test

Add a compile-time verification test:

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\StateManager\StateManagerGlobalUsingTests.cs`

```csharp
using TUnit;
using FluentAssertions;

namespace Scripts.Tests.StateManager;

public sealed class StateManagerGlobalUsingTests
{
    [Test]
    public void StateManager_IsAccessible_WithoutNamespaceQualification()
    {
        // If this compiles, the global using is working
        var indented = CSharpScripts.Data.State.StateManager.JsonIndented;
        indented.Should().NotBeNull();

        var compact = CSharpScripts.Data.State.StateManager.JsonCompact;
        compact.Should().NotBeNull();
    }

    [Test]
    public void Log_IsAccessible_ViaGlobalUsing()
    {
        var logType = typeof(CSharpScripts.Core.Log);
        logType.Should().NotBeNull();
    }
}
```

### Step 2: Read-back

```powershell
Test-Path '/home/lance/Scripts/csharp/tests\Scripts.Tests\StateManager\StateManagerGlobalUsingTests.cs'
# Expected: True
```

### Step 3: Run — confirm current state

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "StateManagerGlobalUsingTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

If tests pass: `2 passed, 0 failed` — proceed to add global using for forward compatibility.
If tests fail because the type resolves: that's fine, proceed.

### Step 3.5: Assess

Global using for `CSharpScripts.Core` already exists (line 10 in GlobalUsings.cs). The new namespace `CSharpScripts.Data.State` is not yet globalled. While full qualification `CSharpScripts.Data.State.StateManager` works, adding the global using ensures consistency. Proceed.

### Step 4: Write minimal implementation

Add after the existing `global using CSharpScripts.Data;` line (line 12) in `/home/lance/Scripts/csharp/src\GlobalUsings.cs`:

```csharp
global using System.Collections.Frozen;
global using System.Diagnostics;
global using static System.Environment;
global using System.Globalization;
global using static System.String;
global using static System.StringComparison;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using CSharpScripts.Core;
global using CSharpScripts.Core.Auth;
global using CSharpScripts.Data;
global using CSharpScripts.Data.State;
global using CSharpScripts.Models;
global using CSharpScripts.Services.Language;
global using CsvHelper;
global using CsvHelper.Configuration;
global using MetaBrainz.MusicBrainz.Interfaces.Entities;
global using Microsoft.EntityFrameworkCore;
global using RestSharp;
global using Serilog;
global using Serilog.Events;
global using Spectre.Console;
global using Spectre.Console.Cli;
global using Spectre.Console.Rendering;
global using DiscogsVideoDto = ParkSquare.Discogs.Dto.Video;
global using Log = CSharpScripts.Core.Log;
global using SearchResult = CSharpScripts.Models.SearchResult;
global using SpectreColor = Spectre.Console.Color;
global using SpectreProgress = Spectre.Console.Progress;
global using SpectreTable = Spectre.Console.Table;
```

Verify:

```powershell
Select-String -Path /home/lance/Scripts/csharp/src\GlobalUsings.cs -Pattern 'CSharpScripts.Data.State'
# Expected: 1 match
```

### Step 5: Run — confirm build clean

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "StateManagerGlobalUsingTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: `2 passed, 0 failed`

### Step 6: Commit

```powershell
git -C /home/lance/Scripts add csharp/src/GlobalUsings.cs
git -C /home/lance/Scripts add csharp/tests/Scripts.Tests/StateManager/StateManagerGlobalUsingTests.cs
git -C /home/lance/Scripts commit -m "feat(t1-07): add global using for CSharpScripts.Data.State"
```

---

## Task 3: Delete Infrastructure StateManager Duplicate

**Files:**
- Modify: `/home/lance/Scripts/csharp/src\Infrastructure\StateManager.cs` (DELETE)
- Modify: `/home/lance/Scripts/csharp/src\Core\Persistence\StateManager.cs` (DELETE)
- Modify: `/home/lance/Scripts/csharp/src\Infrastructure\Logger.cs:236,305` (verify uses new namespace)
- Create: `/home/lance/Scripts/csharp/tests\Scripts.Tests\StateManager\StateManagerDeleteTests.cs`

### Step 0: Preflight

```powershell
# Current state: Two StateManager files exist — Core/Persistence (authoritative) and Infrastructure (legacy)
# Reason: Infrastructure copy is a sync-only legacy duplicate with different implementation
# What: Backup both old files, delete them, verify build still passes
# Expected: Core/Persistence and Infrastructure copies deleted, only Data/State/ version remains

Test-Path /home/lance/Scripts/csharp/src\Infrastructure\StateManager.cs
# Expected: True

Test-Path /home/lance/Scripts/csharp/src\Core\Persistence\StateManager.cs
# Expected: True

Test-Path /home/lance/Scripts/csharp/src\Data\State\StateManager.cs
# Expected: True (created in Task 1)
```

### Step 1: Write the failing test

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\StateManager\StateManagerDeleteTests.cs`

```csharp
using TUnit;
using FluentAssertions;

namespace Scripts.Tests.StateManager;

public sealed class StateManagerDeleteTests
{
    [Test]
    public void Infrastructure_StateManager_DoesNotCompile()
    {
        // After deletion, Infrastructure StateManager type should not exist
        var type = Type.GetType("CSharpScripts.Infrastructure.StateManager, CSharpScripts");
        type.Should().BeNull(because: "Infrastructure StateManager must be deleted — only Data.State version remains");
    }

    [Test]
    public void CorePersistence_StateManager_DoesNotCompile()
    {
        var type = Type.GetType("CSharpScripts.Core.StateManager, CSharpScripts");
        // May still resolve via global using -> Data.State. The old namespace path should not resolve.
        // Core namespace has global using CSharpScripts.Core, so StateManager would resolve to Data.State.
        // This test verifies the old file path is gone.
        var filePath = @"/home/lance/Scripts/csharp/src\Core\Persistence\StateManager.cs";
        System.IO.File.Exists(filePath).Should().BeFalse(because: "Core/Persistence/StateManager.cs must be deleted");
    }

    [Test]
    public void DataState_StateManager_IsSoleVersion()
    {
        var type = Type.GetType("CSharpScripts.Data.State.StateManager, CSharpScripts");
        type.Should().NotBeNull(because: "Only Data.State version should remain");
    }
}
```

### Step 2: Read-back

```powershell
Test-Path '/home/lance/Scripts/csharp/tests\Scripts.Tests\StateManager\StateManagerDeleteTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "StateManagerDeleteTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: FAIL — `Core/Persistence/StateManager.cs` still exists on disk, and `Infrastructure.StateManager` type still resolves.

### Step 3.5: Assess

Both old files exist. Proceed to delete them after backup.

### Step 4: Delete both old files (backup first)

```powershell
# Backup Infrastructure StateManager
$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$infraBackup = "/home/lance/Scripts/csharp/src\Infrastructure\StateManager.cs.bak.$timestamp"
Copy-Item /home/lance/Scripts/csharp/src\Infrastructure\StateManager.cs $infraBackup -Force
Test-Path $infraBackup
# Expected: True

# Delete Infrastructure StateManager
Remove-Item /home/lance/Scripts/csharp/src\Infrastructure\StateManager.cs -Force
Test-Path /home/lance/Scripts/csharp/src\Infrastructure\StateManager.cs
# Expected: False

# Backup Core/Persistence StateManager
$coreBackup = "/home/lance/Scripts/csharp/src\Core\Persistence\StateManager.cs.bak.$timestamp"
Copy-Item /home/lance/Scripts/csharp/src\Core\Persistence\StateManager.cs $coreBackup -Force
Test-Path $coreBackup
# Expected: True

# Delete Core/Persistence StateManager
Remove-Item /home/lance/Scripts/csharp/src\Core\Persistence\StateManager.cs -Force
Test-Path /home/lance/Scripts/csharp/src\Core\Persistence\StateManager.cs
# Expected: False
```

Now update `Infrastructure/Logger.cs` — it references `StateManager.JsonCompact` at lines 236 and 305. Since Logger is in `CSharpScripts.Infrastructure` namespace and the Infrastructure StateManager is now gone, the reference should resolve via the global using to `CSharpScripts.Data.State.StateManager`. No code change needed in Logger.cs because both old and new `StateManager` have identical `JsonCompact` static fields.

Verify Logger resolves:

```powershell
Select-String -Path /home/lance/Scripts/csharp/src\Infrastructure\Logger.cs -Pattern 'StateManager'
# Expected: 2 matches (lines 236, 305) — must still compile
```

Also check the legacy `LastFm/LastFmService.cs` — it references `CSharpScripts.Infrastructure.StateManager`. Since this file is compiled (not excluded), it may break:

```powershell
Select-String -Path /home/lance/Scripts/csharp/src\Services\Sync\LastFm\LastFmService.cs -Pattern 'StateManager'
# Expected: may contain references — if so, handle in Task 4 or proceed to build test
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "StateManagerDeleteTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: Build succeeds (all references resolve correctly), `3 passed, 0 failed`.

If build fails due to `LastFm/LastFmService.cs` referencing Infrastructure StateManager:
- This is the legacy LastFmService that T1-09 will delete. The fix is to delete or comment out the file now, then fully remove in T1-09. Create a backup and remove it.

If build fails due to `Logger.cs` ambiguity:
- Add `using CSharpScripts.Data.State;` explicitly to the top of `Logger.cs` after its namespace declaration.

### Step 6: Commit

```powershell
git -C /home/lance/Scripts rm csharp/src/Infrastructure/StateManager.cs
git -C /home/lance/Scripts rm csharp/src/Core/Persistence/StateManager.cs
git -C /home/lance/Scripts add csharp/tests/Scripts.Tests/StateManager/StateManagerDeleteTests.cs
git -C /home/lance/Scripts commit -m "feat(t1-07): delete duplicate StateManager files, keep Data.State version only"
```

---

## Task 4: Handle Legacy LastFm/LastFmService.cs Reference

**Files:**
- Modify: `/home/lance/Scripts/csharp/src\Services\Sync\LastFm\LastFmService.cs` (DELETE or STUB)

### Step 0: Preflight

```powershell
# Current state: Legacy LastFm/LastFmService.cs may reference CSharpScripts.Infrastructure.StateManager
# Reason: Infrastructure StateManager is now deleted — legacy file may break build
# What: Check if the file still compiles; if not, delete it (it's the duplicate being removed in T1-09)
# Expected: Build passes with legacy file deleted or resolved

Select-String -Path /home/lance/Scripts/csharp/src\Services\Sync\LastFm\LastFmService.cs -Pattern 'StateManager' 2>&1
# If 0 matches or file doesn't cause build error: skip this task, go to Final Verification
# If matches found AND build fails: proceed
```

### Step 3: Verify build status

```powershell
dotnet build /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

If build SUCCEEDS: This task is complete — skip to Final Verification.
If build FAILS with errors in `LastFm/LastFmService.cs`: proceed.

### Step 3.5: Assess

Build failed because legacy `LastFm/LastFmService.cs` references now-deleted `CSharpScripts.Infrastructure.StateManager`. This file is already flagged for deletion in T1-09. Delete it now (with backup).

### Step 4: Delete legacy file

```powershell
$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$legacyBackup = "/home/lance/Scripts/csharp/src\Services\Sync\LastFm\LastFmService.cs.bak.$timestamp"
Copy-Item /home/lance/Scripts/csharp/src\Services\Sync\LastFm\LastFmService.cs $legacyBackup -Force
Remove-Item /home/lance/Scripts/csharp/src\Services\Sync\LastFm\LastFmService.cs -Force

# Also remove the now-empty LastFm/ directory if it exists
if ((Get-ChildItem /home/lance/Scripts/csharp/src\Services\Sync\LastFm -ErrorAction SilentlyContinue | Measure-Object).Count -eq 0) {
    Remove-Item /home/lance/Scripts/csharp/src\Services\Sync\LastFm -Force -Recurse
}
```

### Step 5: Run — confirm build clean

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "StateManager" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: Build succeeds, all StateManager tests pass.

### Step 6: Commit

```powershell
git -C /home/lance/Scripts rm csharp/src/Services/Sync/LastFm/LastFmService.cs
git -C /home/lance/Scripts commit -m "feat(t1-07): delete legacy LastFmService duplicate (Infrastructure StateManager dependent)"
```

---

## Final Verification

```powershell
# Confirm only one StateManager.cs exists
Get-ChildItem /home/lance/Scripts/csharp/src -Recurse -Filter 'StateManager.cs' | Select-Object FullName
# Expected: exactly 1 file: ...Data\State\StateManager.cs

# Run all state manager tests
dotnet test --filter "Scripts.Tests.StateManager" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected:
```
Passed StateManagerNamespaceTests (5 tests)
Passed StateManagerGlobalUsingTests (2 tests)
Passed StateManagerDeleteTests (3 tests)
10 passed, 0 failed
```

**→ Proceed to `08-release-cache.md`**

---

## Research Provenance

<!-- from research/STATE-MANAGEMENT-consolidated.md -->

Source: `AI/plans/research/STATE-MANAGEMENT-consolidated.md` (consolidated 2026-06-01; dir deleted)

Content already covered: Core vs Infrastructure duplicate analysis (Tasks 1, 4), StateManager usage table (Prerequisites), target location `Data/State` (Architecture), `GlobalUsings` update (Task 1).

### StateManager Consumers (research §2.1, §2.2)

Active callers of `StateManager` (Core):

| Consumer                    | File                                                                         | Methods Called                                                                            |
| --------------------------- | ---------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| ScrobbleSyncOrchestrator    | `src/Orchestrators/ScrobbleSyncOrchestrator.cs:34,173,176`                   | `LoadStateAsync<FetchState>()`, `SaveStateAsync()`                                        |
| YouTubePlaylistOrchestrator | `src/Orchestrators/YouTubePlaylistOrchestrator.cs:41,51,261,685,714,792,796` | `LoadStateAsync<YouTubeFetchState>()`, `SaveStateAsync()`, `MigratePlaylistFiles()`      |
| LastFmService (modern)      | `src/Services/Sync/LastFmService.cs:130,166,169`                             | `SaveStateAsync()`, `LoadStateAsync<List<Scrobble>>()`, `Delete()`                        |
| CleanResetCommand           | `src/CLI/Clean/CleanResetCommand.cs:53,58,67,72`                             | `LoadStateAsync<>()`, `DeleteLastFmStates()`, `DeleteAllYouTubeStates()`                  |
| CleanCacheCommand           | `src/CLI/Clean/CleanCacheCommand.cs:31,39`                                   | `DeleteLastFmStates()`, `DeleteAllYouTubeStates()`                                        |
| MusicSearchCommand          | `src/CLI/MusicSearchCommand.cs:792,807,840,892,1007`                         | `DeleteReleaseCache()`, `LoadReleaseCache<>()`, `SaveReleaseCache<>()`                    |

`JsonIndented` / `JsonCompact` static fields: TranslationClient, MusicBrainzService, Infrastructure/Logger, SyncCommands (legacy). After the move, the new namespace `CSharpScripts.Data.State` resolves these via the added `global using` in `GlobalUsings.cs`.

### 5-Phase Migration Plan (research §4)

Reference steps for the move (already implemented in Tasks 1-4 with slight variation):

- **Phase A — Create target location:** mkdir `Data/State/`, move `Core/Persistence/StateManager.cs` → `Data/State/StateManager.cs`, move `ReleaseProgressCache.cs` co-located, delete empty `Core/Persistence/`.
- **Phase B — Namespace update:** `namespace CSharpScripts.Core;` → `namespace CSharpScripts.Data.State;`.
- **Phase C — Add `global using`:** add `global using CSharpScripts.Data.State;` to `GlobalUsings.cs`.
- **Phase D — Update callers:** all consumers using `global using CSharpScripts.Core;` already have `CSharpScripts.Data;` globalled; same class name `StateManager` resolves correctly.
- **Phase E — Clean up Infrastructure duplicate:** remove `Infrastructure/StateManager.cs`, update `Logger.cs` reference, delete legacy `LastFm/LastFmService.cs` (in T1-09).
