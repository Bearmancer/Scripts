# PowerShell Tasks - Comprehensive List

*Generated: December 25, 2025*  
*Source Files: To-Do.md (#15-27, #48-49, #54-68), master_task_list.md, User Requests*

---

## Overview

**Total Tasks:** 22  
**Complexity Distribution:**
- ⭐ Low: 8 tasks
- ⭐⭐ Medium: 10 tasks
- ⭐⭐⭐ High: 3 tasks
- ⭐⭐⭐⭐ Very High: 1 task

**Priority:** HIGH - Profile currently loads in 937ms, target <100ms

---

## Category 1: Profile & Performance Optimization

### PWS-001: Force UTF-8 Encoding Always
**Complexity:** ⭐ Low  
**Source:** To-Do.md #68  
**Priority:** HIGH (foundational)

**Current State:**
```powershell
# Lines 7-13 in profile (duplicated encoding setup)
[Console]::InputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Global:OutputEncoding = [System.Text.Encoding]::UTF8
$PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'
$PSDefaultParameterValues['*:Encoding'] = 'utf8'
$env:PYTHONIOENCODING = 'utf-8'
```

**Target State:**
```powershell
# Use existing Set-Utf8Console function from ScriptsToolkit
Set-Utf8Console  # One call replaces 6+ lines, includes chcp 65001
```

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`
- `C:\Users\Lance\Documents\PowerShell\Microsoft.PowerShell_profile.ps1`
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1` (Set-Utf8Console already exists, lines 32-43)

**Implementation Steps:**
1. ✓ Confirm Set-Utf8Console exists in ScriptsToolkit.psm1
2. Replace encoding statements in profile with `Set-Utf8Console`
3. Ensure module is loaded before calling function (lazy load consideration)

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 5 minutes

---

### PWS-002: Fix Set-Item Null Argument Error
**Complexity:** ⭐ Low  
**Source:** User Request (PowerShell 7.6.0-preview.6 startup error)  
**Priority:** HIGH (breaking error)

**Error Message:**
```
PowerShell 7.6.0-preview.6
Set-Item: Cannot process argument because the value of argument "name" is null.
Loading personal and system profiles took 937ms.
```

**Root Cause Analysis:**
- No `Set-Item` calls found in profile code
- Likely caused by `Import-Module PSCompletions` (line 14) internal initialization
- Possibly null environment variable during module load

**Current Code:**
```powershell
Import-Module PSCompletions
```

**Target State:**
```powershell
try {
    Import-Module PSCompletions -ErrorAction Stop
}
catch {
    Write-Warning "PSCompletions failed to load: $_"
}
```

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1` (line 14)

**Implementation Steps:**
1. Add try-catch around `Import-Module PSCompletions`
2. Test if error persists
3. If persists, defer PSCompletions to lazy load (see PWS-005)

**Dependencies:** None  
**Blocks:** PWS-005 (lazy loading)  
**Estimated Time:** 10 minutes

---

### PWS-003: Eliminate Profile Duplication
**Complexity:** ⭐ Low  
**Source:** User Request  
**Priority:** MEDIUM (cleanup)

**Current State:**
- **System Profile:** `C:\Users\Lance\Documents\PowerShell\Microsoft.PowerShell_profile.ps1` (EMPTY - just has redirect comment)
- **Workspace Profile:** `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1` (ACTUAL profile)

**Problem:**
- Profile duplicated in two locations
- System profile is just a copy of workspace profile
- Changes must be made in two places

**Target State:**
- **System Profile:** Dot-source workspace profile
- **Workspace Profile:** Single source of truth

**System Profile Should Contain:**
```powershell
# Lance's PowerShell Profile
# Source: C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1
. 'C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1'
```

**Files:**
- `C:\Users\Lance\Documents\PowerShell\Microsoft.PowerShell_profile.ps1` (replace entire contents)
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1` (no changes)

**Implementation Steps:**
1. Back up current system profile
2. Replace with dot-source statement
3. Test profile load
4. Document this pattern in explanations/profile_architecture.md

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 5 minutes

---

### PWS-004: Add Differential Timing to Profile
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #22, User Request #4  
**Priority:** HIGH (diagnostic)

**Current State:**
- No timing information
- Profile loads in 937ms (user reported)
- No visibility into which modules are slow

**Target State:**
```
[    0ms] Profile start
[    5ms] UTF-8 configured (Δ5ms)
[   15ms] PSReadLine configured (Δ10ms)
[  234ms] PSCompletions loaded (Δ219ms)  ← SLOW!
[  456ms] PSFzf loaded (Δ222ms)          ← SLOW!
[  512ms] carapace loaded (Δ56ms)
[  568ms] argc loaded (Δ56ms)
─────────────────────────────────────
Profile loaded in 568ms
```

**Implementation Pattern:**
```powershell
$script:ProfileStartTime = [System.Diagnostics.Stopwatch]::StartNew()
$script:LastCheckpoint = 0

function Write-Timing {
    param([string]$Component)
    $elapsed = $script:ProfileStartTime.ElapsedMilliseconds
    $delta = $elapsed - $script:LastCheckpoint
    Write-Host "[$('{0,5}' -f $elapsed)ms] $Component (Δ${delta}ms)" -ForegroundColor DarkGray
    $script:LastCheckpoint = $elapsed
}

Set-StrictMode -Version Latest
Write-Timing "Profile start"

# ... after each major operation
Write-Timing "UTF-8 configured"
Write-Timing "PSReadLine configured"
```

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1` (Write-Timing already exists but shows cumulative, not differential)

**Implementation Steps:**
1. Add stopwatch initialization at profile start
2. Add delta tracking variable
3. Create Write-Timing function (or enhance existing)
4. Insert Write-Timing calls after each module load
5. Display summary at end

**Dependencies:** None  
**Blocks:** PWS-005 (will show which modules to lazy-load)  
**Estimated Time:** 20 minutes

---

### PWS-005: Implement Lazy Module Loading
**Complexity:** ⭐⭐⭐⭐ Very High  
**Source:** To-Do.md #61, User Request  
**Priority:** CRITICAL (performance)

**Current Load Time:** 937ms  
**Target Load Time:** <100ms (90% reduction)

**Modules to Lazy-Load:**

| Module | Current | Strategy | Trigger |
|--------|---------|----------|---------|
| **ScriptsToolkit** | Not loaded at all! | Proxy functions | First function call |
| **PSCompletions** | Eager (line 14) | Defer load | First Tab completion |
| **PSFzf** | Eager if available (line 21) | Defer load | First Ctrl+T/R/Space |
| **carapace** | Eager via Invoke-Expression (line 28) | Defer load | First external command |
| **argc** | Eager for dotnet/winget (line 33) | Already lazy! | Via argc-load command |

**Architecture:**

```
Profile Load (Target: ~60ms)
├─ [0-10ms]  Set-StrictMode, UTF-8 setup
├─ [10-20ms] PSReadLine basic config
├─ [20-40ms] Register 56 proxy functions for ScriptsToolkit
├─ [40-50ms] Register deferred PSCompletions loader
├─ [50-60ms] Register deferred PSFzf loader
└─ [60ms]    Display load summary

First Function Call (e.g., Get-Directories)
└─ Proxy function intercepts
   └─ Loads ScriptsToolkit.psm1
      └─ Replaces proxy with actual function
         └─ Calls actual function
         
First Tab Completion
└─ PSReadLine hook intercepts
   └─ Loads PSCompletions module
      └─ Enables completion

First Ctrl+T
└─ PSReadLine key handler intercepts
   └─ Loads PSFzf module
      └─ Invokes fuzzy finder
```

**Implementation Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1` (major refactor)
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1` (export function list)

**Implementation Steps:**

**Step 1: Create Proxy Function Generator**
```powershell
function Register-LazyModule {
    param(
        [string]$ModuleName,
        [string]$ModulePath,
        [string[]]$Functions
    )
    
    foreach ($func in $Functions) {
        $scriptBlock = {
            # Remove this proxy
            Remove-Item "Function:\$func" -ErrorAction SilentlyContinue
            
            # Load the actual module
            Import-Module $ModulePath -Global -Force
            
            # Call the real function
            & $func @args
        }.GetNewClosure()
        
        New-Item -Path "Function:\$func" -Value $scriptBlock -Force | Out-Null
    }
}
```

**Step 2: Get ScriptsToolkit Function List**
```powershell
# In ScriptsToolkit.psm1, add at end:
$script:ExportedFunctions = @(
    'Get-Directories', 'Get-FilesAndDirectories', 'Show-SyncLog',
    'Invoke-YouTubeSync', 'Invoke-LastFmSync', 'Invoke-AllSync',
    # ... all 56 functions
)

# Export for lazy loading
Export-ModuleMember -Function $script:ExportedFunctions
```

**Step 3: Profile Refactor**
```powershell
# Early in profile
$Script:ToolkitFunctions = @(
    'Get-Directories', 'Get-FilesAndDirectories', # ... all 56
)

Register-LazyModule -ModuleName 'ScriptsToolkit' `
    -ModulePath "$ModulePath\ScriptsToolkit\ScriptsToolkit.psm1" `
    -Functions $Script:ToolkitFunctions
```

**Step 4: Defer PSCompletions**
```powershell
# Instead of: Import-Module PSCompletions
# Register a one-time loader on first tab

$global:__PSCompletionsLoaded = $false
Set-PSReadLineKeyHandler -Key Tab -ScriptBlock {
    if (-not $global:__PSCompletionsLoaded) {
        Import-Module PSCompletions -ErrorAction SilentlyContinue
        $global:__PSCompletionsLoaded = $true
    }
    # Invoke actual tab completion
    [Microsoft.PowerShell.PSConsoleReadLine]::TabCompleteNext()
}
```

**Step 5: Defer PSFzf**
```powershell
# Lazy-load PSFzf only when Ctrl+T/R/Space pressed
function Initialize-PSFzf {
    if (Get-Module -ListAvailable -Name PSFzf) {
        Import-Module PSFzf
        Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t'
        Set-PsFzfOption -PSReadlineChordReverseHistory 'Ctrl+r'
        Set-PSReadLineKeyHandler -Key 'Ctrl+Spacebar' -ScriptBlock { Invoke-FzfTabCompletion }
    }
}

# Register placeholder handlers
Set-PSReadLineKeyHandler -Key 'Ctrl+t' -ScriptBlock { Initialize-PSFzf; Invoke-FuzzySetLocation }
Set-PSReadLineKeyHandler -Key 'Ctrl+r' -ScriptBlock { Initialize-PSFzf; Invoke-FuzzyHistory }
Set-PSReadLineKeyHandler -Key 'Ctrl+Spacebar' -ScriptBlock { Initialize-PSFzf; Invoke-FzfTabCompletion }
```

**Dependencies:** PWS-004 (timing to validate improvement)  
**Blocks:** All other PWS tasks (foundational change)  
**Estimated Time:** 2-3 hours

**Testing Checklist:**
- [ ] Profile loads in <100ms
- [ ] First function call loads ScriptsToolkit
- [ ] Tab completion works (loads PSCompletions)
- [ ] Ctrl+T/R/Space work (loads PSFzf)
- [ ] argc-load still works
- [ ] carapace completions still work

---

## Category 2: Completion System Management

### PWS-006: Configure argc Lazy Loading
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #62-63  
**Priority:** MEDIUM (enhancement)

**Current State:**
- argc loads dotnet/winget completions eagerly (line 33-38)
- `Get-ArgcManifest` exists but not used in profile
- ~500+ commands available but not discoverable

**Target State:**
```powershell
# Profile: Load nothing by default
# User runs: argc-load dotnet winget git docker

# Make manifest available for discovery
Set-Alias -Name 'argc-list' -Value Get-ArgcManifest
Register-ArgumentCompleter -CommandName 'argc-load' -ScriptBlock {
    # Tab completion shows available commands from manifest
}
```

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1` (Get-ArgcManifest exists, line 193)

**Implementation Steps:**
1. Remove eager argc loading from profile
2. Document `argc-load` command in help
3. Add alias `argc-list` → `Get-ArgcManifest`
4. Ensure tab completion works for argc-load (already implemented in ScriptsToolkit.psm1)

**Dependencies:** PWS-005 (ScriptsToolkit lazy loading)  
**Blocks:** None  
**Estimated Time:** 15 minutes

---

### PWS-007: Retain PSFzf Ctrl+Space Keybinding
**Complexity:** ⭐ Low  
**Source:** To-Do.md #64-65  
**Priority:** LOW (already correct)

**Current State:**
```powershell
Set-PSReadLineKeyHandler -Key 'Ctrl+Spacebar' -ScriptBlock { Invoke-FzfTabCompletion }
```

**Target State:** Same (no changes needed)

**Verification:**
- ✓ Ctrl+Space → Fuzzy completion (PSFzf)
- ✓ Tab → Menu completion (PSReadLine/PSCompletions)

**Files:**
- None (configuration is already correct)

**Implementation Steps:**
1. Document current keybindings in explanations/
2. Ensure lazy loading preserves this behavior (PWS-005)

**Dependencies:** PWS-005  
**Blocks:** None  
**Estimated Time:** 5 minutes (documentation only)

---

## Category 3: Path Management

### PWS-008: Update argc Completions Root Path
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #54-57  
**Priority:** LOW (cleanup)

**Current State:**
```powershell
# In ScriptsToolkit.psm1
$Script:ArgcCompletionsRoot = if ($env:ARGC_COMPLETIONS_ROOT) {
    $env:ARGC_COMPLETIONS_ROOT
}
else {
    Join-Path -Path $env:USERPROFILE -ChildPath 'argc-completions'
}
```

**Decision:** Do NOT create tools/ directory (user confirmed)
- argc/carapace installed via cargo/winget (already in PATH)
- No need to centralize binaries
- Just ensure `$env:ARGC_COMPLETIONS_ROOT` points to correct location

**Target State:**
```powershell
# In profile or ScriptsToolkit, verify path
if (-not $env:ARGC_COMPLETIONS_ROOT) {
    $env:ARGC_COMPLETIONS_ROOT = Join-Path $env:USERPROFILE 'argc-completions'
}
```

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1` (lines 10-15)
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1` (if env var needs to be set early)

**Implementation Steps:**
1. Verify `$env:ARGC_COMPLETIONS_ROOT` is set correctly
2. Ensure carapace and argc can find their completions
3. Document expected binary locations in explanations/

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 10 minutes

---

### PWS-009: Audit and Update All Path References
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #59  
**Priority:** MEDIUM (maintenance)

**Scope:** Test all hardcoded paths in .ps1 and .psm1 files

**Known Paths to Verify:**
```powershell
# In ScriptsToolkit.psm1
$Script:RepositoryRoot = Split-Path -Path $PSScriptRoot -Parent
# Should be: C:\Users\Lance\Dev\Scripts

$Script:PythonToolkit = Join-Path -Path $RepositoryRoot -ChildPath 'python\toolkit\cli.py'
# Should be: C:\Users\Lance\Dev\Scripts\python\toolkit\cli.py

$Script:CSharpRoot = Join-Path -Path $RepositoryRoot -ChildPath 'csharp'
# Should be: C:\Users\Lance\Dev\Scripts\csharp

$Script:LogDirectory = Join-Path -Path $RepositoryRoot -ChildPath 'logs'
# Should be: C:\Users\Lance\Dev\Scripts\logs
```

**Files to Audit:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`
- All `.ps1` scripts if any exist

**Implementation Steps:**
1. Run `grep -r "C:\\Users" powershell/` to find hardcoded paths
2. Replace with `$PSScriptRoot` or `$RepositoryRoot` based paths
3. Test all functions that reference paths
4. Verify C# and Python CLIs can be invoked

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 30 minutes

---

## Category 4: Module & Command Management

### PWS-010: Find Unapproved PowerShell Verbs
**Complexity:** ⭐ Low  
**Source:** To-Do.md #48  
**Priority:** LOW (code quality)

**Implementation:**
```powershell
# Function already exists in ScriptsToolkit.psm1 (lines 100-134)
Find-UnapprovedVerbs -ModuleName ScriptsToolkit
```

**Expected Output:**
```
Unapproved verbs in ScriptsToolkit:
  Create-Something -> New-Something
  Delete-Something -> Remove-Something
```

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`

**Implementation Steps:**
1. Load ScriptsToolkit module
2. Run `Find-UnapprovedVerbs`
3. Document results in PWS-011 task

**Dependencies:** PWS-005 (module must be loadable)  
**Blocks:** PWS-011  
**Estimated Time:** 5 minutes

---

### PWS-011: Create Verb Migration Plan
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #49  
**Priority:** LOW (code quality)

**Process:**
1. Get list from PWS-010
2. For each unapproved verb:
   - Determine approved replacement
   - Find all usages in codebase
   - Create rename script
   - Update documentation

**Verb Mappings:**
```powershell
Create → New
Delete → Remove
Make → New
Change → Set
List → Get
Print → Write
Build → New
Execute → Invoke
Load → Import
```

**Files:**
- New file: `markdown/implementation/verb_migration_plan.md`
- All `.psm1` files with unapproved verbs

**Implementation Steps:**
1. Run PWS-010 to get full list
2. Create markdown file with migration plan
3. Execute renames using multi_replace_string_in_file
4. Test all renamed functions
5. Update aliases if any exist

**Dependencies:** PWS-010  
**Blocks:** None  
**Estimated Time:** 1-2 hours (depending on number of violations)

---

## Category 5: Whisper Integration

### PWS-012: Create 'whisp' Alias
**Complexity:** ⭐ Low  
**Source:** To-Do.md #15  
**Priority:** MEDIUM (UX improvement)

**Current State:**
```powershell
Invoke-Whisper -Path 'file.webm' -Model 'large-v3' -Language 'auto'
```

**Target State:**
```powershell
whisp 'file.webm'  # Uses distil-large-v3 + English by default
```

**Implementation:**
```powershell
# In ScriptsToolkit.psm1
function Invoke-WhisperEnglishFast {
    [CmdletBinding()]
    [Alias('whisp')]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string]$Path
    )
    Invoke-Whisper -Path $Path -Model 'distil-large-v3' -Language 'en'
}
```

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`

**Implementation Steps:**
1. Add new function with alias
2. Export function and alias
3. Test: `whisp test.webm`
4. Update help documentation

**Dependencies:** None  
**Blocks:** PWS-018  
**Estimated Time:** 10 minutes

---

### PWS-013: Check whisper-ctranslate2 in Save-YouTubeVideo
**Complexity:** ⭐ Low  
**Source:** To-Do.md #23  
**Priority:** LOW (error handling)

**Task:** Verify if `Save-YouTubeVideo` warns when whisper-ctranslate2 is missing

**Current Pattern in Invoke-Whisper (line 1551):**
```powershell
$null = Get-Command -Name whisper-ctranslate2 -ErrorAction Stop
```

**Check Save-YouTubeVideo:**
```powershell
# Search for similar check
```

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1` (Save-YouTubeVideo function)

**Implementation Steps:**
1. Find Save-YouTubeVideo function
2. Check if it validates whisper-ctranslate2 presence
3. If missing, add check similar to Invoke-Whisper
4. Test with whisper not installed

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 15 minutes

---

### PWS-014: Restore Model Download Progress
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #24-25  
**Priority:** MEDIUM (UX)

**Problem:** When whisper-ctranslate2 downloads a model, output is suppressed

**Current Behavior:**
```
[17:09:26] Transcribing: file.webm
Model: large-v3 | Language: (auto-detect)
[Long pause with no output while downloading]
```

**Target Behavior:**
```
[17:09:26] Transcribing: file.webm
Model: large-v3 | Language: (auto-detect)
Downloading model large-v3...
  [████████████████░░░░] 78% (2.1 GB / 2.7 GB)
```

**Investigation Needed:**
- Check if whisper-ctranslate2 outputs download progress to stderr
- Determine if output is being redirected/suppressed
- Find where stderr is captured

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1` (Invoke-Whisper*, around line 1450)

**Implementation Steps:**
1. Locate Invoke-Whisper function
2. Find stderr redirection (likely `2>&1` or `-RedirectStandardError`)
3. Allow stderr during model download phase
4. Test with fresh model download

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 30 minutes

---

### PWS-015: Improve Whisper Progress Display
**Complexity:** ⭐⭐⭐ High  
**Source:** To-Do.md #16-17, #19-20  
**Priority:** MEDIUM (UX)

**Current Output:**
```
12%|███▋| 478.3/3986.2826875 [02:48<22:43, 2.57seconds/s]
```

**Problems:**
1. "seconds/s" is redundant (should be "s/s" or "sec/s")
2. No labels for numbers (what is 478.3? 3986.2826875?)
3. No clear ETA vs elapsed distinction
4. Overly precise decimals (2826875?)

**Target Output:**
```
12%|███▋| 478s / 3986s [Elapsed: 02:48 | ETA: 22:43 | Speed: 2.6s/s]
```

**Implementation:**
```powershell
# Parse whisper output line by line
# Regex to capture: percentage, current, total, elapsed, eta, speed
# Reformat with labels
```

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1` (Invoke-Whisper*)

**Implementation Steps:**
1. Capture whisper-ctranslate2 stdout
2. Parse progress lines with regex
3. Reformat with clear labels
4. Write formatted output to console
5. Test with various file sizes

**Dependencies:** PWS-014 (related to output handling)  
**Blocks:** None  
**Estimated Time:** 1 hour

---

### PWS-016: Suppress Python Outdated Library Warnings
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #66  
**Priority:** LOW (cosmetic)

**Current Behavior:**
```
DeprecationWarning: pkg_resources is deprecated as an API
UserWarning: whisper-ctranslate2 version 1.0.0 is outdated, upgrade to 1.0.1
```

**Target Behavior:**
```
[No warnings shown]
```

**Implementation:**
```powershell
# In Invoke-ToolkitPython or Invoke-Whisper
$env:PYTHONWARNINGS = 'ignore::DeprecationWarning,ignore::UserWarning'
# Or more specific:
$env:PYTHONWARNINGS = 'ignore::DeprecationWarning:pkg_resources'
```

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1` (Invoke-ToolkitPython, Invoke-Whisper*)

**Implementation Steps:**
1. Set `$env:PYTHONWARNINGS` before calling Python
2. Test with whisper to verify warnings suppressed
3. Ensure actual errors still show

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 10 minutes

---

### PWS-017: Add whisper-ctranslate2 Autocomplete
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #67  
**Priority:** LOW (nice-to-have)

**Research Questions:**
1. Does argc support whisper-ctranslate2?
2. Does carapace support whisper-ctranslate2?
3. Should we create custom completion?

**Investigation:**
```powershell
# Check argc manifest
Get-ArgcManifest | Select-String whisper

# Check carapace
carapace whisper-ctranslate2
```

**If Not Supported:**
Create custom completion using `Register-ArgumentCompleter`

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1` (new completion function)

**Implementation Steps:**
1. Research existing support in argc/carapace
2. If missing, create custom Register-ArgumentCompleter
3. Document whisper-ctranslate2 CLI options
4. Test tab completion

**Dependencies:** PWS-006 (argc configuration)  
**Blocks:** None  
**Estimated Time:** 1 hour

---

### PWS-018: Auto-invoke whisp After YouTube Download
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #60  
**Priority:** MEDIUM (automation)

**Current Behavior:**
```powershell
Save-YouTubeVideo 'https://youtube.com/watch?v=...'
# Downloads video, no transcription
```

**Target Behavior:**
```powershell
Save-YouTubeVideo 'https://youtube.com/watch?v=...'
# Downloads video
# Automatically calls: whisp downloaded_file.webm
```

**Implementation:**
```powershell
function Save-YouTubeVideo {
    param(
        [string]$Url,
        [switch]$NoTranscribe  # Opt-out instead of opt-in
    )
    
    # Download video
    $downloadedFile = # ... yt-dlp logic
    
    # Auto-transcribe unless opted out
    if (-not $NoTranscribe) {
        whisp $downloadedFile
    }
}
```

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`

**Implementation Steps:**
1. Find Save-YouTubeVideo function
2. Add `-NoTranscribe` switch parameter
3. Call whisp on downloaded file by default
4. Test with/without transcription

**Dependencies:** PWS-012 (whisp alias must exist)  
**Blocks:** None  
**Estimated Time:** 20 minutes

---

## Category 6: File System Operations

### PWS-019: Fix File Extension Repair Command
**Complexity:** ⭐⭐⭐ High  
**Source:** master_task_list.md #5  
**Priority:** MEDIUM (utility)

**Current Failed Implementation:**
```powershell
Get-ChildItem -Recurse -File | Where-Object { -not $_.Extension } | 
ForEach-Object { 
    $ext = (ffprobe -v error -show_entries format=format_name -of csv=p=0 $_.FullName 2>$null).Split(',')[0]
    if ($ext) { Rename-Item $_.FullName "$($_.Name).$ext" }
}
```

**Errors:**
1. `Rename-Item: The filename, directory name, or volume label syntax is incorrect`
2. `Cannot retrieve the dynamic parameters for the cmdlet. The specified wildcard character pattern is not valid: [cheersmumbai @ DT`

**Root Causes:**
1. Filenames contain wildcard characters `[` `]` → Rename-Item interprets as pattern
2. ffprobe returns format name (e.g., "matroska") not extension (e.g., "mkv")

**Fixed Implementation:**
```powershell
# Format name to extension mapping
$FormatMap = @{
    'matroska' = 'mkv'
    'avi' = 'avi'
    'mpeg' = 'mpg'
    'mp4' = 'mp4'
    'webm' = 'webm'
    # ... more mappings
}

Get-ChildItem -LiteralPath $PWD -Recurse -File | 
Where-Object { -not $_.Extension } | 
ForEach-Object {
    try {
        $format = (ffprobe -v error -show_entries format=format_name -of csv=p=0 $_.FullName 2>$null).Split(',')[0].Trim()
        $ext = $FormatMap[$format]
        
        if ($ext) {
            $newName = "$($_.Name).$ext"
            # Use -LiteralPath to avoid wildcard interpretation
            Rename-Item -LiteralPath $_.FullName -NewName $newName -ErrorAction Stop
            Write-Host "Renamed: $($_.Name) -> $newName" -ForegroundColor Green
        }
    }
    catch {
        Write-Warning "Failed to rename $($_.Name): $_"
    }
}
```

**Files:**
- None (one-liner, not saved to module per requirement)

**Implementation Steps:**
1. Create format-to-extension mapping table
2. Use `-LiteralPath` instead of positional path
3. Add comprehensive error handling
4. Test with files containing brackets, special chars
5. Document as runnable snippet in explanations/

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 1 hour

---

### PWS-020: Segregate Files by Extension
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #27  
**Priority:** LOW (utility)

**Target Directory:** `D:\Google Drive\Games\Others\Miscellaneous`

**Implementation:**
```powershell
$TargetDir = 'D:\Google Drive\Games\Others\Miscellaneous'

Get-ChildItem -LiteralPath $TargetDir -File | 
Group-Object Extension | 
ForEach-Object {
    $ext = if ($_.Name) { $_.Name.TrimStart('.') } else { 'NoExtension' }
    $destDir = Join-Path $TargetDir $ext
    
    if (-not (Test-Path -LiteralPath $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }
    
    $_.Group | ForEach-Object {
        Move-Item -LiteralPath $_.FullName -Destination $destDir -Force
        Write-Host "Moved: $($_.Name) -> $ext\" -ForegroundColor Cyan
    }
}
```

**Files:**
- None (one-liner)

**Implementation Steps:**
1. Group files by extension
2. Create subdirectory for each extension
3. Move files to respective directories
4. Handle files without extensions

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 15 minutes

---

## Category 7: Task Scheduling

### PWS-021: Fix Auto-Close Terminal on Sync Success
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #41-43  
**Priority:** MEDIUM (UX)

**Problem:** Scheduled sync tasks keep terminal open even after successful completion

**Root Cause Analysis:**
1. C# CLI returns exit code 0 on success?
2. PowerShell wrapper captures exit code?
3. Scheduled task action configured to close on success?

**Investigation:**
```powershell
# Check Register-ScheduledSyncTask
# Look for: -NoExit, -NoProfile flags
# Check if: exit $LASTEXITCODE is called
```

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1` (Register-ScheduledSyncTask, around line 2150)
- `C:\Users\Lance\Dev\Scripts\csharp\src\CLI\SyncCommands.cs` (verify exit codes)

**Implementation Steps:**
1. Review Register-ScheduledSyncTask function
2. Ensure C# CLI returns exit code 0 on success
3. Add `exit $LASTEXITCODE` to PowerShell wrapper
4. Remove `-NoExit` flag if present in scheduled task
5. Test scheduled task execution

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 30 minutes

---

## Category 8: Documentation

### PWS-022: Explain OmniSharp Purpose
**Complexity:** ⭐ Low (Documentation)  
**Source:** To-Do.md #21  
**Priority:** LOW (educational)

**Answer:**
- **OmniSharp** = Language Server Protocol (LSP) implementation for C#
- **Purpose:** Provides IntelliSense, code navigation, refactoring to editors
- **Architecture:** Wraps Roslyn (Microsoft's C# compiler/analyzer)
- **Used By:** VS Code C# extension, Vim, Emacs, Sublime Text

**Files:**
- `C:\Users\Lance\Dev\Scripts\markdown\explanations\vscode_configuration_explained.md` (already created)

**Implementation Steps:**
1. ✓ Already documented in vscode_configuration_explained.md
2. Add to development_environment.md if needed

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** ✓ COMPLETE

---

## Summary

### Priority Matrix

| Priority | Task Count | Total Time Est. |
|----------|------------|-----------------|
| CRITICAL | 1 (PWS-005) | 2-3 hours |
| HIGH | 3 (PWS-001, PWS-002, PWS-004) | 35 minutes |
| MEDIUM | 11 tasks | 5-6 hours |
| LOW | 7 tasks | 2-3 hours |

### Dependencies Graph

```
PWS-001 (UTF-8) ──────┐
PWS-002 (Set-Item) ───┤
PWS-003 (Duplication)─┤
PWS-004 (Timing) ─────┼──► PWS-005 (Lazy Loading) ◄── CRITICAL
                      │
PWS-010 (Find Verbs)──┴──► PWS-011 (Migrate Verbs)

PWS-005 ──► PWS-006 (argc)
          └► PWS-007 (PSFzf)

PWS-012 (whisp) ──► PWS-018 (Auto-transcribe)

PWS-014 (Download) ─┬─► PWS-015 (Progress)
                    └─► PWS-016 (Warnings)
```

### Quick Wins (< 15 minutes)
- PWS-001: UTF-8 encoding (5 min)
- PWS-002: Set-Item error fix (10 min)
- PWS-003: Profile duplication (5 min)
- PWS-007: PSFzf keybinding (5 min)
- PWS-008: argc path (10 min)
- PWS-010: Find verbs (5 min)
- PWS-012: whisp alias (10 min)

### High-Impact Tasks
1. **PWS-005: Lazy Loading** (90% load time reduction)
2. **PWS-004: Differential Timing** (visibility into performance)
3. **PWS-015: Whisper Progress** (significant UX improvement)
