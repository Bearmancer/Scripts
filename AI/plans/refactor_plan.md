# Namespace, Access Modifier & Path Resolution Refactor — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement.

**Goal:** Rename `CSharpScripts.*` namespace to `Scripts.*` in 160 src files, 61 test files. Enforce `internal sealed class` on test classes. Consolidate path resolution via `TestPaths`. Add `<RootNamespace>` to csproj files.

**Architecture:** Bulk `sed` rename (src, tests). Surgical fixes for reflection strings, Windows paths, access modifiers. Verify via `dotnet build` + test.

**Tech Stack:** .NET 10, C#, TUnit, sed, dotnet CLI

---

## Final Schema (Target State)

| Dimension | Final Value |
|-----------|------------|
| Source root namespace | `Scripts` |
| Source sub-namespaces | `Scripts.Core`, `Scripts.Data.*`, `Scripts.Infrastructure`, `Scripts.Models`, `Scripts.Services.*`, `Scripts.Orchestrators`, `Scripts.CLI.*` |
| Test root namespace | `Scripts.Tests` |
| Test sub-namespaces | `Scripts.Tests.<SubFolder>` |
| Main csproj `RootNamespace` | `Scripts` |
| Test csproj `RootNamespace` | `Scripts.Tests` |
| `InternalsVisibleTo` | `Scripts.Tests` |
| Assembly name | `tools` |
| Test class access modifier | `internal sealed class` |
| Test helper access modifier | `internal static class` / `internal abstract class` |
| Path resolution | `TestPaths.*` |
| Hardcoded paths | `TestPaths.Combine(...)` |

---

## Task 1 — csproj: Lock in `RootNamespace`

**Files:**
- Modify: `csharp/CSharpScripts.csproj`
- Modify: `csharp/tests/Scripts.Tests/Scripts.Tests.csproj`

- [ ] **Step 1: Add `<RootNamespace>Scripts</RootNamespace>` to main csproj**

In `csharp/CSharpScripts.csproj`, inside `<PropertyGroup>`:
```xml
<RootNamespace>Scripts</RootNamespace>
```

- [ ] **Step 2: Add `<RootNamespace>Scripts.Tests</RootNamespace>` to test csproj**

In `csharp/tests/Scripts.Tests/Scripts.Tests.csproj`, inside `<PropertyGroup>`:
```xml
<RootNamespace>Scripts.Tests</RootNamespace>
```

- [ ] **Step 3: Restore**

```bash
dotnet restore /home/lance/Scripts/csharp/Scripts.slnx
```
Expected: `Restore complete.`

---

## Task 2 — Bulk rename source namespaces: `CSharpScripts` → `Scripts`

**Files:** 160 `*.cs` files in `csharp/src/` (excludes `bin/`, `obj/`)

- [ ] **Step 1: Run bulk rename on src/**

```bash
find /home/lance/Scripts/csharp/src -name "*.cs" \
  ! -path "*/bin/*" ! -path "*/obj/*" \
  -exec sed -i 's/CSharpScripts\./Scripts./g' {} +
```

Note: replaces `CSharpScripts.` tokens (`namespace`, `using`, `global using`, aliases, references).

- [ ] **Step 2: Verify no `CSharpScripts` token remains in src/**

```bash
grep -rn "CSharpScripts" /home/lance/Scripts/csharp/src --include="*.cs" \
  | grep -v "/bin/" | grep -v "/obj/"
```
Expected: **no output**

- [ ] **Step 3: Verify GlobalUsings.cs correct**

```bash
cat /home/lance/Scripts/csharp/src/GlobalUsings.cs
```
Expected key lines:
```csharp
global using Scripts.Core;
global using Scripts.Core.Auth;
global using Scripts.Data;
global using Scripts.Data.State;
global using Scripts.Models;
global using Scripts.Services.Language;
global using Log = Scripts.Core.Log;
global using SearchResult = Scripts.Models.SearchResult;
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Scripts.Tests")]
```

- [ ] **Step 4: Build source project**

```bash
dotnet build /home/lance/Scripts/csharp/CSharpScripts.csproj -v q 2>&1 | tail -10
```
Expected: `Build succeeded.`

---

## Task 3 — Bulk rename test namespaces: `CSharpScripts` → `Scripts`

**Files:** 61 `*.cs` files in `csharp/tests/Scripts.Tests/` (excludes `bin/`, `obj/`)

- [ ] **Step 1: Run bulk rename on tests/**

```bash
find /home/lance/Scripts/csharp/tests/Scripts.Tests -name "*.cs" \
  ! -path "*/bin/*" ! -path "*/obj/*" \
  -exec sed -i 's/CSharpScripts\./Scripts./g' {} +
```

- [ ] **Step 2: Verify no `CSharpScripts` token remains in tests/**

```bash
grep -rn "CSharpScripts" /home/lance/Scripts/csharp/tests/Scripts.Tests --include="*.cs" \
  | grep -v "/bin/" | grep -v "/obj/"
```
Expected: **no output** (exceptions fixed in Tasks 4–5)

---

## Task 4 — Fix reflection strings + assembly names

**Files:**
- `csharp/tests/Scripts.Tests/StateManager/StateManagerDeleteTests.cs`
- `csharp/tests/Scripts.Tests/StateManager/StateManagerNamespaceTests.cs`
- `csharp/tests/Scripts.Tests/SyncService/LastFmServiceDeleteTests.cs`

> Task 3 sed replaced `CSharpScripts.` with `Scripts.`. `Type.GetType` strings need manual fixes for assembly names. `LastFmServiceDeleteTests` uses `CSharpScripts` as assembly name instead of `tools`.

- [ ] **Step 1: Check Task 3 sed output**

```bash
grep -n "Type.GetType\|assembly" \
  /home/lance/Scripts/csharp/tests/Scripts.Tests/StateManager/StateManagerDeleteTests.cs \
  /home/lance/Scripts/csharp/tests/Scripts.Tests/StateManager/StateManagerNamespaceTests.cs \
  /home/lance/Scripts/csharp/tests/Scripts.Tests/SyncService/LastFmServiceDeleteTests.cs
```

- [ ] **Step 2: Fix `LastFmServiceDeleteTests.cs` assembly reference**

Sed missed the assembly name because no trailing dot. Fix manually to use `tools`:

```csharp
var inlineType = Type.GetType("Scripts.Services.Sync.LastFm.Scrobble, tools");
```

- [ ] **Step 3: Fix `StateManagerNamespaceTests.cs` error messages**

Sed missed `CSharpScripts` in `.Should().NotBeNull(because: ...)` strings because no trailing dot. Fix manually. Check:

```bash
grep -n "CSharpScripts" /home/lance/Scripts/csharp/tests/Scripts.Tests/StateManager/StateManagerNamespaceTests.cs
```

Replace all 5 occurrences: `"StateManager must live in CSharpScripts.Data.State namespace"` → `"StateManager must live in Scripts.Data.State namespace"`

- [ ] **Step 4: Re-verify**

```bash
grep -n "CSharpScripts" \
  /home/lance/Scripts/csharp/tests/Scripts.Tests/StateManager/StateManagerDeleteTests.cs \
  /home/lance/Scripts/csharp/tests/Scripts.Tests/StateManager/StateManagerNamespaceTests.cs \
  /home/lance/Scripts/csharp/tests/Scripts.Tests/SyncService/LastFmServiceDeleteTests.cs
```
Expected: **no output**

---

## Task 5 — Fix `GlobalSetup.cs`: remove spurious `using Scripts.Tests;`

**Files:**
- `csharp/tests/Scripts.Tests/GlobalSetup.cs`

> `GlobalSetup.cs` now in namespace `Scripts.Tests`. `using Scripts.Tests;` causes `CS8019`, fails build.

- [ ] **Step 1: Remove `using Scripts.Tests;`**

```bash
sed -i '/^using Scripts\.Tests;$/d' /home/lance/Scripts/csharp/tests/Scripts.Tests/GlobalSetup.cs
```

- [ ] **Step 2: Verify**

```bash
head -5 /home/lance/Scripts/csharp/tests/Scripts.Tests/GlobalSetup.cs
```
Expected first line: `namespace Scripts.Tests;`

---

## Task 6 — Fix `ReleaseProgress` namespace: drop `Tests` suffix

**Files:**
- `csharp/tests/Scripts.Tests/ReleaseProgress/ReleaseProgressConfigurationTests.cs`
- `csharp/tests/Scripts.Tests/ReleaseProgress/ReleaseProgressEntityTests.cs`
- `csharp/tests/Scripts.Tests/ReleaseProgress/ReleaseProgressServiceTests.cs`

> Namespace `Scripts.Tests.ReleaseProgressTests` redundant. Use `Scripts.Tests.ReleaseProgress`.

- [ ] **Step 1: Rename namespace**

```bash
sed -i 's/namespace Scripts\.Tests\.ReleaseProgressTests/namespace Scripts.Tests.ReleaseProgress/g' \
  /home/lance/Scripts/csharp/tests/Scripts.Tests/ReleaseProgress/ReleaseProgressConfigurationTests.cs \
  /home/lance/Scripts/csharp/tests/Scripts.Tests/ReleaseProgress/ReleaseProgressEntityTests.cs \
  /home/lance/Scripts/csharp/tests/Scripts.Tests/ReleaseProgress/ReleaseProgressServiceTests.cs
```

- [ ] **Step 2: Verify**

```bash
grep "^namespace" /home/lance/Scripts/csharp/tests/Scripts.Tests/ReleaseProgress/*.cs
```
Expected: `namespace Scripts.Tests.ReleaseProgress;`

---

## Task 7 — Fix access modifiers: seal test classes

**Files:**
- `csharp/tests/Scripts.Tests/Guards/EF11GuardTests.cs`
- `csharp/tests/Scripts.Tests/OcrTest.cs`
- `csharp/tests/Scripts.Tests/EntityConfigs/AlbumTrackAdditionalTests.cs`
- `csharp/tests/Scripts.Tests/EntityConfigs/ExecutionLogConfigurationAdditionalTests.cs`
- `csharp/tests/Scripts.Tests/EntityConfigs/FailedTaskAdditionalTests.cs`
- `csharp/tests/Scripts.Tests/EntityConfigs/VideoConfigurationAdditionalTests.cs`

- [ ] **Step 1: Add `sealed`**

```bash
sed -i \
  's/internal class EF11GuardTests/internal sealed class EF11GuardTests/' \
  /home/lance/Scripts/csharp/tests/Scripts.Tests/Guards/EF11GuardTests.cs

sed -i \
  's/internal class OcrTest/internal sealed class OcrTest/' \
  /home/lance/Scripts/csharp/tests/Scripts.Tests/OcrTest.cs

sed -i \
  's/internal class AlbumTrackAdditionalTests/internal sealed class AlbumTrackAdditionalTests/' \
  /home/lance/Scripts/csharp/tests/Scripts.Tests/EntityConfigs/AlbumTrackAdditionalTests.cs

sed -i \
  's/internal class ExecutionLogConfigurationAdditionalTests/internal sealed class ExecutionLogConfigurationAdditionalTests/' \
  /home/lance/Scripts/csharp/tests/Scripts.Tests/EntityConfigs/ExecutionLogConfigurationAdditionalTests.cs

sed -i \
  's/internal class FailedTaskAdditionalTests/internal sealed class FailedTaskAdditionalTests/' \
  /home/lance/Scripts/csharp/tests/Scripts.Tests/EntityConfigs/FailedTaskAdditionalTests.cs

sed -i \
  's/internal class VideoConfigurationAdditionalTests/internal sealed class VideoConfigurationAdditionalTests/' \
  /home/lance/Scripts/csharp/tests/Scripts.Tests/EntityConfigs/VideoConfigurationAdditionalTests.cs
```

- [ ] **Step 2: Verify**

```bash
grep -rn "^\s*internal class " /home/lance/Scripts/csharp/tests/Scripts.Tests --include="*.cs" \
  | grep -v "/bin/" | grep -v "/obj/"
```
Expected: **no output**

---

## Task 8 — Consolidate path resolution: use `TestPaths.*`

**Files:**
- `csharp/tests/Scripts.Tests/Language/NTextCatRemovalGuardTests.cs`
- `csharp/tests/Scripts.Tests/SignOff/TestSuiteHealthTests.cs`
- `csharp/tests/Scripts.Tests/Language/LinguaPackageReferenceTests.cs`

- [ ] **Step 1: `NTextCatRemovalGuardTests.cs`**

Replace:
```csharp
private static readonly string SourceRoot = TestPaths.Combine("csharp", "src");
```
With:
```csharp
private static readonly string SourceRoot = TestPaths.SrcRoot;
```

- [ ] **Step 2: `TestSuiteHealthTests.cs`**

Replace:
```csharp
private static string TestsRoot => TestPaths.Combine("csharp", "tests", "Scripts.Tests");
```
With:
```csharp
private static string TestsRoot => TestPaths.TestsRoot;
```

- [ ] **Step 3: `LinguaPackageReferenceTests.cs`**

Keep as `TestPaths.Combine("csharp", "CSharpScripts.csproj")` (already correct).

---

## Task 9 — Fix hardcoded Windows paths

**Files:**
- `csharp/tests/Scripts.Tests/StateManager/StateManagerDeleteTests.cs`
- `csharp/tests/Scripts.Tests/SyncService/LastFmServiceDeleteTests.cs`

> `C:\Users\Lance\Dev\Scripts\...` fails on Linux.

- [ ] **Step 1: Fix `StateManagerDeleteTests.cs`**

Replace:
```csharp
var filePath = @"C:\Users\Lance\Dev\Scripts\csharp\src\Core\Persistence\StateManager.cs";
System.IO.File.Exists(filePath).Should().BeFalse(because: "Core/Persistence/StateManager.cs must be deleted");
```
With:
```csharp
var filePath = TestPaths.Combine("csharp", "src", "Core", "Persistence", "StateManager.cs");
File.Exists(filePath).Should().BeFalse(because: "Core/Persistence/StateManager.cs must be deleted");
```

- [ ] **Step 2: Fix `LastFmServiceDeleteTests.cs`**

Replace legacy check:
```csharp
var path = @"C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFm\LastFmService.cs";
System.IO.File.Exists(path).Should().BeFalse(
    because: "Legacy duplicate LastFmService must be deleted — canonical version is at Services/Sync/LastFmService.cs");
```
With:
```csharp
var path = TestPaths.Combine("csharp", "src", "Services", "Sync", "LastFm", "LastFmService.cs");
File.Exists(path).Should().BeFalse(
    because: "Legacy duplicate LastFmService must be deleted — canonical version is at Services/Sync/LastFmService.cs");
```

Replace canonical check:
```csharp
var path = @"C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\LastFmService.cs";
System.IO.File.Exists(path).Should().BeTrue(
    because: "Canonical LastFmService at Services/Sync/LastFmService.cs must be preserved");
```
With:
```csharp
var path = TestPaths.Combine("csharp", "src", "Services", "Sync", "LastFmService.cs");
File.Exists(path).Should().BeTrue(
    because: "Canonical LastFmService at Services/Sync/LastFmService.cs must be preserved");
```

---

## Task 10 — Full build & test verify

- [ ] **Step 1: Restore**

```bash
dotnet restore /home/lance/Scripts/csharp/Scripts.slnx
```
Expected: `Restore complete.`

- [ ] **Step 2: Build**

```bash
dotnet build /home/lance/Scripts/csharp/Scripts.slnx -v q 2>&1
```
Expected: `Build succeeded.`, `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 3: Namespace audit**

```bash
grep -rn "CSharpScripts" \
  /home/lance/Scripts/csharp/src \
  /home/lance/Scripts/csharp/tests/Scripts.Tests \
  --include="*.cs" | grep -v "/bin/" | grep -v "/obj/"
```
Expected: **no output**

- [ ] **Step 4: Access modifier audit**

```bash
grep -rn "^\s*internal class " /home/lance/Scripts/csharp/tests/Scripts.Tests --include="*.cs" \
  | grep -v "/bin/" | grep -v "/obj/"
```
Expected: **no output**

- [ ] **Step 5: Path resolution audit**

```bash
grep -rn "AppContext\.BaseDirectory" \
  /home/lance/Scripts/csharp/tests/Scripts.Tests --include="*.cs" \
  | grep -v "/bin/" | grep -v "/obj/"
```
Expected: **no output**

- [ ] **Step 6: Hardcoded path audit**

```bash
grep -rn 'C:\\Users\|@"C:\\' \
  /home/lance/Scripts/csharp/tests/Scripts.Tests --include="*.cs" \
  | grep -v "/bin/" | grep -v "/obj/"
```
Expected: **no output**

- [ ] **Step 7: Run tests**

```bash
dotnet test /home/lance/Scripts/csharp/Scripts.slnx --no-build -v q 2>&1 | tail -20
```
Expected: 0 failures.

- [ ] **Step 8: Commit**

```bash
cd /home/lance/Scripts && git add -A && git commit -m "refactor: rename CSharpScripts.* to Scripts.* everywhere

- Add RootNamespace=Scripts / Scripts.Tests to csproj files
- Bulk rename 160 src + 61 test files (sed: CSharpScripts. -> Scripts.)
- Fix reflection Type.GetType strings: assembly name corrected to 'tools'
- Fix ReleaseProgressTests namespace -> Scripts.Tests.ReleaseProgress
- Seal non-sealed test classes (internal sealed class)
- Consolidate path resolution to TestPaths.SrcRoot/TestsRoot/CSharpRoot
- Fix Windows paths in StateManagerDeleteTests/LastFmServiceDeleteTests"
```
