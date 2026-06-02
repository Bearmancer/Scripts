# T1-15: Testcontainers Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create the `Scripts.Tests` project with TUnit, FluentAssertions, and Testcontainers.PostgreSql, implement a `DatabaseFixture` that spins up `postgres:18-alpine`, and register the test project in `Scripts.slnx`.

**Architecture:** A new test project `csharp/tests/Scripts.Tests/Scripts.Tests.csproj` is created with test NuGet references and a `ProjectReference` to `CSharpScripts.csproj`. `InternalsVisibleTo` attributes are added so tests can access `internal` types (`ScriptsDbContext`, entities). `DatabaseFixture` wraps a `PostgreSqlContainer`, creates a `ScriptsDbContext` connected to it, and runs `Database.MigrateAsync()` on initialization. Per-test fresh containers avoid test pollution.

**Tech Stack:** C# 14 / .NET 10 / TUnit 1.48.6 / FluentAssertions 7.0.0 (Apache 2.0) / Testcontainers.PostgreSql 4.12.0 / PostgreSQL 18 Alpine / EF Core 10 / Npgsql 10

---

## Prerequisites

- T1-14 completed (resilience policies green)
- Docker Desktop running
- `/home/lance/Scripts/csharp\src\CSharpScripts.csproj` exists (monolith project)
- `ScriptsDbContext` with 9 DbSets and EF migrations generated
- TUnit 1.48.6+ (1.0 was full rewrite; 0.9.0 API not compatible). Testcontainers 4.12.0+ (3.x builder API removed).

```powershell
docker ps 2>&1 | Select-String "healthy"
# Expected: container listed (at least Docker daemon running)

Test-Path /home/lance/Scripts/csharp/src\CSharpScripts.csproj
# Expected: True

Test-Path /home/lance/Scripts/Scripts.slnx
# Expected: True
```

---

## Task 1 — Create Test Project with NuGet References

**Files:**
- Create: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Scripts.Tests.csproj`
- Create: `/home/lance/Scripts/csharp/tests\Scripts.Tests\GlobalUsings.cs`
- Modify: `/home/lance/Scripts/Scripts.slnx`
- Modify: `/home/lance/Scripts/csharp/CSharpScripts.csproj`

### Step 0: Preflight

```powershell
# Current state: No csharp/tests/ directory exists. No test project. No test NuGet packages.
# Scripts.slnx has empty /tests/ folder.
# Reason: All TDD phases from T1-00 onward reference test files — test project must exist.
# What: Create test .csproj with TUnit + FluentAssertions + Testcontainers, add to solution.
# Expected: Project created, dotnet restore succeeds, solution references the project.

Test-Path /home/lance/Scripts/csharp/tests
# Expected: False

New-Item -ItemType Directory -Force -Path /home/lance/Scripts/csharp/tests\Scripts.Tests
```

### Step 1: Implement

Create `/home/lance/Scripts/csharp/tests\Scripts.Tests\Scripts.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <RootNamespace>Scripts.Tests</RootNamespace>
    <AssemblyName>Scripts.Tests</AssemblyName>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="TUnit" Version="1.48.6" />
    <PackageReference Include="FluentAssertions" Version="7.0.0" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.12.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\CSharpScripts.csproj" />
  </ItemGroup>
</Project>
```

Create `/home/lance/Scripts/csharp/tests\Scripts.Tests\GlobalUsings.cs`:

```csharp
global using System.Diagnostics;
global using System.Text.Json;
global using FluentAssertions;
global using Microsoft.EntityFrameworkCore;
global using TUnit;
global using TUnit.Assertions;
global using Testcontainers.PostgreSql;
```

Modify `/home/lance/Scripts/Scripts.slnx` — replace empty tests folder (line 6):

OLD:
```xml
	<Folder Name="/tests/" />
```

NEW:
```xml
	<Folder Name="/tests/">
		<Project Path="csharp/tests/Scripts.Tests/Scripts.Tests.csproj" />
	</Folder>
```

Add `InternalsVisibleTo` to `/home/lance/Scripts/csharp/CSharpScripts.csproj` — add after the last `</PackageReference>` inside the `<ItemGroup>`:

```xml
	<ItemGroup>
		<InternalsVisibleTo Include="Scripts.Tests" />
	</ItemGroup>
```

### Step 2: Readback

```powershell
Test-Path /home/lance/Scripts/csharp/tests\Scripts.Tests\Scripts.Tests.csproj
# Expected: True

Test-Path /home/lance/Scripts/csharp/tests\Scripts.Tests\GlobalUsings.cs
# Expected: True

Get-Content /home/lance/Scripts/Scripts.slnx | Select-String "Scripts.Tests.csproj"
# Expected: match found

Get-Content /home/lance/Scripts/csharp/CSharpScripts.csproj | Select-String "InternalsVisibleTo"
# Expected: InternalsVisibleTo Include="Scripts.Tests"
```

### Step 3: Run build (expect GREEN)

```powershell
dotnet restore /home/lance/Scripts/csharp/Scripts.slnx 2>&1
dotnet build /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: Restore succeeded. Build succeeded with 0 errors, 0 warnings.

### Step 4: Run test (expect infrastructure boots despite 0 tests)

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: `Test run successful. Total tests: 0`.

### Step 5: Commit

```powershell
git add /home/lance/Scripts/csharp/tests\
git add /home/lance/Scripts/Scripts.slnx
git add /home/lance/Scripts/csharp/CSharpScripts.csproj
git commit -m "feat(t1-15): create scripts tests project with tunit fluentassertions testcontainers"
```

---

## Task 2 — Implement DatabaseFixture

**Files:**
- Create: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Infrastructure\DatabaseFixture.cs`
- Create: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Infrastructure\FixtureBootstrapTests.cs`

### Step 0: Preflight

```powershell
Test-Path /home/lance/Scripts/csharp/tests\Scripts.Tests\Infrastructure
# Expected: False

New-Item -ItemType Directory -Force -Path /home/lance/Scripts/csharp/tests\Scripts.Tests\Infrastructure
```

### Step 1: Write test

Create `/home/lance/Scripts/csharp/tests\Scripts.Tests\Infrastructure\FixtureBootstrapTests.cs`:

```csharp
using FluentAssertions;
using TUnit;
using CSharpScripts.Data;
using Scripts.Tests.Infrastructure;

namespace Scripts.Tests.Infrastructure;

public sealed class FixtureBootstrapTests
{
    [Test]
    public async Task DatabaseFixture_Initializes_Successfully()
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();

        await Assert.That(fixture.Context).IsNotNull();
        var canConnect = await fixture.Context.Database.CanConnectAsync();
        await Assert.That(canConnect).IsTrue();
    }

    [Test]
    public async Task DatabaseFixture_Migrates_Without_Error()
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();

        var pendingMigrations = await fixture.Context.Database.GetPendingMigrationsAsync();
        await Assert.That(pendingMigrations).IsEmpty();
    }
}
```

### Step 2: Readback

```powershell
$file = '/home/lance/Scripts/csharp/tests\Scripts.Tests\Infrastructure\FixtureBootstrapTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect RED — DatabaseFixture doesn't exist yet)

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx --filter "FixtureBootstrapTests" 2>&1
```

Expected: RED — compilation error: `The type or namespace name 'DatabaseFixture' could not be found`.

### Step 4: Assess

Need DatabaseFixture with `PostgreSqlContainer`, `ScriptsDbContext`, `InitializeAsync()`, and `DisposeAsync()`.

### Step 5: Implement

Create `/home/lance/Scripts/csharp/tests\Scripts.Tests\Infrastructure\DatabaseFixture.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using CSharpScripts.Data;

namespace Scripts.Tests.Infrastructure;

public sealed class DatabaseFixture : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:18-alpine")
        .WithDatabase("scripts_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public ScriptsDbContext Context { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        Context = new ScriptsDbContext(options);
        await Context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (Context is not null)
            await Context.DisposeAsync();
        await _container.DisposeAsync();
    }
}
```

Verify build:

```powershell
dotnet build /home/lance/Scripts/csharp/Scripts.slnx --no-restore 2>&1
```

Expected: Build succeeded with 0 errors.

### Step 6: Run test (expect GREEN)

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx --filter "FixtureBootstrapTests" 2>&1
```

Expected: GREEN. Container spins up (first run pulls `postgres:18-alpine`, ~120MB, takes 30-60s). Tests pass:
- `DatabaseFixture_Initializes_Successfully`: PASS
- `DatabaseFixture_Migrates_Without_Error`: PASS

### Step 7: Commit

```powershell
git add /home/lance/Scripts/csharp/tests\Scripts.Tests\Infrastructure\DatabaseFixture.cs
git add /home/lance/Scripts/csharp/tests\Scripts.Tests\Infrastructure\FixtureBootstrapTests.cs
git commit -m "feat(t1-15): implement databasefixture with testcontainers postgres18 alpine"
```

---

## Task 3 — Entity Integration Tests (Insert + Retrieve Artist)

**Files:**
- Create: `/home/lance/Scripts/csharp/tests\Scripts.Tests\Integration\ArtistEntityIntegrationTests.cs`

### Step 0: Preflight

```powershell
New-Item -ItemType Directory -Force -Path /home/lance/Scripts/csharp/tests\Scripts.Tests\Integration
```

### Step 1: Write test

Create `/home/lance/Scripts/csharp/tests\Scripts.Tests\Integration\ArtistEntityIntegrationTests.cs`:

```csharp
using FluentAssertions;
using TUnit;
using CSharpScripts.Data.Entities;
using Scripts.Tests.Infrastructure;

namespace Scripts.Tests.Integration;

public sealed class ArtistEntityIntegrationTests
{
    [Test]
    public async Task InsertArtist_Persists_To_Database()
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();
        var context = fixture.Context;

        var artist = new Artist { Name = "Test Artist" };
        context.Artists.Add(artist);
        await context.SaveChangesAsync();

        var retrieved = await context.Artists
            .FirstOrDefaultAsync(a => a.Name == "Test Artist");

        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.Id).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task DeleteArtist_Using_ExecuteDelete_Removes_Record()
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();
        var context = fixture.Context;

        var artist = new Artist { Name = "Temp Artist" };
        context.Artists.Add(artist);
        await context.SaveChangesAsync();

        await context.Artists
            .Where(a => a.Name == "Temp Artist")
            .ExecuteDeleteAsync();

        var count = await context.Artists
            .CountAsync(a => a.Name == "Temp Artist");
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task InsertArtist_And_Album_With_ForeignKey_Works()
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();
        var context = fixture.Context;

        var artist = new Artist { Name = "Pink Floyd" };
        context.Artists.Add(artist);
        await context.SaveChangesAsync();

        var album = new Album
        {
            ArtistId = artist.Id,
            Title = "The Dark Side of the Moon",
            ReleaseDate = new DateOnly(1973, 3, 1)
        };
        context.Albums.Add(album);
        await context.SaveChangesAsync();

        var retrieved = await context.Albums
            .Include(a => a.Artist)
            .FirstOrDefaultAsync(a => a.Title == "The Dark Side of the Moon");

        await Assert.That(retrieved).IsNotNull();
        await Assert.That(retrieved!.Artist).IsNotNull();
        await Assert.That(retrieved.Artist!.Name).IsEqualTo("Pink Floyd");
    }
}
```

### Step 2: Readback

```powershell
$file = '/home/lance/Scripts/csharp/tests\Scripts.Tests\Integration\ArtistEntityIntegrationTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect RED if entity properties mismatch, GREEN if aligned)

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx --filter "ArtistEntityIntegrationTests" 2>&1
```

Expected: If entity properties align, tests pass. If entity uses different naming, tests fail with compilation errors — adjust test to match actual entity definitions.

### Step 4: Assess

If entity properties in test don't match actual entity class at `csharp/src/Data/Entities/Artist.cs` and `csharp/src/Data/Entities/Album.cs`, update test to match. Do NOT modify entity definitions.

### Step 5: Implement (adjust tests to actual entity shape if needed)

If `Artist` has different constructor or property names, align tests. Example: if `Artist` requires `ArtistMbId` in constructor, add `Mbid = null` or the equivalent init property.

### Step 6: Run test (expect GREEN)

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx --filter "ArtistEntityIntegrationTests" 2>&1
```

Expected: GREEN — all 3 tests pass against real PostgreSQL container.

### Step 7: Commit

```powershell
git add /home/lance/Scripts/csharp/tests\Scripts.Tests\Integration\ArtistEntityIntegrationTests.cs
git commit -m "feat(t1-15): add artist entity integration tests with testcontainers"
```

---

## Verification Checklist

- [ ] `csharp/tests/Scripts.Tests/Scripts.Tests.csproj` exists with TUnit + FluentAssertions + Testcontainers
- [ ] `InternalsVisibleTo` for `Scripts.Tests` in `CSharpScripts.csproj`
- [ ] `Scripts.slnx` references the test project
- [ ] `DatabaseFixture` creates `postgres:18-alpine` container, runs `MigrateAsync()`
- [ ] `FixtureBootstrapTests`: container connects and migrates (2 PASS)
- [ ] `ArtistEntityIntegrationTests`: insert/delete/FK relationship (3 PASS)
- [ ] `dotnet build` passes with 0 errors
- [ ] `dotnet test` runs all T1-15 tests green
- [ ] All TUnit 1.x assertions use `await Assert.That(...).IsXxx()` (NOT `Assert.Equal` from 0.x)

---

## Research Provenance

<!-- from research/TESTING-INFRASTRUCTURE-consolidated.md -->

Source: `AI/plans/research/TESTING-INFRASTRUCTURE-consolidated.md` (8 source files consolidated) — consolidated 2026-06-01; dir deleted

### Drift Correction (TUnit 0.9.0 → 1.48.6)

Research references TUnit 0.9.0 assertion syntax (`Assert.Equal(x, y)`), which is **stale**. Current TUnit is 1.48.6 — the 1.0 release was a full rewrite with an entirely new API. All assertions in this plan use the 1.x fluent syntax: `await Assert.That(x).IsEqualTo(y)`. See https://tunit.dev/docs/assertions/ for the current API reference. Testcontainers research also references 3.10.0; current 4.12.0 ships a typed builder (basic `.WithImage().WithDatabase().WithUsername().WithPassword().Build()` pattern unchanged for the common case).

### Alternative Considered: Native Database Testing (research §2.1)

Research notes a counter-recommendation: a single persistent local PostgreSQL with transactional rollback can outperform Testcontainers (sub-millisecond rollback vs 2-5s container spin-up). The research `DatabaseFixture` design (research §3.2) wraps an `NpgsqlConnection` + per-test `NpgsqlTransaction` instead of a Testcontainers container.

**Why this plan uses Testcontainers instead:** the orchestrator/PM directive (recorded in `06-repositories.md` Key Findings line 9) calls out Testcontainers as the chosen path; the test project structure is the source of truth, not the research alternative. (AGENTS.md deleted 2026-06-01; the directive it carried survives in `06-repositories.md`.) **Action:** this plan delivers Testcontainers as planned; if T1-15 completes and parallelism is a bottleneck, evaluate migration to native DB testing in a later tier.

### Concurrency Golden Rule (research §4.1)

> **Neither `DbContext` nor `DbConnection` (including `NpgsqlConnection`) is thread-safe.**

For parallel test isolation (TUnit default): each test class/method must own a separate `DbConnection` from the pool with its own transaction. The Testcontainers `DatabaseFixture` (Task 2) instantiates a fresh `ScriptsDbContext` per test method via the container, achieving the same isolation.

### Npgsql Pool Configuration (research §5.1)

Defaults: `MaxPoolSize=100`, `MinPoolSize=0`, `Pooling=true`. TUnit's aggressive parallel scheduling can exhaust the pool if connections aren't promptly disposed. The test fixture must use `await using` to ensure prompt disposal (already enforced in Task 2's `DatabaseFixture.DisposeAsync`).

### Success Criteria (research §8)

- `dotnet test csharp/Scripts.slnx` — all tests pass
- No `PendingModelChangesWarning` exceptions in test logs
- Tests complete in < 5 seconds
- No `NullReferenceException` in `InMemoryTable` or `NpgsqlMigrator` (see `01-entities.md` Research Provenance for the JsonDocument NRE root cause)
