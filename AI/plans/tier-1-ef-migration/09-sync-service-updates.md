# T1-09: Sync Service Updates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Delete the legacy `Sync/LastFm/LastFmService.cs` duplicate, inject `IDbContextFactory<ScriptsDbContext>` into the canonical `LastFmService`, add `EF.Functions.ILike` support for artist/track name lookups, and add `ExecuteDeleteAsync` for YouTube cleanup preparation.

**Architecture:** The canonical `Sync/LastFmService.cs` (namespace `CSharpScripts.Services.Sync.LastFm`) is extended with a primary constructor parameter for `IDbContextFactory<ScriptsDbContext>`. An `ILike` helper method is added for case-insensitive artist name lookups. The legacy `Sync/LastFm/LastFmService.cs` (which redefines models inline and uses sync StateManager) is deleted. For the compiled path, existing `PostgresService.cs` code remains unchanged (already uses `ExecuteUpdateAsync` correctly).

**Key Findings from Research:**
- Duplicate LastFmService.cs exists at `src/Services/Sync/LastFm/LastFmService.cs` (legacy, sync-only, redefines models inline)
- Canonical LastFmService at `src/Services/Sync/LastFmService.cs` is async-first, uses StateManager correctly
- PostgresService.cs already uses ExecuteUpdateAsync for upserts (line 21) and SaveChangesAsync for bulk inserts (line 39) — compliant with EF Core mandates
- ILike/EF.Functions.Like not yet used anywhere — greenfield capability for artist/track name lookups
- No EF11-only patterns (MaxByAsync, MinByAsync, JsonPathExists) found in codebase — all queries are EF10-compatible
- LastFmService needs IDbContextFactory injection for database access (artist lookups, scrobble cleanup)
- ExecuteDeleteAsync will be used for YouTube cleanup operations (future enhancement)

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- Phases 00-08 completed — repositories exist, database is migrated, StateManager moved
- `Sync/LastFmService.cs` exists at `csharp/src/Services/Sync/LastFmService.cs` (canonical, async)
- Legacy `Sync/LastFm/LastFmService.cs` may already be deleted in T1-07 Task 4 — verify
- `PostgresService.cs` exists at `csharp/src/Services/PostgresService.cs` (uses ExecuteUpdateAsync)
- `IDbContextFactory<ScriptsDbContext>` is registered via `AddScriptsDbContext`

---

## File Map

| File | Path | Action |
|------|------|--------|
| `LastFmService.cs` (canonical) | `csharp/src/Services/Sync/LastFmService.cs` | EDIT: inject IDbContextFactory, add ILike lookup, add ExecuteDeleteAsync |
| `LastFmService.cs` (legacy) | `csharp/src/Services/Sync/LastFm/LastFmService.cs` | DELETE if not already done |
| `PostgresService.cs` | `csharp/src/Services/PostgresService.cs` | No changes needed (already uses ExecuteUpdateAsync) |
| Test: SyncServiceTests.cs | `csharp/tests/Scripts.Tests/SyncService/SyncServiceTests.cs` | CREATE |
| Test: LastFmServiceDeleteTests.cs | `csharp/tests/Scripts.Tests/SyncService/LastFmServiceDeleteTests.cs` | CREATE |

---

## Task 1: Delete Legacy LastFm/LastFmService.cs (if not already done)

**Files:**
- Delete: `C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFm\LastFmService.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SyncService\LastFmServiceDeleteTests.cs`

### Step 0: Preflight

```powershell
# Current state: Legacy LastFmService may already have been deleted in T1-07 Task 4
# Reason: Legacy file redefines models inline, uses sync StateManager, different Scrobble type
# What: Verify file status; if still exists, delete it; if already gone, write test confirming it

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFm\LastFmService.cs
# If True: proceed with deletion
# If False: skip this task — already deleted in T1-07

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFmService.cs
# Expected: True (canonical version must exist)
```

### Step 1: Write the test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SyncService\LastFmServiceDeleteTests.cs`

```csharp
using TUnit;
using FluentAssertions;

namespace Scripts.Tests.SyncService;

public sealed class LastFmServiceDeleteTests
{
    [Test]
    public void LegacyLastFmService_FileDoesNotExist()
    {
        var path = @"C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFm\LastFmService.cs";
        System.IO.File.Exists(path).Should().BeFalse(
            because: "Legacy duplicate LastFmService must be deleted — canonical version is at Services/Sync/LastFmService.cs");
    }

    [Test]
    public void CanonicalLastFmService_FileExists()
    {
        var path = @"C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFmService.cs";
        System.IO.File.Exists(path).Should().BeTrue(
            because: "Canonical LastFmService at Services/Sync/LastFmService.cs must be preserved");
    }

    [Test]
    public void LegacyNamespace_DoesNotContainInlineScrobbleDefinition()
    {
        // The legacy file defined its own Scrobble record — verify it can't be found
        var inlineType = Type.GetType("CSharpScripts.Services.Sync.LastFm.Scrobble, CSharpScripts");
        inlineType.Should().BeNull(because: "Inline Scrobble from legacy file must not exist");
    }
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SyncService\LastFmServiceDeleteTests.cs'
# Expected: True
```

### Step 3: Run — verify current state

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "LastFmServiceDeleteTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

If legacy file was already deleted in T1-07: `3 passed, 0 failed` — skip to Task 2.
If legacy file still exists: Test 1 FAILS — proceed to Step 4.

### Step 4: Delete legacy file (if still exists)

```powershell
$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$backupPath = "C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFm\LastFmService.cs.bak.$timestamp"

if (Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFm\LastFmService.cs) {
    Copy-Item C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFm\LastFmService.cs $backupPath -Force
    Remove-Item C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFm\LastFmService.cs -Force

    # Remove empty LastFm/ directory
    $lastFmDir = 'C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFm'
    if ((Get-ChildItem $lastFmDir -ErrorAction SilentlyContinue | Measure-Object).Count -eq 0) {
        Remove-Item $lastFmDir -Force -Recurse
    }
}
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "LastFmServiceDeleteTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `3 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/SyncService/LastFmServiceDeleteTests.cs
if (Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFm) {
    git -C C:\Users\Lance\Dev\Scripts rm csharp/src/Services/Sync/LastFm/LastFmService.cs
}
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-09): delete legacy LastFmService duplicate"
```

---

## Task 2: Inject IDbContextFactory into Canonical LastFmService

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFmService.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SyncService\SyncServiceTests.cs`

### Step 0: Preflight

```powershell
# Current state: LastFmService takes (string apiKey, string username) in constructor, no database access
# Reason: Need database access via IDbContextFactory for artist/track lookups
# What: Add IDbContextFactory<ScriptsDbContext> as additional constructor parameter
# Expected: Constructor accepts contextFactory, build passes

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFmService.cs -Pattern 'IDbContextFactory'
# Expected: 0 matches

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFmService.cs -Pattern 'class LastFmService'
# Expected: 1 match — shows current constructor signature
```

### Step 1: Write the failing test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SyncService\SyncServiceTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Services.Sync.LastFm;

namespace Scripts.Tests.SyncService;

public sealed class SyncServiceTests
{
    [Test]
    public void LastFmService_Constructor_AcceptsDbContextFactory()
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR")!;
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        using var context = new ScriptsDbContext(options);
        var factory = new TestDbContextFactory(context);

        var service = new LastFmService("test-api-key", "test-user", factory);
        service.Should().NotBeNull();
    }

    [Test]
    public async Task ILike_Lookup_FindsArtist_CaseInsensitive()
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR")!;
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        await using var context = new ScriptsDbContext(options);
        context.Database.EnsureCreated();

        // Insert a test artist
        var artistName = "ILikeTest_" + Guid.NewGuid().ToString("N")[..8];
        context.Artists.Add(new CSharpScripts.Data.Entities.Artist { Name = artistName });
        await context.SaveChangesAsync();

        // Case-insensitive lookup via EF.Functions.ILike
        var found = await context.Artists
            .AsNoTracking()
            .FirstOrDefaultAsync(a => EF.Functions.ILike(a.Name, artistName.ToUpper()));

        found.Should().NotBeNull();
        found!.Name.Should().Be(artistName);

        // Cleanup
        context.Artists.Remove(found);
        await context.SaveChangesAsync();
    }

    [Test]
    public async Task ExecuteDeleteAsync_DeletesScrobbles_ByPlatform()
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR")!;
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        await using var context = new ScriptsDbContext(options);
        context.Database.EnsureCreated();

        var testPlatform = "del_test_" + Guid.NewGuid().ToString("N")[..6];
        var scrobble = new CSharpScripts.Data.Entities.Scrobble
        {
            Id = DateTimeOffset.UtcNow.Ticks,
            TrackId = 1,
            ScrobbledAt = DateTimeOffset.UtcNow,
            Platform = testPlatform
        };
        context.Scrobbles.Add(scrobble);
        await context.SaveChangesAsync();

        var deleted = await context.Scrobbles
            .Where(s => s.Platform == testPlatform)
            .ExecuteDeleteAsync();

        deleted.Should().Be(1);
    }
}

internal sealed class TestDbContextFactory(ScriptsDbContext context) : IDbContextFactory<ScriptsDbContext>
{
    public ScriptsDbContext CreateDbContext() => context;
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\SyncService\SyncServiceTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "SyncServiceTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: FAIL — `LastFmService` constructor does not accept `IDbContextFactory<ScriptsDbContext>`.

### Step 3.5: Assess

Current constructor: `LastFmService(string apiKey, string username)`. Needs additional parameter `IDbContextFactory<ScriptsDbContext> contextFactory`.

### Step 4: Write minimal implementation

Update `C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFmService.cs` — change the constructor:

```csharp
using Hqub.Lastfm;
using Hqub.Lastfm.Entities;
using CSharpScripts.Data;
using Scrobble = CSharpScripts.Models.Scrobble;

namespace CSharpScripts.Services.Sync.LastFm;

internal sealed class LastFmService(string apiKey, string username, IDbContextFactory<ScriptsDbContext> contextFactory)
{
	private const int PerPage = 200;

	private readonly LastfmClient Client = new(apiKey: apiKey);

	// Existing FetchScrobblesSinceAsync method remains unchanged

	// Add a helper method for ILike artist name lookup
	internal async Task<CSharpScripts.Data.Entities.Artist?> FindArtistByNameAsync(string name, CancellationToken ct = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(ct);
		return await context.Artists
			.AsNoTracking()
			.FirstOrDefaultAsync(a => EF.Functions.ILike(a.Name, name), ct);
	}
}
```

The full file keeps all existing lines (1-175) and adds:
- `using CSharpScripts.Data;` import
- `IDbContextFactory<ScriptsDbContext> contextFactory` parameter in primary constructor
- `FindArtistByNameAsync` method at the bottom (uses `EF.Functions.ILike` for case-insensitive lookup)

Verify:

```powershell
Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFmService.cs -Pattern 'IDbContextFactory\|ILike\|FindArtistByNameAsync'
# Expected: 3 matches
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "SyncServiceTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `3 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Services/Sync/LastFmService.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/SyncService/SyncServiceTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-09): inject IDbContextFactory into LastFmService, add ILike lookup"
```

---

## Task 3: Verify PostgresService ExecuteUpdateAsync (No Changes Needed)

**Files:**
- No modifications — verification only

### Step 0: Preflight

```powershell
# Current state: PostgresService.cs already uses ExecuteUpdateAsync correctly (lines 20-28)
# Reason: Verify that existing code complies with EF Core mandates
# What: Run verification that ExecuteUpdateAsync is the mutation strategy
# Expected: No changes, just confirmation

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Services\PostgresService.cs -Pattern 'ExecuteUpdateAsync'
# Expected: 1 match (line 21)

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Services\PostgresService.cs -Pattern 'SaveChangesAsync'
# Expected: 1 match (line 39 — BulkInsertTracksAsync, acceptable for AddRange)
```

### Step 3: Verification (no RED phase needed — code is already compliant)

The existing `PostgresService.cs` uses:
- `ExecuteUpdateAsync` for scrobble upsert (line 21) — correct, per mandate
- `SaveChangesAsync` for bulk insert (line 39) — acceptable for `AddRange` batch

No changes are needed. The postgres service already complies with the "prefer ExecuteUpdateAsync/ExecuteDeleteAsync" mandate.

### Step 5: Confirm existing tests

```powershell
dotnet test --filter "Scripts.Tests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

All pre-existing tests must still pass.

### Step 6: No commit needed — verification only

---

## Final Verification

```powershell
# Run all sync service tests
dotnet test --filter "Scripts.Tests.SyncService" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected:
```
Passed LastFmServiceDeleteTests (3 tests)
Passed SyncServiceTests (3 tests)
6 passed, 0 failed
```

**→ Proceed to `10-ef10-queries.md`**
