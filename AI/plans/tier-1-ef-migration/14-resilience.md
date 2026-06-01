# T1-14: Resilience Policies Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add EF Core database retry policy (`EnableRetryOnFailure`) to both DbContext registration paths, add `RetryExhaustedException` to `Core/Resilience.cs`, and delete the legacy `Infrastructure/Resilience.cs` duplicate.

**Architecture:** Npgsql's `EnableRetryOnFailure` is added to `DbContextRegistration.AddScriptsDbContext()` and `ScriptsDbContextFactory.CreateDbContext()` with `maxRetryCount: 3` and `maxRetryDelay: 30s`. The legacy `Infrastructure/Resilience.cs` (199 lines, uses Console logging, creates new pipelines per call, lacks circuit breaker) is backed up and deleted. `RetryExhaustedException` is added to `Core/Resilience.cs` for better error diagnostics when all retries are exhausted.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / Polly 8.6.6 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- T1-13 completed (Lingua migration green)
- `Scripts.Tests` project exists
- `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Resilience.cs` exists (271 lines, Polly v8, circuit breaker)
- `C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\Resilience.cs` exists (199 lines, legacy duplicate)
- `C:\Users\Lance\Dev\Scripts\csharp\src\Data\DbContextRegistration.cs` exists (14 lines)
- `C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContextFactory.cs` exists (17 lines)

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Core\Resilience.cs
# Expected: True

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\Resilience.cs
# Expected: True

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Data\DbContextRegistration.cs
# Expected: True
```

---

## Task 1 — Add EnableRetryOnFailure to DbContextRegistration and Factory

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\DbContextRegistration.cs`
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContextFactory.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\DbRetryPolicyTests.cs`

### Step 0: Preflight

```powershell
# Current state: Both DbContext creation paths use bare .UseNpgsql(connectionString) with NO retry.
# Reason: Transient PostgreSQL errors (connection failures, deadlocks) need automatic retry.
# What: Add .EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: 30s) to both paths.
# Expected: Npgsql retry configured, build passes.

Get-Content C:\Users\Lance\Dev\Scripts\csharp\src\Data\DbContextRegistration.cs
# Expected: opts.UseNpgsql(connectionString: connStr) — NO retry

Get-Content C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContextFactory.cs
# Expected: optionsBuilder.UseNpgsql(connectionString: connStr) — NO retry

New-Item -ItemType Directory -Force -Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience'
```

### Step 1: Write test

```csharp
// C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\DbRetryPolicyTests.cs
using System.Text.RegularExpressions;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Resilience;

public sealed class DbRetryPolicyTests
{
    [Test]
    public async Task DbContextRegistration_Contains_EnableRetryOnFailure()
    {
        var filePath = @"C:\Users\Lance\Dev\Scripts\csharp\src\Data\DbContextRegistration.cs";
        var content = await File.ReadAllTextAsync(filePath);

        content.Should().Contain("EnableRetryOnFailure",
            "because Npgsql transient errors must be retried automatically"
        );
    }

    [Test]
    public async Task DbContextRegistration_RetryCount_Is_Three()
    {
        var filePath = @"C:\Users\Lance\Dev\Scripts\csharp\src\Data\DbContextRegistration.cs";
        var content = await File.ReadAllTextAsync(filePath);

        var match = Regex.Match(content, @"maxRetryCount:\s*(\d+)");
        match.Success.Should().BeTrue("because maxRetryCount must be specified");

        var count = int.Parse(match.Groups[1].Value);
        count.Should().Be(3, "because 3 retries is the standard for transient DB errors");
    }

    [Test]
    public async Task DbContextFactory_Contains_EnableRetryOnFailure()
    {
        var filePath = @"C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContextFactory.cs";
        var content = await File.ReadAllTextAsync(filePath);

        content.Should().Contain("EnableRetryOnFailure",
            "because dotnet ef commands also need retry on transient errors"
        );
    }

    [Test]
    public async Task DbContextFactory_RetryDelay_Is_Thirty_Seconds()
    {
        var filePath = @"C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContextFactory.cs";
        var content = await File.ReadAllTextAsync(filePath);

        var match = Regex.Match(content, @"maxRetryDelay:\s*TimeSpan\.FromSeconds\(\s*(\d+)");
        match.Success.Should().BeTrue("because maxRetryDelay must be specified");

        var seconds = int.Parse(match.Groups[1].Value);
        seconds.Should().Be(30, "because 30s max retry delay is sufficient for transient DB issues");
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\DbRetryPolicyTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect RED — no EnableRetryOnFailure yet)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "DbRetryPolicyTests" 2>&1
```

Expected: RED — `DbContextRegistration_Contains_EnableRetryOnFailure` fails. `DbContextFactory_Contains_EnableRetryOnFailure` fails.

### Step 4: Assess

Two files need modification:
1. `DbContextRegistration.cs` line 12: change `opts.UseNpgsql(connectionString: connStr)` to include `EnableRetryOnFailure`
2. `ScriptsDbContextFactory.cs` line 14: change `optionsBuilder.UseNpgsql(connectionString: connStr)` to include `EnableRetryOnFailure`

### Step 5: Implement

**Edit `C:\Users\Lance\Dev\Scripts\csharp\src\Data\DbContextRegistration.cs` line 12:**

OLD:
```csharp
		return services.AddDbContext<ScriptsDbContext>(opts => opts.UseNpgsql(connectionString: connStr));
```

NEW:
```csharp
		return services.AddDbContext<ScriptsDbContext>(opts => opts.UseNpgsql(
			connectionString: connStr,
			npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
				maxRetryCount: 3,
				maxRetryDelay: TimeSpan.FromSeconds(seconds: 30),
				errorCodesToAdd: null
			)));
```

**Edit `C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContextFactory.cs` line 14:**

OLD:
```csharp
		optionsBuilder.UseNpgsql(connectionString: connStr);
```

NEW:
```csharp
		optionsBuilder.UseNpgsql(
			connectionString: connStr,
			npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
				maxRetryCount: 3,
				maxRetryDelay: TimeSpan.FromSeconds(seconds: 30),
				errorCodesToAdd: null
			));
```

**Verify build:**

```powershell
dotnet build C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj 2>&1
```

Expected: Build succeeded with 0 errors.

### Step 6: Run test (expect GREEN)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "DbRetryPolicyTests" 2>&1
```

Expected: GREEN — all 4 tests pass.

### Step 7: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\src\Data\DbContextRegistration.cs
git add C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContextFactory.cs
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\DbRetryPolicyTests.cs
git commit -m "feat(t1-14): add enable retry on failure to npgsql db context paths"
```

---

## Task 2 — Delete Legacy Infrastructure/Resilience.cs

**Files:**
- Backup: `C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\Resilience.cs` → `Resilience.cs.bak.YYYYMMDD_HHmmss`
- Delete: `C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\Resilience.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\InfrastructureResilienceDeletedTests.cs`

### Step 0: Preflight

```powershell
# Current state: Infrastructure/Resilience.cs (199 lines) is a legacy duplicate of Core/Resilience.cs.
# It uses Console logging instead of Serilog, creates new pipelines per call, lacks circuit breaker.
# No callers import CSharpScripts.Infrastructure — dead code.
# Reason: Eliminate duplicate code, enforce single source of truth for resilience.
# What: Backup to .bak file, delete, verify build still passes.
# Expected: File deleted, build succeeds, tests pass.

$file = 'C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\Resilience.cs'
Test-Path $file
# Expected: True

(Get-Content $file).Count
# Expected: 199

# Verify no callers exist
Get-ChildItem C:\Users\Lance\Dev\Scripts\csharp\src\*.cs -Recurse | Select-String "CSharpScripts.Infrastructure" -SimpleMatch
# Expected: (no output — no callers)
```

### Step 1: Write test

```csharp
// C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\InfrastructureResilienceDeletedTests.cs
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Resilience;

public sealed class InfrastructureResilienceDeletedTests
{
    [Test]
    public void Infrastructure_Resilience_cs_Does_Not_Exist()
    {
        var filePath = @"C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\Resilience.cs";

        File.Exists(filePath).Should().BeFalse(
            "because Infrastructure/Resilience.cs is a legacy duplicate superseded by Core/Resilience.cs"
        );
    }

    [Test]
    public void Infrastructure_Resilience_Bak_Exists()
    {
        var bakDir = @"C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure";
        var bakFiles = Directory.GetFiles(bakDir, "Resilience.cs.bak.*");

        bakFiles.Should().NotBeEmpty(
            "because a timestamped .bak file must be created before deletion"
        );
    }

    [Test]
    public void Build_Succeeds_After_Infrastructure_Resilience_Deletion()
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build C:\\Users\\Lance\\Dev\\Scripts\\csharp\\CSharpScripts.csproj --no-restore",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        process.ExitCode.Should().Be(0,
            $"because deleting dead code must not break the build.\nStdOut: {output}\nStdErr: {error}"
        );
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\InfrastructureResilienceDeletedTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect RED — Infrastructure/Resilience.cs still exists)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "InfrastructureResilienceDeletedTests" 2>&1
```

Expected: RED. `Infrastructure_Resilience_cs_Does_Not_Exist` fails — file still exists.

### Step 4: Assess

Must backup and delete the file. The backup timestamp format is `yyyyMMdd_HHmmss`.

### Step 5: Implement

```powershell
# Create timestamped backup
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$source = 'C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\Resilience.cs'
$backup = "C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\Resilience.cs.bak.$timestamp"

Copy-Item -Path $source -Destination $backup -ErrorAction Stop
Test-Path $backup
# Expected: True

# Delete the original
Remove-Item -Path $source -ErrorAction Stop
Test-Path $source
# Expected: False

# Verify build still passes
dotnet build C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj --no-restore 2>&1
```

Expected: Backup created. Original deleted. Build succeeded with 0 errors.

### Step 6: Run test (expect GREEN)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "InfrastructureResilienceDeletedTests" 2>&1
```

Expected: GREEN — all 3 tests pass:
- `Infrastructure_Resilience_cs_Does_Not_Exist`: PASS
- `Infrastructure_Resilience_Bak_Exists`: PASS
- `Build_Succeeds_After_Infrastructure_Resilience_Deletion`: PASS

### Step 7: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\Resilience.cs.bak.*
git rm C:\Users\Lance\Dev\Scripts\csharp\src\Infrastructure\Resilience.cs
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\InfrastructureResilienceDeletedTests.cs
git commit -m "feat(t1-14): delete legacy infrastructure resilience duplicate after backup"
```

---

## Task 3 — Add RetryExhaustedException to Core/Resilience.cs

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Resilience.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\RetryExhaustedExceptionTests.cs`

### Step 0: Preflight

```powershell
# Current state: Core/Resilience.cs has OnRetry callbacks that log and may throw DailyQuotaExceededException.
# When all retries are exhausted, Polly returns the last Outcome.Exception. No dedicated exception type exists
# for "all retries exhausted" diagnostics.
# Reason: The legacy Infrastructure/Resilience.cs defined RetryExhaustedException at line 186. Core lacks it.
# What: Add RetryExhaustedException class definition to Core/Resilience.cs.
# Expected: New exception type available, build passes.

Get-Content C:\Users\Lance\Dev\Scripts\csharp\src\Core\Resilience.cs | Select-String "RetryExhaustedException"
# Expected: (no output — exception not defined in Core)
```

### Step 1: Write test

```csharp
// C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\RetryExhaustedExceptionTests.cs
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Resilience;

public sealed class RetryExhaustedExceptionTests
{
    [Test]
    public void RetryExhaustedException_Is_Defined()
    {
        var exType = Type.GetType("CSharpScripts.Core.RetryExhaustedException, tools");

        exType.Should().NotBeNull(
            "because Core/Resilience.cs must define RetryExhaustedException"
        );
    }

    [Test]
    public void RetryExhaustedException_Is_Exception_Subclass()
    {
        var exType = Type.GetType("CSharpScripts.Core.RetryExhaustedException, tools");

        exType.Should().NotBeNull();
        exType!.IsSubclassOf(typeof(Exception)).Should().BeTrue(
            "because RetryExhaustedException must inherit from Exception"
        );
    }

    [Test]
    public void RetryExhaustedException_Constructor_Accepts_Operation_And_InnerException()
    {
        var exType = Type.GetType("CSharpScripts.Core.RetryExhaustedException, tools");

        exType.Should().NotBeNull();

        var constructor = exType!.GetConstructor([typeof(string), typeof(Exception)]);
        constructor.Should().NotBeNull(
            "because the exception must accept operation name and inner exception"
        );

        var inner = new InvalidOperationException("test inner");
        var instance = constructor!.Invoke(["TestOperation", inner]) as Exception;

        instance.Should().NotBeNull();
        instance!.Message.Should().Contain("TestOperation");
        instance!.InnerException.Should().Be(inner);
    }

    [Test]
    public void RetryExhaustedException_Constructor_Accepts_Operation_Only()
    {
        var exType = Type.GetType("CSharpScripts.Core.RetryExhaustedException, tools");

        exType.Should().NotBeNull();

        var constructor = exType!.GetConstructor([typeof(string)]);
        constructor.Should().NotBeNull(
            "because the exception must accept operation name without inner exception"
        );

        var instance = constructor!.Invoke(["TestOperationOnly"]) as Exception;

        instance.Should().NotBeNull();
        instance!.Message.Should().Contain("TestOperationOnly");
        instance!.InnerException.Should().BeNull();
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\RetryExhaustedExceptionTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect RED — RetryExhaustedException not defined)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "RetryExhaustedExceptionTests" 2>&1
```

Expected: RED. `RetryExhaustedException_Is_Defined` returns null — type not found.

### Step 4: Assess

Add a `RetryExhaustedException` class at the end of `Core/Resilience.cs` (after line 271). It should inherit `Exception`, have constructors with `(string operation)` and `(string operation, Exception innerException)`, and produce a message like `"All retry attempts exhausted for operation: {operation}"`.

### Step 5: Implement

Append to `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Resilience.cs` after the last class member (after line 271):

```csharp

public sealed class RetryExhaustedException : Exception
{
	public RetryExhaustedException(string operation)
		: base(message: $"All retry attempts exhausted for operation: {operation}")
	{
	}

	public RetryExhaustedException(string operation, Exception innerException)
		: base(
			message: $"All retry attempts exhausted for operation: {operation}",
			innerException: innerException
		)
	{
	}
}
```

**Note:** The existing `Resilience` class is `internal static`. The `RetryExhaustedException` is `public sealed` so it can be caught by external callers.

**Verify build:**

```powershell
dotnet build C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj 2>&1
```

Expected: Build succeeded with 0 errors.

### Step 6: Run test (expect GREEN)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "RetryExhaustedExceptionTests" 2>&1
```

Expected: GREEN — all 4 tests pass.

### Step 7: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\src\Core\Resilience.cs
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\RetryExhaustedExceptionTests.cs
git commit -m "feat(t1-14): add retry exhausted exception to core resilience"
```

---

## Task 4 — Polly v8 Retry and Circuit Breaker Behavior Tests

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\PollyBehaviorTests.cs`

### Step 0: Preflight

```powershell
# Current state: Core/Resilience.cs has retry pipeline with 10 attempts, exponential backoff, jitter,
# and circuit breaker (FailureRatio 0.5, SamplingDuration 3min, BreakDuration 30s).
# These are tested only manually. Add behavioral tests.
# Reason: Verify retry and circuit breaker work as expected at the Polly level.
# What: Create integration tests using Polly's built-in pipeline testing capability.
# Expected: Tests pass, confirming retry tries 3+ times and circuit breaker opens after failures.

$testFile = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\PollyBehaviorTests.cs'
Test-Path $testFile
# Expected: False
```

### Step 1: Write tests

```csharp
// C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\PollyBehaviorTests.cs
using FluentAssertions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using TUnit;

namespace Scripts.Tests.Resilience;

public sealed class PollyBehaviorTests
{
    [Test]
    public async Task Retry_Pipeline_Retries_Three_Times_On_HttpRequestException()
    {
        var attemptCount = 0;
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(10),
                BackoffType = DelayBackoffType.Constant,
                UseJitter = false,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>(),
                OnRetry = _ =>
                {
                    Interlocked.Increment(ref attemptCount);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        try
        {
            await pipeline.ExecuteAsync(_ =>
            {
                throw new HttpRequestException("transient failure");
            });
        }
        catch (HttpRequestException)
        {
            // expected after all retries exhausted
        }

        attemptCount.Should().Be(3,
            "because the pipeline must retry 3 times before failing"
        );
    }

    [Test]
    public async Task Circuit_Breaker_Opens_After_Three_Consecutive_Failures()
    {
        var circuitStates = new List<CircuitState>();
        var pipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                SamplingDuration = TimeSpan.FromMinutes(1),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromMilliseconds(200),
                OnOpened = args =>
                {
                    circuitStates.Add(CircuitState.Open);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    circuitStates.Add(CircuitState.Closed);
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    circuitStates.Add(CircuitState.HalfOpen);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        // Cause 3 consecutive failures to open the circuit
        for (var i = 0; i < 4; i++)
        {
            try
            {
                await pipeline.ExecuteAsync(_ =>
                {
                    throw new InvalidOperationException($"failure {i + 1}");
                });
            }
            catch (Exception)
            {
                // expected
            }

            await Task.Delay(10);
        }

        circuitStates.Should().Contain(CircuitState.Open,
            "because the circuit breaker must open after 3 consecutive failures"
        );
    }

    [Test]
    public async Task Polly_Retry_Pipeline_Does_Not_Retry_On_Success()
    {
        var attemptCount = 0;
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(10),
                BackoffType = DelayBackoffType.Constant,
                UseJitter = false,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>(),
                OnRetry = _ =>
                {
                    Interlocked.Increment(ref attemptCount);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        var result = await pipeline.ExecuteAsync(_ =>
        {
            Interlocked.Increment(ref attemptCount);
            return ValueTask.FromResult("success");
        });

        result.Should().Be("success");
        attemptCount.Should().Be(1,
            "because the pipeline must not retry when the first attempt succeeds"
        );
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\PollyBehaviorTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect GREEN — Polly v8 is already configured correctly)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "PollyBehaviorTests" 2>&1
```

Expected: GREEN — all 3 tests pass. Polly v8 v8.6.6 is already referenced and functional.

### Step 4: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Resilience\PollyBehaviorTests.cs
git commit -m "feat(t1-14): add polly v8 retry and circuit breaker behavior tests"
```

---

## Verification Checklist

- [ ] `DbContextRegistration.AddScriptsDbContext` includes `EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: 30s)`
- [ ] `ScriptsDbContextFactory.CreateDbContext` includes `EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: 30s)`
- [ ] `Infrastructure/Resilience.cs` backed up as `Resilience.cs.bak.YYYYMMDD_HHmmss`
- [ ] `Infrastructure/Resilience.cs` deleted
- [ ] `RetryExhaustedException` defined in `Core/Resilience.cs` as `public sealed class`
- [ ] `RetryExhaustedException` has `(string operation)` and `(string operation, Exception innerException)` constructors
- [ ] `dotnet build` passes with 0 errors
- [ ] `dotnet test` — DbRetryPolicyTests: 4/4 PASS
- [ ] `dotnet test` — InfrastructureResilienceDeletedTests: 3/3 PASS
- [ ] `dotnet test` — RetryExhaustedExceptionTests: 4/4 PASS
- [ ] `dotnet test` — PollyBehaviorTests: 3/3 PASS
