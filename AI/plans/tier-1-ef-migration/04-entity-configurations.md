# T1-04: Entity Configurations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create `SourceRecordConfiguration`, add critical indexes (FiberyEntity composite unique, SessionId, TaskName, Platform), fix missing column types (Album.ReleaseDate→date, Video.UploadDate→date, Video.SyncedAt→timestamptz), and add missing indexes across all 9 entity configurations.

**Architecture:** Each configuration change follows the TDD loop: write a test asserting the configuration exists → run RED → add the fluent configuration → run GREEN → commit. Configuration classes are `internal sealed class : IEntityTypeConfiguration<T>` in `csharp/src/Data/Configuration/`. All column types use `HasColumnType()`, all indexes use `HasDatabaseName()`, all lambdas use `static`.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- Phases 00-03 completed — all 9 entities registered, SourceRecord DbSet exists, VideoConfiguration static-fixed
- 8 configuration files exist in `csharp/src/Data/Configuration/` (Artist, Album, Track, Scrobble, Video, ExecutionLog, FailedTask, FiberyEntity)
- `SourceRecordConfiguration.cs` does NOT yet exist (creates in Task 1)
- InMemory EF provider available for tests

---

## File Map

| File | Path | Action |
|------|------|--------|
| `SourceRecordConfiguration.cs` | `csharp/src/Data/Configuration/SourceRecordConfiguration.cs` | CREATE |
| `ArtistConfiguration.cs` | `csharp/src/Data/Configuration/ArtistConfiguration.cs:13` | EDIT: add index |
| `AlbumConfiguration.cs` | `csharp/src/Data/Configuration/AlbumConfiguration.cs:12-13` | EDIT: add column type + index |
| `TrackConfiguration.cs` | `csharp/src/Data/Configuration/TrackConfiguration.cs:12` | EDIT: add column type + composite unique index |
| `ScrobbleConfiguration.cs` | `csharp/src/Data/Configuration/ScrobbleConfiguration.cs:10-23` | EDIT: add column types + indexes |
| `VideoConfiguration.cs` | `csharp/src/Data/Configuration/VideoConfiguration.cs:12-16` | EDIT: add column types + named indexes |
| `ExecutionLogConfiguration.cs` | `csharp/src/Data/Configuration/ExecutionLogConfiguration.cs:11-17` | EDIT: add indexes |
| `FailedTaskConfiguration.cs` | `csharp/src/Data/Configuration/FailedTaskConfiguration.cs:10-16` | EDIT: add indexes |
| `FiberyEntityConfiguration.cs` | `csharp/src/Data/Configuration/FiberyEntityConfiguration.cs:11-13` | EDIT: add default, column types, indexes |
| Test: see each task | `csharp/tests/Scripts.Tests/EntityConfigs/` | CREATE per task |

---

## Task 1: Create SourceRecordConfiguration

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\SourceRecordConfiguration.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\SourceRecordConfigurationTests.cs`

### Step 0: Preflight

```powershell
# Current state: SourceRecord has DbSet but NO configuration file
# Reason: EF Core cannot map SourceRecord to a table without configuration
# What: Create SourceRecordConfiguration with table name, PK, indexes, column types
# Expected: Configuration loaded, entity discoverable with correct table name "source_records"

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\SourceRecordConfiguration.cs
# Expected: False
```

### Step 1: Write the failing test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\SourceRecordConfigurationTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

public sealed class SourceRecordConfigurationTests
{
    [Test]
    public async Task SourceRecord_HasCorrectTableName()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(SourceRecord));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("source_records");
    }

    [Test]
    public async Task SourceRecord_HasCompositeUniqueIndex_OnSourceIdAndEntityType()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(SourceRecord));

        entityType.Should().NotBeNull();
        var indexes = entityType!.GetIndexes().ToList();
        indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "SourceId"));
        indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "EntityType"));
    }

    [Test]
    public async Task SourceRecord_RawData_IsJsonb()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(SourceRecord));
        var rawDataProp = entityType!.FindProperty("RawData");

        rawDataProp.Should().NotBeNull();
        rawDataProp!.GetColumnType().Should().Be("jsonb");
    }
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\SourceRecordConfigurationTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "SourceRecordConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: FAIL with `Expected entityType not to be <null>` or `Expected entityType!.GetTableName() to be "source_records", but found null.`

### Step 3.5: Assess

No configuration exists for SourceRecord. The model cannot map it. Proceed to create.

### Step 4: Write minimal implementation

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\SourceRecordConfiguration.cs`

```csharp
#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class SourceRecordConfiguration : IEntityTypeConfiguration<SourceRecord>
{
	public void Configure(EntityTypeBuilder<SourceRecord> b)
	{
		b.ToTable(name: "source_records");
		b.HasKey(static e => e.Id);
		b.Property(static e => e.Id).HasDefaultValueSql(sql: "gen_random_uuid()");
		b.HasIndex(static e => e.SourceId).HasDatabaseName(name: "idx_source_records_source_id");
		b.HasIndex(static e => e.EntityType).HasDatabaseName(name: "idx_source_records_entity_type");
		b.HasIndex(static e => new { e.SourceId, e.EntityType })
			.IsUnique()
			.HasDatabaseName(name: "idx_source_records_source_entity_type");
		b.Property(static e => e.RawData).HasColumnType(typeName: "jsonb");
	}
}
```

Verify:

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\SourceRecordConfiguration.cs
# Expected: True
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "SourceRecordConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `3 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Configuration/SourceRecordConfiguration.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/EntityConfigs/SourceRecordConfigurationTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-04): create SourceRecordConfiguration"
```

---

## Task 2: Add FiberyEntity Composite Unique Index + Column Types

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\FiberyEntityConfiguration.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\FiberyEntityConfigurationTests.cs`

### Step 0: Preflight

```powershell
# Current state: FiberyEntityConfiguration has PK + jsonb on RawData only
# Reason: Missing critical indexes — FiberyId/EntityType composite unique, EntityType standalone
# What: Add default UUID generation, column types, composite unique index, EntityType index
# Expected: Configuration includes all indexes

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\FiberyEntityConfiguration.cs -Pattern 'FiberyId'
# Expected: 0 matches (no FiberyId property reference in config)
```

### Step 1: Write the failing test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\FiberyEntityConfigurationTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

public sealed class FiberyEntityConfigurationTests
{
    [Test]
    public async Task FiberyEntity_HasCompositeUniqueIndex_OnFiberyIdAndEntityType()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(FiberyEntity));

        entityType.Should().NotBeNull();
        var indexes = entityType!.GetIndexes().ToList();
        indexes.Should().Contain(i =>
            i.Properties.Any(p => p.Name == "FiberyId") &&
            i.Properties.Any(p => p.Name == "EntityType") &&
            i.IsUnique);
    }

    [Test]
    public async Task FiberyEntity_HasEntityTypeIndex()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(FiberyEntity));

        var indexes = entityType!.GetIndexes().ToList();
        indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "EntityType") && !i.IsUnique);
    }

    [Test]
    public async Task FiberyEntity_FiberyId_HasColumnType()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(FiberyEntity));
        var prop = entityType!.FindProperty("FiberyId");

        prop.Should().NotBeNull();
        prop!.GetColumnType().Should().Be("varchar(255)");
    }
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\FiberyEntityConfigurationTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "FiberyEntityConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: FAIL — indexes not found.

### Step 3.5: Assess

FiberyEntityConfiguration only has PK and jsonb. Missing all indexes and column types. Confirmed.

### Step 4: Write minimal implementation

Replace contents of `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\FiberyEntityConfiguration.cs`:

```csharp
#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class FiberyEntityConfiguration : IEntityTypeConfiguration<FiberyEntity>
{
	public void Configure(EntityTypeBuilder<FiberyEntity> b)
	{
		b.ToTable(name: "fibery_entities");
		b.HasKey(static e => e.Id);
		b.Property(static e => e.Id).HasDefaultValueSql(sql: "gen_random_uuid()");
		b.Property(static e => e.FiberyId).HasColumnType(typeName: "varchar(255)");
		b.Property(static e => e.EntityType).HasColumnType(typeName: "varchar(100)");
		b.Property(static e => e.RawData).HasColumnType(typeName: "jsonb");
		b.HasIndex(static e => e.EntityType).HasDatabaseName(name: "idx_fibery_entities_entity_type");
		b.HasIndex(static e => new { e.FiberyId, e.EntityType })
			.IsUnique()
			.HasDatabaseName(name: "idx_fibery_entities_fibery_id_type");
	}
}
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "FiberyEntityConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `3 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Configuration/FiberyEntityConfiguration.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/EntityConfigs/FiberyEntityConfigurationTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-04): add FiberyEntity composite unique index and column types"
```

---

## Task 3: Fix AlbumConfiguration — ReleaseDate Column Type

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\AlbumConfiguration.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\AlbumConfigurationTests.cs`

### Step 0: Preflight

```powershell
# Current state: AlbumConfiguration has no column type for ReleaseDate, no ReleaseDate index
# Reason: DateOnly should map to PostgreSQL "date" type to avoid timestamp default
# What: Add .HasColumnType("date") on ReleaseDate, add ReleaseDate index
# Expected: ReleaseDate column type is "date"

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\AlbumConfiguration.cs -Pattern 'ReleaseDate'
# Expected: 0 matches
```

### Step 1: Write the failing test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\AlbumConfigurationTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

public sealed class AlbumConfigurationTests
{
    [Test]
    public async Task Album_ReleaseDate_ColumnType_IsDate()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Album));
        var prop = entityType!.FindProperty("ReleaseDate");

        prop.Should().NotBeNull();
        prop!.GetColumnType().Should().Be("date");
    }

    [Test]
    public async Task Album_HasReleaseDate_Index()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Album));
        var indexes = entityType!.GetIndexes().ToList();

        indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "ReleaseDate"));
    }
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\AlbumConfigurationTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet test --filter "AlbumConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: FAIL — ReleaseDate column type is not "date" (defaults to timestamp), no ReleaseDate index.

### Step 3.5: Assess

Confirmed missing. Proceed.

### Step 4: Write minimal implementation

Replace `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\AlbumConfiguration.cs`:

```csharp
#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class AlbumConfiguration : IEntityTypeConfiguration<Album>
{
	public void Configure(EntityTypeBuilder<Album> b)
	{
		b.ToTable(name: "albums");
		b.Property(static a => a.Id).UseIdentityAlwaysColumn();
		b.Property(static a => a.ReleaseDate).HasColumnType(typeName: "date");
		b.HasIndex(static a => a.ArtistId);
		b.HasIndex(static a => new { a.ArtistId, a.Title }).IsUnique().HasDatabaseName(name: "idx_albums_title");
		b.HasIndex(static a => a.ReleaseDate).HasDatabaseName(name: "idx_albums_release_date");

		b.HasOne(static a => a.Artist)
			.WithMany(static a => a.Albums)
			.HasForeignKey(static a => a.ArtistId)
			.ExcludeForeignKeyFromMigrations();
	}
}
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "AlbumConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `2 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Configuration/AlbumConfiguration.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/EntityConfigs/AlbumConfigurationTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-04): add Album.ReleaseDate column type and index"
```

---

## Task 4: Fix TrackConfiguration — Duration Column Type + Composite Unique

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\TrackConfiguration.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\TrackConfigurationTests.cs`

### Step 0: Preflight

```powershell
# Current state: TrackConfiguration has no Duration column type, no (ArtistId, Title) unique index
# Reason: Duration should be explicit "integer", (ArtistId, Title) should be unique
# What: Add column type + composite unique index
# Expected: Duration is "integer", composite unique exists

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\TrackConfiguration.cs -Pattern 'Duration'
# Expected: 0 matches
```

### Step 1: Write the failing test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\TrackConfigurationTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

public sealed class TrackConfigurationTests
{
    [Test]
    public async Task Track_Duration_ColumnType_IsInteger()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Track));
        var prop = entityType!.FindProperty("Duration");

        prop.Should().NotBeNull();
        prop!.GetColumnType().Should().Be("integer");
    }

    [Test]
    public async Task Track_HasCompositeUnique_OnArtistIdAndTitle()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Track));
        var indexes = entityType!.GetIndexes().ToList();

        indexes.Should().Contain(i =>
            i.Properties.Any(p => p.Name == "ArtistId") &&
            i.Properties.Any(p => p.Name == "Title") &&
            i.IsUnique);
    }
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\TrackConfigurationTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet test --filter "TrackConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: FAIL — no Duration column type, no composite unique (ArtistId, Title).

### Step 3.5: Assess

Confirmed. Proceed.

### Step 4: Write minimal implementation

Replace `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\TrackConfiguration.cs`:

```csharp
#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
	public void Configure(EntityTypeBuilder<Track> b)
	{
		b.ToTable(name: "tracks");
		b.Property(static t => t.Id).UseIdentityAlwaysColumn();
		b.Property(static t => t.Duration).HasColumnType(typeName: "integer");
		b.HasIndex(static t => t.ArtistId);
		b.HasIndex(static t => t.AlbumId);
		b.HasIndex(static t => t.Title).HasDatabaseName(name: "idx_tracks_title");
		b.HasIndex(static t => new { t.ArtistId, t.Title })
			.IsUnique()
			.HasDatabaseName(name: "idx_tracks_artist_title");

		b.HasOne(static t => t.Artist)
			.WithMany(static a => a.Tracks)
			.HasForeignKey(static t => t.ArtistId)
			.ExcludeForeignKeyFromMigrations();

		b.HasOne(static t => t.Album)
			.WithMany(static a => a.Tracks)
			.HasForeignKey(static t => t.AlbumId)
			.ExcludeForeignKeyFromMigrations();
	}
}
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "TrackConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `2 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Configuration/TrackConfiguration.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/EntityConfigs/TrackConfigurationTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-04): add Track.Duration column type and composite unique index"
```

---

## Task 5: Fix ScrobbleConfiguration — Platform Column Type + Indexes

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\ScrobbleConfiguration.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\ScrobbleConfigurationTests.cs`

### Step 0: Preflight

```powershell
# Current state: No Platform column type, no Platform index, no standalone ScrobbledAt index
# Reason: Platform is conceptually an enum — needs varchar mapping and index for filtering
# What: Add Platform column type "varchar(50)", Platform index, standalone ScrobbledAt index
# Expected: Configuration includes all three

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\ScrobbleConfiguration.cs -Pattern 'Platform'
# Expected: 0 matches
```

### Step 1: Write the failing test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\ScrobbleConfigurationTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

public sealed class ScrobbleConfigurationTests
{
    [Test]
    public async Task Scrobble_Platform_ColumnType_IsVarchar()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Scrobble));
        var prop = entityType!.FindProperty("Platform");

        prop.Should().NotBeNull();
        prop!.GetColumnType().Should().Be("varchar(50)");
    }

    [Test]
    public async Task Scrobble_HasPlatform_Index()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Scrobble));
        var indexes = entityType!.GetIndexes().ToList();

        indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "Platform"));
    }

    [Test]
    public async Task Scrobble_HasStandaloneScrobbledAt_Index()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Scrobble));
        var indexes = entityType!.GetIndexes().ToList();

        indexes.Should().Contain(i =>
            i.Properties.Count == 1 &&
            i.Properties.Any(p => p.Name == "ScrobbledAt") &&
            !i.IsUnique);
    }
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\ScrobbleConfigurationTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet test --filter "ScrobbleConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: FAIL — Platform column type and indexes not found.

### Step 3.5: Assess

Confirmed. Proceed.

### Step 4: Write minimal implementation

Replace `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\ScrobbleConfiguration.cs`:

```csharp
#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scrobble = CSharpScripts.Data.Entities.Scrobble;

namespace CSharpScripts.Data.Configuration;

internal sealed class ScrobbleConfiguration : IEntityTypeConfiguration<Scrobble>
{
	public void Configure(EntityTypeBuilder<Scrobble> b)
	{
		b.ToTable(name: "scrobbles");
		b.Property(static s => s.Id).UseIdentityAlwaysColumn();
		b.Property(static s => s.Platform).HasColumnType(typeName: "varchar(50)");
		b.HasIndex(static s => s.TrackId);
		b.Property(static s => s.ScrobbledAt).HasColumnType(typeName: "timestamptz");
		b.HasIndex(static s => new { s.TrackId, s.ScrobbledAt })
			.IsUnique()
			.HasDatabaseName(name: "idx_scrobbles_timestamp");
		b.HasIndex(static s => s.Platform).HasDatabaseName(name: "idx_scrobbles_platform");
		b.HasIndex(static s => s.ScrobbledAt).HasDatabaseName(name: "idx_scrobbles_scrobbled_at");

		b.HasOne(static s => s.Track)
			.WithMany(static t => t.Scrobbles)
			.HasForeignKey(static s => s.TrackId)
			.ExcludeForeignKeyFromMigrations();
	}
}
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "ScrobbleConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `3 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Configuration/ScrobbleConfiguration.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/EntityConfigs/ScrobbleConfigurationTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-04): add Scrobble.Platform column type, Platform and ScrobbledAt indexes"
```

---

## Task 6: Fix VideoConfiguration — Column Types + Named Indexes

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\VideoConfiguration.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\VideoConfigurationTests.cs`

### Step 0: Preflight

```powershell
# Current state: UploadDate has no column type (should be "date"), SyncedAt has no column type (should be "timestamptz"), Title has no index, indexes are unnamed
# Reason: DateOnly → date, DateTimeOffset → timestamptz, name all indexes
# What: Add column types + Title index + name all indexes
# Expected: All properties have correct types, all indexes have names

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\VideoConfiguration.cs -Pattern 'SyncedAt|UploadDate.*date|Title'
# Expected: UploadDate matched (index only, no column type), SyncedAt 0 matches, Title 0 matches
```

### Step 1: Write the failing test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\VideoConfigurationTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

public sealed class VideoConfigurationTests
{
    [Test]
    public async Task Video_UploadDate_ColumnType_IsDate()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Video));
        var prop = entityType!.FindProperty("UploadDate");

        prop.Should().NotBeNull();
        prop!.GetColumnType().Should().Be("date");
    }

    [Test]
    public async Task Video_SyncedAt_ColumnType_IsTimestamptz()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Video));
        var prop = entityType!.FindProperty("SyncedAt");

        prop.Should().NotBeNull();
        prop!.GetColumnType().Should().Be("timestamptz");
    }

    [Test]
    public async Task Video_HasTitle_Index()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Video));
        var indexes = entityType!.GetIndexes().ToList();

        indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "Title"));
    }
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\VideoConfigurationTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet test --filter "VideoConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: FAIL — column types and Title index not found.

### Step 3.5: Assess

Confirmed. Proceed.

### Step 4: Write minimal implementation

Replace `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\VideoConfiguration.cs`:

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
		b.Property(static v => v.UploadDate).HasColumnType(typeName: "date");
		b.Property(static v => v.SyncedAt).HasColumnType(typeName: "timestamptz");
		b.Property(static v => v.Metadata).HasColumnType(typeName: "jsonb");
		b.HasIndex(static v => v.Url).IsUnique().HasDatabaseName(name: "idx_videos_url");
		b.HasIndex(static v => v.ChannelName).HasDatabaseName(name: "idx_videos_channel");
		b.HasIndex(static v => v.UploadDate).HasDatabaseName(name: "idx_videos_upload_date");
		b.HasIndex(static v => v.Title).HasDatabaseName(name: "idx_videos_title");
	}
}
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "VideoConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `3 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Configuration/VideoConfiguration.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/EntityConfigs/VideoConfigurationTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-04): add Video column types, Title index, named indexes"
```

---

## Task 7: Fix ExecutionLogConfiguration — SessionId + Timestamp Indexes

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\ExecutionLogConfiguration.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\ExecutionLogConfigurationTests.cs`

### Step 0: Preflight

```powershell
# Current state: ExecutionLogConfiguration has no SessionId index, no Timestamp index
# Reason: SessionId is primary query pattern; Timestamp for time-range queries
# What: Add SessionId and Timestamp indexes
# Expected: Two new indexes in configuration

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\ExecutionLogConfiguration.cs -Pattern 'SessionId|Timestamp.*index'
# Expected: 0 matches for index lines (Property lines exist)
```

### Step 1: Write the failing test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\ExecutionLogConfigurationTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

public sealed class ExecutionLogConfigurationTests
{
    [Test]
    public async Task ExecutionLog_HasSessionId_Index()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(ExecutionLog));
        var indexes = entityType!.GetIndexes().ToList();

        indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "SessionId"));
    }

    [Test]
    public async Task ExecutionLog_HasTimestamp_Index()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(ExecutionLog));
        var indexes = entityType!.GetIndexes().ToList();

        indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "Timestamp"));
    }
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\ExecutionLogConfigurationTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet test --filter "ExecutionLogConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: FAIL — SessionId and Timestamp indexes not found.

### Step 3.5: Assess

Confirmed. Proceed.

### Step 4: Write minimal implementation

Replace `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\ExecutionLogConfiguration.cs`:

```csharp
#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class ExecutionLogConfiguration : IEntityTypeConfiguration<ExecutionLog>
{
	public void Configure(EntityTypeBuilder<ExecutionLog> b)
	{
		b.ToTable(name: "execution_logs");
		b.HasKey(static e => e.Id);
		b.Property(static e => e.Id).ValueGeneratedOnAdd();
		b.Property(static e => e.Timestamp)
			.HasColumnType(typeName: "timestamptz")
			.HasDefaultValueSql(sql: "CURRENT_TIMESTAMP");
		b.Property(static e => e.Payload).HasColumnType(typeName: "jsonb");
		b.HasIndex(static e => e.SessionId).HasDatabaseName(name: "idx_execution_logs_session_id");
		b.HasIndex(static e => e.Timestamp).HasDatabaseName(name: "idx_execution_logs_timestamp");
	}
}
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "ExecutionLogConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `2 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Configuration/ExecutionLogConfiguration.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/EntityConfigs/ExecutionLogConfigurationTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-04): add ExecutionLog SessionId and Timestamp indexes"
```

---

## Task 8: Fix FailedTaskConfiguration — TaskName + Timestamp Indexes

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\FailedTaskConfiguration.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\FailedTaskConfigurationTests.cs`

### Step 0: Preflight

```powershell
# Current state: FailedTaskConfiguration has no TaskName index, no Timestamp index
# Reason: Querying by task name and time-range are common patterns
# What: Add TaskName and Timestamp indexes
# Expected: Two new indexes

Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\FailedTaskConfiguration.cs -Pattern 'TaskName|Timestamp.*index'
# Expected: 0 matches for index lines
```

### Step 1: Write the failing test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\FailedTaskConfigurationTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.EntityConfigs;

public sealed class FailedTaskConfigurationTests
{
    [Test]
    public async Task FailedTask_HasTaskName_Index()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(FailedTask));
        var indexes = entityType!.GetIndexes().ToList();

        indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "TaskName"));
    }

    [Test]
    public async Task FailedTask_HasTimestamp_Index()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(FailedTask));
        var indexes = entityType!.GetIndexes().ToList();

        indexes.Should().Contain(i => i.Properties.Any(p => p.Name == "Timestamp"));
    }
}
```

### Step 2: Read-back

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\EntityConfigs\FailedTaskConfigurationTests.cs'
# Expected: True
```

### Step 3: Run — confirm RED

```powershell
dotnet test --filter "FailedTaskConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: FAIL — TaskName and Timestamp indexes not found.

### Step 3.5: Assess

Confirmed. Proceed.

### Step 4: Write minimal implementation

Replace `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\FailedTaskConfiguration.cs`:

```csharp
#pragma warning disable CS0168, IDE0059, IDE0060, CA2000, CS8604
using CSharpScripts.Data.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSharpScripts.Data.Configuration;

internal sealed class FailedTaskConfiguration : IEntityTypeConfiguration<FailedTask>
{
	public void Configure(EntityTypeBuilder<FailedTask> b)
	{
		b.ToTable(name: "failed_tasks");
		b.HasKey(static e => e.Id);
		b.Property(static e => e.Id).ValueGeneratedOnAdd();
		b.Property(static e => e.Timestamp)
			.HasColumnType(typeName: "timestamptz")
			.HasDefaultValueSql(sql: "CURRENT_TIMESTAMP");
		b.HasIndex(static e => e.TaskName).HasDatabaseName(name: "idx_failed_tasks_task_name");
		b.HasIndex(static e => e.Timestamp).HasDatabaseName(name: "idx_failed_tasks_timestamp");
	}
}
```

### Step 5: Run — confirm GREEN

```powershell
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test   --filter "FailedTaskConfigurationTests" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `2 passed, 0 failed`

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Configuration/FailedTaskConfiguration.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/EntityConfigs/FailedTaskConfigurationTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-04): add FailedTask TaskName and Timestamp indexes"
```

---

## Final Verification

```powershell
# Run all entity configuration tests
dotnet test --filter "Scripts.Tests.EntityConfigs" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected:
```
Passed SourceRecordConfigurationTests (3 tests)
Passed FiberyEntityConfigurationTests (3 tests)
Passed AlbumConfigurationTests (2 tests)
Passed TrackConfigurationTests (2 tests)
Passed ScrobbleConfigurationTests (3 tests)
Passed VideoConfigurationTests (3 tests)
Passed ExecutionLogConfigurationTests (2 tests)
Passed FailedTaskConfigurationTests (2 tests)
20 passed, 0 failed
```

**→ Proceed to `05-migrations.md`**
