# Phase 3: Google Deprecation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Assert Google Sheets integration components are deprecated and completely removed from the repository.

**Architecture:** PowerShell script assertions validating the absence of the 6 Google-related files and Google references in the orchestrator files.

**Tech Stack:** PowerShell

---

### Task 3.1: Assert Google Sheets files are absent

**Files:**
- Create: `.kilo/tests/AssertGoogleFilesAbsent.ps1`

- [ ] **Step 1: Write the PowerShell validation script**

Create `.kilo/tests/AssertGoogleFilesAbsent.ps1` with the following content:
```powershell
$ErrorActionPreference = 'Stop'
$files = @(
    "C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\GoogleSheetsService.cs",
    "C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\GoogleSheetsContext.cs",
    "C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\SheetFormattingService.cs",
    "C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\SheetMetadataService.cs",
    "C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\SheetRowService.cs",
    "C:\Users\Lance\Dev\Scripts\csharp\src\Services\Sync\SpreadsheetBootstrapper.cs"
)

foreach ($f in $files) {
    if (Test-Path $f) {
        throw "Google file still exists: $f"
    }
}
Write-Output "PASS"
```

- [ ] **Step 2: Run the script to verify it passes**

Run: `pwsh -File C:\Users\Lance\Dev\Scripts\.kilo\tests\AssertGoogleFilesAbsent.ps1`
Expected: `PASS`

- [ ] **Step 3: Commit**

```bash
git add .kilo/tests/AssertGoogleFilesAbsent.ps1
git commit -m "test: assert Google Sheets files are absent"
```

---

### Task 3.2: Assert no Google DI in ScrobbleSyncOrchestrator

**Files:**
- Create: `.kilo/tests/AssertNoGoogleInScrobbleSync.ps1`

- [ ] **Step 1: Write the PowerShell validation script**

Create `.kilo/tests/AssertNoGoogleInScrobbleSync.ps1` with the following content:
```powershell
$ErrorActionPreference = 'Stop'
$path = "C:\Users\Lance\Dev\Scripts\csharp\src\Orchestrators\ScrobbleSyncOrchestrator.cs"
$c = Get-Content $path -Raw
if ($c -match 'GoogleSheets|SheetFormatting|SheetMetadata|SheetRow|SpreadsheetBootstrapper') {
    throw "Google dependency references found in ScrobbleSyncOrchestrator.cs!"
}
Write-Output "PASS"
```

- [ ] **Step 2: Run the script to verify it passes**

Run: `pwsh -File C:\Users\Lance\Dev\Scripts\.kilo\tests\AssertNoGoogleInScrobbleSync.ps1`
Expected: `PASS`

- [ ] **Step 3: Commit**

```bash
git add .kilo/tests/AssertNoGoogleInScrobbleSync.ps1
git commit -m "test: assert no Google references in ScrobbleSyncOrchestrator"
```

---

### Task 3.3: Assert no Google DI in YouTubePlaylistOrchestrator

**Files:**
- Create: `.kilo/tests/AssertNoGoogleInYouTubePlaylist.ps1`

- [ ] **Step 1: Write the PowerShell validation script**

Create `.kilo/tests/AssertNoGoogleInYouTubePlaylist.ps1` with the following content:
```powershell
$ErrorActionPreference = 'Stop'
$path = "C:\Users\Lance\Dev\Scripts\csharp\src\Orchestrators\YouTubePlaylistOrchestrator.cs"
$c = Get-Content $path -Raw
if ($c -match 'GoogleSheets|SheetFormatting|SheetMetadata|SheetRow|SpreadsheetBootstrapper') {
    throw "Google dependency references found in YouTubePlaylistOrchestrator.cs!"
}
Write-Output "PASS"
```

- [ ] **Step 2: Run the script to verify it passes**

Run: `pwsh -File C:\Users\Lance\Dev\Scripts\.kilo\tests\AssertNoGoogleInYouTubePlaylist.ps1`
Expected: `PASS`

- [ ] **Step 3: Commit**

```bash
git add .kilo/tests/AssertNoGoogleInYouTubePlaylist.ps1
git commit -m "test: assert no Google references in YouTubePlaylistOrchestrator"
```
