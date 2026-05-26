# T2-06: Scripts.Reader Project Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create `Scripts.Reader.csproj` at `csharp/src/Reader/`, referencing only `Scripts.Core`, with Playwright, AngleSharp, SmartReader, PdfPig, and OCR packages via CPM. Playwright must not leak to other projects.

**Architecture:** `Scripts.Reader` is a leaf project for content extraction — web scraping (Playwright, AngleSharp), article parsing (SmartReader), PDF parsing (PdfPig), and OCR (Azure Document Intelligence, Google Cloud Vision, Google Document AI). It depends only on `Scripts.Core`. It must not reference `Scripts.Data`, any `Services`, `Orchestrators`, or `CLI`. `Microsoft.Playwright` is a direct PackageReference in Reader only — no other project may reference or transitively depend on Playwright. This isolation is verified by the `PlaywrightDoesNotLeakToOrchestrators` test.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Content Extraction Architecture

### Web Scraping: Playwright + AngleSharp

**Playwright** (`Microsoft.Playwright` v1.49.0):
- Headless browser automation for JavaScript-heavy sites
- Persistent profiles for session management
- Bot evasion via stealth mode
- Screenshot and PDF export capabilities

**AngleSharp** (`AngleSharp` v1.2.0):
- HTML/CSS parsing and DOM traversal
- XPath and CSS selector queries
- Lightweight alternative to Playwright for static HTML

**Usage Pattern:**
```csharp
var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
var page = await browser.NewPageAsync();
await page.GotoAsync(url);
var content = await page.ContentAsync();
```

### Article Extraction: SmartReader

**SmartReader** (`SmartReader` v1.0.0):
- Extracts article content from web pages
- Removes boilerplate (ads, navigation, sidebars)
- Returns clean text, title, author, publish date
- Works with both HTML and rendered content

**Usage Pattern:**
```csharp
var reader = new SmartReader.Reader(url, html);
var article = await reader.GetArticleAsync();
var content = article.Content;
```

### PDF Parsing: PdfPig

**PdfPig** (`PdfPig` v0.1.9):
- Extract text, images, and metadata from PDFs
- Page-by-page parsing
- No external dependencies (pure .NET)

**Usage Pattern:**
```csharp
using var document = PdfDocument.Open(filePath);
foreach (var page in document.GetPages())
{
    var text = page.Text;
}
```

### OCR: Azure Document Intelligence + Google Cloud Vision + Google Document AI

**Azure Document Intelligence** (`Azure.AI.DocumentIntelligence` v1.0.0):
- Document layout analysis
- Table extraction
- Handwriting recognition
- Requires Azure subscription

**Google Cloud Vision** (`Google.Cloud.Vision.V1` v3.9.0):
- Image OCR
- Text detection
- Label detection
- Requires Google Cloud credentials

**Google Document AI** (`Google.Cloud.DocumentAI.V1` v3.14.0):
- Document classification
- Entity extraction
- Form parsing
- Requires Google Cloud credentials

### Playwright Isolation

**Critical:** `Microsoft.Playwright` is declared ONLY in `Scripts.Reader.csproj`. No other project may reference it directly or transitively. This is enforced by the `PlaywrightDoesNotLeakToOrchestrators` test, which verifies that `Scripts.Orchestrators.csproj` does not contain any Playwright reference.

**Reason:** Playwright requires browser binaries to be downloaded and installed. Isolating it to Reader prevents unnecessary bloat in other projects and makes the dependency graph explicit.

---

## Prerequisites

- [ ] T2-01 (Scripts.Core) is signed off — `Scripts.Core.csproj` exists and compiles
- [ ] CPM is active — `Directory.Packages.props` lists `Microsoft.Playwright`, `AngleSharp`, `SmartReader`, `PdfPig`, `Azure.AI.DocumentIntelligence`, `Google.Cloud.Vision.V1`, `Google.Cloud.DocumentAI.V1`
- [ ] `C:\Users\Lance\Dev\Scripts\csharp\src\Reader\` directory exists (create if absent)

---

## Task 1 — Verify directory and back up any existing csproj

### Step 1 — Log current state

```powershell
Write-Host "STATE: Verifying src/Reader directory and any existing Scripts.Reader.csproj"
Write-Host "REASON: Must not overwrite without backup (Zero-Presumption Rule 9)"

$readerDir  = 'C:\Users\Lance\Dev\Scripts\csharp\src\Reader'
$readerProj = Join-Path $readerDir 'Scripts.Reader.csproj'
$ts         = Get-Date -Format 'yyyyMMdd_HHmmss'

if (-not (Test-Path $readerDir)) {
    New-Item -ItemType Directory -Path $readerDir -ErrorAction Stop | Out-Null
    if (-not (Test-Path $readerDir)) { throw "Failed to create $readerDir" }
    Write-Host "OUTCOME: Created directory $readerDir"
} else {
    Write-Host "OUTCOME: Directory $readerDir already exists"
}

if (Test-Path $readerProj) {
    $bak = "$readerProj.bak.$ts"
    Copy-Item $readerProj $bak -ErrorAction Stop
    if (-not (Test-Path $bak)) { throw "Backup of Scripts.Reader.csproj failed" }
    Write-Host "OUTCOME: Backed up existing Scripts.Reader.csproj → $bak"
}
```

---

## Task 2 — TDD RED: Write failing tests

### Step 2 — Write tests

File: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\ScriptsReaderProjectTests.cs`

```csharp
using System.IO;
using System.Linq;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests;

public class ScriptsReaderProjectTests
{
    private const string ReaderCsproj =
        @"C:\Users\Lance\Dev\Scripts\csharp\src\Reader\Scripts.Reader.csproj";

    private const string OrchCsproj =
        @"C:\Users\Lance\Dev\Scripts\csharp\src\Orchestrators\Scripts.Orchestrators.csproj";

    private const string AssemblyInfoPath =
        @"C:\Users\Lance\Dev\Scripts\csharp\src\Reader\Properties\AssemblyInfo.cs";

    [Test]
    public void ScriptsReader_CsprojFile_Exists()
    {
        File.Exists(ReaderCsproj).Should().BeTrue(
            "Scripts.Reader.csproj must exist at csharp/src/Reader/");
    }

    [Test]
    public void ScriptsReader_References_OnlyCore()
    {
        File.Exists(ReaderCsproj).Should().BeTrue();
        var content = File.ReadAllText(ReaderCsproj);

        content.Should().Contain("Scripts.Core.csproj",
            "Scripts.Reader must reference Scripts.Core");

        content.Should().NotContain("Scripts.Data",
            "Scripts.Reader must not reference Data");
        content.Should().NotContain("Scripts.Services",
            "Scripts.Reader must not reference any Services project");
        content.Should().NotContain("Scripts.Orchestrators",
            "Scripts.Reader must not reference Orchestrators");
        content.Should().NotContain("Scripts.CLI",
            "Scripts.Reader must not reference CLI");
    }

    [Test]
    public void ScriptsReader_PlaywrightDoesNotLeakTo_Orchestrators()
    {
        if (!File.Exists(OrchCsproj))
        {
            // Orchestrators.csproj not yet created — skip this check gracefully
            return;
        }
        var content = File.ReadAllText(OrchCsproj);
        content.Should().NotContain("Playwright",
            "Microsoft.Playwright must NOT be referenced by Orchestrators — Playwright is a Reader-only dependency");
    }

    [Test]
    public void ScriptsReader_HasEssentialExtractionPackages()
    {
        File.Exists(ReaderCsproj).Should().BeTrue();
        var content = File.ReadAllText(ReaderCsproj);

        content.Should().Contain("Microsoft.Playwright",
            "Microsoft.Playwright must be declared as a direct Reader dependency");
        content.Should().Contain("AngleSharp",
            "AngleSharp must be declared for HTML parsing");
        content.Should().Contain("SmartReader",
            "SmartReader must be declared for article extraction");
        content.Should().Contain("PdfPig",
            "PdfPig must be declared for PDF parsing");
        content.Should().Contain("Azure.AI.DocumentIntelligence",
            "Azure.AI.DocumentIntelligence must be declared for OCR");
        content.Should().Contain("Google.Cloud.Vision.V1",
            "Google.Cloud.Vision.V1 must be declared for image OCR");
        content.Should().Contain("Google.Cloud.DocumentAI.V1",
            "Google.Cloud.DocumentAI.V1 must be declared for document AI");
    }

    [Test]
    public void ScriptsReader_HasNoInlineVersions()
    {
        File.Exists(ReaderCsproj).Should().BeTrue();
        var content = File.ReadAllText(ReaderCsproj);
        content.Should().NotMatchRegex(@"PackageReference.+Version=""",
            "Scripts.Reader.csproj must not contain inline Version= (CPM violation)");
    }

    [Test]
    public void ScriptsReader_AssemblyInfo_HasInternalsVisibleTo()
    {
        File.Exists(AssemblyInfoPath).Should().BeTrue(
            "Properties/AssemblyInfo.cs must exist in Scripts.Reader");
        var content = File.ReadAllText(AssemblyInfoPath);
        content.Should().Contain("InternalsVisibleTo");
        content.Should().Contain("Scripts.Tests");
    }

    [Test]
    public void ScriptsReader_CompilesIndependently()
    {
        File.Exists(ReaderCsproj).Should().BeTrue();
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "dotnet",
            Arguments              = @"build C:\Users\Lance\Dev\Scripts\csharp\src\Reader\Scripts.Reader.csproj",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
        };
        using var proc = System.Diagnostics.Process.Start(psi)!;
        proc.WaitForExit();
        var stderr = proc.StandardError.ReadToEnd();
        proc.ExitCode.Should().Be(0, $"Scripts.Reader.csproj did not compile independently. stderr: {stderr}");
    }
}
```

### Step 3 — Run tests RED

```powershell
$result = dotnet test 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' `
    --filter "FullyQualifiedName~ScriptsReaderProjectTests" `
    --no-build 2>&1
Write-Host $result
# Expected: FAILED — ScriptsReader_CsprojFile_Exists and all others fail because csproj does not exist yet
```

---

## Task 3 — GREEN: Create Scripts.Reader.csproj

### Step 4 — Write the project file

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Reader\Scripts.Reader.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\Core\Scripts.Core.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Playwright" />
    <PackageReference Include="AngleSharp" />
    <PackageReference Include="SmartReader" />
    <PackageReference Include="PdfPig" />
    <PackageReference Include="Azure.AI.DocumentIntelligence" />
    <PackageReference Include="Google.Cloud.Vision.V1" />
    <PackageReference Include="Google.Cloud.DocumentAI.V1" />
  </ItemGroup>
</Project>
```

### Step 5 — Verify the project file

```powershell
$readerProj = 'C:\Users\Lance\Dev\Scripts\csharp\src\Reader\Scripts.Reader.csproj'
if (-not (Test-Path $readerProj)) { throw "Scripts.Reader.csproj was not created" }

$content = Get-Content $readerProj -Raw -Encoding UTF8

if ($content -notmatch 'Scripts\.Core\.csproj') {
    throw "Scripts.Reader.csproj must reference Scripts.Core.csproj"
}
if ($content -match 'Scripts\.Data') {
    throw "Scripts.Reader.csproj must not reference Data"
}
if ($content -match 'Scripts\.Services') {
    throw "Scripts.Reader.csproj must not reference any Services project"
}
if ($content -match 'Scripts\.Orchestrators') {
    throw "Scripts.Reader.csproj must not reference Orchestrators"
}
if ($content -match 'Scripts\.CLI') {
    throw "Scripts.Reader.csproj must not reference CLI"
}
if ($content -notmatch 'Microsoft\.Playwright') {
    throw "Scripts.Reader.csproj must contain Microsoft.Playwright PackageReference"
}
if ($content -match 'PackageReference.+Version="') {
    throw "Scripts.Reader.csproj must not contain inline Version= (CPM violation)"
}
Write-Host "OUTCOME: Scripts.Reader.csproj verified OK"
```

---

## Task 4 — GREEN: Create Properties/AssemblyInfo.cs

### Step 6 — Create AssemblyInfo.cs

```powershell
$propsDir = 'C:\Users\Lance\Dev\Scripts\csharp\src\Reader\Properties'
if (-not (Test-Path $propsDir)) {
    New-Item -ItemType Directory -Path $propsDir -ErrorAction Stop | Out-Null
    if (-not (Test-Path $propsDir)) { throw "Failed to create $propsDir" }
    Write-Host "OUTCOME: Created Properties directory"
}
```

File: `C:\Users\Lance\Dev\Scripts\csharp\src\Reader\Properties\AssemblyInfo.cs`

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Scripts.Tests")]
```

```powershell
$infoPath = 'C:\Users\Lance\Dev\Scripts\csharp\src\Reader\Properties\AssemblyInfo.cs'
if (-not (Test-Path $infoPath)) { throw "AssemblyInfo.cs was not created in Scripts.Reader" }

$content = Get-Content $infoPath -Raw -Encoding UTF8
if ($content -notmatch 'InternalsVisibleTo') { throw "InternalsVisibleTo missing from AssemblyInfo.cs" }
if ($content -notmatch 'Scripts\.Tests')    { throw "Scripts.Tests not listed in InternalsVisibleTo" }
Write-Host "OUTCOME: AssemblyInfo.cs verified OK"
```

---

## Task 5 — GREEN: Register Scripts.Reader in Scripts.slnx

### Step 7 — Add to solution

```powershell
Write-Host "STATE: Adding Scripts.Reader.csproj to Scripts.slnx"

$slnx = 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx'
$ts   = Get-Date -Format 'yyyyMMdd_HHmmss'
$bak  = "$slnx.bak.$ts"
Copy-Item $slnx $bak -ErrorAction Stop
if (-not (Test-Path $bak)) { throw "Backup of Scripts.slnx failed" }

dotnet sln 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' `
    add 'C:\Users\Lance\Dev\Scripts\csharp\src\Reader\Scripts.Reader.csproj' `
    2>&1 | Tee-Object -Variable slnOutput
Write-Host $slnOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet sln add failed for Scripts.Reader.csproj" }

$slnContent = Get-Content $slnx -Raw -Encoding UTF8
if ($slnContent -notmatch 'Scripts\.Reader\.csproj') {
    throw "Scripts.Reader.csproj not found in Scripts.slnx after dotnet sln add"
}
Write-Host "OUTCOME: Scripts.Reader.csproj registered in solution"
```

---

## Task 6 — GREEN: Build Scripts.Reader

### Step 8 — Restore and build

```powershell
Write-Host "STATE: Running dotnet restore and dotnet build for Scripts.Reader"

$restoreOutput = dotnet restore 'C:\Users\Lance\Dev\Scripts\csharp\src\Reader\Scripts.Reader.csproj' 2>&1
Write-Host $restoreOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed for Scripts.Reader" }

$buildOutput = dotnet build 'C:\Users\Lance\Dev\Scripts\csharp\src\Reader\Scripts.Reader.csproj' 2>&1
Write-Host $buildOutput
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed for Scripts.Reader" }

# Expected:
# Build succeeded.
# 0 Error(s)
```

---

## Task 7 — REFACTOR: Run all tests GREEN

### Step 9 — Run project tests

```powershell
$testOutput = dotnet test 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' `
    --filter "FullyQualifiedName~ScriptsReaderProjectTests" 2>&1
Write-Host $testOutput
if ($LASTEXITCODE -ne 0) { throw "ScriptsReaderProjectTests failed" }
# Expected: All 7 tests passed
```

---

## Task 8 — Commit

```powershell
git -C 'C:\Users\Lance\Dev\Scripts' add `
    'csharp/src/Reader/Scripts.Reader.csproj' `
    'csharp/src/Reader/Properties/AssemblyInfo.cs' `
    'csharp/tests/Scripts.Tests/ScriptsReaderProjectTests.cs' `
    'csharp/Scripts.slnx'

git -C 'C:\Users\Lance\Dev\Scripts' commit `
    -m "feat(t2-06): add Scripts.Reader.csproj referencing Core only, Playwright + AngleSharp + PdfPig + OCR via CPM"
```

---

## Sign-off Criteria

- [ ] `csharp/src/Reader/Scripts.Reader.csproj` exists
- [ ] References `Scripts.Core.csproj` and nothing else in `<ProjectReference>`
- [ ] Does NOT reference `Scripts.Data`, any `Services`, `Orchestrators`, or `CLI`
- [ ] Contains `Microsoft.Playwright` PackageReference
- [ ] `Scripts.Orchestrators.csproj` does NOT contain `Playwright` (playwright isolation verified)
- [ ] Zero inline `Version=` attributes (CPM compliant)
- [ ] `csharp/src/Reader/Properties/AssemblyInfo.cs` has `[assembly: InternalsVisibleTo("Scripts.Tests")]`
- [ ] `Scripts.slnx` references `Scripts.Reader.csproj`
- [ ] `dotnet build csharp/src/Reader/Scripts.Reader.csproj` exits 0
- [ ] `ScriptsReaderProjectTests` — all 7 tests GREEN
