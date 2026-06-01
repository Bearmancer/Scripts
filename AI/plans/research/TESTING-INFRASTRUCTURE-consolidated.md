# Testing Infrastructure & Integration Tests — Consolidated Research

**Consolidated from:** 20260522-t1-15-testcontainers-research.md, 20260525-deep-dive-transaction-rollback-safety.md, 20260525-efcore-async-thread-safety.md, 20260525-integration-testing-parallelization-optimizations.md, 20260525-native-database-testing-research.md, 20260525-npgsql-connection-pooling-nuances.md, angle-1-testcontainers.md, angle-5-test-isolation.md

---

## 1. Current State — Gap Analysis

### 1.1 No Test Project Exists

| Artifact               | Expected Path                                     | Exists? |
| ---------------------- | ------------------------------------------------- | ------- |
| Test project (.csproj) | `csharp/tests/Scripts.Tests/Scripts.Tests.csproj` | **No**  |
| Test directory         | `csharp/tests/`                                   | **No**  |
| Solution file (.slnx)  | `csharp/Scripts.slnx`                             | **No**  |
| SmokeTests.cs          | `csharp/tests/CSharpScripts.Tests/SmokeTests.cs`  | **No**  |

### 1.2 No Test NuGet Packages Referenced

| Package                     | Status    | Requires Version          |
| --------------------------- | --------- | ------------------------- |
| `Testcontainers.PostgreSql` | ❌ Missing | `3.10.0` (per T2-00 plan) |
| `TUnit`                     | ❌ Missing | `0.9.0` (per T2-00 plan)  |
| `FluentAssertions`          | ❌ Missing | `7.0.0` (per T2-00 plan)  |

---

## 2. Recommended Testing Strategy

### 2.1 Native Database Testing (Recommended)

**Approach:** Use a single, persistent local PostgreSQL instance with transactional rollback for test isolation.

**Advantages:**
- Eliminates Testcontainers overhead (2-5 seconds per container startup)
- Enables aggressive test parallelism (TUnit's default)
- Sub-millisecond transaction rollback vs 2-5 second container spin-up
- Simpler infrastructure (no Docker socket contention)

**Disadvantages:**
- Requires careful transaction management
- Cannot test database-level features that require separate connections (e.g., advisory locks)

### 2.2 Testcontainers (Alternative)

**Approach:** Spin up a single shared PostgreSQL container per test assembly.

**Advantages:**
- Isolated database instance
- Can test database-level features
- No dependency on local PostgreSQL

**Disadvantages:**
- 2-5 second startup overhead per container
- Resource exhaustion under aggressive parallelism
- Docker socket contention

**Recommendation:** Start with native database testing. Move to Testcontainers if needed for specific database-level testing.

---

## 3. DatabaseFixture Design (Native Database)

### 3.1 Architecture

```
DatabaseFixture : IAsyncDisposable
    ├── NpgsqlConnection (to local PostgreSQL)
    ├── NpgsqlTransaction (per test)
    ├── ScriptsDbContext (enlisted in transaction)
    └── InitializeAsync(): Open connection → begin transaction → migrate
```

### 3.2 Implementation

**Path:** `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Infrastructure\DatabaseFixture.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Npgsql;
using CSharpScripts.Data;

namespace Scripts.Tests.Infrastructure;

public sealed class DatabaseFixture : IAsyncDisposable
{
    private readonly string _connectionString = "Host=localhost;Database=pg_db_tests;Username=lance;Password=lance";
    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;

    public ScriptsDbContext Context { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _connection = new NpgsqlConnection(_connectionString);
        await _connection.OpenAsync();
        _transaction = await _connection.BeginTransactionAsync();

        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(_connection)
            .Options;

        Context = new ScriptsDbContext(options);
        await Context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            await _transaction.RollbackAsync();
        
        if (_connection is not null)
            await _connection.DisposeAsync();
        
        await Context.DisposeAsync();
    }
}
```

### 3.3 Assembly-Level Setup

```csharp
[Before(Assembly)]
public static async Task SetupDatabase()
{
    var connection = new NpgsqlConnection("Host=localhost;Database=pg_db_tests;Username=lance;Password=lance");
    await connection.OpenAsync();
    
    var context = new ScriptsDbContext(
        new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connection)
            .Options
    );
    
    await context.Database.EnsureDeletedAsync();
    await context.Database.MigrateAsync();
    await connection.CloseAsync();
}
```

---

## 4. Concurrency & Thread-Safety

### 4.1 The Golden Rule

> **Neither `DbContext` nor `DbConnection` (including `NpgsqlConnection`) is thread-safe.**

### 4.2 Parallel Isolation Solution

To run parallel tests without thread conflicts:
1. **Do NOT share the `DbConnection` instance** across tests
2. **Give each test class or test method its own separate `DbConnection`** instance opened from the pool
3. **Start a separate transaction** on each connection
4. Connection pooling (handled natively by Npgsql) ensures sub-millisecond overhead

### 4.3 Async/Await Safety

EF Core and Npgsql are fully optimized for asynchronous execution. However, they do not support concurrent asynchronous operations on the same context instance.

**Since each test method instantiates its own `ScriptsDbContext`** (and its own local transaction/connection) inside `DatabaseFixture`, there is no risk of concurrent async access to the same context.

---

## 5. Npgsql Connection Pooling

### 5.1 Default Pool Configuration

| Parameter     | Default | Notes                                |
| ------------- | ------- | ------------------------------------ |
| `MaxPoolSize` | 100     | Sufficient for ~100 concurrent tests |
| `MinPoolSize` | 0       | Lazy initialization                  |
| `Pooling`     | true    | Enabled by default                   |

### 5.2 Connection Pool Exhaustion Risk

If `TUnit` schedules and executes more than 100 tests concurrently, and each test holds its connection open for the duration of the test method, the pool will become exhausted.

**Resolution:**
1. Ensure connections are strictly closed and disposed immediately after each test finishes using `await using`
2. Set `MaxPoolSize=128` (or higher) in the test connection string if parallel thread concurrency is exceptionally high
3. Limit parallel test execution concurrency at the test runner level if needed

---

## 6. Test Project Structure

### 6.1 New Test Project: `Scripts.Tests.csproj`

**Path:** `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Scripts.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <RootNamespace>Scripts.Tests</RootNamespace>
    <AssemblyName>Scripts.Tests</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="TUnit" Version="0.9.0" />
    <PackageReference Include="FluentAssertions" Version="7.0.0" />
    <PackageReference Include="Npgsql" Version="10.0.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\CSharpScripts.csproj" />
  </ItemGroup>
</Project>
```

### 6.2 Required Files

| File                                                           | Purpose                                      | Status |
| -------------------------------------------------------------- | -------------------------------------------- | ------ |
| `csharp/tests/Scripts.Tests/Scripts.Tests.csproj`              | Test project                                 | ❌      |
| `csharp/tests/Scripts.Tests/Infrastructure/DatabaseFixture.cs` | PG connection fixture                        | ❌      |
| `csharp/tests/Scripts.Tests/GlobalUsings.cs`                   | Test-global usings (TUnit, FluentAssertions) | ❌      |
| `csharp/Scripts.slnx`                                          | Solution file referencing all projects       | ❌      |

---

## 7. TUnit Test Pattern Examples

### 7.1 Fixture Bootstrap Test

```csharp
using FluentAssertions;
using TUnit;
using Scripts.Tests.Infrastructure;

namespace Scripts.Tests.E2eTests;

public sealed class FixtureBootstrapTests
{
    [Test]
    public async Task DatabaseFixture_InitializesSuccessfully()
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();

        fixture.Context.Should().NotBeNull();
        var canConnect = await fixture.Context.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }
}
```

### 7.2 Entity Integration Test

```csharp
using FluentAssertions;
using TUnit;
using CSharpScripts.Data.Entities;
using Scripts.Tests.Infrastructure;

namespace Scripts.Tests.Integration;

public sealed class ArtistRepositoryTests
{
    [Test]
    public async Task InsertArtist_PersistsToDatabase()
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
        retrieved!.Id.Should().BeGreaterThan(0);
    }
}
```

---

## 8. Success Criteria

- Running `dotnet test csharp/Scripts.slnx` results in all tests passing
- No `PendingModelChangesWarning` exceptions in the test logs
- Tests complete in < 5 seconds
- All integration tests pass successfully without colliding
- No `NullReferenceException` in `InMemoryTable` or `NpgsqlMigrator`

---

## 9. File Paths

```
Test Project:
  C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Scripts.Tests.csproj

Test Infrastructure:
  C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Infrastructure\DatabaseFixture.cs
  C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\GlobalUsings.cs

Solution:
  C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx
```
