# Implementation Plan: Repository Assessment and Integration

> **For agentic workers:** REQUIRED SUB-SKILL: Invoke `superpowers:subagent-driven-development` before executing any task. Each numbered task is a discrete subagent delegation unit. Parent agent tracks completion and gates the next task on GREEN + committed.

**Goal:** Fix the Lingua build error (T1-13), consolidate documentation, establish the Fibery ingestion pipeline (EF Core entity + service), and analyze git history — all without interfering with the Tier 1 EF Core migration boundary.

**TDD Enforcement:** Every task follows the 7-step loop: Preflight → RED test → Read-back → Confirm RED → GREEN impl → Confirm GREEN → Commit. No production code without a failing test first.

**Testing framework:** TUnit + FluentAssertions. Never use xUnit `[Fact]` or NUnit `[TestCase]`.

---

## Phase 1: Lingua Build Error Resolution (T1-13 Prerequisite)

### Task 1.1: Fix LanguageIdentifier.cs enum casing and null comparisons

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Services\Language\LanguageIdentifier.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language\LanguageIdentifierCompilationTests.cs`

**Step 0: Preflight**

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Services\Language\LanguageIdentifier.cs -ErrorAction Stop
```

Expected: True

**Step 1: Write failing test**

```csharp
namespace Scripts.Tests.Language;

public sealed class LanguageIdentifierCompilationTests
{
    [Test]
    public void LanguageIdentifier_HasNoScreamingSnakeCaseReferences()
    {
        var path = @"C:\Users\Lance\Dev\Scripts\csharp\src\Services\Language\LanguageIdentifier.cs";
        var source = File.ReadAllText(path, Encoding.UTF8);
        var forbidden = new[]
        {
            "Language.ENGLISH", "Language.FRENCH", "Language.GERMAN",
            "Language.SPANISH", "Language.PORTUGUESE", "Language.ITALIAN",
            "Language.DUTCH", "Language.RUSSIAN", "Language.CHINESE",
            "Language.JAPANESE", "Language.KOREAN", "Language.ARABIC",
            "Language.HINDI"
        };
        foreach (var token in forbidden)
        {
            source.Should().NotContain(token, because: $"{token} must be PascalCase");
        }
    }
}
```

**Step 2: Read-back**

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Language\LanguageIdentifierCompilationTests.cs' -ErrorAction Stop
```

Expected: True

**Step 3: Run — confirm RED**

```powershell
$out = dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "LanguageIdentifierCompilationTests" 2>&1
Write-Host $out
if ($out -match "Failed") { Write-Host "CONFIRMED RED" } else { Write-Host "UNEXPECTED RESULT" }
```

Expected: FAIL — LanguageIdentifier.cs still contains SCREAMING_SNAKE_CASE references.

**Step 3.5: Assess**

Open `LanguageIdentifier.cs` and audit all `Language.XXXX` references. Confirm they exist before proceeding.

**Step 4: Write minimal implementation**

```powershell
$timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
$src = 'C:\Users\Lance\Dev\Scripts\csharp\src\Services\Language\LanguageIdentifier.cs'
Copy-Item $src "$src.bak.$timestamp" -ErrorAction Stop
Test-Path "$src.bak.$timestamp" -ErrorAction Stop

$mappings = @{
    'Language\.ENGLISH'    = 'Language.English'
    'Language\.FRENCH'     = 'Language.French'
    'Language\.GERMAN'     = 'Language.German'
    'Language\.SPANISH'    = 'Language.Spanish'
    'Language\.PORTUGUESE' = 'Language.Portuguese'
    'Language\.ITALIAN'    = 'Language.Italian'
    'Language\.DUTCH'      = 'Language.Dutch'
    'Language\.RUSSIAN'    = 'Language.Russian'
    'Language\.CHINESE'    = 'Language.Chinese'
    'Language\.JAPANESE'   = 'Language.Japanese'
    'Language\.KOREAN'     = 'Language.Korean'
    'Language\.ARABIC'     = 'Language.Arabic'
    'Language\.HINDI'      = 'Language.Hindi'
    'Language\.BENGALI'    = 'Language.Bengali'
    'Language\.CATALAN'    = 'Language.Catalan'
    'Language\.CZECH'      = 'Language.Czech'
    'Language\.DANISH'     = 'Language.Danish'
    'Language\.FINNISH'    = 'Language.Finnish'
    'Language\.GREEK'      = 'Language.Greek'
    'Language\.HUNGARIAN'  = 'Language.Hungarian'
    'Language\.NORWEGIAN'  = 'Language.Norwegian'
    'Language\.POLISH'     = 'Language.Polish'
    'Language\.ROMANIAN'   = 'Language.Romanian'
    'Language\.SLOVAK'     = 'Language.Slovak'
    'Language\.SWEDISH'    = 'Language.Swedish'
    'Language\.TURKISH'    = 'Language.Turkish'
    'Language\.UKRAINIAN'  = 'Language.Ukrainian'
    'Language\.VIETNAMESE' = 'Language.Vietnamese'
    'Language\.THAI'       = 'Language.Thai'
}

$content = Get-Content $src -Raw -Encoding UTF8 -ErrorAction Stop
foreach ($old in $mappings.Keys) {
    $content = $content -replace $old, $mappings[$old]
}
$content = $content -replace 'Language\s+(\w+)\s*==\s*null', 'Language? $1 == null'
[System.IO.File]::WriteAllText($src, $content, [System.Text.Encoding]::UTF8)
Write-Host "Applied fixes to LanguageIdentifier.cs"
```

**Step 5: Run — confirm GREEN**

```powershell
$out = dotnet restore C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
$build = dotnet build C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
Write-Host $build
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$test = dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "LanguageIdentifierCompilationTests" 2>&1
Write-Host $test
if ($test -notmatch "Passed") { throw "Test failed" }
```

Expected: Build clean, 1 test passing.

**Step 6: Commit**

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Services/Language/LanguageIdentifier.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/Language/LanguageIdentifierCompilationTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "fix(t1-13): fix Lingua enum casing and null comparisons in LanguageIdentifier"
```

---

## Phase 2: Fibery Ingestion Pipeline

### Task 2.1: Create FiberyEntity EF Core entity

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\FiberyEntity.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Fibery\FiberyEntityTests.cs`

**Step 0: Preflight**

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\FiberyEntity.cs -ErrorAction Stop
```

Expected: False (does not exist yet)

**Step 1: Write failing test**

```csharp
namespace Scripts.Tests.Fibery;

public sealed class FiberyEntityTests
{
    [Test]
    public void FiberyEntity_HasRequired_Properties()
    {
        var props = typeof(CSharpScripts.Data.Entities.FiberyEntity)
            .GetProperties().Select(p => p.Name).ToList();

        props.Should().Contain("Id");
        props.Should().Contain("FiberyId");
        props.Should().Contain("EntityType");
        props.Should().Contain("RawData");
        props.Should().Contain("ImportedAt");
        props.Should().Contain("SourcePath");
    }

    [Test]
    public void FiberyEntity_Id_IsGuid()
    {
        typeof(CSharpScripts.Data.Entities.FiberyEntity)
            .GetProperty("Id")!.PropertyType.Should().Be(typeof(Guid));
    }
}
```

**Step 2: Read-back**

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Fibery\FiberyEntityTests.cs' -ErrorAction Stop
```

Expected: True

**Step 3: Run — confirm RED**

```powershell
$out = dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "FiberyEntityTests" 2>&1
Write-Host $out
if ($out -match "error CS0246") { Write-Host "CONFIRMED RED — type not found" }
```

Expected: FAIL — `FiberyEntity` type not found.

**Step 3.5: Assess**

Type does not exist. Proceed to create entity.

**Step 4: Write minimal implementation**

```csharp
namespace CSharpScripts.Data.Entities;

internal sealed class FiberyEntity
{
    public Guid Id { get; init; }
    public required string FiberyId { get; init; }
    public required string EntityType { get; init; }
    public required JsonDocument RawData { get; init; }
    public DateTimeOffset ImportedAt { get; init; }
    public required string SourcePath { get; init; }
}
```

Verify:

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\FiberyEntity.cs -ErrorAction Stop
```

Expected: True

**Step 5: Run — confirm GREEN**

```powershell
$build = dotnet build C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
if ($LASTEXITCODE -ne 0) { throw "Build failed: $build" }
$test = dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "FiberyEntityTests" 2>&1
Write-Host $test
if ($test -notmatch "Passed") { throw "Tests failed" }
```

Expected: 2 passed, 0 failed.

**Step 6: Commit**

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Entities/FiberyEntity.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/Fibery/FiberyEntityTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(fibery): add FiberyEntity EF Core entity"
```

---

### Task 2.2: Create FiberyEntityConfiguration and register DbSet

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\FiberyEntityConfiguration.cs`
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Fibery\FiberyEntityConfigurationTests.cs`

**Step 0: Preflight**

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\FiberyEntityConfiguration.cs -ErrorAction Stop
Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs -Pattern 'FiberyEntity'
```

Expected: False, 0 matches.

**Step 1: Write failing test**

```csharp
namespace Scripts.Tests.Fibery;

public sealed class FiberyEntityConfigurationTests
{
    [Test]
    public async Task FiberyEntity_HasCorrectTableName()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(CSharpScripts.Data.Entities.FiberyEntity));

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("fibery_entities");
    }

    [Test]
    public async Task FiberyEntity_FiberyId_HasUniqueIndex()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(CSharpScripts.Data.Entities.FiberyEntity));
        var indexes = entityType!.GetIndexes().ToList();

        indexes.Should().Contain(i =>
            i.Properties.Any(p => p.Name == "FiberyId") && i.IsUnique);
    }

    [Test]
    public async Task FiberyEntity_RawData_IsJsonb()
    {
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ScriptsDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(CSharpScripts.Data.Entities.FiberyEntity));
        var prop = entityType!.FindProperty("RawData");

        prop.Should().NotBeNull();
        prop!.GetColumnType().Should().Be("jsonb");
    }
}
```

**Step 2: Read-back**

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Fibery\FiberyEntityConfigurationTests.cs' -ErrorAction Stop
```

Expected: True

**Step 3: Run — confirm RED**

```powershell
$out = dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "FiberyEntityConfigurationTests" 2>&1
Write-Host $out
```

Expected: FAIL — entity type not found (not in context model).

**Step 3.5: Assess**

No configuration or DbSet registered. Proceed.

**Step 4: Write minimal implementation**

`FiberyEntityConfiguration.cs`:
```csharp
namespace CSharpScripts.Data.Configuration;

internal sealed class FiberyEntityConfiguration : IEntityTypeConfiguration<FiberyEntity>
{
    public void Configure(EntityTypeBuilder<FiberyEntity> b)
    {
        b.ToTable("fibery_entities");
        b.HasKey(static e => e.Id);
        b.Property(static e => e.FiberyId).HasMaxLength(255).IsRequired();
        b.Property(static e => e.EntityType).HasMaxLength(100).IsRequired();
        b.Property(static e => e.RawData).HasColumnType("jsonb").IsRequired();
        b.Property(static e => e.ImportedAt).IsRequired();
        b.Property(static e => e.SourcePath).IsRequired();
        b.HasIndex(static e => e.FiberyId).IsUnique();
        b.HasIndex(static e => e.EntityType);
    }
}
```

Add to `ScriptsDbContext.cs` after `SourceRecords` line:
```csharp
public DbSet<FiberyEntity> FiberyEntities => Set<FiberyEntity>();
```

Verify:

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\FiberyEntityConfiguration.cs -ErrorAction Stop
$match = Select-String -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs -Pattern 'FiberyEntities'
if (-not $match) { throw "DbSet not found in ScriptsDbContext" }
```

**Step 5: Run — confirm GREEN**

```powershell
$build = dotnet build C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
if ($LASTEXITCODE -ne 0) { throw "Build failed: $build" }
$test = dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "FiberyEntityConfigurationTests" 2>&1
Write-Host $test
if ($test -notmatch "Passed") { throw "Tests failed" }
```

Expected: 3 passed, 0 failed.

**Step 6: Commit**

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Configuration/FiberyEntityConfiguration.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/ScriptsDbContext.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/Fibery/FiberyEntityConfigurationTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(fibery): add FiberyEntityConfiguration and DbSet"
```

---

### Task 2.3: Generate and apply FiberyEntities migration

**Step 0: Preflight**

```powershell
$env:PGCONNSTR -ErrorAction Stop
docker ps | Select-String 'postgres' -ErrorAction Stop
```

**Step 3: Generate migration**

```powershell
$out = dotnet ef migrations add AddFiberyEntities `
    --project C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj `
    --output-dir src\Data\Migrations 2>&1
Write-Host $out
if ($out -notmatch "Done") { throw "Migration generation failed" }
```

**Apply migration:**

```powershell
$out = dotnet ef database update `
    --project C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj 2>&1
Write-Host $out
if ($LASTEXITCODE -ne 0) { throw "Migration apply failed" }
```

**Verify table exists:**

```powershell
$out = docker exec postgres psql -U postgres -d scripts -c "\d fibery_entities" 2>&1
Write-Host $out
if ($out -notmatch "fibery_id") { throw "Table not created correctly" }
```

**Step 6: Commit**

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Migrations/
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(fibery): add AddFiberyEntities migration"
```

---

### Task 2.4: Create FiberyIngestionService

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\Fibery\FiberyIngestionService.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Fibery\FiberyIngestionServiceTests.cs`

**Step 0: Preflight**

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Fibery -ErrorAction Stop
```

Expected: False (directory does not exist)

```powershell
New-Item -ItemType Directory -Force -Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\Fibery -ErrorAction Stop
```

**Step 1: Write failing test**

```csharp
namespace Scripts.Tests.Fibery;

public sealed class FiberyIngestionServiceTests
{
    [Test]
    public async Task IngestAsync_WithValidDirectory_ReturnsResult()
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR")
            ?? throw new InvalidOperationException("PGCONNSTR not set");
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        var factory = new TestFiberyContextFactory(options);
        var service = new FiberyIngestionService(factory);

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(
            Path.Combine(tempDir, "test.md"),
            "# Test\nfibery-id: test-001",
            Encoding.UTF8);

        var result = await service.IngestAsync(tempDir, CancellationToken.None);

        result.Should().NotBeNull();
        result.FilesProcessed.Should().BeGreaterThan(0);

        Directory.Delete(tempDir, recursive: true);
    }

    [Test]
    public async Task IngestAsync_IsIdempotent()
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR")
            ?? throw new InvalidOperationException("PGCONNSTR not set");
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        var factory = new TestFiberyContextFactory(options);
        var service = new FiberyIngestionService(factory);

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(
            Path.Combine(tempDir, "entity.md"),
            "# Entity\nfibery-id: idem-001",
            Encoding.UTF8);

        var result1 = await service.IngestAsync(tempDir, CancellationToken.None);
        var result2 = await service.IngestAsync(tempDir, CancellationToken.None);

        result1.FilesProcessed.Should().Be(result2.FilesProcessed);
        result2.Errors.Should().Be(0);

        Directory.Delete(tempDir, recursive: true);
    }
}

internal sealed class TestFiberyContextFactory(DbContextOptions<ScriptsDbContext> options)
    : IDbContextFactory<ScriptsDbContext>
{
    public ScriptsDbContext CreateDbContext() => new(options);
}
```

**Step 2: Read-back**

```powershell
Test-Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Fibery\FiberyIngestionServiceTests.cs' -ErrorAction Stop
```

**Step 3: Run — confirm RED**

```powershell
$out = dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "FiberyIngestionServiceTests" 2>&1
Write-Host $out
if ($out -match "error CS0246") { Write-Host "CONFIRMED RED — FiberyIngestionService not found" }
```

**Step 3.5: Assess**

Service does not exist. Proceed.

**Step 4: Write minimal implementation**

```csharp
namespace CSharpScripts.Data.Fibery;

internal sealed class FiberyIngestionService(IDbContextFactory<ScriptsDbContext> contextFactory)
{
    public async Task<IngestionResult> IngestAsync(string rootPath, CancellationToken ct)
    {
        var files = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories)
            .Where(static f => f.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                            || f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        int processed = 0, inserted = 0, updated = 0, errors = 0;

        foreach (var file in files)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file, Encoding.UTF8, ct);
                var fiberyId = ExtractFiberyId(content, file);
                var entityType = DetermineEntityType(file, rootPath);
                var relativePath = Path.GetRelativePath(rootPath, file);
                var rawData = JsonDocument.Parse(JsonSerializer.Serialize(new { content, path = relativePath }));

                await using var context = await contextFactory.CreateDbContextAsync(ct);

                var existing = await context.FiberyEntities
                    .FirstOrDefaultAsync(e => e.FiberyId == fiberyId, ct);

                if (existing is not null)
                {
                    await context.FiberyEntities
                        .Where(e => e.FiberyId == fiberyId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(e => e.RawData, rawData)
                            .SetProperty(e => e.ImportedAt, DateTimeOffset.UtcNow)
                            .SetProperty(e => e.SourcePath, relativePath),
                            ct);
                    updated++;
                }
                else
                {
                    var entity = new Entities.FiberyEntity
                    {
                        Id = Guid.NewGuid(),
                        FiberyId = fiberyId,
                        EntityType = entityType,
                        RawData = rawData,
                        ImportedAt = DateTimeOffset.UtcNow,
                        SourcePath = relativePath
                    };
                    context.FiberyEntities.Add(entity);
                    await context.SaveChangesAsync(ct);
                    inserted++;
                }

                processed++;
            }
            catch (Exception)
            {
                errors++;
            }
        }

        return new IngestionResult(processed, inserted, updated, errors);
    }

    private static string ExtractFiberyId(string content, string filePath)
    {
        var match = Regex.Match(content, @"fibery-id:\s*([a-zA-Z0-9\-]+)");
        return match.Success ? match.Groups[1].Value : Path.GetFileNameWithoutExtension(filePath);
    }

    private static string DetermineEntityType(string filePath, string rootPath)
    {
        var relative = Path.GetRelativePath(rootPath, filePath);
        var parts = relative.Split(Path.DirectorySeparatorChar);
        return parts.Length >= 2 ? parts[0] : "Unknown";
    }
}

internal sealed record IngestionResult(
    int FilesProcessed,
    int RecordsInserted,
    int RecordsUpdated,
    int Errors);
```

**Step 5: Run — confirm GREEN**

```powershell
$build = dotnet build C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
if ($LASTEXITCODE -ne 0) { throw "Build failed: $build" }
$test = dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx --filter "FiberyIngestionServiceTests" 2>&1
Write-Host $test
if ($test -notmatch "Passed") { throw "Tests failed" }
```

Expected: 2 passed, 0 failed.

**Step 6: Commit**

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Fibery/FiberyIngestionService.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/Fibery/FiberyIngestionServiceTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(fibery): add FiberyIngestionService with idempotent upsert"
```

---

## Phase 3: Full Build and Test Verification

### Task 3.1: Full suite validation

**Step 0: Preflight**

```powershell
$env:PGCONNSTR -ErrorAction Stop
```

**Step 3: Run full build and tests**

```powershell
$restore = dotnet restore C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
Write-Host $restore
if ($LASTEXITCODE -ne 0) { throw "Restore failed" }

$build = dotnet build C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
Write-Host $build
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

$test = dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
Write-Host $test
if ($LASTEXITCODE -ne 0) { throw "Tests failed" }

$passCount = ($test | Select-String -Pattern 'passed').Line
Write-Host "Result: $passCount"
```

Expected: All tests passing, zero failures, zero build warnings.

**Step 6: Commit (if uncommitted changes)**

```powershell
$status = git -C C:\Users\Lance\Dev\Scripts status --porcelain
if ($status) {
    git -C C:\Users\Lance\Dev\Scripts add -A
    git -C C:\Users\Lance\Dev\Scripts commit -m "chore: full validation — build clean, all tests green"
}
```

---

## Checkpoint: All Phases Complete

- [ ] T1-13: LanguageIdentifier.cs compiles clean (Phase 1)
- [ ] FiberyEntity exists and is configured (Task 2.1, 2.2)
- [ ] fibery_entities table exists in PostgreSQL (Task 2.3)
- [ ] FiberyIngestionService passes idempotency test (Task 2.4)
- [ ] Full suite: 170+ passing, 0 failing (Phase 3)
- [ ] `dotnet build` clean with zero warnings

If any checkpoint fails, do NOT proceed. Diagnose and fix before continuing.
