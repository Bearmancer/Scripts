# T1-12: Logging Relocation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Relocate the log directory from `$ProjectRoot/logs/` to `%USERPROFILE%\.cache\logs\scripts\`, add `Ben.Demystifier` for cleaned stack traces, remove `ServiceType.Sheets` from the enum, and ensure the log directory is created automatically.

**Architecture:** Change `Paths.LogDirectory` from `Path.Combine(ProjectRoot, "logs")` to `Path.Combine(Environment.GetFolderPath(UserProfile), ".cache", "logs", "scripts")`. Add `Directory.CreateDirectory` in `Log` static constructor. Add `Ben.Demystifier` NuGet and call `.Demystify()` on exceptions in `Log.Error(Exception, ...)` and `Log.Fatal(Exception, ...)`. Remove `ServiceType.Sheets` from the enum and its logger initialization. No Console sink added (Spectre.Console `Ui` class handles user-facing output independently).

**Tech Stack:** C# 14 / .NET 10 / Serilog / Ben.Demystifier / Spectre.Console / TUnit / FluentAssertions

---

## Prerequisites

- T1-11 completed (CompiledModel generated and green)
- `Scripts.Tests` project exists and is referenced in `Scripts.slnx`
- `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Paths.cs` exists
- `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Log.cs` exists

```powershell
Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Core\Paths.cs
# Expected: True

Test-Path C:\Users\Lance\Dev\Scripts\csharp\src\Core\Log.cs
# Expected: True

Test-Path C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Scripts.Tests.csproj
# Expected: True
```

---

## Task 1 — Relocate LogDirectory to `%USERPROFILE%\.cache\logs\scripts`

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Paths.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging\LogDirectoryTests.cs`

### Step 0: Preflight

```powershell
# Current state: LogDirectory = Path.Combine(ProjectRoot, "logs") → C:\Users\Lance\Dev\Scripts\logs
# Reason: AGENTS.md §9 mandates %USERPROFILE%\.cache\logs\scripts\
# What: Change LogDirectory to use Environment.GetFolderPath(UserProfile) + .cache\logs\scripts
# Expected: LogDirectory resolves to C:\Users\Lance\.cache\logs\scripts

Get-Content C:\Users\Lance\Dev\Scripts\csharp\src\Core\Paths.cs | Select-String "LogDirectory"
# Expected: public static readonly string LogDirectory = Path.Combine(path1: ProjectRoot, path2: "logs");

$testFile = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging\LogDirectoryTests.cs'
Test-Path $testFile
# Expected: False

New-Item -ItemType Directory -Force -Path 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging'
```

### Step 1: Write tests

```csharp
// C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging\LogDirectoryTests.cs
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Logging;

public sealed class LogDirectoryTests
{
    [Test]
    public void LogDirectory_Points_To_UserProfile_Cache_Logs_Scripts()
    {
        var logDir = CSharpScripts.Core.Paths.LogDirectory;

        var expectedBase = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cache",
            "logs",
            "scripts"
        );

        logDir.Should().Be(expectedBase,
            $"because AGENTS.md §9 mandates %USERPROFILE%\\.cache\\logs\\scripts\\\nActual: {logDir}\nExpected: {expectedBase}"
        );
    }

    [Test]
    public void LogDirectory_Does_Not_Point_To_ProjectRoot()
    {
        var logDir = CSharpScripts.Core.Paths.LogDirectory;
        var projectRoot = CSharpScripts.Core.Paths.ProjectRoot;

        logDir.Should().NotContain(projectRoot,
            $"because logs must live outside the project root.\nLogDirectory: {logDir}\nProjectRoot: {projectRoot}"
        );
    }

    [Test]
    public void LogDirectory_Is_Absolute_Path()
    {
        var logDir = CSharpScripts.Core.Paths.LogDirectory;

        Path.IsPathRooted(logDir).Should().BeTrue(
            $"because log paths must be absolute.\nValue: {logDir}"
        );
    }

    [Test]
    public async Task LogDirectory_Is_Created_Automatically()
    {
        // Access the static Log class to trigger static constructor
        var logDir = CSharpScripts.Core.Paths.LogDirectory;

        // The directory should be created by Log's static constructor
        var dirInfo = new DirectoryInfo(logDir);
        dirInfo.Exists.Should().BeTrue(
            $"because Log static constructor must create the directory.\nPath: {logDir}"
        );
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging\LogDirectoryTests.cs'
Test-Path $file
# Expected: True

Get-Content $file | Select-String "LogDirectory_Points_To" | Select-Object -First 1
```

### Step 3: Run test (expect RED — LogDirectory still points to ProjectRoot/logs)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "LogDirectoryTests" 2>&1
```

Expected: RED. `LogDirectory_Points_To_UserProfile_Cache_Logs_Scripts` fails — actual value is `C:\Users\Lance\Dev\Scripts\logs`, expected `C:\Users\Lance\.cache\logs\scripts`. `LogDirectory_Does_Not_Point_To_ProjectRoot` and `LogDirectory_Is_Created_Automatically` also fail.

### Step 4: Assess

Need to change one line in `Paths.cs` and add `Directory.CreateDirectory` in `Log.cs` static constructor.

### Step 5: Implement

**Edit `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Paths.cs` line 9:**

OLD:
```csharp
	public static readonly string LogDirectory = Path.Combine(path1: ProjectRoot, path2: "logs");
```

NEW:
```csharp
	public static readonly string LogDirectory = Path.Combine(
		path1: Environment.GetFolderPath(folder: SpecialFolder.UserProfile),
		path2: ".cache",
		path3: "logs",
		path4: "scripts"
	);
```

**Edit `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Log.cs` static constructor (after line 22 `private static readonly AsyncLocal<ServiceType?> ActiveServiceLocal = new();`):**

Add `Directory.CreateDirectory` before the logger initialization:

OLD (lines 23-35):
```csharp
#pragma warning disable CA1810
	static Log()
	{
		ServiceLoggers = new Dictionary<ServiceType, ILogger>
		{
```

NEW:
```csharp
#pragma warning disable CA1810
	static Log()
	{
		Directory.CreateDirectory(path: Paths.LogDirectory);

		ServiceLoggers = new Dictionary<ServiceType, ILogger>
		{
```

**Verify build:**

```powershell
dotnet build C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj 2>&1
```

Expected: Build succeeded with 0 errors.

### Step 6: Run test (expect GREEN)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "LogDirectoryTests" 2>&1
```

Expected: GREEN — all 4 tests pass:
- `LogDirectory_Points_To_UserProfile_Cache_Logs_Scripts`: PASS
- `LogDirectory_Does_Not_Point_To_ProjectRoot`: PASS
- `LogDirectory_Is_Absolute_Path`: PASS
- `LogDirectory_Is_Created_Automatically`: PASS

### Step 7: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\src\Core\Paths.cs
git add C:\Users\Lance\Dev\Scripts\csharp\src\Core\Log.cs
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging\LogDirectoryTests.cs
git commit -m "feat(t1-12): relocate log directory to userprofile .cache logs scripts"
```

---

## Task 2 — Add Ben.Demystifier for Stack Trace Cleaning

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj`
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Log.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging\BenDemystifierTests.cs`

### Step 0: Preflight

```powershell
# Current state: Ben.Demystifier not referenced anywhere. Log.Error(Exception, ...) and Log.Fatal(Exception, ...)
# pass the raw exception without .Demystify().
# Reason: AGENTS.md §9: "Stack traces: demystified via Ben.Demystifier"
# What: Add Ben.Demystifier NuGet, call .Demystify() in Error/Fatal exception overloads.
# Expected: NuGet added, Log methods call .Demystify(), build succeeds.

Get-Content C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj | Select-String "Ben.Demystifier"
# Expected: (no output — package not referenced)

Get-Content C:\Users\Lance\Dev\Scripts\csharp\src\Core\Log.cs | Select-String "Demystify"
# Expected: (no output — .Demystify() not called)
```

### Step 1: Write test

```csharp
// C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging\BenDemystifierTests.cs
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Logging;

public sealed class BenDemystifierTests
{
    [Test]
    public void Demystify_Is_Available_As_Extension_Method()
    {
        // Verify Ben.Demystifier is referenced and .Demystify() compiles
        var ex = new InvalidOperationException("test");

        // This line must compile — proves Ben.Demystifier is referenced
        var demystified = ex.Demystify();

        demystified.Should().NotBeNull();
        demystified.Message.Should().Be("test");
    }

    [Test]
    public void Demystified_Exception_Has_Cleaned_StackTrace()
    {
        Exception ex;

        try
        {
            ThrowDeepAsync().GetAwaiter().GetResult();
            throw new InvalidOperationException("should not reach here");
        }
        catch (Exception caught)
        {
            ex = caught;
        }

        var demystified = ex.Demystify();

        demystified.StackTrace.Should().NotContain(
            "System.Runtime.CompilerServices",
            "because Ben.Demystifier removes async state machine frames"
        );
    }

    private static async Task ThrowDeepAsync()
    {
        await Task.Yield();
        await InnerAsync();
    }

    private static async Task InnerAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException("deep failure");
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging\BenDemystifierTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect RED — Ben.Demystifier not referenced, .Demystify() won't compile)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "BenDemystifierTests" 2>&1
```

Expected: RED — compilation error: `'Exception' does not contain a definition for 'Demystify'`.

### Step 4: Assess

Two changes needed:
1. Add `<PackageReference Include="Ben.Demystifier" Version="*" />` to `CSharpScripts.csproj`
2. Add `using System.Diagnostics;` (already present via GlobalUsings) and call `.Demystify()` in `Log.Error(Exception, ...)` and `Log.Fatal(Exception, ...)`

### Step 5: Implement

**Edit `C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj`:**

Add after line 26 (after `Azure.Identity`):

```xml
		<PackageReference Include="Ben.Demystifier" Version="*" />
```

**Edit `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Log.cs`:**

Change `Error(Exception ex, ...)` at line 108:

OLD:
```csharp
	public static void Error(Exception ex, string messageTemplate, params object?[] args) =>
		ActiveLogger.Error(exception: ex, messageTemplate: messageTemplate, propertyValues: args);
```

NEW:
```csharp
	public static void Error(Exception ex, string messageTemplate, params object?[] args) =>
		ActiveLogger.Error(exception: ex.Demystify(), messageTemplate: messageTemplate, propertyValues: args);
```

Change `Fatal(Exception ex, ...)` at line 114:

OLD:
```csharp
	public static void Fatal(Exception ex, string messageTemplate, params object?[] args) =>
		ActiveLogger.Fatal(exception: ex, messageTemplate: messageTemplate, propertyValues: args);
```

NEW:
```csharp
	public static void Fatal(Exception ex, string messageTemplate, params object?[] args) =>
		ActiveLogger.Fatal(exception: ex.Demystify(), messageTemplate: messageTemplate, propertyValues: args);
```

**Run restore and build:**

```powershell
dotnet restore C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj 2>&1
dotnet build C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj 2>&1
```

Expected: Restore succeeded, Build succeeded with 0 errors.

### Step 6: Run test (expect GREEN)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "BenDemystifierTests" 2>&1
```

Expected: GREEN — both tests pass:
- `Demystify_Is_Available_As_Extension_Method`: PASS
- `Demystified_Exception_Has_Cleaned_StackTrace`: PASS

### Step 7: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj
git add C:\Users\Lance\Dev\Scripts\csharp\src\Core\Log.cs
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging\BenDemystifierTests.cs
git commit -m "feat(t1-12): add ben demystifier for cleaned stack traces in log error fatal"
```

---

## Task 3 — Remove `ServiceType.Sheets` from Enum

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Log.cs`
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging\ServiceTypeTests.cs`

### Step 0: Preflight

```powershell
# Current state: ServiceType enum has 6 values: LastFm, YouTube, Sheets, Music, Read, Cloud.
# Sheets is referenced in the enum (line 12) and in BuildTimeout switch (line 147).
# Reason: Google Sheets is retained for backward compatibility during migration but managed separately.
# Remove Sheets from ServiceType to align with post-migration architecture.
# What: Remove Sheets = 2 from enum, remove its logger initialization, remove from BuildTimeout switch.
# Expected: Enum has 5 values, build compiles, no Sheets references remain.

Get-Content C:\Users\Lance\Dev\Scripts\csharp\src\Core\Log.cs | Select-String "Sheets"
# Expected: lines 12 (enum), 31 (logger init), 148 (BuildTimeout)

# Check for other Sheets references in Resilience.cs
Get-Content C:\Users\Lance\Dev\Scripts\csharp\src\Core\Resilience.cs | Select-String "Sheets"
# Expected: line 147 (BuildTimeout)
```

### Step 1: Write test

```csharp
// C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging\ServiceTypeTests.cs
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.Logging;

public sealed class ServiceTypeTests
{
    [Test]
    public void ServiceType_Does_Not_Contain_Sheets()
    {
        var enumValues = System.Enum.GetNames<CSharpScripts.Core.ServiceType>();

        enumValues.Should().NotContain("Sheets",
            "because Google Sheets is deprecated and removed from the logging enum"
        );
    }

    [Test]
    public void ServiceType_Has_Exactly_Five_Values()
    {
        var enumValues = System.Enum.GetNames<CSharpScripts.Core.ServiceType>();

        enumValues.Should().HaveCount(5,
            $"because Sheets was removed, leaving 5 values.\nActual: [{string.Join(", ", enumValues)}]"
        );
    }

    [Test]
    public void ServiceType_Contains_Expected_Values()
    {
        var enumValues = System.Enum.GetNames<CSharpScripts.Core.ServiceType>();

        enumValues.Should().Contain(["LastFm", "YouTube", "Music", "Read", "Cloud"],
            "because these are the 5 active service types post-Sheets-removal"
        );
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging\ServiceTypeTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect RED — Sheets still present in enum)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "ServiceTypeTests" 2>&1
```

Expected: RED. `ServiceType_Does_Not_Contain_Sheets` fails — "Sheets" found in enum. `ServiceType_Has_Exactly_Five_Values` fails — has 6 values.

### Step 4: Assess

Need to:
1. Remove `Sheets` from `ServiceType` enum in `Log.cs`
2. Remove `ServiceType.Sheets` logger initialization from static constructor
3. Remove `ServiceType.Sheets => 120` from `BuildTimeout` switch in `Resilience.cs`
4. Verify no other files reference `ServiceType.Sheets`

### Step 5: Implement

**Check for other Sheets references before deleting:**

```powershell
$result = Get-ChildItem C:\Users\Lance\Dev\Scripts\csharp\src\*.cs -Recurse | Select-String "ServiceType\.Sheets" -SimpleMatch
if ($result) { Write-Host "WARNING: Additional Sheets references found:"; $result }
```

**Edit `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Log.cs` — Remove `Sheets` from enum (lines 8-16):**

OLD:
```csharp
internal enum ServiceType
{
	LastFm,
	YouTube,
	Sheets,
	Music,
	Read,
	Cloud
}
```

NEW:
```csharp
internal enum ServiceType
{
	LastFm,
	YouTube,
	Music,
	Read,
	Cloud
}
```

**Edit `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Log.cs` — Remove Sheets logger init (line 31):**

OLD:
```csharp
			[key: ServiceType.Sheets] = BuildServiceLogger(filename: "sheets.jsonl"),
```

REMOVE this line.

**Edit `C:\Users\Lance\Dev\Scripts\csharp\src\Core\Resilience.cs` — Remove Sheets timeout switch case (line 147):**

OLD:
```csharp
		var timeoutSeconds = service switch
		{
			ServiceType.LastFm => 30,
			ServiceType.YouTube => 60,
			ServiceType.Sheets => 120,
			ServiceType.Music => 30,
			ServiceType.Read => 60,
			ServiceType.Cloud => 60,
			_ => 30
		};
```

NEW:
```csharp
		var timeoutSeconds = service switch
		{
			ServiceType.LastFm => 30,
			ServiceType.YouTube => 60,
			ServiceType.Music => 30,
			ServiceType.Read => 60,
			ServiceType.Cloud => 60,
			_ => 30
		};
```

**Verify build:**

```powershell
dotnet build C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj 2>&1
```

Expected: Build succeeded with 0 errors.

### Step 6: Run test (expect GREEN)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "ServiceTypeTests" 2>&1
```

Expected: GREEN — all 3 tests pass:
- `ServiceType_Does_Not_Contain_Sheets`: PASS
- `ServiceType_Has_Exactly_Five_Values`: PASS
- `ServiceType_Contains_Expected_Values`: PASS

### Step 7: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\src\Core\Log.cs
git add C:\Users\Lance\Dev\Scripts\csharp\src\Core\Resilience.cs
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Logging\ServiceTypeTests.cs
git commit -m "feat(t1-12): remove servicetype sheets from enum and related code"
```

---

## Verification Checklist

- [ ] `Paths.LogDirectory` resolves to `C:\Users\Lance\.cache\logs\scripts`
- [ ] `Directory.CreateDirectory(Paths.LogDirectory)` called in `Log` static constructor
- [ ] `Ben.Demystifier` NuGet package referenced in `CSharpScripts.csproj`
- [ ] `Log.Error(Exception, ...)` calls `ex.Demystify()`
- [ ] `Log.Fatal(Exception, ...)` calls `ex.Demystify()`
- [ ] `ServiceType` enum has exactly 5 values (LastFm, YouTube, Music, Read, Cloud)
- [ ] No `ServiceType.Sheets` references anywhere in `csharp/src/`
- [ ] `dotnet build` passes with 0 errors
- [ ] `dotnet test` — LogDirectoryTests: 4/4 PASS
- [ ] `dotnet test` — BenDemystifierTests: 2/2 PASS
- [ ] `dotnet test` — ServiceTypeTests: 3/3 PASS
