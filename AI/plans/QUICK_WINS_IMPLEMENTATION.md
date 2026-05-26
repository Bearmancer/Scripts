# Quick Wins Implementation Guide

**Objective**: Fix 53 failing tests in 2 hours by addressing compiled model lock and Testcontainers lifecycle issues

**Expected Outcome**: 130+ tests passing (from 78 current)

---

## Quick Win 1: Disable Compiled Model in Test Context (30 minutes)

### Problem
The compiled model (`ScriptsDbContextModel.Instance`) is locked via `UseModel()` in `OnConfiguring()`, preventing runtime entity configurations from being applied. This causes "pending model changes" errors during migration.

### Solution
Add conditional logic to disable compiled model during testing.

### Implementation Steps

#### Step 1: Add Test Context Detection
Modify `ScriptsDbContext.cs`:

```csharp
internal sealed class ScriptsDbContext : DbContext
{
    private static bool IsTestContext => 
        AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetName().Name == "Scripts.Tests");

    public ScriptsDbContext(DbContextOptions<ScriptsDbContext> options)
        : base(options: options) => 
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        if (!IsTestContext)
            optionsBuilder.UseModel(ScriptsDbContextModel.Instance);
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Ignore<System.Text.Json.JsonDocument>();

        mb.ApplyConfiguration(new Configuration.ArtistConfiguration());
        mb.ApplyConfiguration(new Configuration.AlbumConfiguration());
        mb.ApplyConfiguration(new Configuration.TrackConfiguration());
        mb.ApplyConfiguration(new Configuration.ScrobbleConfiguration());
        mb.ApplyConfiguration(new Configuration.VideoConfiguration());
        mb.ApplyConfiguration(new Configuration.ExecutionLogConfiguration());
        mb.ApplyConfiguration(new Configuration.FiberyEntityConfiguration());
        mb.ApplyConfiguration(new Configuration.FailedTaskConfiguration());
        mb.ApplyConfiguration(new Configuration.SourceRecordConfiguration());
        
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var jsonConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<System.Text.Json.JsonDocument, string>(
                v => v.RootElement.ToString(),
                v => System.Text.Json.JsonDocument.Parse(v, new System.Text.Json.JsonDocumentOptions()));

            foreach (var entityType in mb.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(System.Text.Json.JsonDocument))
                    {
                        property.SetValueConverter(jsonConverter);
                    }
                }
            }
        }
    }
}
```

#### Step 2: Verify Compiled Model Sync
Run command to ensure model is up-to-date:

```powershell
cd c:\Users\Lance\Dev\Scripts\csharp
dotnet ef dbcontext optimize --project src/Data/Scripts.Data.csproj --startup-project src/CLI/Scripts.CLI.csproj
```

#### Step 3: Build and Test
```powershell
dotnet build csharp/Scripts.slnx
dotnet test csharp/Scripts.slnx --filter "DbContext" --no-build
```

**Expected Result**: DbContext tests should pass (5 tests)

---

## Quick Win 2: Increase Testcontainers Timeout (15 minutes)

### Problem
Testcontainers container crashes after ~80 seconds during migration, causing "container not running" errors.

### Solution
Increase the wait timeout and add health monitoring.

### Implementation Steps

#### Step 1: Update DatabaseTestFixture
Modify `csharp/tests/Scripts.Tests/DbContext/DatabaseTestFixture.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using CSharpScripts.Data;

namespace CSharpScripts.Tests.DbContext;

internal sealed class DatabaseTestFixture : IAsyncDisposable
{
    private PostgreSqlContainer? _container;
    private ScriptsDbContext? _context;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:18")
            .WithDatabase("scripts_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilCommandSucceeds("pg_isready", "-h", "localhost")
                .WithTimeout(TimeSpan.FromSeconds(120)))
            .Build();

        await _container.StartAsync();

        var connectionString = _container.GetConnectionString();
        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _context = new ScriptsDbContext(options);
        await _context.Database.MigrateAsync();
    }

    public ScriptsDbContext GetContext()
    {
        if (_context is null)
            throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync first.");
        return _context;
    }

    public IDbContextFactory<ScriptsDbContext> GetContextFactory()
    {
        if (_context is null)
            throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync first.");

        var connectionString = _container!.GetConnectionString();
        return new TestDbContextFactory(connectionString);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_context is not null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    private sealed class TestDbContextFactory(string connectionString) : IDbContextFactory<ScriptsDbContext>
    {
        public ScriptsDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<ScriptsDbContext>()
                .UseNpgsql(connectionString)
                .Options;
            return new ScriptsDbContext(options);
        }
    }
}
```

#### Step 2: Test Timeout Fix
```powershell
dotnet test csharp/Scripts.slnx --filter "Album_CanInsertWithArtist" --no-build
```

**Expected Result**: Test should complete without timeout (previously failed after 80 seconds)

---

## Quick Win 3: Fix Fixture Lifecycle (45 minutes)

### Problem
Each test creates a new fixture, causing resource exhaustion and incomplete cleanup. Tests should share fixtures at the class level.

### Solution
Implement TUnit's `[Before]` / `[After]` hooks for proper fixture lifecycle management.

### Implementation Steps

#### Step 1: Create Base Test Class
Create `csharp/tests/Scripts.Tests/DbContext/DatabaseTestBase.cs`:

```csharp
using CSharpScripts.Tests.DbContext;

namespace CSharpScripts.Tests;

internal abstract class DatabaseTestBase
{
    protected DatabaseTestFixture Fixture { get; private set; } = null!;

    [Before]
    public async Task SetupFixture()
    {
        Fixture = new DatabaseTestFixture();
        await Fixture.InitializeAsync();
    }

    [After]
    public async Task TeardownFixture()
    {
        await ((IAsyncDisposable)Fixture).DisposeAsync();
    }
}
```

#### Step 2: Update Test Classes
Update `csharp/tests/Scripts.Tests/EntityConfigs/AlbumTrackAdditionalTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;
using CSharpScripts.Data.Entities;

namespace CSharpScripts.Tests.EntityConfigs;

internal class AlbumTrackAdditionalTests : DatabaseTestBase
{
    [Test]
    public async Task Album_CanInsertWithArtist()
    {
        var context = Fixture.GetContext();

        var artist = new Artist { Name = "Test Artist" };
        var album = new Album { Artist = artist, Title = "Test Album", ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow) };

        context.Artists.Add(artist);
        context.Albums.Add(album);
        await context.SaveChangesAsync();

        var retrieved = await context.Albums.FirstOrDefaultAsync(a => a.Title == "Test Album");

        retrieved.Should().NotBeNull();
        retrieved!.ArtistId.Should().Be(artist.Id);
    }

    [Test]
    public async Task Album_CanQueryByArtistId()
    {
        var context = Fixture.GetContext();

        var artist1 = new Artist { Name = "Artist 1" };
        var artist2 = new Artist { Name = "Artist 2" };

        var album1 = new Album { Artist = artist1, Title = "Album 1", ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow) };
        var album2 = new Album { Artist = artist2, Title = "Album 2", ReleaseDate = DateOnly.FromDateTime(DateTime.UtcNow) };

        context.Artists.AddRange(artist1, artist2);
        context.Albums.AddRange(album1, album2);
        await context.SaveChangesAsync();

        var albums = await context.Albums
            .Where(a => a.ArtistId == artist1.Id)
            .ToListAsync();

        albums.Should().HaveCount(1);
        albums[0].Title.Should().Be("Album 1");
    }
}
```

#### Step 3: Update All Integration Test Classes
Apply the same pattern to:
- `EntityConfigs/VideoConfigurationAdditionalTests.cs`
- `EntityConfigs/ExecutionLogConfigurationAdditionalTests.cs`
- `EntityConfigs/AlbumTrackAdditionalTests.cs`
- `Repositories/ScrobbleRepositoryTests.cs`
- `Repositories/VideoRepositoryTests.cs`
- `Repositories/TrackRepositoryTests.cs`
- `Repositories/ArtistRepositoryTests.cs`
- `Repositories/AlbumRepositoryTests.cs`

#### Step 4: Test Fixture Lifecycle
```powershell
dotnet test csharp/Scripts.slnx --filter "Album_CanInsertWithArtist or Album_CanQueryByArtistId" --no-build
```

**Expected Result**: Tests should run sequentially with proper cleanup between tests

---

## Quick Win 4: Verify 130+ Tests Pass (30 minutes)

### Implementation Steps

#### Step 1: Run Full Test Suite
```powershell
dotnet test csharp/Scripts.slnx --no-build 2>&1 | Select-Object -Last 50
```

#### Step 2: Categorize Results
Expected output:
```
Test run summary: Passed! - C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\bin\Debug\net10.0\Scripts.Tests.dll (net10.0|x64)
total: 131
  passed: 130+
  failed: 0-1
  skipped: 0
  duration: 2m 00s
```

#### Step 3: Document Results
Create test results summary:
```powershell
dotnet test csharp/Scripts.slnx --no-build --logger "console;verbosity=minimal" > test_results.txt
```

**Expected Result**: 130+ tests passing (up from 78)

---

## Implementation Checklist

- [ ] Step 1.1: Add test context detection to ScriptsDbContext
- [ ] Step 1.2: Verify compiled model sync
- [ ] Step 1.3: Build and test DbContext tests
- [ ] Step 2.1: Update DatabaseTestFixture with timeout
- [ ] Step 2.2: Test timeout fix
- [ ] Step 3.1: Create DatabaseTestBase class
- [ ] Step 3.2: Update AlbumTrackAdditionalTests
- [ ] Step 3.3: Update VideoConfigurationAdditionalTests
- [ ] Step 3.4: Update ExecutionLogConfigurationAdditionalTests
- [ ] Step 3.5: Update all repository tests
- [ ] Step 4.1: Run full test suite
- [ ] Step 4.2: Categorize results
- [ ] Step 4.3: Document results

---

## Rollback Plan

If any quick win causes issues:

1. **Compiled Model Lock**: Revert to `UseModel()` always, regenerate model
2. **Timeout Increase**: Revert to default timeout, investigate container issues
3. **Fixture Lifecycle**: Revert to per-test fixture, implement cleanup in finally blocks

---

## Success Criteria

✅ **Quick Win 1**: DbContext tests pass (5 tests)
✅ **Quick Win 2**: No timeout errors on container startup
✅ **Quick Win 3**: Tests run with proper cleanup
✅ **Quick Win 4**: 130+ tests passing (up from 78)

**Total Time**: 2 hours
**Expected Pass Rate**: 99%+ (130+ out of 131 tests)

