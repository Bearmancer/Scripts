# Structural Inspection Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix all Rider structural inspections — add `CancellationToken` parameters to every async repository method, reduce `public` visibility to `internal` for classes only used within their project, and verify `SpectreTypeRegistrar` is actually used (removing any suppression workarounds).

**Architecture:** Each fix follows the TDD loop: write a reflection or file-content test, watch it fail, apply the fix, watch it pass. After all fixes, `dotnet build` must report zero errors and zero warnings with `TreatWarningsAsErrors=true`.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Pre-flight

- [ ] **Step 0: Pre-flight validation**

```powershell
Get-Command pwsh   -ErrorAction Stop
Get-Command dotnet -ErrorAction Stop
Get-Command git    -ErrorAction Stop

dotnet restore C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx -ErrorAction Stop
dotnet build   C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1 | Tee-Object -Variable buildOutput
# Capture current warning count for baseline
$warnings = ($buildOutput | Select-String ' warning ').Count
Write-Host "Baseline warning count: $warnings"
```

---

## Task 1: CancellationToken on all async repository methods

**Files:**
- Modify: `csharp/src/Data/Repositories/ScrobbleRepository.cs`
- Modify: `csharp/src/Data/Repositories/VideoRepository.cs`
- Modify: any other repository in `csharp/src/Data/Repositories/`
- Modify: `csharp/tests/Scripts.Tests/StructuralTests/CancellationTokenTests.cs` (create)

- [ ] **Step 1: Write failing CancellationToken reflection tests**

```csharp
// csharp/tests/Scripts.Tests/StructuralTests/CancellationTokenTests.cs
using System.Reflection;
using FluentAssertions;
using TUnit;
using CSharpScripts.Data;

namespace Scripts.Tests.StructuralTests;

public class CancellationTokenTests
{
    private static IEnumerable<MethodInfo> GetAsyncRepositoryMethods()
    {
        var assembly = typeof(ScriptsDbContext).Assembly;
        return assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Repository") && !t.IsInterface)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m.Name.EndsWith("Async"));
    }

    [Test]
    public void AllAsyncRepositoryMethods_HaveCancellationTokenParameter()
    {
        var violations = GetAsyncRepositoryMethods()
            .Where(m => !m.GetParameters().Any(p => p.ParameterType == typeof(CancellationToken)))
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")
            .ToList();

        violations.Should().BeEmpty(
            $"these async methods are missing CancellationToken: {string.Join(", ", violations)}");
    }

    [Test]
    public void AllAsyncRepositoryMethods_HaveCancellationToken_AsLastParameter()
    {
        var violations = GetAsyncRepositoryMethods()
            .Where(m =>
            {
                var @params = m.GetParameters();
                var last = @params.LastOrDefault();
                return last is null || last.ParameterType != typeof(CancellationToken);
            })
            .Select(m => $"{m.DeclaringType!.Name}.{m.Name}")
            .ToList();

        violations.Should().BeEmpty(
            $"CancellationToken must be the LAST parameter in: {string.Join(", ", violations)}");
    }

    [Test]
    public void ScrobbleRepository_GetLatestAsync_HasCancellationToken()
    {
        var method = typeof(ScrobbleRepository).GetMethod("GetLatestAsync")!;
        var last = method.GetParameters().LastOrDefault();
        last.Should().NotBeNull();
        last!.ParameterType.Should().Be(typeof(CancellationToken));
        last.HasDefaultValue.Should().BeTrue("CancellationToken must default to default");
    }
}
```

- [ ] **Step 2: Read-back**

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\StructuralTests\CancellationTokenTests.cs'
Test-Path $file | Should -Be $true
Write-Host "Read-back OK"
```

- [ ] **Step 3: Run — confirm RED**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "CancellationTokenTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: at least one test fails listing methods that lack `CancellationToken`.

- [ ] **Step 3.5: State assessment**

Read the failure output. List every method that is missing `CancellationToken`. Proceed to fix them all in Step 4.

- [ ] **Step 4: Add `CancellationToken ct = default` to every flagged method**

For each method identified, add `CancellationToken ct = default` as the last parameter and thread it through to every inner async call.

Example — `ScrobbleRepository`:
```csharp
// Before
public async Task<Scrobble?> GetLatestAsync()
    => await _context.Scrobbles
        .OrderByDescending(s => s.ScrobbledAt)
        .FirstOrDefaultAsync();

// After
public async Task<Scrobble?> GetLatestAsync(CancellationToken ct = default)
    => await _context.Scrobbles
        .OrderByDescending(s => s.ScrobbledAt)
        .FirstOrDefaultAsync(ct);
```

Example — `VideoRepository`:
```csharp
// Before
public async Task<Video?> GetByYoutubeIdAsync(string youtubeId)
    => await _context.Videos.FirstOrDefaultAsync(v => v.YoutubeId == youtubeId);

// After
public async Task<Video?> GetByYoutubeIdAsync(string youtubeId, CancellationToken ct = default)
    => await _context.Videos.FirstOrDefaultAsync(v => v.YoutubeId == youtubeId, ct);
```

Apply this pattern to ALL async methods in ALL repository classes.

- [ ] **Step 5: Run — confirm GREEN**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "CancellationTokenTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: all CancellationToken tests PASS.

- [ ] **Step 6: Commit**

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/Data/Repositories/ `
    csharp/tests/Scripts.Tests/StructuralTests/CancellationTokenTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t4-02a): add CancellationToken to all async repository methods"
```

---

## Task 2: Reduce public visibility to internal

**Files:**
- Modify: any `public` class in `csharp/src/` that is only used internally
- Modify: `csharp/tests/Scripts.Tests/StructuralTests/VisibilityTests.cs` (create)

- [ ] **Step 1: Write failing visibility tests**

```csharp
// csharp/tests/Scripts.Tests/StructuralTests/VisibilityTests.cs
using FluentAssertions;
using TUnit;
using CSharpScripts.Data;

namespace Scripts.Tests.StructuralTests;

public class VisibilityTests
{
    [Test]
    public void TextNormalizer_IsInternal()
    {
        var type = typeof(ScriptsDbContext).Assembly.GetType("CSharpScripts.Data.TextNormalizer");
        type.Should().NotBeNull("TextNormalizer must exist in Scripts.Data assembly");
        type!.IsPublic.Should().BeFalse("TextNormalizer is an internal utility — must not be public");
    }

    [Test]
    public void ServiceRegistration_IsInternal()
    {
        var assembly = typeof(CSharpScripts.CLI.Program).Assembly;
        var type = assembly.GetType("CSharpScripts.CLI.ServiceRegistration");
        type.Should().NotBeNull();
        type!.IsPublic.Should().BeFalse("ServiceRegistration is CLI-internal");
    }
}
```

> **Note:** Update type names to match the actual classes flagged by Rider. The two tests above are examples — audit `csharp/src/` for `public` classes that have no `public` callers across project boundaries.

- [ ] **Step 2: Read-back**

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\StructuralTests\VisibilityTests.cs'
Test-Path $file | Should -Be $true
Write-Host "Read-back OK"
```

- [ ] **Step 3: Run — confirm RED**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "VisibilityTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: tests that reference `public` classes fail (`.IsPublic` is `true`).

- [ ] **Step 4: Change visibility to `internal`**

For each class identified:
1. Open the file.
2. Change `public class` → `internal class` (or `public static class` → `internal static class`).
3. Verify no `public` API in any other project references it (it should not, by design).

Example:
```csharp
// Before
public static class TextNormalizer { ... }

// After
internal static class TextNormalizer { ... }
```

- [ ] **Step 5: Run — confirm GREEN**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "VisibilityTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: all visibility tests PASS.

- [ ] **Step 6: Commit**

```powershell
git -C C:\Users\Lance\Dev\Scripts add csharp/src/ `
    csharp/tests/Scripts.Tests/StructuralTests/VisibilityTests.cs
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t4-02b): reduce public visibility to internal for non-public types"
```

---

## Task 3: Verify SpectreTypeRegistrar is used (no suppression workarounds)

**Files:**
- Modify: `csharp/src/CLI/Program.cs` (if registrar is unused)
- Modify: `csharp/tests/Scripts.Tests/StructuralTests/SpectreRegistrarTests.cs` (create)

- [ ] **Step 1: Write failing usage test**

```csharp
// csharp/tests/Scripts.Tests/StructuralTests/SpectreRegistrarTests.cs
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.StructuralTests;

public class SpectreRegistrarTests
{
    [Test]
    public void SpectreTypeRegistrar_IsUsed_InProgramCs()
    {
        var programContent = File.ReadAllText(
            @"C:\Users\Lance\Dev\Scripts\csharp\src\CLI\Program.cs");

        programContent.Should().Contain("SpectreTypeRegistrar",
            "Program.cs must instantiate SpectreTypeRegistrar, not suppress it");
    }

    [Test]
    public void SpectreTypeRegistrar_HasNoSuppressMessage_Attributes()
    {
        var registrarFile = @"C:\Users\Lance\Dev\Scripts\csharp\src\CLI\SpectreTypeRegistrar.cs";
        if (!File.Exists(registrarFile)) return; // class may be inlined

        var content = File.ReadAllText(registrarFile);
        content.Should().NotContain("[SuppressMessage",
            "SpectreTypeRegistrar must not use SuppressMessage — wire it correctly instead");
    }
}
```

- [ ] **Step 2: Read-back**

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\StructuralTests\SpectreRegistrarTests.cs'
Test-Path $file | Should -Be $true
Write-Host "Read-back OK"
```

- [ ] **Step 3: Run — confirm RED or GREEN**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "SpectreRegistrarTests" `
    --logger "console;verbosity=detailed" 2>&1
```

If RED: `Program.cs` does not use `SpectreTypeRegistrar`. Proceed to Step 4.
If GREEN: already wired correctly. Skip to Task 4.

- [ ] **Step 4: Wire SpectreTypeRegistrar into Program.cs**

In `Program.cs`, when building the Spectre.Console CLI app, pass the `SpectreTypeRegistrar`:

```csharp
var registrar = new SpectreTypeRegistrar(services);
var app = new CommandApp(registrar);
app.Configure(config =>
{
    config.AddCommand<SyncCommand>("sync");
    // ... other commands
});
return app.Run(args);
```

Remove any `[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]` or similar attributes from `SpectreTypeRegistrar.cs`.

- [ ] **Step 5: Run — confirm GREEN**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "SpectreRegistrarTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: both tests PASS.

---

## Task 4: Final build — zero warnings

- [ ] **Step 1: Full build — confirm zero warnings**

```powershell
$build = dotnet build C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx 2>&1
$build | Write-Host
$errorLines = $build | Where-Object { $_ -match ' error ' }
$warnLines  = $build | Where-Object { $_ -match ' warning ' }
$errorLines.Count | Should -Be 0
$warnLines.Count  | Should -Be 0
Write-Host "Build clean: 0 errors, 0 warnings"
```

- [ ] **Step 2: Full test suite — no regressions**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --logger "console;verbosity=normal" 2>&1
```

Expected: all tests PASS.

- [ ] **Step 3: Commit all structural fixes**

```powershell
git -C C:\Users\Lance\Dev\Scripts add `
    csharp/src/ `
    csharp/tests/Scripts.Tests/StructuralTests/
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t4-02c): zero-warning build — all structural inspections resolved"
```

---

## Acceptance Criteria

- [ ] Every `*Async` method in every repository has `CancellationToken ct = default` as last parameter
- [ ] `TextNormalizer` and `ServiceRegistration` (and any other internal-only types) are `internal`
- [ ] `SpectreTypeRegistrar` is instantiated in `Program.cs` with no `[SuppressMessage]`
- [ ] `dotnet build csharp/Scripts.slnx` → `0 Error(s). 0 Warning(s).`
- [ ] All tests pass (no regressions)
