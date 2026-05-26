# T1-15: Testcontainers Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create the `Scripts.Tests` project with TUnit, FluentAssertions, and Testcontainers.PostgreSql, implement a `DatabaseFixture` that spins up `postgres:18-alpine`, and register the test project in `Scripts.slnx`.

**Architecture:** A new test project `csharp/tests/Scripts.Tests/Scripts.Tests.csproj` is created with test NuGet references and a `ProjectReference` to `CSharpScripts.csproj`. `InternalsVisibleTo` attributes are added so tests can access `internal` types (`ScriptsDbContext`, entities). `DatabaseFixture` wraps a `PostgreSqlContainer`, creates a `ScriptsDbContext` connected to it, and runs `Database.MigrateAsync()` on initialization. Per-test fresh containers avoid test pollution.

**Tech Stack:** C# 14 / .NET 10 / TUnit 0.9.0 / FluentAssertions 7.0.0 / Testcontainers.PostgreSql 3.10.0 / PostgreSQL 18 Alpine / EF Core 10 / Npgsql 10

---

## Prerequisites

- T1-14 completed (resilience policies green)
- Docker Desktop running
- `C:\Users\Lance\Dev\Scripts\csharp\src\CSharpScripts.csproj` exists (monolith project)
- `ScriptsDbContext` with 9 DbSets and EF migrations generated

```powershell
docker ps 2>&1 | Select-String "healthy"
# Expected: container listed (at least Docker daemon running)

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\CSharpScripts.csproj
# Expected: True

Test-Path C:\Users\Lance\Dev\Scripts\Scripts.slnx
# Expected: True
```

---

## Task 1 — Create Test Project with NuGet References

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Scripts.Tests.csproj`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\GlobalUsings.cs`
- Modify: `C:\Users\Lance\Dev\Scripts\Scripts.slnx`
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj`

### Step 0: Preflight

```powershell
# Current state: No csharp/tests/ directory exists. No test project. No test NuGet packages.
# Scripts.slnx has empty /tests/ folder.
# Reason: All TDD phases from T1-00 onward reference test files — test project must exist.
# What: Create test .csproj with TUnit + FluentAssertions + Testcontainers, add to solution.
# Expected: Project created, dotnet restore succeeds, solution references the project.

Test-Path C:\Users\Lance\Dev\Scripts\csharp\tests
# Expected: False

New-Item -ItemType Directory -Force -Path C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests
```

### Step 1: Implement

Create `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Scripts.Tests.csproj`:

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
    <PackageReference Include="TUnit" Version="0.9.0" />
    <PackageReference Include="FluentAssertions" Version="7.0.0" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="3.10.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\CSharpScripts.csproj" />
  </ItemGroup>
</Project>
```

Create `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\GlobalUsings.cs`:

```csharp
global using System.Diagnostics;
global using System.Text.Json;
global using FluentAssertions;
global using Microsoft.EntityFrameworkCore;
global using TUnit;
global using Testcontainers.PostgreSql;
```

Modify `C:\Users\Lance\Dev\Scripts\Scripts.slnx` — replace empty tests folder (line 6):

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

Add `InternalsVisibleTo` to `C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj` — add after the last `</PackageReference>` inside the `<ItemGroup>`:

```xml
	<ItemGroup>
		<InternalsVisibleTo Include="Scripts.Tests" />
	</ItemGroup>
```

### Step 2: Readback

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Scripts.Tests.csproj
# Expected: True

Test-Path C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\GlobalUsings.cs
# Expected: True

Get-Content C:\Users\Lance\Dev\Scripts\Scripts.slnx | Select-String "Scripts.Tests.csproj"
# Expected: match found

Get-Content C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj | Select-String "InternalsVisibleTo"
# Expected: InternalsVisibleTo Include="Scripts.Tests"
```

### Step 3: Run build (expect GREEN)

```powershell
dotnet restore C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet build C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: Restore succeeded. Build succeeded with 0 errors, 0 warnings.

### Step 4: Run test (expect infrastructure boots despite 0 tests)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected: `Test run successful. Total tests: 0`.

### Step 5: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\tests\
git add C:\Users\Lance\Dev\Scripts\Scripts.slnx
git add C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj
git commit -m "feat(t1-15): create scripts tests project with tunit fluentassertions testcontainers"
```

---

## Task 2 — Implement DatabaseFixture

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Infrastructure\DatabaseFixture.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Infrastructure\FixtureBootstrapTests.cs`

### Step 0: Preflight

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Infrastructure
# Expected: False

New-Item -ItemType Directory -Force -Path C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Infrastructure
```

### Step 1: Write test

Create `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Infrastructure\FixtureBootstrapTests.cs`:

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

        fixture.Context.Should().NotBeNull();
        var canConnect = await fixture.Context.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }

    [Test]
    public async Task DatabaseFixture_Migrates_Without_Error()
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();

        var pendingMigrations = await fixture.Context.Database.GetPendingMigrationsAsync();
        pendingMigrations.Should().BeEmpty();
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Infrastructure\FixtureBootstrapTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect RED — DatabaseFixture doesn't exist yet)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "FixtureBootstrapTests" 2>&1
```

Expected: RED — compilation error: `The type or namespace name 'DatabaseFixture' could not be found`.

### Step 4: Assess

Need DatabaseFixture with `PostgreSqlContainer`, `ScriptsDbContext`, `InitializeAsync()`, and `DisposeAsync()`.

### Step 5: Implement

Create `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Infrastructure\DatabaseFixture.cs`:

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
dotnet build C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --no-restore 2>&1
```

Expected: Build succeeded with 0 errors.

### Step 6: Run test (expect GREEN)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "FixtureBootstrapTests" 2>&1
```

Expected: GREEN. Container spins up (first run pulls `postgres:18-alpine`, ~120MB, takes 30-60s). Tests pass:
- `DatabaseFixture_Initializes_Successfully`: PASS
- `DatabaseFixture_Migrates_Without_Error`: PASS

### Step 7: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Infrastructure\DatabaseFixture.cs
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Infrastructure\FixtureBootstrapTests.cs
git commit -m "feat(t1-15): implement databasefixture with testcontainers postgres18 alpine"
```

---

## Task 3 — Entity Integration Tests (Insert + Retrieve Artist)

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Integration\ArtistEntityIntegrationTests.cs`

### Step 0: Preflight

```powershell
New-Item -ItemType Directory -Force -Path C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Integration
```

### Step 1: Write test

Create `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Integration\ArtistEntityIntegrationTests.cs`:

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

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().NotBe(Guid.Empty);
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
        count.Should().Be(0);
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

        retrieved.Should().NotBeNull();
        retrieved!.Artist.Should().NotBeNull();
        retrieved.Artist!.Name.Should().Be("Pink Floyd");
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Integration\ArtistEntityIntegrationTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect RED if entity properties mismatch, GREEN if aligned)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "ArtistEntityIntegrationTests" 2>&1
```

Expected: If entity properties align, tests pass. If entity uses different naming, tests fail with compilation errors — adjust test to match actual entity definitions.

### Step 4: Assess

If entity properties in test don't match actual entity class at `csharp/src/Data/Entities/Artist.cs` and `csharp/src/Data/Entities/Album.cs`, update test to match. Do NOT modify entity definitions.

### Step 5: Implement (adjust tests to actual entity shape if needed)

If `Artist` has different constructor or property names, align tests. Example: if `Artist` requires `ArtistMbId` in constructor, add `Mbid = null` or the equivalent init property.

### Step 6: Run test (expect GREEN)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "ArtistEntityIntegrationTests" 2>&1
```

Expected: GREEN — all 3 tests pass against real PostgreSQL container.

### Step 7: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Integration\ArtistEntityIntegrationTests.cs
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
