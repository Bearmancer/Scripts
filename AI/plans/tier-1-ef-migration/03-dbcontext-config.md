# T1-03: DbContext Configuration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add missing `DbSet<SourceRecord>` to ScriptsDbContext, fix non-static lambda in VideoConfiguration, and verify NoTracking + ApplyConfigurationsFromAssembly behavior.

**Architecture:** Reflection and InMemory-based tests verify DbContext configuration. The SourceRecord entity exists but has no DbSet and no configuration — both must be added. VideoConfiguration has one style fix (non-static lambda → static). NoTracking is already default (constructor-set), and configurations are already assembly-loaded.

**Key Findings from Research:**
- ScriptsDbContext currently has 8 DbSet properties; SourceRecord is missing (entity exists but unmapped)
- NoTracking is already set in constructor: `ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking`
- `ApplyConfigurationsFromAssembly()` is already called in OnModelCreating — all 8 configuration files are auto-discovered
- SourceRecordConfiguration does NOT exist — must be created with table name "source_records", Guid PK with `gen_random_uuid()` default, composite unique index on (SourceId, EntityType), JSONB RawData
- VideoConfiguration uses non-static lambdas (style inconsistency) — all other 7 configs use static lambdas
- All 8 existing configurations implement `IEntityTypeConfiguration<T>` correctly
- DbContext registration in DbContextRegistration.cs and ScriptsDbContextFactory.cs both lack `EnableRetryOnFailure` — will be added in T1-14 (resilience)

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- Phases 00-02 completed — all 9 entities exist, Mbid properties removed
- `ScriptsDbContext.cs` exists at `csharp/src/Data/ScriptsDbContext.cs` with 8 DbSet properties and `ApplyConfigurationsFromAssembly`
- `SourceRecord.cs` exists at `csharp/src/Data/Entities/SourceRecord.cs` (Guid Id, string SourceId, string EntityType, JsonDocument? RawData)
- `VideoConfiguration.cs` exists at `csharp/src/Data/Configuration/VideoConfiguration.cs` (with non-static lambdas)
- InMemory EF provider available: `Microsoft.EntityFrameworkCore.InMemory` package in project

---

## File Map

| File | Path | Action |
|------|------|--------|
| `ScriptsDbContext.cs` | `csharp/src/Data/ScriptsDbContext.cs:20` | EDIT: add DbSet\<SourceRecord\> after FailedTasks |
| `VideoConfiguration.cs` | `csharp/src/Data/Configuration/VideoConfiguration.cs:12-16` | EDIT: change `v =>` to `static v =>` |
| Test: DbContextNoTrackingTests.cs | `csharp/tests/Scripts.Tests/DbContext/DbContextNoTrackingTests.cs` | CREATE |
| Test: DbContextConfigLoadingTests.cs | `csharp/tests/Scripts.Tests/DbContext/DbContextConfigLoadingTests.cs` | CREATE |
| Test: DbContextSourceRecordDbSetTests.cs | `csharp/tests/Scripts.Tests/DbContext/DbContextSourceRecordDbSetTests.cs` | CREATE |

---

## Task 1: Verify NoTracking Is Default

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\DbContext\DbContextNoTrackingTests.cs`

### Step 0: Preflight

```powershell
# Current state: ScriptsDbContext sets NoTracking in constructor
# Reason: Confirm tracking behavior via test
# What: Test that context defaults to NoTracking
# Expected: QueryTrackingBehavior is NoTracking

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs -Pattern 'NoTracking'
# Expected: 1 match (line 10)
```

### Step 1: Write the failing test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\DbContext\DbContextNoTrackingTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;

namespace Scripts.Tests.DbContext;

public sealed class DbContextNoTrackingTests
{
    [Test]
    public void DbContext_DefaultsTo_NoTracking()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase("NoTrackingTest_" + Guid.NewGuid())
            .Options;

        using var context = new ScriptsDbContext(options);
        context.ChangeTracker.QueryTrackingBehavior.Should().Be(QueryTrackingBehavior.NoTracking);
    }

    [Test]
    public void DbContext_CanExplicitly_TrackEntity()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase("TrackExplicitlyTest_" + Guid.NewGuid())
            .Options;

        using var context = new ScriptsDbContext(options);
        var entry = context.Attach(new CSharpScripts.Data.Entities.ExecutionLog
        {
            Id = 0,
            SessionId = "test-session",
            Timestamp = DateTimeOffset.UtcNow
        });

        entry.State.Should().Be(EntityState.Unchanged);
    }
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\DbContext\DbContextNoTrackingTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet test --filter "DbContextNoTrackingTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: PASS (NoTracking is already set). If the test project cannot reference InMemory, the test will fail with a DI/Microsoft.EntityFrameworkCore.InMemory dependency error — that's the RED signal. If InMemory is already a dependency, this should pass immediately; skip to Step 6 commit.

### Step 3.5: Assess

If test cannot build because `UseInMemoryDatabase` is not available, add `Microsoft.EntityFrameworkCore.InMemory` to the test project:

```powershell
dotnet add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Scripts.Tests.csproj package Microsoft.EntityFrameworkCore.InMemory
```

Then retry Step 3. Otherwise, tests already GREEN (functionality pre-exists).

### Step 4: No implementation needed

NoTracking is already set in `ScriptsDbContext.cs:10`:
```csharp
: base(options: options) => ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "DbContextNoTrackingTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `2 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/DbContext/DbContextNoTrackingTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-03): add NoTracking behavior tests"
```

---

## Task 2: Verify ApplyConfigurationsFromAssembly Loads All Configs

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\DbContext\DbContextConfigLoadingTests.cs`

### Step 0: Preflight

```powershell
# Current state: OnModelCreating calls ApplyConfigurationsFromAssembly
# Reason: Verify all IEntityTypeConfiguration implementations are auto-discovered
# What: Test that the model has configurations for each known entity
# Expected: Model finds entity types for Artist, Album, Track, Scrobble, Video, ExecutionLog, FailedTask, FiberyEntity

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs -Pattern 'ApplyConfigurationsFromAssembly'
# Expected: 1 match
```

### Step 1: Write the test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\DbContext\DbContextConfigLoadingTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.DbContext;

public sealed class DbContextConfigLoadingTests
{
    [Test]
    public async Task OnModelCreating_Discovers_AllConfigEntities()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase("ConfigDiscoveryTest_" + Guid.NewGuid())
            .Options;

        await using var context = new ScriptsDbContext(options);
        var model = context.Model;

        var entityTypes = model.GetEntityTypes().Select(e => e.ClrType).ToList();

        entityTypes.Should().Contain(typeof(Artist));
        entityTypes.Should().Contain(typeof(Album));
        entityTypes.Should().Contain(typeof(Track));
        entityTypes.Should().Contain(typeof(Scrobble));
        entityTypes.Should().Contain(typeof(Video));
        entityTypes.Should().Contain(typeof(ExecutionLog));
        entityTypes.Should().Contain(typeof(FailedTask));
        entityTypes.Should().Contain(typeof(FiberyEntity));
    }

    [Test]
    public async Task ArtistsTable_HasCorrectName()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase("TableNameTest_" + Guid.NewGuid())
            .Options;

        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Artist));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("artists");
    }

    [Test]
    public async Task ScrobblesTable_HasCorrectTimestampColumnType()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase("ColumnTypeTest_" + Guid.NewGuid())
            .Options;

        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Scrobble));
        var scrobbledAt = entityType!.FindProperty("ScrobbledAt");

        scrobbledAt.Should().NotBeNull();
        scrobbledAt!.GetColumnType().Should().Be("timestamptz");
    }
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\DbContext\DbContextConfigLoadingTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED or GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "DbContextConfigLoadingTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: All 3 tests PASS (configurations already exist and are loaded by `ApplyConfigurationsFromAssembly`). If SourceRecord is not yet in the model, test 1 will FAIL with "Expected entityTypes to contain SourceRecord" — this is expected until SourceRecord DbSet is added in Task 3.

### Step 3.5: Assess

If all tests pass, configurations are already being loaded correctly. If Test 1 fails on SourceRecord, that confirms the gap that Task 3 addresses.

### Step 4: No implementation needed

The `ApplyConfigurationsFromAssembly` call on `ScriptsDbContext.cs:23` already loads all 8 configuration classes. No code changes needed for this task.

### Step 5: Run — confirm GREEN

```powershell
dotnet test --filter "DbContextConfigLoadingTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `3 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/DbContext/DbContextConfigLoadingTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-03): add config loading verification tests"
```

---

## Task 3: Add DbSet\<SourceRecord\> to ScriptsDbContext

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\DbContext\DbContextSourceRecordDbSetTests.cs`
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs`

### Step 0: Preflight

```powershell
# Current state: SourceRecord entity exists at src/Data/Entities/SourceRecord.cs,
# but NO DbSet in ScriptsDbContext, NO configuration file
# Reason: Entity is invisible to EF Core — cannot be queried, migrated, or seeded
# What: Add DbSet<SourceRecord> to ScriptsDbContext
# Expected: DbSet property exists and entity is discoverable by model

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs -Pattern 'SourceRecord'
# Expected: 0 matches (no DbSet exists)

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\SourceRecord.cs
# Expected: True
```

### Step 1: Write the failing test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\DbContext\DbContextSourceRecordDbSetTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.DbContext;

public sealed class DbContextSourceRecordDbSetTests
{
    [Test]
    public void DbContext_HasSourceRecords_DbSet()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase("SourceRecordDbSetTest_" + Guid.NewGuid())
            .Options;

        using var context = new ScriptsDbContext(options);
        context.SourceRecords.Should().NotBeNull();
    }

    [Test]
    public async Task SourceRecord_IsInModel_AfterDbSetAdded()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase("SourceRecordModelTest_" + Guid.NewGuid())
            .Options;

        await using var context = new ScriptsDbContext(options);
        var model = context.Model;

        var sourceRecordType = model.FindEntityType(typeof(SourceRecord));
        sourceRecordType.Should().NotBeNull(because: "SourceRecord entity must be discoverable by the model");
    }
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\DbContext\DbContextSourceRecordDbSetTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "DbContextSourceRecordDbSetTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: FAIL with
```
Error CS1061: 'ScriptsDbContext' does not contain a definition for 'SourceRecords'
```
or
```
Expected sourceRecordType not to be <null> because SourceRecord entity must be discoverable by the model.
```

### Step 3.5: Assess

SourceRecord entity exists but is not registered in DbContext. Confirmed gap. Proceed to add DbSet.

### Step 4: Write minimal implementation

Add `DbSet<SourceRecord>` to `C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs` after the `FailedTasks` line (line 20):

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

	protected override void OnModelCreating(ModelBuilder mb) =>
		mb.ApplyConfigurationsFromAssembly(assembly: typeof(ScriptsDbContext).Assembly);
}
```

Verify:

```powershell
Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs -Pattern 'SourceRecord'
# Expected: 1 match (the new DbSet line)
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "DbContextSourceRecordDbSetTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `2 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/ScriptsDbContext.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/DbContext/DbContextSourceRecordDbSetTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-03): add SourceRecord DbSet to ScriptsDbContext"
```

---

## Task 4: Fix Non-Static Lambdas in VideoConfiguration

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\VideoConfiguration.cs`

### Step 0: Preflight

```powershell
# Current state: VideoConfiguration uses instance lambdas (v => v.Id) while all other 7 configs use static (static v => v.Id)
# Reason: Code-style inconsistency — all configs should use static lambdas to avoid closure allocation
# What: Change 5 lambdas from v => to static v =>
# Expected: Build clean, no behavioral change

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\VideoConfiguration.cs -Pattern '=>'
# Expected: 5 matches (Id, Url, ChannelName, UploadDate, Metadata) — none prefixed with static
```

### Step 1: Write the test

A style validation cannot be directly tested via unit tests (both static and non-static compile identically). Instead, we verify the configuration still works after applying the `static` fix.

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\DbContext\VideoConfigurationStyleTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.DbContext;

public sealed class VideoConfigurationStyleTests
{
    [Test]
    public async Task VideoConfiguration_StillHas_UrlUniqueIndex_AfterStaticFix()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase("VideoStyleTest_" + Guid.NewGuid())
            .Options;

        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Video));

        entityType.Should().NotBeNull();
        var urlProperty = entityType!.FindProperty("Url");
        urlProperty.Should().NotBeNull();
        urlProperty!.IsNullable.Should().BeFalse();
    }

    [Test]
    public async Task VideoConfiguration_StillHas_MetadataJsonbType_AfterStaticFix()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase("VideoMetaTest_" + Guid.NewGuid())
            .Options;

        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Video));
        var metadataProp = entityType!.FindProperty("Metadata");

        metadataProp.Should().NotBeNull();
        metadataProp!.GetColumnType().Should().Be("jsonb");
    }
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\DbContext\VideoConfigurationStyleTests.cs'
# Expected: True
```

### Step 3: Run — confirm tests pass (pre-fix baseline)

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "VideoConfigurationStyleTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `2 passed, 0 failed` (tests pass before fix; they validate behavior, not style)

### Step 3.5: Assess

Behavioral tests pass. Proceed with style fix — the tests serve as regression guard.

### Step 4: Write minimal implementation

Change all 5 lambdas in `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\VideoConfiguration.cs` from instance to static:

```csharp
#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class VideoConfiguration : IEntityTypeConfiguration<Video>
{
	public void Configure(EntityTypeBuilder<Video> b)
	{
		b.ToTable(name: "videos");
		b.Property(static v => v.Id).UseIdentityAlwaysColumn();
		b.HasIndex(static v => v.Url).IsUnique();
		b.HasIndex(static v => v.ChannelName);
		b.HasIndex(static v => v.UploadDate);
		b.Property(static v => v.Metadata).HasColumnType(typeName: "jsonb");
	}
}
```

Verify:

```powershell
Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\VideoConfiguration.cs -Pattern 'static'
# Expected: 5 matches (all lambdas now use static keyword)
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "VideoConfigurationStyleTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `2 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Configuration/VideoConfiguration.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/DbContext/VideoConfigurationStyleTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-03): normalize VideoConfiguration to static lambdas"
```

---

## Final Verification

```powershell
# Run all DbContext tests
dotnet test --filter "Scripts.Tests.DbContext" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected:
```
Passed DbContextNoTrackingTests (2 tests)
Passed DbContextConfigLoadingTests (3 tests)
Passed DbContextSourceRecordDbSetTests (2 tests)
Passed VideoConfigurationStyleTests (2 tests)
9 passed, 0 failed
```

**→ Proceed to `04-entity-configurations.md`**
