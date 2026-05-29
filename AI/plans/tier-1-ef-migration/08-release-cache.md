# T1-08: Release Cache Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace the CSV-file-based `ReleaseProgressCache` with an EF Core-backed `ReleaseProgress` entity, creating a per-track incremental append entity stored in PostgreSQL.

**Architecture:** A new `ReleaseProgress` entity maps `TrackInfo` fields to relational columns (with JSONB for `Soloists`). A new `ReleaseProgressConfiguration` defines the table, indexes, and column types. `ReleaseProgressCache` is rewritten as a DI-registered service using `IDbContextFactory<ScriptsDbContext>`, replacing CSV file I/O with `AddAsync`/`ToListAsync`/`ExecuteDeleteAsync`. The old CSV cache files are left on disk (backward compatible). The Infrastructure duplicate is deleted. CsvHelper dependency remains until Tier 2 modular split.

**Key Findings from Research:**
- ReleaseProgressCache currently uses CSV file-based storage at `state/cache/{releaseId}.csv` (CsvHelper)
- Dual caching system exists: ReleaseProgressCache (CSV, per-track incremental) + StateManager.ReleaseCache (JSON, batch)
- TrackInfo model has 13 fields: DiscNumber, TrackNumber, Title, Duration, RecordingYear, Composer, WorkName, Conductor, Orchestra, Soloists (List<string>), Artist, RecordingVenue, RecordingId
- ReleaseProgress entity adds: Id (long auto-increment), ReleaseId (text), CreatedAt (timestamptz with CURRENT_TIMESTAMP default)
- Soloists field maps to JSONB (List<string> serialized)
- Composite unique index on (ReleaseId, DiscNumber, TrackNumber) prevents duplicate track entries
- ReleaseProgressService replaces CSV cache with EF Core operations: AppendTrackAsync (INSERT), LoadAsync (SELECT ORDER BY), DeleteAsync (DELETE WHERE)
- MusicSearchCommand uses ReleaseProgressCache.AppendTrack() and ReleaseProgressCache.Load() — will be updated in T1-09
- CSV files remain on disk for backward compatibility; new code uses database

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- Phases 00-07 completed — repositories exist, StateManager in Data/State
- `ReleaseProgressCache.cs` exists at `csharp/src/Core/Persistence/ReleaseProgressCache.cs` (CSV-based)
- `ReleaseProgressCache.cs` duplicate exists at `csharp/src/Infrastructure/ReleaseProgressCache.cs`
- `MusicSearchCommand.cs` uses `ReleaseProgressCache.AppendTrack()` and `ReleaseProgressCache.Load()`
- `TrackInfo` model exists at `csharp/src/Models/Music.cs:25`
- Migration tooling available — `dotnet ef migrations add` works

---

## File Map

| File | Path | Action |
|------|------|--------|
| `ReleaseProgress.cs` | `csharp/src/Data/Entities/ReleaseProgress.cs` | CREATE |
| `ReleaseProgressConfiguration.cs` | `csharp/src/Data/Configuration/ReleaseProgressConfiguration.cs` | CREATE |
| `ScriptsDbContext.cs` | `csharp/src/Data/ScriptsDbContext.cs` | EDIT: add DbSet\<ReleaseProgress\> |
| `ReleaseProgressService.cs` | `csharp/src/Data/Persistence/ReleaseProgressService.cs` | CREATE (replaces CSV cache) |
| `ReleaseProgressCache.cs` (Core) | `csharp/src/Core/Persistence/ReleaseProgressCache.cs` | DELETE (backup first) |
| `ReleaseProgressCache.cs` (Infra) | `csharp/src/Infrastructure/ReleaseProgressCache.cs` | DELETE (backup first) |
| Migration files | `csharp/src/Data/Migrations/*_AddReleaseProgress.cs` | CREATE (auto-generated) |
| Test files | `csharp/tests/Scripts.Tests/ReleaseProgress/` | CREATE |

---

## Task 1: Create ReleaseProgress Entity

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\ReleaseProgress.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\ReleaseProgress\ReleaseProgressEntityTests.cs`

### Step 0: Preflight

```powershell
# Current state: No ReleaseProgress entity exists. TrackInfo is a model (not an entity)
# Reason: Need a per-track incremental progress entity to replace CSV file-based cache
# What: Create ReleaseProgress entity with all TrackInfo fields mapped as columns
# Expected: 14-column entity with auto-increment PK, composite unique index on (ReleaseId, DiscNumber, TrackNumber)

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\ReleaseProgress.cs
# Expected: False
```

### Step 1: Write the failing test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\ReleaseProgress\ReleaseProgressEntityTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.ReleaseProgress;

public sealed class ReleaseProgressEntityTests
{
    [Test]
    public void ReleaseProgress_HasRequired_Properties()
    {
        var props = typeof(CSharpScripts.Data.Entities.ReleaseProgress).GetProperties().Select(p => p.Name).ToList();

        props.Should().Contain("Id");
        props.Should().Contain("ReleaseId");
        props.Should().Contain("DiscNumber");
        props.Should().Contain("TrackNumber");
        props.Should().Contain("Title");
        props.Should().Contain("Duration");
        props.Should().Contain("RecordingYear");
        props.Should().Contain("Composer");
        props.Should().Contain("WorkName");
        props.Should().Contain("Conductor");
        props.Should().Contain("Orchestra");
        props.Should().Contain("Soloists");
        props.Should().Contain("Artist");
        props.Should().Contain("RecordingVenue");
        props.Should().Contain("RecordingId");
        props.Should().Contain("CreatedAt");
    }

    [Test]
    public void ReleaseProgress_Id_IsLong()
    {
        typeof(CSharpScripts.Data.Entities.ReleaseProgress)
            .GetProperty("Id")!.PropertyType.Should().Be(typeof(long));
    }

    [Test]
    public void ReleaseProgress_CanBeInstantiated_WithDefaults()
    {
        var rp = new CSharpScripts.Data.Entities.ReleaseProgress
        {
            ReleaseId = "abc123",
            DiscNumber = 1,
            TrackNumber = 1,
            Title = "Test Track"
        };

        rp.ReleaseId.Should().Be("abc123");
        rp.DiscNumber.Should().Be(1);
        rp.Soloists.Should().BeNull();
        rp.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\ReleaseProgress\ReleaseProgressEntityTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "ReleaseProgressEntityTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: FAIL — `Error CS0246: The type or namespace name 'ReleaseProgress' could not be found`.

### Step 3.5: Assess

Entity does not exist. Proceed.

### Step 4: Write minimal implementation

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\ReleaseProgress.cs`

```csharp
using System.Text.Json;

namespace CSharpScripts.Data.Entities;

internal sealed record ReleaseProgress
{
	public long Id { get; set; }
	public string ReleaseId { get; set; } = null!;
	public int DiscNumber { get; set; }
	public int TrackNumber { get; set; }
	public string Title { get; set; } = null!;
	public string? Duration { get; set; }
	public int? RecordingYear { get; set; }
	public string? Composer { get; set; }
	public string? WorkName { get; set; }
	public string? Conductor { get; set; }
	public string? Orchestra { get; set; }
	public JsonDocument? Soloists { get; set; }
	public string? Artist { get; set; }
	public string? RecordingVenue { get; set; }
	public string? RecordingId { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

Verify:

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\ReleaseProgress.cs
# Expected: True
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "ReleaseProgressEntityTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `3 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Entities/ReleaseProgress.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/ReleaseProgress/ReleaseProgressEntityTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-08): add ReleaseProgress entity"
```

---

## Task 2: Create ReleaseProgressConfiguration and Add DbSet

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\ReleaseProgressConfiguration.cs`
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\ReleaseProgress\ReleaseProgressConfigurationTests.cs`

### Step 0: Preflight

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\ReleaseProgressConfiguration.cs
# Expected: False

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs -Pattern 'ReleaseProgress'
# Expected: 0 matches
```

### Step 1: Write the failing test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\ReleaseProgress\ReleaseProgressConfigurationTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.ReleaseProgress;

public sealed class ReleaseProgressConfigurationTests
{
    [Test]
    public async Task ReleaseProgress_HasCorrectTableName()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(CSharpScripts.Data.Entities.ReleaseProgress));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("release_progress");
    }

    [Test]
    public async Task ReleaseProgress_HasCompositeUniqueIndex()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(CSharpScripts.Data.Entities.ReleaseProgress));

        var indexes = entityType!.GetIndexes().ToList();
        indexes.Should().Contain(i =>
            i.Properties.Any(p => p.Name == "ReleaseId") &&
            i.Properties.Any(p => p.Name == "DiscNumber") &&
            i.Properties.Any(p => p.Name == "TrackNumber") &&
            i.IsUnique);
    }

    [Test]
    public async Task ReleaseProgress_Soloists_IsJsonb()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(CSharpScripts.Data.Entities.ReleaseProgress));
        var prop = entityType!.FindProperty("Soloists");

        prop.Should().NotBeNull();
        prop!.GetColumnType().Should().Be("jsonb");
    }
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\ReleaseProgress\ReleaseProgressConfigurationTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet test --filter "ReleaseProgressConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: FAIL — entity type not found (no DbSet, no configuration).

### Step 3.5: Assess

Confirmed. Proceed.

### Step 4: Write minimal implementation

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\ReleaseProgressConfiguration.cs`

```csharp
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class ReleaseProgressConfiguration : IEntityTypeConfiguration<ReleaseProgress>
{
	public void Configure(EntityTypeBuilder<ReleaseProgress> b)
	{
		b.ToTable(name: "release_progress");
		b.HasKey(static e => e.Id);
		b.Property(static e => e.Id).ValueGeneratedOnAdd();
		b.HasIndex(static e => new { e.ReleaseId, e.DiscNumber, e.TrackNumber })
			.IsUnique()
			.HasDatabaseName(name: "idx_release_progress_track");
		b.Property(static e => e.ReleaseId).HasColumnType(typeName: "text");
		b.Property(static e => e.Soloists).HasColumnType(typeName: "jsonb");
		b.Property(static e => e.CreatedAt)
			.HasColumnType(typeName: "timestamptz")
			.HasDefaultValueSql(sql: "CURRENT_TIMESTAMP");
	}
}
```

Add `DbSet<ReleaseProgress>` to `ScriptsDbContext.cs` — insert after the `SourceRecords` line:

```csharp
public DbSet<ReleaseProgress> ReleaseProgress => Set<ReleaseProgress>();
```

Full ScriptsDbContext after edit:

```csharp
#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using EntityScrobble = CSharpScripts.Data.Entities.Scrobble;

namespace CSharpScripts.Data;

internal sealed class ScriptsDbContext : DbContext
{
	public ScriptsDbContext(DbContextOptions<ScriptsDbContext> options)
		: base(options: options) => ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

	public DbSet<Artist> Artists => Set<Artist>();
	public DbSet<Album> Albums => Set<Album>();
	public DbSet<Track> Tracks => Set<Track>();
	public DbSet<EntityScrobble> Scrobbles => Set<EntityScrobble>();
	public DbSet<Video> Videos => Set<Video>();

	public DbSet<ExecutionLog> ExecutionLogs => Set<ExecutionLog>();
	public DbSet<FiberyEntity> FiberyEntities => Set<FiberyEntity>();
	public DbSet<FailedTask> FailedTasks => Set<FailedTask>();
	public DbSet<SourceRecord> SourceRecords => Set<SourceRecord>();
	public DbSet<ReleaseProgress> ReleaseProgress => Set<ReleaseProgress>();

	protected override void OnModelCreating(ModelBuilder mb)
	{
		mb.HasPostgresExtension("unaccent");
		mb.HasPostgresExtension("pg_trgm");
		mb.ApplyConfigurationsFromAssembly(assembly: typeof(ScriptsDbContext).Assembly);
	}
}
```

Verify:

```powershell
Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs -Pattern 'ReleaseProgress'
# Expected: 1 match

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\ReleaseProgressConfiguration.cs
# Expected: True
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "ReleaseProgressConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `3 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Configuration/ReleaseProgressConfiguration.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/ScriptsDbContext.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/ReleaseProgress/ReleaseProgressConfigurationTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-08): add ReleaseProgressConfiguration and DbSet"
```

---

## Task 3: Generate Migration for ReleaseProgress Table

**Files:**
- Auto-create: `csharp/src/Data/Migrations/*_AddReleaseProgress.cs`

### Step 0: Preflight

```powershell
# Current state: ReleaseProgress entity and config exist, not yet migrated
# Reason: Need a migration to create the release_progress table
# What: Run dotnet ef migrations add AddReleaseProgress
# Expected: Migration file created

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Migrations
# Expected: True (from InitialCreate)
```

### Step 3: Run migration generation

```powershell
dotnet ef migrations add AddReleaseProgress `
    --project C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj `
    --output-dir src\Data\Migrations `
    2>&1
```

Expected:
```
Build started...
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
```

Apply migration:

```powershell
dotnet ef database update `
    --project C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj `
    2>&1
```

Expected:
```
Applying migration '..._AddReleaseProgress'.
Done.
```

Verify table exists:

```powershell
docker exec postgres psql -U postgres -d scripts -c "\d release_progress" 2>&1
```

Expected: Table schema with columns Id, ReleaseId, DiscNumber, TrackNumber, Title, Duration, etc.

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Migrations/
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-08): add ReleaseProgress migration"
```

---

## Task 4: Create ReleaseProgressService (Replace CSV Cache)

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Persistence\ReleaseProgressService.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\ReleaseProgress\ReleaseProgressServiceTests.cs`

### Step 0: Preflight

```powershell
# Current state: ReleaseProgressCache exists as static CSV file-based class
# Reason: Replace CSV file I/O with Entity Framework Core database operations
# What: Create ReleaseProgressService using IDbContextFactory
# Expected: AppendTrack → INSERT, Load → SELECT ORDER BY, Delete → DELETE WHERE

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Persistence
# Expected: False

New-Item -ItemType Directory -Force -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Persistence
```

### Step 1: Write the failing test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\ReleaseProgress\ReleaseProgressServiceTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Persistence;
using CSharpScripts.Models;
using System.Text.Json;

namespace Scripts.Tests.ReleaseProgress;

public sealed class ReleaseProgressServiceTests : IDisposable
{
    private readonly ScriptsDbContext _context;
    private readonly ReleaseProgressService _service;
    private readonly string _releaseId = "test-release-" + Guid.NewGuid().ToString("N")[..8];

    public ReleaseProgressServiceTests()
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR")!;
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        _context = new ScriptsDbContext(options);
        _context.Database.EnsureCreated();

        var factory = new TestDbContextFactory(_context);
        _service = new ReleaseProgressService(factory);
    }

    public void Dispose()
    {
        _context.ReleaseProgress.Where(r => r.ReleaseId == _releaseId).ExecuteDelete();
        _context.Dispose();
    }

    [Test]
    public async Task AppendTrackAsync_InsertsTrack()
    {
        var track = new TrackInfo(1, 1, "Test Track", null, null, null, null, null, null, [], null, null, null);

        await _service.AppendTrackAsync(_releaseId, track);

        var loaded = await _service.LoadAsync(_releaseId);
        loaded.Should().HaveCount(1);
        loaded[0].Title.Should().Be("Test Track");
        loaded[0].DiscNumber.Should().Be(1);
        loaded[0].TrackNumber.Should().Be(1);
    }

    [Test]
    public async Task LoadAsync_ReturnsOrderedTracks()
    {
        var track1 = new TrackInfo(1, 2, "Track 2", null, null, null, null, null, null, [], null, null, null);
        var track2 = new TrackInfo(1, 1, "Track 1", null, null, null, null, null, null, [], null, null, null);

        await _service.AppendTrackAsync(_releaseId, track1);
        await _service.AppendTrackAsync(_releaseId, track2);

        var loaded = await _service.LoadAsync(_releaseId);
        loaded.Should().HaveCount(2);
        loaded[0].TrackNumber.Should().Be(1);
        loaded[1].TrackNumber.Should().Be(2);
    }

    [Test]
    public async Task DeleteAsync_RemovesAllTracks()
    {
        var track = new TrackInfo(1, 1, "Delete Me", null, null, null, null, null, null, [], null, null, null);
        await _service.AppendTrackAsync(_releaseId, track);

        await _service.DeleteAsync(_releaseId);

        var loaded = await _service.LoadAsync(_releaseId);
        loaded.Should().BeEmpty();
    }
}

internal sealed class TestDbContextFactory(ScriptsDbContext context) : IDbContextFactory<ScriptsDbContext>
{
    public ScriptsDbContext CreateDbContext() => context;
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\ReleaseProgress\ReleaseProgressServiceTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet test --filter "ReleaseProgressServiceTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: FAIL — `ReleaseProgressService` not found.

### Step 3.5: Assess

Confirmed. Proceed.

### Step 4: Write minimal implementation

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Persistence\ReleaseProgressService.cs`

```csharp
using CSharpScripts.Data.Entities;
using CSharpScripts.Models;
using System.Text.Json;

namespace CSharpScripts.Data.Persistence;

internal sealed class ReleaseProgressService(IDbContextFactory<ScriptsDbContext> contextFactory)
{
    public async Task AppendTrackAsync(string releaseId, TrackInfo track, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var entity = new ReleaseProgress
        {
            ReleaseId = releaseId,
            DiscNumber = track.DiscNumber,
            TrackNumber = track.TrackNumber,
            Title = track.Title,
            Duration = track.Duration?.ToString(),
            RecordingYear = track.RecordingYear,
            Composer = track.Composer,
            WorkName = track.WorkName,
            Conductor = track.Conductor,
            Orchestra = track.Orchestra,
            Soloists = track.Soloists.Count > 0
                ? JsonSerializer.SerializeToDocument(track.Soloists)
                : null,
            Artist = track.Artist,
            RecordingVenue = track.RecordingVenue,
            RecordingId = track.RecordingId
        };

        context.ReleaseProgress.Add(entity);
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<TrackInfo>> LoadAsync(string releaseId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var rows = await context.ReleaseProgress
            .AsNoTracking()
            .Where(r => r.ReleaseId == releaseId)
            .OrderBy(r => r.DiscNumber)
            .ThenBy(r => r.TrackNumber)
            .ToListAsync(ct);

        return rows.Select(MapToTrackInfo).ToList();
    }

    public async Task<int> DeleteAsync(string releaseId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);
        return await context.ReleaseProgress
            .Where(r => r.ReleaseId == releaseId)
            .ExecuteDeleteAsync(cancellationToken: ct);
    }

    private static TrackInfo MapToTrackInfo(ReleaseProgress r)
    {
        List<string> soloists = [];
        if (r.Soloists is not null)
        {
            try
            {
                soloists = JsonSerializer.Deserialize<List<string>>(r.Soloists.RootElement.GetRawText()) ?? [];
            }
            catch { }
        }

        TimeSpan? duration = null;
        if (r.Duration is not null && TimeSpan.TryParse(r.Duration, out var ts))
            duration = ts;

        return new TrackInfo(
            r.DiscNumber,
            r.TrackNumber,
            r.Title,
            duration,
            r.RecordingYear,
            r.Composer,
            r.WorkName,
            r.Conductor,
            r.Orchestra,
            soloists,
            r.Artist,
            r.RecordingVenue,
            r.RecordingId
        );
    }
}
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "ReleaseProgressServiceTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `3 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Persistence/ReleaseProgressService.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/ReleaseProgress/ReleaseProgressServiceTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-08): create ReleaseProgressService with EF Core backend"
```

---

## Task 5: Delete Old CSV-Based ReleaseProgressCache Files

**Files:**
- Delete: `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Persistence\ReleaseProgressCache.cs`
- Delete: `C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\ReleaseProgressCache.cs`

### Step 0: Preflight

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Core\Persistence\ReleaseProgressCache.cs
# Expected: True

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\ReleaseProgressCache.cs
# Expected: True
```

### Step 4: Delete with backup

```powershell
$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'

# Backup and delete Core version
Copy-Item C:\Users\Lance\Dev\Scripts\csharp\src\Core\Persistence\ReleaseProgressCache.cs "C:\Users\Lance\Dev\Scripts\csharp\src\Core\Persistence\ReleaseProgressCache.cs.bak.$timestamp" -Force
Remove-Item C:\Users\Lance\Dev\Scripts\csharp\src\Core\Persistence\ReleaseProgressCache.cs -Force

# Backup and delete Infrastructure version
Copy-Item C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\ReleaseProgressCache.cs "C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\ReleaseProgressCache.cs.bak.$timestamp" -Force
Remove-Item C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\ReleaseProgressCache.cs -Force
```

Verify:

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Core\Persistence\ReleaseProgressCache.cs
# Expected: False

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\ReleaseProgressCache.cs
# Expected: False
```

### Step 5: Run — confirm build clean

```powershell
dotnet build C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: Build succeeds (no references to deleted files from compiled path — `CLI/` and `Orchestrators/` are excluded from compilation).

If build fails because `MusicSearchCommand.cs` references `ReleaseProgressCache`:
- This file is in `src/CLI/` which is `<Compile Remove="src\CLI\**" />` in the .csproj — it should not cause build errors.
- If it does, the reference existed before the removal. MusicSearchCommand will be updated in a later tier (T2-07 Scripts.CLI).

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts rm csharp/src/Core/Persistence/ReleaseProgressCache.cs
git -C C:\Users\Lance\Dev\Scripts rm csharp/src/Infrastructure/ReleaseProgressCache.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-08): delete CSV-based ReleaseProgressCache duplicates"
```

---

## Final Verification

```powershell
# Run all ReleaseProgress tests
dotnet test --filter "Scripts.Tests.ReleaseProgress" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected:
```
Passed ReleaseProgressEntityTests (3 tests)
Passed ReleaseProgressConfigurationTests (3 tests)
Passed ReleaseProgressServiceTests (3 tests)
9 passed, 0 failed
```

**→ Proceed to `09-sync-service-updates.md`**
