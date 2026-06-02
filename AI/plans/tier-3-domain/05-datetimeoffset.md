# DateTimeOffset Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Migrate all domain `DateTime` usage to `DateTimeOffset`, centralize format strings in `DateTimeFormats` (Core), and ensure no entity property remains typed as `DateTime`.

**Architecture:** PostgreSQL `timestamptz` maps to .NET `DateTimeOffset` (not `DateTime`). All database-stored timestamps must use `DateTimeOffset` for unambiguous UTC handling. Format strings are centralized in `DateTimeFormats` in `Scripts.Core`. The `DateTimeExtensions` extension class must be updated to accept `DateTimeOffset` instead of `DateTime`.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## DateTimeOffset Migration Context (from ADVANCED-FEATURES research)

### Why DateTimeOffset?

PostgreSQL `timestamptz` (timestamp with time zone) maps to .NET `DateTimeOffset`, not `DateTime`:
- **DateTimeOffset**: Stores UTC offset explicitly — unambiguous across time zones
- **DateTime**: No offset information — ambiguous when serialized/deserialized

### Logging Path Migration

**Current**: `<project_root>/logs/` (e.g., `/home/lance/Scripts/logs\`)
**Target**: `%USERPROFILE%\.cache\logs\scripts\`

Update `Paths.cs`:
```csharp
public static readonly string LogDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".cache", "logs", "scripts"
);
```

### Ben.Demystifier Integration

Add to `Log.cs` for stack trace demystification:
```csharp
public static void Error(Exception ex, string messageTemplate, params object?[] args) =>
    ActiveLogger.Error(exception: ex.Demystify(), messageTemplate: messageTemplate, propertyValues: args);
```

Add NuGet package: `<PackageReference Include="Ben.Demystifier" Version="*" />`

### DateTimeFormats Centralization

Create `Scripts.Core.DateTimeFormats.cs` with centralized format strings and timezone helper:

```csharp
namespace Scripts.Core;

internal static class DateTimeFormats
{
    public const string Iso8601 = "yyyy-MM-ddTHH:mm:ssZ";
    public const string Display = "yyyy/MM/dd HH:mm:ss";
    public const string DisplayDate = "yyyy/MM/dd";

    internal static class TimeZoneHelper
    {
        private static readonly TimeZoneInfo IstZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

        public static DateTime ToIst(DateTimeOffset utcDate) =>
            TimeZoneInfo.ConvertTime(utcDate, IstZone).DateTime;
    }
}
```

### DateTimeExtensions Update

Change extension receiver from `DateTime` to `DateTimeOffset`:

```csharp
namespace Scripts.Core;

internal static class DateTimeExtensions
{
    extension(DateTimeOffset utcDate)
    {
        internal string ToDisplay() =>
            TimeZoneHelper.ToIst(utcDate)
                .ToString(format: "yyyy/MM/dd HH:mm:ss", provider: CultureInfo.InvariantCulture);

        internal string ToDisplayDate() =>
            TimeZoneHelper.ToIst(utcDate)
                .ToString(format: "yyyy/MM/dd", provider: CultureInfo.InvariantCulture);
    }
}
```

---

## Pre-flight Checks

```powershell
if (-not (Get-Command pwsh -ErrorAction SilentlyContinue)) { throw "pwsh not found" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "dotnet SDK not found" }
dotnet --version | Select-String "^10\." || throw ".NET 10 SDK not found"

# T3 depends on T2 sign-off — Scripts.slnx must exist
if (-not (Test-Path '/home/lance/Scripts/csharp/Scripts.slnx')) {
    throw 'Tier 2 sign-off required — Scripts.slnx not found. Run T2 plans first.'
}

dotnet restore /home/lance/Scripts/csharp/Scripts.slnx -ErrorAction Stop
dotnet build   /home/lance/Scripts/csharp/Scripts.slnx --no-restore -ErrorAction Stop
# Expected: Build succeeded. 0 Error(s).
```

---

## Task 1 — TDD RED: Write DateTimeOffset pre-condition tests

**Current State:** Some entity properties use `DateTime`, some use `DateTimeOffset`. `DateTimeExtensions` accepts `DateTime`. Orchestrators use `DateTime.UtcNow`.
**Reason:** Failing tests define the target state — all entity properties must be `DateTimeOffset`, no `DateTime` properties in entities.
**What:** Create `T305_DateTimeOffsetTests.cs` in `Scripts.Tests\T3\`.
**Expected Outcome:** Tests compile; 1+ tests fail if any entity still has `DateTime` properties.

### Step 1.1 — Create test file

```powershell
$dir = "/home/lance/Scripts/csharp/tests\Scripts.Tests\T3"
New-Item -ItemType Directory -Path $dir -Force -ErrorAction Stop
Test-Path $dir | Should -Be $true
```

Create file `/home/lance/Scripts/csharp/tests\Scripts.Tests\T3\T305_DateTimeOffsetTests.cs`:

```csharp
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using TUnit.Core;

namespace Scripts.Tests.T3;

public class T305_DateTimeOffsetTests
{
    private const string DataEntitiesDir =
        @"/home/lance/Scripts/csharp/src\Data\Entities";

    private const string DateTimeFormatsFile =
        @"/home/lance/Scripts/csharp/src\Core\DateTimeFormats.cs";

    private const string OrchestratorsDir =
        @"/home/lance/Scripts/csharp/src\Orchestrators";

    [Test]
    public void NoEntityProperties_UseDateTime_InsteadOfDateTimeOffset()
    {
        Directory.Exists(DataEntitiesDir).Should().BeTrue(
            $"because Data Entities directory must exist at {DataEntitiesDir}");

        var entityFiles = Directory
            .GetFiles(DataEntitiesDir, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(f => !f.Contains(@"\obj\"))
            .ToList();

        entityFiles.Should().NotBeEmpty();

        var violations = new System.Collections.Generic.List<string>();

        foreach (var file in entityFiles)
        {
            var content = File.ReadAllText(file);

            // Match property declarations with DateTime type (not DateTimeOffset)
            var dateTimeProps = Regex.Matches(
                content,
                @"\bDateTime\s+\w+\s*\{"
            );

            foreach (Match m in dateTimeProps)
            {
                violations.Add($"{Path.GetFileName(file)}: {m.Value.Trim()}");
            }

            // Also catch nullable DateTime? properties
            var nullableProps = Regex.Matches(
                content,
                @"\bDateTime\?\s+\w+\s*\{"
            );

            foreach (Match m in nullableProps)
            {
                violations.Add($"{Path.GetFileName(file)}: {m.Value.Trim()}");
            }
        }

        violations.Should().BeEmpty(
            $"because all entity timestamp properties must use DateTimeOffset, not DateTime. Violations found:\n{string.Join("\n", violations)}");
    }

    [Test]
    public void DateTimeFormats_IsLocated_InCoreModule()
    {
        File.Exists(DateTimeFormatsFile).Should().BeTrue(
            $"because DateTimeFormats.cs must exist at {DateTimeFormatsFile}");

        var content = File.ReadAllText(DateTimeFormatsFile);
        content.Should().Contain("namespace Scripts.Core",
            "because DateTimeFormats must be in the Core namespace");
        content.Should().Contain("Iso8601",
            "because DateTimeFormats must contain at least the Iso8601 format string");
    }

    [Test]
    public void DateTimeExtensions_AcceptsDateTimeOffset_NotDateTime()
    {
        var extensionsFile = @"/home/lance/Scripts/csharp/src\Core\DateTimeExtensions.cs";

        if (!File.Exists(extensionsFile))
        {
            // Extensions file may not exist yet — not a failure
            return;
        }

        var content = File.ReadAllText(extensionsFile);

        // Should NOT accept plain DateTime as the extension receiver
        content.Should().NotMatchRegex(
            @"extension\(\s*DateTime\s+\w+",
            "because DateTimeExtensions must extend DateTimeOffset, not DateTime");
        content.Should().MatchRegex(
            @"extension\(\s*DateTimeOffset\s+\w+",
            "because DateTimeExtensions must extend DateTimeOffset");
    }

    [Test]
    public void Orchestrators_DoNotUse_DateTimeUtcNow()
    {
        Directory.Exists(OrchestratorsDir).Should().BeTrue();

        var files = Directory
            .GetFiles(OrchestratorsDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(@"\obj\"))
            .ToList();

        var violations = new System.Collections.Generic.List<string>();

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);

            // Match DateTime.UtcNow (but not DateTimeOffset.UtcNow)
            var matches = Regex.Matches(content, @"\bDateTime\.UtcNow\b");

            foreach (Match m in matches)
            {
                violations.Add($"{Path.GetFileName(file)}: line containing 'DateTime.UtcNow'");
            }
        }

        violations.Should().BeEmpty(
            $"because orchestrators must use DateTimeOffset.UtcNow, not DateTime.UtcNow. Violations:\n{string.Join("\n", violations)}");
    }
}
```

### Step 1.2 — Run to confirm RED

```powershell
dotnet restore /home/lance/Scripts/csharp/Scripts.slnx -ErrorAction Stop

dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "FullyQualifiedName~T305_DateTimeOffsetTests" `
    2>&1 | Tee-Object -Variable testOutput

Write-Host ($testOutput -join "`n")
# Expected: 1+ tests fail — some entities use DateTime, orchestrators use DateTime.UtcNow
# If all pass → already migrated → skip to commit
```

---

## Task 2 — Audit: Find all DateTime usages in entities and orchestrators

**Current State:** Unknown exactly where `DateTime` is used in entity properties and orchestrators.
**Reason:** Need exact file:line locations to plan replacements.
**What:** Grep for `DateTime` (but not `DateTimeOffset`) in entities, and `DateTime.UtcNow` in orchestrators.
**Expected Outcome:** Complete list of all violations.

```powershell
$entitiesDir = "/home/lance/Scripts/csharp/src\Data\Entities"
$orchDir     = "/home/lance/Scripts/csharp/src\Orchestrators"

Write-Host "=== Entity files: DateTime property declarations ==="
Get-ChildItem $entitiesDir -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    ForEach-Object {
        $file = $_.FullName
        $content = Get-Content $file -Raw -Encoding UTF8
        if ($content -match '\bDateTime[^O].*\{' -or $content -match '\bDateTime\?\s+\w+\s*\{') {
            Write-Host "VIOLATION: $($_.Name)"
            # Show relevant lines
            Get-Content $file -Encoding UTF8 |
                Select-String '\bDateTime(\?)?\s+\w+' |
                ForEach-Object { "  L$($_.LineNumber): $($_.Line.Trim())" }
        }
    }

Write-Host "=== Orchestrator files: DateTime.UtcNow ==="
Get-ChildItem $orchDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    Select-String "DateTime\.UtcNow" |
    ForEach-Object { "  $($_.Path):$($_.LineNumber): $($_.Line.Trim())" }

Write-Host "=== DateTimeExtensions.cs — check extension parameter type ==="
$extFile = "/home/lance/Scripts/csharp/src\Core\DateTimeExtensions.cs"
if (Test-Path $extFile) {
    Get-Content $extFile |
        Select-String "extension" |
        ForEach-Object { "  $($_.Line.Trim())" }
}
```

---

## Task 3 — GREEN: Fix each entity property from DateTime to DateTimeOffset

> Skip this task if Task 2 found no `DateTime` properties in entities.

**Current State:** Entity files contain `DateTime` or `DateTime?` property declarations.
**Reason:** PostgreSQL `timestamptz` maps to .NET `DateTimeOffset`. All stored timestamps must use it.
**What:** For each entity property, change `DateTime` to `DateTimeOffset` and update EF configuration.
**Expected Outcome:** Zero `DateTime` (non-Offset) property declarations in entity files.

### Step 3.1 — Back up each entity file

For each entity file with a violation (replace `<EntityName>` with actual name):

```powershell
$file = "/home/lance/Scripts/csharp/src\Data\Entities\<EntityName>.cs"
$bak  = "$file.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $file -Destination $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true
Write-Host "Backed up: $bak"
```

### Step 3.2 — Replace DateTime with DateTimeOffset in entity file

```powershell
$file    = "/home/lance/Scripts/csharp/src\Data\Entities\<EntityName>.cs"
$content = Get-Content $file -Raw -Encoding UTF8

# Replace DateTime? → DateTimeOffset? (must do before plain DateTime to avoid double-matching)
$updated = $content -replace "\bDateTime\?\s+(\w+)\s*\{", "DateTimeOffset? `$1 {"
# Replace DateTime → DateTimeOffset (only for property declarations)
$updated = $updated -replace "\bDateTime\s+(\w+)\s*\{", "DateTimeOffset `$1 {"

Set-Content -Path $file -Value $updated -Encoding UTF8 -ErrorAction Stop

# Verify
$check = Get-Content $file -Raw -Encoding UTF8
$check | Should -Match "DateTimeOffset"
$check | Should -Not -Match "\bDateTime[^O].*\{"
Write-Host "Fixed: $file"
```

### Step 3.3 — Update EF configuration for TimestampTz column type

If the entity has a corresponding configuration file in `csharp/src/Data/Configuration/`:

```powershell
$configDir = "/home/lance/Scripts/csharp/src\Data\Configuration"
$configFile = "$configDir\<EntityName>Configuration.cs"

if (Test-Path $configFile) {
    $bak = "$configFile.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    Copy-Item -Path $configFile -Destination $bak -ErrorAction Stop
    Test-Path $bak | Should -Be $true

    $content = Get-Content $configFile -Raw -Encoding UTF8
    # Ensure column type is timestamptz for any DateTimeOffset columns
    if ($content -match "\.HasColumnType\(" -and $content -notmatch "timestamp") {
        $updated = $content -replace "\.HasColumnType\(([^)]*)\)",
            '.HasColumnType("timestamp with time zone")'
        Set-Content -Path $configFile -Value $updated -Encoding UTF8 -ErrorAction Stop
        Write-Host "Updated EF config: $configFile"
    }
}
```

---

## Task 4 — GREEN: Replace DateTime.UtcNow with DateTimeOffset.UtcNow in orchestrators

> Skip this task if Task 2 found no `DateTime.UtcNow` in orchestrators.

**Current State:** Orchestrators use `DateTime.UtcNow` for timestamping `LastUpdated` and `PlaylistSnapshot`.
**Reason:** `DateTimeOffset.UtcNow` provides unambiguous UTC offset information.
**What:** Replace all occurrences of `DateTime.UtcNow` with `DateTimeOffset.UtcNow` in orchestrator files.
**Expected Outcome:** Zero `DateTime.UtcNow` in orchestrators directory.

### Step 4.1 — Back up each orchestrator file with violations

```powershell
$orchDir = "/home/lance/Scripts/csharp/src\Orchestrators"

Get-ChildItem $orchDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    ForEach-Object {
        $content = Get-Content $_.FullName -Raw -Encoding UTF8
        if ($content -match "DateTime\.UtcNow") {
            $file = $_.FullName
            $bak  = "$file.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
            Copy-Item -Path $file -Destination $bak -ErrorAction Stop
            Test-Path $bak | Should -Be $true
            Write-Host "Backed up: $bak"
        }
    }
```

### Step 4.2 — Apply replacement

```powershell
Get-ChildItem $orchDir -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notlike "*\obj\*" } |
    ForEach-Object {
        $content = Get-Content $_.FullName -Raw -Encoding UTF8
        if ($content -match "DateTime\.UtcNow") {
            $updated = $content -replace "\bDateTime\.UtcNow\b", "DateTimeOffset.UtcNow"
            Set-Content -Path $_.FullName -Value $updated -Encoding UTF8 -ErrorAction Stop

            $check = Get-Content $_.FullName -Raw -Encoding UTF8
            $check | Should -Not -Match "DateTime\.UtcNow"
            Write-Host "Fixed DateTime.UtcNow in: $($_.Name)"
        }
    }
```

---

## Task 5 — GREEN: Update DateTimeExtensions to use DateTimeOffset

> Skip if `DateTimeExtensions.cs` does not exist or already uses `DateTimeOffset`.

**Current State:** `DateTimeExtensions.cs` in Core has `extension(DateTime utcDate)`.
**Reason:** Extensions must operate on `DateTimeOffset` since that is the canonical timestamp type.
**What:** Change the extension receiver type from `DateTime` to `DateTimeOffset`. Update `ToLocalTime()` to use the `TimeZoneHelper.ToIst()` method already in `DateTimeFormats.cs`.
**Expected Outcome:** `DateTimeExtensions.cs` extends `DateTimeOffset`, not `DateTime`.

### Step 5.1 — Update DateTimeExtensions.cs

```powershell
$extFile = "/home/lance/Scripts/csharp/src\Core\DateTimeExtensions.cs"

if (-not (Test-Path $extFile)) {
    Write-Host "DateTimeExtensions.cs does not exist — skipping"
    return
}

$bak = "$extFile.bak.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item -Path $extFile -Destination $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true
```

Update the file content to use `DateTimeOffset`:

```csharp
namespace Scripts.Core;

internal static class DateTimeExtensions
{
	extension(DateTimeOffset utcDate)
	{
		internal string ToDisplay() =>
			TimeZoneHelper.ToIst(utcDate)
				.ToString(format: "yyyy/MM/dd HH:mm:ss", provider: CultureInfo.InvariantCulture);

		internal string ToDisplayDate() =>
			TimeZoneHelper.ToIst(utcDate)
				.ToString(format: "yyyy/MM/dd", provider: CultureInfo.InvariantCulture);
	}
}
```

```powershell
Set-Content -Path $extFile -Value @'
namespace Scripts.Core;

internal static class DateTimeExtensions
{
	extension(DateTimeOffset utcDate)
	{
		internal string ToDisplay() =>
			TimeZoneHelper.ToIst(utcDate)
				.ToString(format: "yyyy/MM/dd HH:mm:ss", provider: CultureInfo.InvariantCulture);

		internal string ToDisplayDate() =>
			TimeZoneHelper.ToIst(utcDate)
				.ToString(format: "yyyy/MM/dd", provider: CultureInfo.InvariantCulture);
	}
}
'@ -Encoding UTF8 -ErrorAction Stop

# Verify
$check = Get-Content $extFile -Raw -Encoding UTF8
$check | Should -Match "DateTimeOffset utcDate"
$check | Should -Not -Match "DateTime utcDate"
Write-Host "DateTimeExtensions now extends DateTimeOffset"
```

---

## Task 6 — GREEN: Generate EF Core migration for DateTimeOffset column changes

> Skip if no DateTime → DateTimeOffset changes were made to entity properties.

**Current State:** Entity properties changed from `DateTime` to `DateTimeOffset`.
**Reason:** EF Core must update the PostgreSQL column types to `timestamptz`.
**What:** Generate a new EF migration and apply it to the local database.
**Expected Outcome:** Migration generated and applied; database columns use `timestamp with time zone`.

```powershell
# Load .env variables for connection string
Get-Content /home/lance/Scripts/.env | ForEach-Object {
    if ($_ -match '^([^#][^=]+)=(.+)$') {
        [System.Environment]::SetEnvironmentVariable($Matches[1], $Matches[2])
    }
}

# Verify PGCONNSTR is set
if (-not $env:PGCONNSTR) { throw "PGCONNSTR environment variable is not set" }

# Generate migration
dotnet ef migrations add MigrateToDateTimeOffset `
    --project /home/lance/Scripts/csharp/src\Data\Scripts.Data.csproj `
    --startup-project /home/lance/Scripts/csharp/src\CLI\Scripts.CLI.csproj `
    -ErrorAction Stop 2>&1 | Tee-Object -Variable migOut

$migOut | Select-String "Done" | Should -Not -BeNullOrEmpty
Write-Host "Migration generated: MigrateToDateTimeOffset"

# Apply to local database
dotnet ef database update `
    --project /home/lance/Scripts/csharp/src\Data\Scripts.Data.csproj `
    --startup-project /home/lance/Scripts/csharp/src\CLI\Scripts.CLI.csproj `
    -ErrorAction Stop 2>&1 | Tee-Object -Variable updateOut

$updateOut | Select-String "Done" | Should -Not -BeNullOrEmpty
Write-Host "Database updated with DateTimeOffset migration"
```

---

## Task 7 — Build and test GREEN

**Current State:** All DateTime → DateTimeOffset changes applied, migration generated.
**Reason:** Confirm compilation succeeds, all tests pass including T305.
**What:** Full restore → build → targeted test run → full test suite.
**Expected Outcome:** 0 build errors, all T305 tests pass, full suite green.

```powershell
dotnet restore /home/lance/Scripts/csharp/Scripts.slnx -ErrorAction Stop

$buildOut = dotnet build /home/lance/Scripts/csharp/Scripts.slnx --no-restore 2>&1
$buildOut | Select-String "0 Error" | Should -Not -BeNullOrEmpty
Write-Host "Build: GREEN"

# Run DateTimeOffset tests
$testOut = dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "FullyQualifiedName~T305_DateTimeOffsetTests" 2>&1
$testOut | Select-String "Failed: 0" | Should -Not -BeNullOrEmpty
Write-Host "T305 tests: GREEN"

# Full suite — DateTimeOffset change affects many callers
$fullTestOut = dotnet test /home/lance/Scripts/csharp/Scripts.slnx 2>&1
$fullTestOut | Select-String "Failed: 0" | Should -Not -BeNullOrEmpty
Write-Host "Full test suite: GREEN"
```

Expected output:
```
Test Run Successful.
Tests: 4 (4 passed)
```

---

## Task 8 — REFACTOR: Commit DateTimeOffset migration

**Current State:** All tests green, all entities use DateTimeOffset, extensions updated, migration generated.
**Reason:** Record DateTimeOffset migration as a discrete commit.
**What:** Stage all changes including migration files, commit.
**Expected Outcome:** Commit `feat(t3-05)` in git log.

```powershell
Set-Location /home/lance/Scripts -ErrorAction Stop

gitleaks detect --no-git 2>&1 | Select-String "leaks found" | ForEach-Object {
    throw "Gitleaks found secrets — abort commit"
}

git add csharp/src/Data/Entities/ 2>&1
git add csharp/src/Orchestrators/ 2>&1
git add csharp/src/Core/DateTimeExtensions.cs 2>&1
git add csharp/src/Core/DateTimeFormats.cs 2>&1
git add csharp/tests/Scripts.Tests/T3/T305_DateTimeOffsetTests.cs 2>&1
git add csharp/src/Data/Migrations/ 2>&1
# Remove .bak files from staging area — they should NOT be committed
git reset -- "*.bak*" 2>$null

git status 2>&1 | Write-Host

git commit -m "feat(t3-05): migrate DateTime to DateTimeOffset — entities, orchestrators, extensions, EF migration" `
    -ErrorAction Stop 2>&1 | Tee-Object -Variable commitOut

$commitOut | Select-String "feat\(t3-05\)" | Should -Not -BeNullOrEmpty
Write-Host "Committed: t3-05"
```

---

## Completion Criteria

| Check | Command | Expected |
|-------|---------|----------|
| Build clean | `dotnet build csharp/Scripts.slnx` | `0 Error(s)` |
| Tests pass | `dotnet test --filter T305` | `Failed: 0` |
| Full suite green | `dotnet test csharp/Scripts.slnx` | `Failed: 0` |
| No DateTime in entities | `grep "\bDateTime\b" csharp/src/Data/Entities/*.cs \| grep -v DateTimeOffset` | No output |
| No DateTime.UtcNow in orchestrators | `grep "DateTime.UtcNow" csharp/src/Orchestrators/**/*.cs` | No output |
| DateTimeExtensions uses DateTimeOffset | `grep "DateTimeOffset" csharp/src/Core/DateTimeExtensions.cs` | At least 1 match |
| DateTimeFormats in Core | `Test-Path csharp/src/Core/DateTimeFormats.cs` | `True` |
| EF migration applied | `dotnet ef database update` | `Done.` |
| Commit present | `git log --oneline -1` | `feat(t3-05)` |
