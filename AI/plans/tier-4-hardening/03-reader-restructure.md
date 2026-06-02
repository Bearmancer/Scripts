# Reader Directory Restructure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reorganize the flat `src/Reader/` directory into six subdirectories (`Extraction/`, `Local/`, `Ocr/`, `Output/`, `Quality/`, `Validation/`) so files group by responsibility, not alphabet.

**Architecture:** Each file move is: backup → `Move-Item` → `Test-Path` assertion on new location → `Test-Path` assertion on old location (must be gone) → `dotnet build` to confirm namespace resolution still works. No namespace changes — files stay in their current namespace. The `.csproj` uses a glob include so no project file edits are needed.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions / PowerShell

---

## Pre-flight

- [ ] **Step 0: Pre-flight validation**

```powershell
Get-Command pwsh   -ErrorAction Stop
Get-Command dotnet -ErrorAction Stop
Get-Command git    -ErrorAction Stop

# Confirm Reader project exists
$readerProj = '/home/lance/Scripts/csharp/src\Reader\Scripts.Reader.csproj'
Test-Path $readerProj | Should -Be $true

# Baseline build — must be green before any moves
dotnet build /home/lance/Scripts/csharp/Scripts.slnx -ErrorAction Stop 2>&1 | Tee-Object -Variable buildOut
$buildOut | Where-Object { $_ -match ' error ' } | Should -BeNullOrEmpty

# Inventory existing files
$readerDir = '/home/lance/Scripts/csharp/src\Reader'
Get-ChildItem $readerDir -Filter '*.cs' | Select-Object Name | Sort-Object Name
```

---

## Target Structure

```
src/Reader/
├── BrowserSession.cs
├── Scripts.Reader.csproj
├── Extraction/
│   ├── HtmlCleanupHelper.cs
│   ├── JstorExtractor.cs
│   └── StandardExtractor.cs
├── Local/
│   ├── LocalEpubExtractor.cs
│   ├── LocalImageExtractor.cs
│   └── LocalPdfExtractor.cs
├── Ocr/
│   ├── AzureDocumentIntelligenceOcrProvider.cs
│   ├── DocumentAiOcrProvider.cs
│   ├── GoogleVisionOcrProvider.cs
│   ├── IOcrProvider.cs
│   ├── OcrTextCleanup.cs
│   └── TesseractOcrProvider.cs
├── Output/
│   └── EpubWriter.cs
├── Quality/
│   ├── ArticleStructureDetector.cs
│   ├── PdfContentQuality.cs
│   ├── PdfTypeDetector.cs
│   └── WebExtractionQualityAnalyzer.cs
└── Validation/
    ├── CalibreClient.cs
    └── EpubValidator.cs
```

> **Adjustment rule:** If any file in the spec does not exist in the actual `src/Reader/` directory, skip it. If there are extra files not in the spec, assign them to the most logical subdirectory and document the choice in the commit message.

---

## Task 1: Write file-location tests

**Files:**
- Create: `csharp/tests/Scripts.Tests/StructuralTests/ReaderStructureTests.cs`

- [ ] **Step 1: Write failing structure tests**

```csharp
// csharp/tests/Scripts.Tests/StructuralTests/ReaderStructureTests.cs
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.StructuralTests;

public class ReaderStructureTests
{
    private const string ReaderRoot = @"/home/lance/Scripts/csharp/src\Reader";

    // Extraction
    [Test] public void JstorExtractor_IsIn_ExtractionDir()
        => File.Exists(Path.Combine(ReaderRoot, "Extraction", "JstorExtractor.cs")).Should().BeTrue();

    [Test] public void StandardExtractor_IsIn_ExtractionDir()
        => File.Exists(Path.Combine(ReaderRoot, "Extraction", "StandardExtractor.cs")).Should().BeTrue();

    [Test] public void HtmlCleanupHelper_IsIn_ExtractionDir()
        => File.Exists(Path.Combine(ReaderRoot, "Extraction", "HtmlCleanupHelper.cs")).Should().BeTrue();

    // Local
    [Test] public void LocalEpubExtractor_IsIn_LocalDir()
        => File.Exists(Path.Combine(ReaderRoot, "Local", "LocalEpubExtractor.cs")).Should().BeTrue();

    [Test] public void LocalPdfExtractor_IsIn_LocalDir()
        => File.Exists(Path.Combine(ReaderRoot, "Local", "LocalPdfExtractor.cs")).Should().BeTrue();

    [Test] public void LocalImageExtractor_IsIn_LocalDir()
        => File.Exists(Path.Combine(ReaderRoot, "Local", "LocalImageExtractor.cs")).Should().BeTrue();

    // Ocr
    [Test] public void IOcrProvider_IsIn_OcrDir()
        => File.Exists(Path.Combine(ReaderRoot, "Ocr", "IOcrProvider.cs")).Should().BeTrue();

    [Test] public void AzureOcrProvider_IsIn_OcrDir()
        => File.Exists(Path.Combine(ReaderRoot, "Ocr", "AzureDocumentIntelligenceOcrProvider.cs")).Should().BeTrue();

    [Test] public void TesseractOcrProvider_IsIn_OcrDir()
        => File.Exists(Path.Combine(ReaderRoot, "Ocr", "TesseractOcrProvider.cs")).Should().BeTrue();

    [Test] public void OcrTextCleanup_IsIn_OcrDir()
        => File.Exists(Path.Combine(ReaderRoot, "Ocr", "OcrTextCleanup.cs")).Should().BeTrue();

    // Output
    [Test] public void EpubWriter_IsIn_OutputDir()
        => File.Exists(Path.Combine(ReaderRoot, "Output", "EpubWriter.cs")).Should().BeTrue();

    // Quality
    [Test] public void ArticleStructureDetector_IsIn_QualityDir()
        => File.Exists(Path.Combine(ReaderRoot, "Quality", "ArticleStructureDetector.cs")).Should().BeTrue();

    [Test] public void PdfTypeDetector_IsIn_QualityDir()
        => File.Exists(Path.Combine(ReaderRoot, "Quality", "PdfTypeDetector.cs")).Should().BeTrue();

    [Test] public void WebExtractionQualityAnalyzer_IsIn_QualityDir()
        => File.Exists(Path.Combine(ReaderRoot, "Quality", "WebExtractionQualityAnalyzer.cs")).Should().BeTrue();

    // Validation
    [Test] public void EpubValidator_IsIn_ValidationDir()
        => File.Exists(Path.Combine(ReaderRoot, "Validation", "EpubValidator.cs")).Should().BeTrue();

    [Test] public void CalibreClient_IsIn_ValidationDir()
        => File.Exists(Path.Combine(ReaderRoot, "Validation", "CalibreClient.cs")).Should().BeTrue();

    // BrowserSession stays at root
    [Test] public void BrowserSession_RemainsAt_ReaderRoot()
        => File.Exists(Path.Combine(ReaderRoot, "BrowserSession.cs")).Should().BeTrue();
}
```

- [ ] **Step 2: Read-back**

```powershell
$file = '/home/lance/Scripts/csharp/tests\Scripts.Tests\StructuralTests\ReaderStructureTests.cs'
Test-Path $file | Should -Be $true
Write-Host "Read-back OK"
```

- [ ] **Step 3: Run — confirm RED**

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "ReaderStructureTests" `
    --logger "console;verbosity=normal" 2>&1
```

Expected: all tests fail — files are still flat in `src/Reader/`.

---

## Task 2: Move Extraction/ files

- [ ] **Step 1: Move Extraction files**

```powershell
$readerRoot = '/home/lance/Scripts/csharp/src\Reader'
$extractionDir = Join-Path $readerRoot 'Extraction'
New-Item -ItemType Directory -Path $extractionDir -Force -ErrorAction Stop

$extractionFiles = @('HtmlCleanupHelper.cs', 'JstorExtractor.cs', 'StandardExtractor.cs')
foreach ($f in $extractionFiles) {
    $src = Join-Path $readerRoot $f
    $dst = Join-Path $extractionDir $f
    if (Test-Path $src) {
        $bak = $src + '.bak.' + (Get-Date -Format 'yyyyMMdd_HHmmss')
        Copy-Item $src $bak -ErrorAction Stop
        Move-Item $src $dst -ErrorAction Stop
        Test-Path $dst | Should -Be $true
        Test-Path $src | Should -Be $false
        Write-Host "Moved: $f"
    } else {
        Write-Warning "Skipped (not found): $f"
    }
}
```

- [ ] **Step 2: Build — confirm no errors after Extraction move**

```powershell
dotnet build /home/lance/Scripts/csharp/Scripts.slnx 2>&1 | Tee-Object -Variable b
$b | Where-Object { $_ -match ' error ' } | Should -BeNullOrEmpty
Write-Host "Build clean after Extraction/ move"
```

---

## Task 3: Move Local/ files

- [ ] **Step 1: Move Local files**

```powershell
$readerRoot = '/home/lance/Scripts/csharp/src\Reader'
$localDir = Join-Path $readerRoot 'Local'
New-Item -ItemType Directory -Path $localDir -Force -ErrorAction Stop

$localFiles = @('LocalEpubExtractor.cs', 'LocalImageExtractor.cs', 'LocalPdfExtractor.cs')
foreach ($f in $localFiles) {
    $src = Join-Path $readerRoot $f
    $dst = Join-Path $localDir $f
    if (Test-Path $src) {
        $bak = $src + '.bak.' + (Get-Date -Format 'yyyyMMdd_HHmmss')
        Copy-Item $src $bak -ErrorAction Stop
        Move-Item $src $dst -ErrorAction Stop
        Test-Path $dst | Should -Be $true
        Test-Path $src | Should -Be $false
        Write-Host "Moved: $f"
    } else {
        Write-Warning "Skipped (not found): $f"
    }
}
```

- [ ] **Step 2: Build — confirm no errors after Local move**

```powershell
dotnet build /home/lance/Scripts/csharp/Scripts.slnx 2>&1 | Tee-Object -Variable b
$b | Where-Object { $_ -match ' error ' } | Should -BeNullOrEmpty
Write-Host "Build clean after Local/ move"
```

---

## Task 4: Move Ocr/ files

- [ ] **Step 1: Move Ocr files**

```powershell
$readerRoot = '/home/lance/Scripts/csharp/src\Reader'
$ocrDir = Join-Path $readerRoot 'Ocr'
New-Item -ItemType Directory -Path $ocrDir -Force -ErrorAction Stop

$ocrFiles = @(
    'AzureDocumentIntelligenceOcrProvider.cs',
    'DocumentAiOcrProvider.cs',
    'GoogleVisionOcrProvider.cs',
    'IOcrProvider.cs',
    'OcrTextCleanup.cs',
    'TesseractOcrProvider.cs'
)
foreach ($f in $ocrFiles) {
    $src = Join-Path $readerRoot $f
    $dst = Join-Path $ocrDir $f
    if (Test-Path $src) {
        $bak = $src + '.bak.' + (Get-Date -Format 'yyyyMMdd_HHmmss')
        Copy-Item $src $bak -ErrorAction Stop
        Move-Item $src $dst -ErrorAction Stop
        Test-Path $dst | Should -Be $true
        Test-Path $src | Should -Be $false
        Write-Host "Moved: $f"
    } else {
        Write-Warning "Skipped (not found): $f"
    }
}
```

- [ ] **Step 2: Build — confirm no errors after Ocr move**

```powershell
dotnet build /home/lance/Scripts/csharp/Scripts.slnx 2>&1 | Tee-Object -Variable b
$b | Where-Object { $_ -match ' error ' } | Should -BeNullOrEmpty
Write-Host "Build clean after Ocr/ move"
```

---

## Task 5: Move Output/ files

- [ ] **Step 1: Move Output files**

```powershell
$readerRoot = '/home/lance/Scripts/csharp/src\Reader'
$outputDir = Join-Path $readerRoot 'Output'
New-Item -ItemType Directory -Path $outputDir -Force -ErrorAction Stop

$outputFiles = @('EpubWriter.cs')
foreach ($f in $outputFiles) {
    $src = Join-Path $readerRoot $f
    $dst = Join-Path $outputDir $f
    if (Test-Path $src) {
        $bak = $src + '.bak.' + (Get-Date -Format 'yyyyMMdd_HHmmss')
        Copy-Item $src $bak -ErrorAction Stop
        Move-Item $src $dst -ErrorAction Stop
        Test-Path $dst | Should -Be $true
        Test-Path $src | Should -Be $false
        Write-Host "Moved: $f"
    } else {
        Write-Warning "Skipped (not found): $f"
    }
}
```

- [ ] **Step 2: Build — confirm no errors after Output move**

```powershell
dotnet build /home/lance/Scripts/csharp/Scripts.slnx 2>&1 | Tee-Object -Variable b
$b | Where-Object { $_ -match ' error ' } | Should -BeNullOrEmpty
```

---

## Task 6: Move Quality/ files

- [ ] **Step 1: Move Quality files**

```powershell
$readerRoot = '/home/lance/Scripts/csharp/src\Reader'
$qualityDir = Join-Path $readerRoot 'Quality'
New-Item -ItemType Directory -Path $qualityDir -Force -ErrorAction Stop

$qualityFiles = @(
    'ArticleStructureDetector.cs',
    'PdfContentQuality.cs',
    'PdfTypeDetector.cs',
    'WebExtractionQualityAnalyzer.cs'
)
foreach ($f in $qualityFiles) {
    $src = Join-Path $readerRoot $f
    $dst = Join-Path $qualityDir $f
    if (Test-Path $src) {
        $bak = $src + '.bak.' + (Get-Date -Format 'yyyyMMdd_HHmmss')
        Copy-Item $src $bak -ErrorAction Stop
        Move-Item $src $dst -ErrorAction Stop
        Test-Path $dst | Should -Be $true
        Test-Path $src | Should -Be $false
        Write-Host "Moved: $f"
    } else {
        Write-Warning "Skipped (not found): $f"
    }
}
```

- [ ] **Step 2: Build — confirm no errors after Quality move**

```powershell
dotnet build /home/lance/Scripts/csharp/Scripts.slnx 2>&1 | Tee-Object -Variable b
$b | Where-Object { $_ -match ' error ' } | Should -BeNullOrEmpty
```

---

## Task 7: Move Validation/ files

- [ ] **Step 1: Move Validation files**

```powershell
$readerRoot = '/home/lance/Scripts/csharp/src\Reader'
$validationDir = Join-Path $readerRoot 'Validation'
New-Item -ItemType Directory -Path $validationDir -Force -ErrorAction Stop

$validationFiles = @('CalibreClient.cs', 'EpubValidator.cs')
foreach ($f in $validationFiles) {
    $src = Join-Path $readerRoot $f
    $dst = Join-Path $validationDir $f
    if (Test-Path $src) {
        $bak = $src + '.bak.' + (Get-Date -Format 'yyyyMMdd_HHmmss')
        Copy-Item $src $bak -ErrorAction Stop
        Move-Item $src $dst -ErrorAction Stop
        Test-Path $dst | Should -Be $true
        Test-Path $src | Should -Be $false
        Write-Host "Moved: $f"
    } else {
        Write-Warning "Skipped (not found): $f"
    }
}
```

- [ ] **Step 2: Build — confirm no errors after Validation move**

```powershell
dotnet build /home/lance/Scripts/csharp/Scripts.slnx 2>&1 | Tee-Object -Variable b
$b | Where-Object { $_ -match ' error ' } | Should -BeNullOrEmpty
```

---

## Task 8: Run structure tests — confirm GREEN

- [ ] **Step 1: Run structure tests**

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "ReaderStructureTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: all tests that had matching files PASS. Tests for files that were not found on disk were skipped during move (not a failure — document which files were absent).

- [ ] **Step 2: Full test suite — no regressions**

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --logger "console;verbosity=normal" 2>&1
```

- [ ] **Step 3: Clean up .bak files**

```powershell
Get-ChildItem '/home/lance/Scripts/csharp/src\Reader' -Recurse -Filter '*.bak.*' |
    Remove-Item -Force -ErrorAction Stop
Write-Host "Backup files removed"
```

- [ ] **Step 4: Commit**

```powershell
git -C /home/lance/Scripts add csharp/src/Reader/ `
    csharp/tests/Scripts.Tests/StructuralTests/ReaderStructureTests.cs
git -C /home/lance/Scripts commit -m "feat(t4-03): restructure Reader into Extraction/Local/Ocr/Output/Quality/Validation subdirs"
```

---

## Acceptance Criteria

- [ ] All 16 `ReaderStructureTests` pass (or are skipped for genuinely absent files with documentation)
- [ ] `BrowserSession.cs` remains at `src/Reader/BrowserSession.cs`
- [ ] `dotnet build csharp/Scripts.slnx` → `0 Error(s). 0 Warning(s).`
- [ ] No `.bak.*` files remain in the Reader directory
- [ ] All pre-existing tests still pass
