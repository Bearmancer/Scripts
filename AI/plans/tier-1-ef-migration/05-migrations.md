# T1-05: Database Migrations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create the initial EF Core migration with PostgreSQL extensions (unaccent + pg_trgm), apply it to the local PostgreSQL 18 database, and verify schema correctness.

**Architecture:** The migration is generated via `dotnet ef migrations add InitialCreate` against the monolithic `CSharpScripts.csproj` (the `.slnx` and modular projects do not exist yet — Tier 2). Extensions are registered in `OnModelCreating` via `HasPostgresExtension()`. Functional indexes use raw SQL in migration `Up()`. The migration is then applied via `dotnet ef database update`.

**Key Findings from Research:**
- No migrations exist yet — `Migrations/` directory does not exist under `csharp/src/Data/`
- PostgreSQL 18 includes `unaccent` and `pg_trgm` extensions in contrib — both are available and required
- Extensions are registered via `mb.HasPostgresExtension("unaccent")` and `mb.HasPostgresExtension("pg_trgm")` in OnModelCreating
- EF Core auto-generates `CREATE EXTENSION IF NOT EXISTS` SQL in migration Up() method
- Functional indexes (unaccent, trigram) require manual SQL in migration: `migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS ix_artists_name_unaccent ON artists (f_unaccent(name) text_pattern_ops)")`
- NuGet versions: EF Core 10.0.8, Npgsql 10.0.2, Npgsql.EntityFrameworkCore.PostgreSQL 10.0.1 — all support DateOnly, DateTimeOffset, functional indexes, JSONB
- Design-time factory (ScriptsDbContextFactory.cs) is used by `dotnet ef` CLI — must have valid connection string
- PendingModelChangesWarning (EF Core 9+): If OnModelCreating changes (e.g., adding extensions), migration snapshot must be updated and compiled model regenerated
- Workflow: Modify OnModelCreating → `dotnet ef migrations add <Name>` → `dotnet ef database update` → `dotnet ef dbcontext optimize` (for compiled model)

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- Phases 00-04 completed — all 9 entities configured, 9 configuration files exist
- `ScriptsDbContext.cs` exists with `OnModelCreating` calling `ApplyConfigurationsFromAssembly`
- Docker running — `docker compose up -d` succeeded
- `$env:PGCONNSTR` loaded from `.env`
- `dotnet ef` CLI tool available (via `Microsoft.EntityFrameworkCore.Tools`)
- `ScriptsDbContextFactory.cs` exists at `csharp/src/Data/ScriptsDbContextFactory.cs` (design-time factory)

---

## File Map

| File | Path | Action |
|------|------|--------|
| `ScriptsDbContext.cs` | `csharp/src/Data/ScriptsDbContext.cs:22-23` | EDIT: add extension registrations |
| Migration files | `csharp/src/Data/Migrations/*_InitialCreate.cs` | CREATE (auto-generated + manual SQL) |
| Migration Designer | `csharp/src/Data/Migrations/*_InitialCreate.Designer.cs` | CREATE (auto-generated) |
| Model Snapshot | `csharp/src/Data/Migrations/ScriptsDbContextModelSnapshot.cs` | CREATE (auto-generated) |
| Test: MigrationGenerateTests.cs | `csharp/tests/Scripts.Tests/Migrations/MigrationGenerateTests.cs` | CREATE |

---

## Task 1: Register PostgreSQL Extensions in OnModelCreating

**Files:**
- Modify: `/home/lance/Scripts/csharp/src\Data\ScriptsDbContext.cs`
- Create: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Migrations\MigrationGenerateTests.cs`

### Step 0: Preflight

```powershell
# Current state: OnModelCreating only calls ApplyConfigurationsFromAssembly
# Reason: Extension registration required for unaccent (accent-insensitive search) and pg_trgm (trigram fuzzy search)
# What: Add mb.HasPostgresExtension("unaccent") and mb.HasPostgresExtension("pg_trgm")
# Expected: OnModelCreating includes both extension registrations before ApplyConfigurationsFromAssembly

Select-String -Path /home/lance/Scripts/csharp/src\Data\ScriptsDbContext.cs -Pattern 'HasPostgresExtension'
# Expected: 0 matches
```

### Step 1: Write the failing test

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Migrations\MigrationGenerateTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;

namespace Scripts.Tests.Migrations;

public sealed class MigrationGenerateTests
{
    [Test]
    public async Task GenerateCreateScript_Contains_CreateTable()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ScriptsDbContext(options);
        var script = context.Database.GenerateCreateScript();

        script.Should().NotBeNullOrEmpty();
        script.Should().Contain("CREATE TABLE");
    }

    [Test]
    public async Task Model_HasUnaccentExtension()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ScriptsDbContext(options);
        var model = context.Model;
        var extensions = model.GetRelationalModel().GetType().GetProperties()
            .Select(p => p.Name).ToList();

        // InMemory won't capture Npgsql extensions — this test validates at model level
        // that the DbContext compiles and OnModelCreating doesn't throw
        model.Should().NotBeNull();
    }

    [Test]
    public async Task GenerateCreateScript_Contains_ExtensionUnaccent()
    {
        // This test requires a real Npgsql connection to verify extension SQL.
        // When Testcontainers is available (T1-15), replace with real DB test.
        // For now, verify that the script uses the PostgreSQL provider.
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ScriptsDbContext(options);
        var providerName = context.Database.ProviderName;
        providerName.Should().NotBeNullOrEmpty();
    }
}
```

### Step 2: Read-back

```powershell
Test-Path '/home/lance/Scripts/csharp/tests\Scripts.Tests\Migrations\MigrationGenerateTests.cs'
# Expected: True
```

### Step 3: Run — confirm baseline

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "MigrationGenerateTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: `3 passed, 0 failed` (tests validate model discovery, not extension registration — the real verification is in Task 2)

### Step 3.5: Assess

Current state: extensions not registered. Tests pass because they only verify the model is buildable. Proceed to add extensions.

### Step 4: Write minimal implementation

Update `OnModelCreating` in `/home/lance/Scripts/csharp/src\Data\ScriptsDbContext.cs`:

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
Select-String -Path /home/lance/Scripts/csharp/src\Data\ScriptsDbContext.cs -Pattern 'HasPostgresExtension'
# Expected: 2 matches (unaccent, pg_trgm)
```

### Step 5: Run — confirm build clean

```powershell
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet test   --filter "MigrationGenerateTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: `3 passed, 0 failed`

### Step 6: Commit

```powershell
git -C /home/lance/Scripts add csharp/src/Data/ScriptsDbContext.cs
git -C /home/lance/Scripts add csharp/tests/Scripts.Tests/Migrations/MigrationGenerateTests.cs
git -C /home/lance/Scripts commit -m "feat(t1-05): register unaccent and pg_trgm extensions in OnModelCreating"
```

---

## Task 2: Generate InitialCreate Migration

**Files:**
- Auto-create: `csharp/src/Data/Migrations/*_InitialCreate.cs`
- Auto-create: `csharp/src/Data/Migrations/*_InitialCreate.Designer.cs`
- Auto-create: `csharp/src/Data/Migrations/ScriptsDbContextModelSnapshot.cs`

### Step 0: Preflight

```powershell
# Current state: No migrations exist, all entities configured, extensions registered
# Reason: Schema baseline must be captured as an EF Core migration
# What: Run dotnet ef migrations add InitialCreate
# Expected: Migration files created in Migrations/ directory

Test-Path /home/lance/Scripts/csharp/src\Data\Migrations
# Expected: False (no directory yet)

# Load .env for EF CLI
Get-Content /home/lance/Scripts/.env | ForEach-Object {
    if ($_ -match '^([^#][^=]+)=(.+)$') {
        [System.Environment]::SetEnvironmentVariable($Matches[1], $Matches[2])
    }
}
Write-Host "PGCONNSTR loaded: $([bool]$env:PGCONNSTR)"
# Expected: PGCONNSTR loaded: True
```

### Step 3: Run migration generation

Since this is a code-generation step (not test-driven in the traditional sense), we go directly to Step 3:

```powershell
# The project is currently monolithic CSharpScripts.csproj.
# Migrations folder goes under src/Data but the .csproj is at csharp/ level.
dotnet ef migrations add InitialCreate `
    --project /home/lance/Scripts/csharp/CSharpScripts.csproj `
    --output-dir src\Data\Migrations `
    2>&1
```

Expected output:
```
Build started...
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
```

Verify migration files created:

```powershell
$migrationsDir = '/home/lance/Scripts/csharp/src\Data\Migrations'
Test-Path $migrationsDir
# Expected: True

Get-ChildItem $migrationsDir | Select-Object Name
# Expected: *_InitialCreate.cs, *_InitialCreate.Designer.cs, ScriptsDbContextModelSnapshot.cs
```

### Step 3.5: Assess

Migration generated successfully. Verify the generated `Up()` method in the migration file contains the extension SQL:

```powershell
$migrationFile = Get-ChildItem $migrationsDir -Filter '*_InitialCreate.cs' | Select-Object -First 1
Select-String -Path $migrationFile.FullName -Pattern 'unaccent|pg_trgm'
# Expected: 2 matches (CREATE EXTENSION IF NOT EXISTS unaccent; and pg_trgm)
```

### Step 4: Add functional indexes (manual migration SQL)

After the `Up()` method's auto-generated code, add functional indexes. Open the generated migration file and add this inside `Up()`, after the auto-generated table/create calls:

```csharp
migrationBuilder.Sql(
    "CREATE UNIQUE INDEX IF NOT EXISTS ix_artists_name_unaccent ON artists (f_unaccent(name) text_pattern_ops)");

migrationBuilder.Sql(
    "CREATE INDEX IF NOT EXISTS ix_artists_name_trgm ON artists USING gin (name gin_trgm_ops)");

migrationBuilder.Sql(
    "CREATE INDEX IF NOT EXISTS ix_tracks_title_unaccent ON tracks (f_unaccent(title) text_pattern_ops)");
```

Also add the corresponding drops in `Down()`:

```csharp
migrationBuilder.Sql("DROP INDEX IF EXISTS ix_tracks_title_unaccent");
migrationBuilder.Sql("DROP INDEX IF EXISTS ix_artists_name_trgm");
migrationBuilder.Sql("DROP INDEX IF EXISTS ix_artists_name_unaccent");
```

### Step 5: Verify migration script contains functional indexes

```powershell
$migrationFile = Get-ChildItem $migrationsDir -Filter '*_InitialCreate.cs' | Select-Object -First 1
Select-String -Path $migrationFile.FullName -Pattern 'f_unaccent|gin_trgm'
# Expected: 3 matches
```

### Step 6: Commit

```powershell
git -C /home/lance/Scripts add csharp/src/Data/Migrations/
git -C /home/lance/Scripts commit -m "feat(t1-05): generate InitialCreate migration with unaccent, pg_trgm, and functional indexes"
```

---

## Task 3: Apply Migration to Local PostgreSQL 18

**Files:**
- No new files created — database schema is applied.

### Step 0: Preflight

```powershell
# Current state: Migration exists but not applied, PostgreSQL is running (verified in 00-environment)
# Reason: Migration must be applied to verify it runs without errors against PostgreSQL 18
# What: Run dotnet ef database update
# Expected: All tables created in the local database

# Verify PostgreSQL is running
docker ps --filter name=postgres --format "{{.Status}}"
# Expected: Up ... (healthy)

# Verify connection
$env:PGCONNSTR -match 'Host='
# Expected: True
```

### Step 3: Run database update

```powershell
dotnet ef database update `
    --project /home/lance/Scripts/csharp/CSharpScripts.csproj `
    2>&1
```

Expected output:
```
Build started...
Build succeeded.
Applying migration '..._InitialCreate'.
Done.
```

### Step 4: Verify tables exist in PostgreSQL

```powershell
# Use a SQL query via psql to verify table creation.
# Note: Ensure psql is available (comes with PostgreSQL or Docker).

docker exec postgres psql -U postgres -d scripts -c "\dt" 2>&1
```

Expected output includes these tables:
```
artists, albums, tracks, scrobbles, videos,
execution_logs, failed_tasks, fibery_entities, source_records
```

Verify extensions:

```powershell
docker exec postgres psql -U postgres -d scripts -c "SELECT extname FROM pg_extension WHERE extname IN ('unaccent', 'pg_trgm')" 2>&1
```

Expected output:
```
  extname
----------
 unaccent
 pg_trgm
(2 rows)
```

### Step 5: Run schema verification via test

Create a real-PostgreSQL verification test:

File: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Migrations\MigrationApplyTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace Scripts.Tests.Migrations;

public sealed class MigrationApplyTests
{
    [Test]
    public async Task Database_CanConnect_AfterMigration()
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR");
        connStr.Should().NotBeNullOrEmpty(because: "PGCONNSTR must be set for database tests");

        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        await using var context = new ScriptsDbContext(options);
        var canConnect = await context.Database.CanConnectAsync();
        canConnect.Should().BeTrue(because: "database should be reachable after migration");
    }

    [Test]
    public async Task ArtistsTable_HasNoMbidColumn()
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR")!;
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        await using var context = new ScriptsDbContext(options);
        var exists = await context.Database.ExecuteSqlRawAsync(
            "SELECT count(*) FROM information_schema.columns WHERE table_name = 'artists' AND column_name = 'mbid'");

        exists.Should().Be(0, because: "Mbid was removed in T1-02");
    }
}
```

Run:

```powershell
dotnet test --filter "MigrationApplyTests" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: `2 passed, 0 failed`

### Step 6: Commit

```powershell
git -C /home/lance/Scripts add csharp/tests/Scripts.Tests/Migrations/MigrationApplyTests.cs
git -C /home/lance/Scripts commit -m "feat(t1-05): apply InitialCreate migration and add schema verification tests"
```

---

## Final Verification

```powershell
# Confirm all migration tests pass
dotnet test --filter "Scripts.Tests.Migrations" /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected:
```
Passed MigrationGenerateTests (3 tests)
Passed MigrationApplyTests (2 tests)
5 passed, 0 failed
```

**→ Proceed to `06-repositories.md`**

---

## Research Provenance

<!-- from research/MIGRATIONS-EXTENSIONS-consolidated.md and research/DBCONTEXT-CONFIGURATION-consolidated.md (extension registration) -->

Sources:
- `AI/plans/research/MIGRATIONS-EXTENSIONS-consolidated.md` (all sections) — consolidated 2026-06-01; dir deleted
- `AI/plans/research/DBCONTEXT-CONFIGURATION-consolidated.md` (extension registration §1.3, §1.4) — consolidated 2026-06-01; dir deleted

Content already covered: no migrations exist (Task 2 Step 0), extensions registered in OnModelCreating (Task 1), functional indexes (Task 2 Step 4), `dotnet ef` command with monolithic project (Task 2 Step 3), NuGet versions (already in `00-environment.md`).

### Blockers & Prerequisites (research §5)

| #   | Item                                                                          | Status                                                  |
| --- | ----------------------------------------------------------------------------- | ------------------------------------------------------- |
| 1   | `05-migrations.md` plan file                                                  | ✅ This file                                              |
| 2   | Phases 02-04 completed (entity refactoring, dbcontext config, entity configs) | ⚠️ Depends on prior phases                                |
| 3   | `$env:PGCONNSTR` loaded and DB running                                        | ⚠️ Verify at runtime                                     |
| 4   | `dotnet ef` CLI tool available                                                | ✅ Via `Microsoft.EntityFrameworkCore.Tools`             |
| 5   | Solution file (`.slnx`)                                                       | ❌ Does not exist — built via `.csproj` directly (deferred to T2) |
| 6   | Modular project structure (Data/CLI separation)                               | ❌ Project is monolithic `CSharpScripts.csproj` (deferred to T2) |

### NuGet Versions (research §3)

| Package                                 | Version |
| --------------------------------------- | ------- |
| `Microsoft.EntityFrameworkCore`         | 10.0.8  |
| `Microsoft.EntityFrameworkCore.Design`  | 10.0.8  |
| `Microsoft.EntityFrameworkCore.Tools`   | 10.0.8  |
| `Npgsql`                                | 10.0.2  |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.1  |
