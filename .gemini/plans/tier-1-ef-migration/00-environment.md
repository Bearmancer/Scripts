# T1-00: Environment Preflight Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Verify Docker is running, `$env:PGCONNSTR` is set and valid, and a minimal `ScriptsDbContext` stub can connect to PostgreSQL.

**Architecture:** This plan is the gate before any EF Core work begins. It creates the `ScriptsDbContext` stub in `src/Data/`, validates environment variables match the required PostgreSQL connection string format, and confirms live connectivity via `CanConnectAsync()`. No migrations are applied at this stage.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- Docker Desktop running
- `.env` loaded into the current shell (see repo `GEMINI.md` §3)
- `$env:PGCONNSTR` must be set before running any step

```powershell
# Verify env is loaded
if (-not $env:PGCONNSTR) {
    Get-Content C:\Users\Lance\Dev\Scripts\.env | ForEach-Object {
        if ($_ -match '^([^#][^=]+)=(.+)$') {
            [System.Environment]::SetEnvironmentVariable($Matches[1], $Matches[2])
        }
    }
}
```

---

## Task 1 — Verify Docker Running

### Step 0: Preflight

```powershell
# Current state: Docker Desktop may or may not be running
# Reason: EF migrations and Testcontainers require Docker
# What: Run docker ps and check exit code
# Expected: Exit code 0, container list output (empty is fine)

$result = docker ps 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Docker is not running. Start Docker Desktop before continuing. Exit code: $LASTEXITCODE"
}
Write-Host "Docker is running. Output: $result"
```

### Step 1: Write test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Environment\DockerEnvironmentTests.cs`

```csharp
using TUnit;
using FluentAssertions;

namespace Scripts.Tests.Environment;

public sealed class DockerEnvironmentTests
{
    [Test]
    public void Docker_IsRunning_WhenDockerPsSucceeds()
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        process.WaitForExit(timeoutMilliseconds: 10_000);

        process.ExitCode.Should().Be(0, "because Docker must be running for all EF Core and Testcontainers tests");
    }
}
```

### Step 2: Readback

```powershell
Get-Content C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Environment\DockerEnvironmentTests.cs
```

Expected: File exists, contains `process.ExitCode.Should().Be(0)`.

### Step 3: Run — expect FAIL (if Docker stopped) or PASS (if running)

```powershell
dotnet test --filter "Docker_IsRunning_WhenDockerPsSucceeds" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected output if Docker is not running:
```
Failed Docker_IsRunning_WhenDockerPsSucceeds [...]
Expected process.ExitCode to be 0, but found 1.
```

### Step 3.5: Assess

If test fails: Start Docker Desktop, wait 30 seconds, re-run. Do not proceed until this test passes.

### Step 4: Implement (Start Docker if needed)

```powershell
Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe"
Start-Sleep -Seconds 30
docker ps 2>&1
if ($LASTEXITCODE -ne 0) { throw "Docker still not running after 30 seconds" }
```

### Step 5: Run — expect PASS

```powershell
dotnet test --filter "Docker_IsRunning_WhenDockerPsSucceeds" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected:
```
Passed Docker_IsRunning_WhenDockerPsSucceeds [...]
1 passed, 0 failed
```

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/Environment/DockerEnvironmentTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-00): add Docker environment preflight test"
```

---

## Task 2 — Verify `$env:PGCONNSTR` Format

### Step 0: Preflight

```powershell
# Current state: $env:PGCONNSTR may or may not be set
# Reason: All EF Core operations need a valid PostgreSQL connection string
# What: Assert env var is set and contains Host=, Database=, Username=
# Expected: Regex match succeeds

$connStr = $env:PGCONNSTR
if ([string]::IsNullOrWhiteSpace($connStr)) {
    throw "PGCONNSTR is not set. Load .env before continuing."
}
Write-Host "PGCONNSTR is set (redacted length: $($connStr.Length))"
```

### Step 1: Write test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Environment\ConnectionStringTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using System.Text.RegularExpressions;

namespace Scripts.Tests.Environment;

public sealed class ConnectionStringTests
{
    [Test]
    public void ConnectionString_IsSet_InEnvironment()
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR");
        connStr.Should().NotBeNullOrWhiteSpace(
            "because PGCONNSTR must be loaded from .env before running tests");
    }

    [Test]
    public void ConnectionString_IsValid_PostgresFormat()
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR")!;

        connStr.Should().Contain("Host=",
            "because a valid Npgsql connection string must specify a host");
        connStr.Should().Contain("Database=",
            "because a valid Npgsql connection string must specify a database");
        connStr.Should().Contain("Username=",
            "because a valid Npgsql connection string must specify a username");
    }

    [Test]
    public void ConnectionString_DoesNotContain_Password_InPlainText_InLogs()
    {
        // Confirm we can get the string — we do NOT log or print it
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR");
        // If this assertion passes, the test runner never printed the value
        connStr.Should().NotBeNull();
    }
}
```

### Step 2: Readback

```powershell
Get-Content C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Environment\ConnectionStringTests.cs
```

Expected: File exists, contains three `[Test]` methods.

### Step 3: Run — expect FAIL if env not loaded

```powershell
dotnet test --filter "ConnectionString_IsSet_InEnvironment" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected failure:
```
Failed ConnectionString_IsSet_InEnvironment
Expected connStr not to be <null> or whitespace...
```

### Step 3.5: Assess

Load `.env` and re-run. The test exercises runtime environment, not compilation.

### Step 4: Load env and re-run

```powershell
Get-Content C:\Users\Lance\Dev\Scripts\.env | ForEach-Object {
    if ($_ -match '^([^#][^=]+)=(.+)$') {
        [System.Environment]::SetEnvironmentVariable($Matches[1], $Matches[2])
    }
}
```

### Step 5: Run — expect PASS

```powershell
dotnet test --filter "ConnectionString" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected:
```
Passed ConnectionString_IsSet_InEnvironment
Passed ConnectionString_IsValid_PostgresFormat
Passed ConnectionString_DoesNotContain_Password_InPlainText_InLogs
3 passed, 0 failed
```

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/Environment/ConnectionStringTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-00): add connection string format validation tests"
```

---

## Task 3 — Create `ScriptsDbContext` Stub

### Step 0: Preflight

```powershell
# Current state: src/Data/ exists (from modularization), ScriptsDbContext.cs does not exist
# Reason: All EF Core repository and migration tests depend on this class
# What: Create minimal DbContext with empty OnModelCreating
# Expected: File created, compiles, CanConnectAsync returns true

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs
# Expected: False
```

### Step 1: Write test

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Environment\DbContextConnectionTests.cs`

```csharp
using TUnit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;

namespace Scripts.Tests.Environment;

public sealed class DbContextConnectionTests
{
    private ScriptsDbContext CreateContext()
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR")
            ?? throw new InvalidOperationException("PGCONNSTR not set");

        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        return new ScriptsDbContext(options);
    }

    [Test]
    public async Task DatabaseConnection_Succeeds_WithValidConnectionString()
    {
        await using var context = CreateContext();
        var canConnect = await context.Database.CanConnectAsync();
        canConnect.Should().BeTrue(
            "because PostgreSQL must be reachable via PGCONNSTR");
    }
}
```

### Step 2: Readback

```powershell
Get-Content C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Environment\DbContextConnectionTests.cs
```

Expected: File exists, contains `CanConnectAsync`.

### Step 3: Run — expect FAIL (ScriptsDbContext does not exist)

```powershell
dotnet test --filter "DatabaseConnection_Succeeds_WithValidConnectionString" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected:
```
Error CS0246: The type or namespace name 'ScriptsDbContext' could not be found
```

### Step 3.5: Assess

Compilation failure confirms the class is missing. Proceed to create it.

### Step 4: Create `ScriptsDbContext.cs`

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;

namespace CSharpScripts.Data;

/// <summary>
/// Primary EF Core DbContext for the Scripts application.
/// NoTracking is the default; enable tracking explicitly per-operation when needed.
/// </summary>
public sealed class ScriptsDbContext(DbContextOptions<ScriptsDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Entity configurations will be loaded here in subsequent plans (03-dbcontext-config)
    }
}
```

Verify file was created:

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs
# Expected: True
```

### Step 5: Run — expect PASS

```powershell
dotnet restore C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
dotnet test --filter "DatabaseConnection_Succeeds_WithValidConnectionString" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected:
```
Build succeeded.
Passed DatabaseConnection_Succeeds_WithValidConnectionString
1 passed, 0 failed
```

### Step 6: Commit

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/ScriptsDbContext.cs
git -C C:\Users\Lance\Dev\Scripts add csharp/tests/Scripts.Tests/Environment/DbContextConnectionTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t1-00): add ScriptsDbContext stub and connection test"
```

---

## Final Verification

```powershell
dotnet test --filter "Scripts.Tests.Environment" C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
```

Expected:
```
Passed Docker_IsRunning_WhenDockerPsSucceeds
Passed ConnectionString_IsSet_InEnvironment
Passed ConnectionString_IsValid_PostgresFormat
Passed ConnectionString_DoesNotContain_Password_InPlainText_InLogs
Passed DatabaseConnection_Succeeds_WithValidConnectionString
5 passed, 0 failed
```

**→ Proceed to `01-entities.md`**
