# T1-11: Compiled Model Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Generate and commit an EF Core compiled model for `ScriptsDbContext` using `dotnet ef dbcontext optimize`, reducing cold-start time by avoiding runtime model discovery.

**Architecture:** Add `<EFOptimizeContext>true</EFOptimizeContext>` and `<EFScaffoldModelStage>build</EFScaffoldModelStage>` to `CSharpScripts.csproj` so the compiled model regenerates on every `dotnet build` when the EF model changes. Add `Microsoft.EntityFrameworkCore.Tasks` NuGet package. Run `dotnet ef dbcontext optimize` to generate `CompiledModels/` output. EF9+ auto-detects the compiled model — no `.UseModel()` call needed.

**Key Findings from Research:**
- Compiled models bypass OnModelCreating reflection overhead — pre-generated source code loads entity metadata instantly at startup
- EF Core 9+ auto-detects compiled model when DbContext and model types are in same assembly — no .UseModel() call needed
- PendingModelChangesWarning (EF Core 9+): If OnModelCreating changes, migration snapshot must be updated and compiled model regenerated
- Workflow: Modify OnModelCreating → `dotnet ef migrations add <Name>` → `dotnet ef database update` → `dotnet ef dbcontext optimize`
- MSBuild properties: EFOptimizeContext=true enables optimization, EFScaffoldModelStage=build regenerates on every build
- Microsoft.EntityFrameworkCore.Tasks package provides the build-time model generation task
- Output directory: CompiledModels/ (configurable via --output-dir flag)
- Namespace: CSharpScripts.Data.Compiled (configurable via --namespace flag)
- No code changes needed in DbContext — EF auto-detects and uses compiled model

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Prerequisites

- T1-10 completed (all EF10 guard tests green)
- Docker running, `$env:PGCONNSTR` loaded and valid
- `ScriptsDbContext` with 8 DbSets compiled and accessible
- `ScriptsDbContextFactory` (IDesignTimeDbContextFactory) exists at `csharp/src/Data/ScriptsDbContextFactory.cs`

```powershell
# Verify prerequisites
docker ps 2>&1 | Select-String "healthy"
# Expected: container listed

if (-not $env:PGCONNSTR) {
    Get-Content C:\Users\Lance\Dev\Scripts\.env | ForEach-Object {
        if ($_ -match '^([^#][^=]+)=(.+)$') {
            [System.Environment]::SetEnvironmentVariable($Matches[1], $Matches[2])
        }
    }
}
Write-Host "PGCONNSTR: $env:PGCONNSTR" -ForegroundColor Green
# Expected: PGCONNSTR set to PostgreSQL connection string
```

---

## Task 1 — Add MSBuild Properties and Tasks Package

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj`

### Step 0: Preflight

```powershell
# Current state: No EFOptimizeContext property, no Microsoft.EntityFrameworkCore.Tasks package.
# Reason: EF compiled model requires these MSBuild properties and the Tasks package.
# What: Add <EFOptimizeContext>true</EFOptimizeContext>, <EFScaffoldModelStage>build</EFScaffoldModelStage>,
#       and <PackageReference Include="Microsoft.EntityFrameworkCore.Tasks" Version="*" />.
# Expected: csproj modified, dotnet restore succeeds.

$csproj = 'C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj'
Test-Path $csproj
# Expected: True

Get-Content $csproj | Select-String "EFOptimizeContext"
# Expected: (no output — property does not exist)

Get-Content $csproj | Select-String "Microsoft.EntityFrameworkCore.Tasks"
# Expected: (no output — package not referenced)
```

### Step 1: Write test

```csharp
// C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\CompiledModel\CompiledModelGenerationTests.cs
using System.Xml.Linq;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.CompiledModel;

public sealed class CompiledModelGenerationTests
{
    private static readonly string CsprojPath =
        @"C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj";

    [Test]
    public async Task Csproj_Contains_EFOptimizeContext_Property()
    {
        var xml = await File.ReadAllTextAsync(CsprojPath);
        var doc = XDocument.Parse(xml);
        var ns = doc.Root!.GetDefaultNamespace();

        var propertyGroup = doc.Root
            .Elements(ns + "PropertyGroup")
            .FirstOrDefault();

        propertyGroup.Should().NotBeNull();

        var optimizeElement = propertyGroup!
            .Elements(ns + "EFOptimizeContext")
            .FirstOrDefault();

        optimizeElement.Should().NotBeNull("because EFOptimizeContext must be true for compiled model");
        optimizeElement!.Value.Should().Be("true");
    }

    [Test]
    public async Task Csproj_Contains_EFScaffoldModelStage_Property()
    {
        var xml = await File.ReadAllTextAsync(CsprojPath);
        var doc = XDocument.Parse(xml);
        var ns = doc.Root!.GetDefaultNamespace();

        var propertyGroup = doc.Root
            .Elements(ns + "PropertyGroup")
            .FirstOrDefault();

        propertyGroup.Should().NotBeNull();

        var scaffoldElement = propertyGroup!
            .Elements(ns + "EFScaffoldModelStage")
            .FirstOrDefault();

        scaffoldElement.Should().NotBeNull("because EFScaffoldModelStage must be 'build'");
        scaffoldElement!.Value.Should().Be("build");
    }

    [Test]
    public async Task Csproj_References_EntityFrameworkCore_Tasks()
    {
        var xml = await File.ReadAllTextAsync(CsprojPath);
        var doc = XDocument.Parse(xml);
        var ns = doc.Root!.GetDefaultNamespace();

        var taskRef = doc.Root!
            .Elements(ns + "ItemGroup")
            .SelectMany(g => g.Elements(ns + "PackageReference"))
            .FirstOrDefault(e => e.Attribute("Include")?.Value
                == "Microsoft.EntityFrameworkCore.Tasks");

        taskRef.Should().NotBeNull("because compiled model generation requires the Tasks package");
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\CompiledModel\CompiledModelGenerationTests.cs'
Test-Path $file
# Expected: True

Get-Content $file | Select-String "EFOptimizeContext_Property"
# Expected: Csproj_Contains_EFOptimizeContext_Property
```

### Step 3: Run test (expect RED — properties and package not yet added)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "CompiledModelGenerationTests" 2>&1
```

Expected: RED — 3 tests fail. `Csproj_Contains_EFOptimizeContext_Property` fails with `optimizeElement is null`. `Csproj_Contains_EFScaffoldModelStage_Property` fails similarly. `Csproj_References_EntityFrameworkCore_Tasks` fails — package not found.

### Step 4: Assess

Three MSBuild changes needed:
1. Add `<EFOptimizeContext>true</EFOptimizeContext>` inside `<PropertyGroup>`
2. Add `<EFScaffoldModelStage>build</EFScaffoldModelStage>` inside same `<PropertyGroup>`
3. Add `<PackageReference Include="Microsoft.EntityFrameworkCore.Tasks" Version="*" />` inside `<ItemGroup>`

### Step 5: Implement

Add to `C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj` in the `<PropertyGroup>` block (after line 8 `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`):

```xml
		<EFOptimizeContext>true</EFOptimizeContext>
		<EFScaffoldModelStage>build</EFScaffoldModelStage>
```

Add in the `<ItemGroup>` block (after line 38 `</PackageReference>` closing `Microsoft.EntityFrameworkCore.Tools`):

```xml
		<PackageReference Include="Microsoft.EntityFrameworkCore.Tasks" Version="*" />
```

Full replacement details:

**Edit 1** — Insert after line 8 (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`):

OLD:
```xml
		<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
	</PropertyGroup>
```

NEW:
```xml
		<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
		<EFOptimizeContext>true</EFOptimizeContext>
		<EFScaffoldModelStage>build</EFScaffoldModelStage>
	</PropertyGroup>
```

**Edit 2** — Insert after line 39 (closing `</PackageReference>` for `Microsoft.EntityFrameworkCore.Tools`):

OLD:
```xml
		<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="*">
		</PackageReference>
```

NEW:
```xml
		<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="*">
		</PackageReference>
		<PackageReference Include="Microsoft.EntityFrameworkCore.Tasks" Version="*" />
```

**Edit 3** — Run restore:

```powershell
dotnet restore C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj 2>&1
```

Expected: Restore completed successfully.

**Edit 4** — Verify build:

```powershell
dotnet build C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj --no-restore 2>&1
```

Expected: Build succeeded with 0 errors.

### Step 6: Run test (expect GREEN)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "CompiledModelGenerationTests" 2>&1
```

Expected: GREEN — all 3 tests pass (EFOptimizeContext=true, EFScaffoldModelStage=build, Tasks package referenced).

### Step 7: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\CompiledModel\CompiledModelGenerationTests.cs
git commit -m "feat(t1-11): add ef optimize context msbuild properties and tasks package"
```

---

## Task 2 — Run `dotnet ef dbcontext optimize` and Verify Output

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\CompiledModels\` (generated directory)
- Verify: No `.UseModel()` call exists in codebase

### Step 0: Preflight

```powershell
# Current state: No CompiledModels/ directory exists. No compiled model generated.
# Reason: EF cold-start time must be reduced. Compiled model skips runtime model discovery.
# What: Run dotnet ef dbcontext optimize, verify output exists.
# Expected: CompiledModels/ directory created with ScriptsDbContextModel.cs and related files.

Test-Path C:\Users\Lance\Dev\Scripts\csharp\CompiledModels
# Expected: False

# Verify dotnet ef tool is available
dotnet ef --version 2>&1
# Expected: Entity Framework Core .NET Command-line Tools 10.0.x
```

### Step 1: Write test

```csharp
// C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\CompiledModel\CompiledModelFileTests.cs
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.CompiledModel;

public sealed class CompiledModelFileTests
{
    private static readonly string CompiledModelDir =
        @"C:\Users\Lance\Dev\Scripts\csharp\CompiledModels";

    [Test]
    public async Task CompiledModels_Directory_Exists()
    {
        var dirInfo = new DirectoryInfo(CompiledModelDir);
        dirInfo.Exists.Should().BeTrue(
            "because dotnet ef dbcontext optimize generates CompiledModels/"
        );
    }

    [Test]
    public async Task CompiledModels_Contains_ScriptsDbContextModel()
    {
        var modelFiles = Directory.GetFiles(
            CompiledModelDir,
            "ScriptsDbContextModel*.cs",
            SearchOption.TopDirectoryOnly
        );

        modelFiles.Should().NotBeEmpty(
            "because compiled model must include ScriptsDbContextModel.cs"
        );
    }

    [Test]
    public async Task CompiledModels_Contains_ScriptsDbContextModelBuilder()
    {
        var builderFile = Path.Combine(
            CompiledModelDir,
            "ScriptsDbContextModelBuilder.cs"
        );

        File.Exists(builderFile).Should().BeTrue(
            "because compiled model must include the model builder"
        );
    }

    [Test]
    public async Task No_UseModel_Call_Exists_In_Source()
    {
        var srcRoot = @"C:\Users\Lance\Dev\Scripts\csharp\src";
        var allFiles = Directory.GetFiles(srcRoot, "*.cs", SearchOption.AllDirectories);

        var violations = new List<string>();
        foreach (var file in allFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            if (content.Contains(".UseModel("))
                violations.Add(file);
        }

        violations.Should().BeEmpty(
            "because EF9+ auto-detects compiled model — .UseModel() is an anti-pattern"
        );
    }

    [Test]
    public async Task Build_Succeeds_After_CompiledModel_Generation()
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
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        process.ExitCode.Should().Be(0,
            $"because compiled model must not break build.\nStdOut: {output}\nStdErr: {error}"
        );
    }
}
```

### Step 2: Readback

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\CompiledModel\CompiledModelFileTests.cs'
Test-Path $file
# Expected: True
```

### Step 3: Run test (expect RED — no CompiledModels directory yet)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "CompiledModelFileTests" 2>&1
```

Expected: RED. `CompiledModels_Directory_Exists` fails — directory doesn't exist. `Build_Succeeds_After_CompiledModel_Generation` may fail depending on prior state (we haven't optimized yet).

### Step 4: Assess

Must run `dotnet ef dbcontext optimize` to generate the compiled model. This requires:
1. `$env:PGCONNSTR` set (factory reads it)
2. Docker PostgreSQL running (factory connects to read schema)
3. The command: `dotnet ef dbcontext optimize --project csharp/CSharpScripts.csproj --output-dir CompiledModels --namespace CSharpScripts.Data.Compiled`

### Step 5: Implement

```powershell
# Ensure PGCONNSTR loaded and Docker running
if (-not $env:PGCONNSTR) {
    throw "PGCONNSTR is not set. Load .env first."
}

# Run the optimize command
dotnet ef dbcontext optimize `
    --project C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj `
    --output-dir CompiledModels `
    --namespace CSharpScripts.Data.Compiled 2>&1
```

Expected: Command completes successfully, directory `C:\Users\Lance\Dev\Scripts\csharp\CompiledModels\` created with multiple `.cs` files.

```powershell
# Verify generated files
Get-ChildItem C:\Users\Lance\Dev\Scripts\csharp\CompiledModels\*.cs | Select-Object Name
# Expected: ScriptsDbContextModel.cs, ScriptsDbContextModelBuilder.cs, plus entity type files

# Verify .UseModel() is NOT called anywhere
$result = Get-ChildItem C:\Users\Lance\Dev\Scripts\csharp\src\*.cs -Recurse | Select-String ".UseModel(" -SimpleMatch
if ($result) { throw ".UseModel() found — this is an anti-pattern in EF9+" }
Write-Host "No .UseModel() calls found — correct." -ForegroundColor Green
```

```powershell
# Verify build passes with compiled model
dotnet build C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj 2>&1
```

Expected: Build succeeded with 0 errors and 0 warnings.

### Step 6: Run test (expect GREEN)

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "CompiledModelFileTests" 2>&1
```

Expected: GREEN — all 5 tests pass:
- `CompiledModels_Directory_Exists`: PASS
- `CompiledModels_Contains_ScriptsDbContextModel`: PASS
- `CompiledModels_Contains_ScriptsDbContextModelBuilder`: PASS
- `No_UseModel_Call_Exists_In_Source`: PASS
- `Build_Succeeds_After_CompiledModel_Generation`: PASS

### Step 7: Commit

```powershell
git add C:\Users\Lance\Dev\Scripts\csharp\CompiledModels\
git add C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\CompiledModel\CompiledModelFileTests.cs
git commit -m "feat(t1-11): generate ef core compiled model for scripts dbcontext"
```

---

## Verification Checklist

- [ ] `CSharpScripts.csproj` contains `<EFOptimizeContext>true</EFOptimizeContext>`
- [ ] `CSharpScripts.csproj` contains `<EFScaffoldModelStage>build</EFScaffoldModelStage>`
- [ ] `CSharpScripts.csproj` references `Microsoft.EntityFrameworkCore.Tasks`
- [ ] `csharp/CompiledModels/` directory exists with generated `.cs` files
- [ ] `ScriptsDbContextModel.cs` exists in CompiledModels
- [ ] `ScriptsDbContextModelBuilder.cs` exists in CompiledModels
- [ ] No `.UseModel()` call exists anywhere in `csharp/src/`
- [ ] `dotnet build` passes with 0 errors and 0 warnings
- [ ] `dotnet test` — CompiledModelGenerationTests: 3/3 PASS
- [ ] `dotnet test` — CompiledModelFileTests: 5/5 PASS
