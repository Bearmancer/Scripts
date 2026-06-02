# DI Container Wiring Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire all services into the Microsoft DI container in `Program.cs` via a dedicated `ServiceRegistration.cs` extension class in the CLI project.

**Architecture:** `ServiceRegistration.cs` in `Scripts.CLI` exposes a single `AddScriptsServices` extension method that registers `ScriptsDbContext`, all repository implementations, `StateManager`, and `ReleaseProgressCache`. `Program.cs` calls it before building the service provider. No service locator — every dependency is resolved through the container.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Pre-flight

- [ ] **Step 0: Pre-flight validation**

```powershell
# Verify tooling exists
Get-Command pwsh   -ErrorAction Stop
Get-Command dotnet -ErrorAction Stop
Get-Command git    -ErrorAction Stop

# Verify PGCONNSTR is set
if (-not $env:PGCONNSTR) { throw 'PGCONNSTR not set — load .env first' }

# Restore solution
dotnet restore /home/lance/Scripts/csharp/Scripts.slnx -ErrorAction Stop
```

Expected: restore succeeds with 0 errors.

---

## Task 1: Write failing DI resolution tests

**Files:**
- Modify: `csharp/tests/Scripts.Tests/DiWiringTests.cs` (create new)

- [ ] **Step 1: Write the failing tests**

```csharp
// csharp/tests/Scripts.Tests/DiWiringTests.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using TUnit;
using Scripts.Data;

namespace Scripts.Tests;

public class DiWiringTests
{
    [Test]
    public void ServiceProvider_Resolves_IScrobbleRepository()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ScriptsDbContext>(opts =>
            opts.UseNpgsql(Environment.GetEnvironmentVariable("PGCONNSTR")
                ?? throw new InvalidOperationException("PGCONNSTR not set")));
        services.AddScoped<IScrobbleRepository, ScrobbleRepository>();

        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<IScrobbleRepository>();
        repo.Should().NotBeNull();
    }

    [Test]
    public void ServiceProvider_Resolves_IVideoRepository()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ScriptsDbContext>(opts =>
            opts.UseNpgsql(Environment.GetEnvironmentVariable("PGCONNSTR")
                ?? throw new InvalidOperationException("PGCONNSTR not set")));
        services.AddScoped<IVideoRepository, VideoRepository>();

        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<IVideoRepository>();
        repo.Should().NotBeNull();
    }

    [Test]
    public void ServiceProvider_Resolves_StateManager()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ScriptsDbContext>(opts =>
            opts.UseNpgsql(Environment.GetEnvironmentVariable("PGCONNSTR")
                ?? throw new InvalidOperationException("PGCONNSTR not set")));
        services.AddSingleton<StateManager>();

        var provider = services.BuildServiceProvider();
        var sm = provider.GetRequiredService<StateManager>();
        sm.Should().NotBeNull();
    }

    [Test]
    public void ServiceProvider_Resolves_ReleaseProgressCache()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ScriptsDbContext>(opts =>
            opts.UseNpgsql(Environment.GetEnvironmentVariable("PGCONNSTR")
                ?? throw new InvalidOperationException("PGCONNSTR not set")));
        services.AddSingleton<ReleaseProgressCache>();

        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<ReleaseProgressCache>();
        cache.Should().NotBeNull();
    }

    [Test]
    public void AddScriptsServices_RegistersAllServices_WhenEnvVarSet()
    {
        var services = new ServiceCollection();
        services.AddScriptsServices();

        var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IScrobbleRepository>().Should().NotBeNull();
        provider.GetRequiredService<IVideoRepository>().Should().NotBeNull();
        provider.GetRequiredService<StateManager>().Should().NotBeNull();
        provider.GetRequiredService<ReleaseProgressCache>().Should().NotBeNull();
    }
}
```

- [ ] **Step 2: Read-back — verify test file was written**

```powershell
$testFile = '/home/lance/Scripts/csharp/tests\Scripts.Tests\DiWiringTests.cs'
Test-Path $testFile | Should -Be $true
(Get-Content $testFile -Raw) | Should -Match 'AddScriptsServices'
Write-Host "Read-back OK"
```

- [ ] **Step 3: Run tests — confirm RED**

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "DiWiringTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: compile error — `AddScriptsServices` does not exist yet.

- [ ] **Step 3.5: State assessment**

Failure is expected: `ServiceRegistration.cs` and `AddScriptsServices` have not been created yet.

---

## Task 2: Implement ServiceRegistration.cs

**Files:**
- Create: `csharp/src/CLI/ServiceRegistration.cs`

- [ ] **Step 4: Write `ServiceRegistration.cs`**

```csharp
// csharp/src/CLI/ServiceRegistration.cs
namespace Scripts.CLI;

internal static class ServiceRegistration
{
    internal static IServiceCollection AddScriptsServices(this IServiceCollection services)
    {
        var connStr = Environment.GetEnvironmentVariable("PGCONNSTR")
            ?? throw new InvalidOperationException(
                "PGCONNSTR environment variable is not set. Load .env before running.");

        services.AddDbContext<ScriptsDbContext>(opts =>
            opts.UseNpgsql(connStr)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<IScrobbleRepository, ScrobbleRepository>();
        services.AddScoped<IVideoRepository, VideoRepository>();
        services.AddSingleton<StateManager>();
        services.AddSingleton<ReleaseProgressCache>();

        return services;
    }
}
```

- [ ] **Step 5: Read-back — verify `ServiceRegistration.cs` written**

```powershell
$file = '/home/lance/Scripts/csharp/src\CLI\ServiceRegistration.cs'
Test-Path $file | Should -Be $true
(Get-Content $file -Raw) | Should -Match 'AddScriptsServices'
Write-Host "Read-back OK"
```

- [ ] **Step 6: Run tests — confirm GREEN**

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "DiWiringTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: all 5 `DiWiringTests` pass.

---

## Task 3: Update Program.cs to call AddScriptsServices

**Files:**
- Modify: `csharp/src/CLI/Program.cs`

- [ ] **Step 1: Write the failing test (Program.cs integration)**

```csharp
// Add to csharp/tests/Scripts.Tests/DiWiringTests.cs
[Test]
public void ProgramCs_CallsAddScriptsServices()
{
    var programContent = File.ReadAllText(
        @"/home/lance/Scripts/csharp/src\CLI\Program.cs");
    programContent.Should().Contain("AddScriptsServices",
        "Program.cs must delegate registration to ServiceRegistration.AddScriptsServices");
}
```

- [ ] **Step 2: Run test — confirm RED**

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "ProgramCs_CallsAddScriptsServices" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: FAIL — `Program.cs` does not contain `AddScriptsServices` yet.

- [ ] **Step 3: Modify `Program.cs`**

Locate the service registration block in `Program.cs` and replace the manual inline registrations with the extension call:

```csharp
// In Program.cs — inside the builder/services block:
services.AddScriptsServices();
```

Remove any pre-existing inline `AddDbContext`, `AddScoped<IScrobbleRepository>`, etc. that `AddScriptsServices` now covers. Keep all Spectre.Console command registrations untouched.

- [ ] **Step 4: Run test — confirm GREEN**

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "ProgramCs_CallsAddScriptsServices" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: PASS.

- [ ] **Step 5: Full build check**

```powershell
dotnet build /home/lance/Scripts/csharp/Scripts.slnx 2>&1
```

Expected: `Build succeeded. 0 Error(s). 0 Warning(s).`

- [ ] **Step 6: Commit**

```powershell
git -C /home/lance/Scripts add `
    csharp/src/CLI/ServiceRegistration.cs `
    csharp/src/CLI/Program.cs `
    csharp/tests/Scripts.Tests/DiWiringTests.cs
git -C /home/lance/Scripts commit -m "feat(t4-00): wire all services into Microsoft DI container"
```

---

## Acceptance Criteria

- [ ] `ServiceRegistration.cs` exists at `csharp/src/CLI/ServiceRegistration.cs`
- [ ] `Program.cs` calls `services.AddScriptsServices()`
- [ ] All 6 `DiWiringTests` pass
- [ ] `dotnet build csharp/Scripts.slnx` → `0 Error(s). 0 Warning(s).`
- [ ] No connection strings, passwords, or secrets appear in any committed file
