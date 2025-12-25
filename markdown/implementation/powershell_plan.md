# PowerShell Implementation Plan
*Generated: December 25, 2025*  
*Tasks: 22 PowerShell tasks from consolidated_tasks.md*

---

## Phase 1: Profile Foundation & Error Fixes (Priority: IMMEDIATE)

### PWS-002: Fix Set-Item null argument error
**Complexity:** ⭐ Low | **Duration:** 30 min | **Priority:** P0

**Problem:**
```
PowerShell 7.6.0-preview.6
Set-Item: Cannot process argument because the value of argument "name" is null.
Loading personal and system profiles took 937ms.
```

**Root Cause:** `Import-Module PSCompletions` (line 14) may fail during initialization, causing Set-Item errors internally.

**Solution:**
```powershell
# In Microsoft.PowerShell_profile.ps1

# Wrap risky module imports in try/catch
try {
    Import-Module PSCompletions -ErrorAction Stop
} catch {
    Write-Warning "PSCompletions failed to load: $_"
}

try {
    if (Get-Module -ListAvailable -Name PSFzf) {
        Import-Module PSFzf -ErrorAction Stop
        Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t'
        Set-PsFzfOption -PSReadlineChordReverseHistory 'Ctrl+r'
        Set-PSReadLineKeyHandler -Key 'Ctrl+Spacebar' -ScriptBlock { Invoke-FzfTabCompletion }
    }
} catch {
    Write-Warning "PSFzf failed to load: $_"
}
```

**Files to Edit:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`

**Testing:**
1. Restart PowerShell
2. Verify no "Set-Item" error
3. Test tab completion works
4. Test Ctrl+Space fzf works

---

### PWS-003: Eliminate profile duplication
**Complexity:** ⭐ Low | **Duration:** 15 min | **Priority:** P0

**Problem:** Two profile files with duplicate content

**Current State:**
- `C:\Users\Lance\Documents\PowerShell\Microsoft.PowerShell_profile.ps1` - Empty (1 line: points to Scripts)
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1` - Full profile (40 lines)

**Solution:**

**File 1: `C:\Users\Lance\Documents\PowerShell\Microsoft.PowerShell_profile.ps1`**
```powershell
# PowerShell Profile Redirect
# This file sources the actual profile from the development workspace
# Backup location: C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1

$workspaceProfile = 'C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1'

if (Test-Path -LiteralPath $workspaceProfile) {
    . $workspaceProfile
} else {
    Write-Warning "Workspace profile not found: $workspaceProfile"
}
```

**File 2: `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`**
- Keep as-is (this is the source of truth)
- All edits happen here
- File 1 dot-sources this

**Benefits:**
- Single source of truth
- Version controlled (in Scripts repo)
- Easy backup ($PROFILE always exists)
- Can switch between profiles easily

**Testing:**
1. Update File 1 with redirect
2. Keep File 2 unchanged
3. Restart PowerShell
4. Verify `$PROFILE` points to File 1
5. Verify File 1 sources File 2
6. Test all completions work

---

### PWS-001: Force UTF-8 encoding always
**Complexity:** ⭐ Low | **Duration:** 10 min | **Priority:** P1

**Problem:** UTF-8 encoding set manually in profile; should use ScriptsToolkit function

**Current (Manual):**
```powershell
[Console]::InputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Global:OutputEncoding = [System.Text.Encoding]::UTF8
$PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'
$PSDefaultParameterValues['*:Encoding'] = 'utf8'
$env:PYTHONIOENCODING = 'utf-8'
```

**Solution (Use ScriptsToolkit):**
```powershell
# Early in profile, before module imports
if (Test-Path 'C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1') {
    Import-Module -Name 'C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1' -Function 'Set-Utf8Console' -ErrorAction SilentlyContinue
    if (Get-Command -Name Set-Utf8Console -ErrorAction SilentlyContinue) {
        Set-Utf8Console
    }
}
```

**Files to Edit:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`

**Testing:**
1. Restart PowerShell
2. Verify `[Console]::OutputEncoding` is UTF8
3. Test unicode output: `"Test: ♪♫♬ αβγ 中文 日本語"`

---

### PWS-004: Add differential timing to profile
**Complexity:** ⭐⭐ Medium | **Duration:** 45 min | **Priority:** P1

**Problem:** Need to track load time per module (differential, not cumulative)

**Target Output:**
```
[    0ms] UTF-8 configured
[   45ms] PSReadLine configured (Δ45ms)
[  234ms] PSCompletions loaded (Δ189ms)
[  456ms] PSFzf loaded (Δ222ms)
[  789ms] carapace loaded (Δ333ms)
[  937ms] argc loaded (Δ148ms)

Profile loaded in 937ms
```

**Solution:**
```powershell
# At very start of profile
$script:ProfileTimer = [System.Diagnostics.Stopwatch]::StartNew()
$script:LastCheckpoint = 0

function Write-ProfileTiming {
    param([string]$Component)
    $current = $script:ProfileTimer.ElapsedMilliseconds
    $delta = $current - $script:LastCheckpoint
    $script:LastCheckpoint = $current
    Write-Host "[$('{0,5}' -f $current)ms] $Component (Δ$($delta)ms)" -ForegroundColor DarkGray
}

# Use throughout profile:
Set-Utf8Console
Write-ProfileTiming "UTF-8 configured"

Set-PSReadLineOption -PredictionSource HistoryAndPlugin
Set-PSReadLineOption -PredictionViewStyle ListView
Set-PSReadLineOption -Colors @{ "Selection" = "`e[7m" }
Write-ProfileTiming "PSReadLine configured"

try {
    Import-Module PSCompletions -ErrorAction Stop
    Write-ProfileTiming "PSCompletions loaded"
} catch {
    Write-Warning "PSCompletions failed: $_"
}

# ... etc for each module

# At end:
$script:ProfileTimer.Stop()
Write-Host ""
Write-Host "Profile loaded in $($script:ProfileTimer.ElapsedMilliseconds)ms" -ForegroundColor Cyan
```

**Files to Edit:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`

**Testing:**
1. Restart PowerShell
2. Verify timing output shows
3. Verify delta calculations correct
4. Identify slowest module
5. Record total load time

---

## Phase 2: Lazy Module Loading (Priority: HIGH)

### PWS-005: Implement lazy module loading
**Complexity:** ⭐⭐⭐ High | **Duration:** 4-6 hours | **Priority:** P1

**Goal:** Reduce profile load from 937ms to <100ms

**Modules to Lazy-Load:**
1. **ScriptsToolkit** (56 functions) - NOT currently loaded
2. **PSCompletions** - Currently eager
3. **PSFzf** - Currently eager
4. **carapace** - Currently eager
5. **argc** - Currently eager

#### Strategy 1: Lazy Load ScriptsToolkit (HIGHEST IMPACT)

**Problem:** 56 functions not available because module not imported

**Solution:** Register proxy functions that load module on first use

```powershell
# Get list of functions from module
$moduleManifest = Import-PowerShellDataFile 'C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psd1'
$modulePath = 'C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1'

# Parse module to extract function names
$functionNames = @(
    'Get-Directories', 'dirs', 'tree',
    'Get-VideoChapters', 'Get-VideoResolution',
    'Invoke-Whisper', 'Invoke-WhisperJapanese', 'Invoke-WhisperFolder',
    'Save-YouTubeVideo',
    'Show-SyncLog',
    'Invoke-YouTubeSync', 'Invoke-LastFmSync', 'Invoke-AllSync',
    'Import-ArgcCompletions', 'argc-load',
    'Get-ArgcManifest', 'Get-ArgcLoadedCommands',
    'Initialize-Completions',
    'Show-BinaryLocations',
    'Find-UnapprovedVerbs'
    # ... add all 56 functions
)

# Create proxy functions
$script:ToolkitLoaded = $false

foreach ($functionName in $functionNames) {
    $proxyFunction = @"
function global:$functionName {
    if (-not `$script:ToolkitLoaded) {
        Import-Module '$modulePath' -Global -ErrorAction Stop
        `$script:ToolkitLoaded = `$true
    }
    & (Get-Command -Name '$functionName' -Module ScriptsToolkit) @args
}
"@
    Invoke-Expression $proxyFunction
}
```

**Optimization:** Extract function names dynamically:
```powershell
$ast = [System.Management.Automation.Language.Parser]::ParseFile(
    $modulePath,
    [ref]$null,
    [ref]$null
)
$functions = $ast.FindAll({
    $args[0] -is [System.Management.Automation.Language.FunctionDefinitionAst]
}, $true)
$functionNames = $functions | ForEach-Object { $_.Name }
```

#### Strategy 2: Lazy Load PSCompletions

**Current (Eager):**
```powershell
Import-Module PSCompletions
```

**New (Lazy):**
```powershell
$script:PSCompletionsLoaded = $false

# Register on first tab completion
$originalTabExpansion = $function:TabExpansion2
$function:global:TabExpansion2 = {
    if (-not $script:PSCompletionsLoaded) {
        try {
            Import-Module PSCompletions -ErrorAction Stop
            $script:PSCompletionsLoaded = $true
        } catch {
            Write-Warning "PSCompletions failed to load: $_"
        }
    }
    & $originalTabExpansion @args
}.GetNewClosure()
```

#### Strategy 3: Lazy Load PSFzf

**Current (Eager):**
```powershell
if (Get-Module -ListAvailable -Name PSFzf) {
    Import-Module PSFzf
    Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t'
    Set-PsFzfOption -PSReadlineChordReverseHistory 'Ctrl+r'
    Set-PSReadLineKeyHandler -Key 'Ctrl+Spacebar' -ScriptBlock { Invoke-FzfTabCompletion }
}
```

**New (Lazy):**
```powershell
$script:PSFzfLoaded = $false

function Initialize-PSFzf {
    if ($script:PSFzfLoaded) { return }
    
    if (Get-Module -ListAvailable -Name PSFzf) {
        Import-Module PSFzf -ErrorAction Stop
        Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t'
        Set-PsFzfOption -PSReadlineChordReverseHistory 'Ctrl+r'
        $script:PSFzfLoaded = $true
    }
}

# Lazy load on Ctrl+Space
Set-PSReadLineKeyHandler -Key 'Ctrl+Spacebar' -ScriptBlock {
    Initialize-PSFzf
    if (Get-Command -Name Invoke-FzfTabCompletion -ErrorAction SilentlyContinue) {
        Invoke-FzfTabCompletion
    }
}

# Lazy load on Ctrl+T
Set-PSReadLineKeyHandler -Key 'Ctrl+t' -ScriptBlock {
    Initialize-PSFzf
    if (Get-Command -Name Invoke-FzfPsReadlineProvider -ErrorAction SilentlyContinue) {
        Invoke-FzfPsReadlineProvider
    }
}

# Lazy load on Ctrl+R
Set-PSReadLineKeyHandler -Key 'Ctrl+r' -ScriptBlock {
    Initialize-PSFzf
    if (Get-Command -Name Invoke-FzfPsReadlineHistoryReverseSearch -ErrorAction SilentlyContinue) {
        Invoke-FzfPsReadlineHistoryReverseSearch
    }
}
```

#### Strategy 4: Defer carapace initialization

**Current (Eager):**
```powershell
if (Get-Command -Name carapace -CommandType Application -ErrorAction SilentlyContinue) {
    $env:CARAPACE_BRIDGES = 'zsh,fish,bash,inshellisense'
    carapace _carapace | Out-String | Invoke-Expression
}
```

**Analysis:** Carapace completions are already lazy per-command, but shell bridge initialization (`carapace _carapace`) is expensive.

**New (Deferred):**
```powershell
$script:CarapaceLoaded = $false

function Initialize-Carapace {
    if ($script:CarapaceLoaded) { return }
    
    if (Get-Command -Name carapace -CommandType Application -ErrorAction SilentlyContinue) {
        $env:CARAPACE_BRIDGES = 'zsh,fish,bash,inshellisense'
        carapace _carapace | Out-String | Invoke-Expression
        $script:CarapaceLoaded = $true
    }
}

# Register deferred initialization (load after first command)
Register-EngineEvent -SourceIdentifier PowerShell.OnIdle -MaxTriggerCount 1 -Action {
    Initialize-Carapace
}
```

#### Strategy 5: Keep argc lazy (Already exists!)

**Current:** `argc --argc-completions powershell` only loads dotnet/winget eagerly

**Already Lazy:** ScriptsToolkit has `Import-ArgcCompletions` function

**Keep:** Current behavior is correct

**Profile Change:**
```powershell
# Remove eager loading:
# if (Get-Command -Name argc ...) {
#     argc --argc-completions powershell @argcCmds | Invoke-Expression
# }

# Replace with:
# (argc completions will be loaded via Import-ArgcCompletions on-demand)
```

**Final Profile Structure:**
```powershell
# Fast startup (<100ms target)
$script:ProfileTimer = [System.Diagnostics.Stopwatch]::StartNew()

# UTF-8 (fast)
Set-Utf8Console

# PSReadLine basic config (fast)
Set-PSReadLineOption -PredictionSource HistoryAndPlugin
Set-PSReadLineOption -PredictionViewStyle ListView
Set-PSReadLineOption -Colors @{ "Selection" = "`e[7m" }

# Register ScriptsToolkit proxy functions (fast)
Register-ToolkitProxies

# Register lazy loaders (fast)
Register-PSFzfKeyHandlers
Register-CarapaceDeferredInit

Write-Host "Profile loaded in $($script:ProfileTimer.ElapsedMilliseconds)ms" -ForegroundColor Cyan
Write-Host "  ScriptsToolkit: 56 functions (lazy)" -ForegroundColor DarkGray
Write-Host "  PSFzf: Ctrl+Space/T/R (lazy)" -ForegroundColor DarkGray
Write-Host "  carapace: ~500 commands (deferred)" -ForegroundColor DarkGray
Write-Host "  argc: Import-ArgcCompletions for on-demand loading" -ForegroundColor DarkGray
```

**Files to Edit:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`

**Testing:**
1. Restart PowerShell - should be <100ms
2. Test `Get-Directories` - should load ScriptsToolkit on first call
3. Press Ctrl+Space - should load PSFzf
4. Type command - should load carapace on idle
5. Run `Import-ArgcCompletions git` - should work
6. Verify all 56 ScriptsToolkit functions available

---

### PWS-006: Configure argc lazy loading
**Complexity:** ⭐⭐ Medium | **Duration:** 1 hour | **Priority:** P2

**Goal:** Make argc manifest visible in PSCompletions menu

**Functions Already Exist:**
- `Get-ArgcManifest` - Fetches ~500+ commands from GitHub
- `Import-ArgcCompletions` (alias: `argc-load`) - Loads completions dynamically
- `Get-ArgcLoadedCommands` - Shows loaded commands

**Solution:**

**Step 1:** Expose functions from ScriptsToolkit
```powershell
# In profile (after lazy load setup)
# Functions auto-available via proxy
```

**Step 2:** Add PSCompletions integration (if possible)
```powershell
# Research: Does PSCompletions support external completion providers?
# If yes: Register argc manifest with PSCompletions
# If no: Use argc-load directly
```

**Step 3:** Create helper for discovery
```powershell
# Already exists: Get-ArgcManifest returns array of command names
# User can: Get-ArgcManifest | Out-GridView -Title "Select argc completions" -OutputMode Multiple | Import-ArgcCompletions
```

**Files:** None (uses existing ScriptsToolkit functions)

**Testing:**
1. Run `Get-ArgcManifest` - should show ~500 commands
2. Run `Get-ArgcManifest | Select-Object -First 10 | Import-ArgcCompletions`
3. Verify completions work for loaded commands
4. Run `Get-ArgcLoadedCommands` - should show loaded list

---

### PWS-007: Retain PSFzf Ctrl+Space keybinding
**Complexity:** ⭐ Low | **Duration:** 15 min | **Priority:** P2

**Current:** Already configured correctly

**Verify:**
```powershell
Set-PSReadLineKeyHandler -Key 'Ctrl+Spacebar' -ScriptBlock { Invoke-FzfTabCompletion }
```

**PSCompletions Menu:** Should use Tab key by default

**Action:** No changes needed; verify after lazy loading implementation

**Testing:**
1. Press Tab - should show PSCompletions menu
2. Press Ctrl+Space - should show fuzzy finder
3. Verify both work independently

---

## Phase 3: Binary Path Management (Priority: MEDIUM)

### PWS-008: Centralize tool paths
**Complexity:** ⭐⭐ Medium | **Duration:** 2 hours | **Priority:** P2

**Goal:** Move argc, carapace, fzf to `C:\Users\Lance\Dev\Scripts\tools\bin\`

**Step 1: Find current locations**
```powershell
Get-Command argc, carapace, fzf | Select-Object Name, Source
```

**Step 2: Create centralized directory**
```powershell
$toolsDir = 'C:\Users\Lance\Dev\Scripts\tools\bin'
New-Item -ItemType Directory -Path $toolsDir -Force
```

**Step 3: Move binaries**
```powershell
# Find argc
$argcPath = (Get-Command argc -ErrorAction SilentlyContinue).Source
if ($argcPath) {
    Copy-Item -Path $argcPath -Destination "$toolsDir\argc.exe"
}

# Find carapace
$carapacePath = (Get-Command carapace -ErrorAction SilentlyContinue).Source
if ($carapacePath) {
    Copy-Item -Path $carapacePath -Destination "$toolsDir\carapace.exe"
}

# Find fzf
$fzfPath = (Get-Command fzf -ErrorAction SilentlyContinue).Source
if ($fzfPath) {
    Copy-Item -Path $fzfPath -Destination "$toolsDir\fzf.exe"
}
```

**Step 4: Update profile PATH**
```powershell
# In profile
$toolsBin = 'C:\Users\Lance\Dev\Scripts\tools\bin'
if (Test-Path $toolsBin) {
    $env:PATH = $toolsBin + [IO.Path]::PathSeparator + $env:PATH
}
```

**Step 5: Update ScriptsToolkit.psm1**
```powershell
# Line 11-16 in ScriptsToolkit.psm1
$Script:ArgcCompletionsRoot = 'C:\Users\Lance\Dev\Scripts\tools\argc-completions'

# Update Set-CompletionPaths function
function Set-CompletionPaths {
    param(
        [Parameter()]
        [string]$ArgcRoot = 'C:\Users\Lance\Dev\Scripts\tools\argc-completions'
    )
    # ... rest of function
}
```

**Step 6: Search C# code for binary paths**
```powershell
Get-ChildItem -Path C:\Users\Lance\Dev\Scripts\csharp\src -Recurse -Include *.cs |
    Select-String -Pattern 'argc|carapace|fzf|ffprobe|python' |
    Where-Object { $_.Line -notmatch '^\s*//' }
```

**Files to Edit:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`
- Any C# files with hardcoded paths

**Testing:**
1. Restart PowerShell
2. Verify `Get-Command argc` points to new location
3. Test argc completions work
4. Test carapace completions work
5. Test fzf works (Ctrl+T, Ctrl+R, Ctrl+Space)

---

### PWS-009: Update all path references
**Complexity:** ⭐⭐ Medium | **Duration:** 1-2 hours | **Priority:** P2

**Goal:** Audit and fix all hardcoded paths

**Step 1: Find hardcoded paths in PowerShell**
```powershell
Get-ChildItem -Path powershell -Recurse -Include *.ps1, *.psm1 |
    Select-String -Pattern 'C:\\Users\\|C:/Users/' |
    Where-Object { $_.Line -notmatch '^\s*#' } |
    Group-Object Path |
    ForEach-Object {
        [PSCustomObject]@{
            File = $_.Name
            Count = $_.Count
            Lines = ($_.Group | Select-Object -ExpandProperty LineNumber) -join ', '
        }
    }
```

**Step 2: Find hardcoded paths in C#**
```powershell
Get-ChildItem -Path csharp\src -Recurse -Include *.cs |
    Select-String -Pattern 'C:\\\\Users\\\\|@"C:|"C:' |
    Where-Object { $_.Line -notmatch '^\s*//' } |
    Group-Object Path |
    ForEach-Object {
        [PSCustomObject]@{
            File = $_.Name
            Count = $_.Count
        }
    }
```

**Step 3: Verify ScriptsToolkit paths**
```powershell
# Check ScriptsToolkit.psm1 lines 1-7
$Script:RepositoryRoot = Split-Path -Path $PSScriptRoot -Parent  # Should be C:\Users\Lance\Dev\Scripts
$Script:PythonToolkit = Join-Path -Path $RepositoryRoot -ChildPath 'python\toolkit\cli.py'
$Script:CSharpRoot = Join-Path -Path $RepositoryRoot -ChildPath 'csharp'
$Script:LogDirectory = Join-Path -Path $RepositoryRoot -ChildPath 'logs'
```

**Step 4: Create path test script**
```powershell
# Test all paths are valid
$paths = @{
    'RepositoryRoot' = 'C:\Users\Lance\Dev\Scripts'
    'PythonToolkit' = 'C:\Users\Lance\Dev\Scripts\python\toolkit\cli.py'
    'CSharpRoot' = 'C:\Users\Lance\Dev\Scripts\csharp'
    'LogDirectory' = 'C:\Users\Lance\Dev\Scripts\logs'
    'ToolsBin' = 'C:\Users\Lance\Dev\Scripts\tools\bin'
}

foreach ($name in $paths.Keys) {
    $path = $paths[$name]
    $exists = Test-Path -LiteralPath $path
    Write-Host "$name : $path - $(if ($exists) { '[✓]' } else { '[✗ MISSING]' })"
}
```

**Files to Audit:** All `.cs`, `.ps1`, `.psm1` files

**Testing:**
1. Run path audit scripts
2. Fix each hardcoded path
3. Re-run tests
4. Verify all paths resolve correctly

---

## Phase 4: Module & Command Management (Priority: LOW)

### PWS-010: Find unapproved PowerShell verbs
**Complexity:** ⭐ Low | **Duration:** 15 min | **Priority:** P3

**Solution:** Function already exists!

**Steps:**
```powershell
# 1. Import module
Import-Module C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1

# 2. Run function
Find-UnapprovedVerbs -ModuleName ScriptsToolkit

# 3. Output to file
Find-UnapprovedVerbs -ModuleName ScriptsToolkit |
    Export-Csv -Path C:\Users\Lance\Dev\Scripts\markdown\unapproved_verbs.csv -NoTypeInformation
```

**Expected Output:**
```
Unapproved verbs in ScriptsToolkit:
  Create-Something -> New-Something
  Delete-Something -> Remove-Something
```

**Files:** None (uses existing function)

**Testing:**
1. Run command
2. Review output
3. Create PWS-011 plan based on results

---

### PWS-011: Create verb migration plan
**Complexity:** ⭐⭐ Medium | **Duration:** 2-3 hours | **Priority:** P3

**Dependencies:** PWS-010 must complete first

**Process:**

**Step 1:** Get unapproved verbs
```powershell
$unapproved = Find-UnapprovedVerbs -ModuleName ScriptsToolkit
```

**Step 2:** For each unapproved verb, create migration plan
```markdown
## Verb Migration Plan

### Create → New
**Functions:**
- Create-OutputDirectory → New-OutputDirectory
- Create-Gif → New-Gif

**Aliases to Add:**
- `create` → `New-*` (for backward compat)

**Find/Replace:**
```powershell
# ScriptsToolkit.psm1
function Create-OutputDirectory  →  function New-OutputDirectory

# All calling scripts
Create-OutputDirectory  →  New-OutputDirectory
```

**Files to Update:**
- ScriptsToolkit.psm1
- (Search for callers)
```

**Step 3:** Create implementation file
```
markdown/implementation/verb_migration_plan.md
```

**Files to Create:**
- `markdown/implementation/verb_migration_plan.md`

**Testing:**
1. Create plan document
2. Review with user before implementing
3. Implement one verb at a time
4. Test each change

---

## Phase 5: Whisper Integration (Priority: MEDIUM)

### PWS-012: Create 'whisp' alias
**Complexity:** ⭐ Low | **Duration:** 30 min | **Priority:** P2

**Solution:**

**In ScriptsToolkit.psm1:**
```powershell
function Invoke-WhisperEnglishFast {
    <#
    .SYNOPSIS
        Fast English transcription using Whisper (distil-large-v3.5).
    
    .DESCRIPTION
        Alias: whisp
        Transcribes audio/video using distil-large-v3.5 model with English language preset.
    
    .PARAMETER Path
        Path to audio or video file to transcribe.
    
    .EXAMPLE
        whisp '.\interview.mp4'
        Transcribes interview.mp4 using fast English model.
    #>
    [CmdletBinding()]
    [Alias('whisp')]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [ValidateScript({ Test-Path -LiteralPath $_ })]
        [string]$Path
    )
    
    process {
        Invoke-Whisper -Path $Path -Model 'distil-large-v3.5' -Language 'en'
    }
}
```

**Files to Edit:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`

**Testing:**
1. Import module: `Import-Module ScriptsToolkit -Force`
2. Test alias: `whisp '.\test.mp4'`
3. Verify uses distil-large-v3.5 and English
4. Check output quality

---

### PWS-013: Check whisper in Save-YouTubeVideo
**Complexity:** ⭐ Low | **Duration:** 30 min | **Priority:** P3

**Goal:** Verify warning shown when whisper-ctranslate2 missing

**Current Code (Invoke-Whisper, line 1551):**
```powershell
try {
    $null = Get-Command -Name whisper-ctranslate2 -ErrorAction Stop
} catch {
    Write-Warning "whisper-ctranslate2 not found"
    return
}
```

**Check Save-YouTubeVideo:**
```powershell
# Search for whisper check in Save-YouTubeVideo
Select-String -Path ScriptsToolkit.psm1 -Pattern 'whisper-ctranslate2' -Context 5, 5
```

**If Missing, Add:**
```powershell
function Save-YouTubeVideo {
    # ... existing params ...
    
    # Check if whisper available
    $whisperAvailable = $null -ne (Get-Command -Name whisper-ctranslate2 -ErrorAction SilentlyContinue)
    if (-not $whisperAvailable) {
        Write-Warning "whisper-ctranslate2 not found. Transcription will be skipped."
        Write-Warning "Install: pip install whisper-ctranslate2"
    }
    
    # ... download video logic ...
    
    # Only transcribe if whisper available
    if ($whisperAvailable) {
        Invoke-Whisper -Path $videoPath
    }
}
```

**Files to Edit:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`

**Testing:**
1. Rename whisper-ctranslate2.exe temporarily
2. Run Save-YouTubeVideo
3. Verify warning shows
4. Rename back
5. Verify transcription works

---

### PWS-014: Restore model download progress
**Complexity:** ⭐⭐ Medium | **Duration:** 1-2 hours | **Priority:** P3

**Problem:** Model download progress suppressed

**Current (Suppressed):**
```powershell
# Invoke-Whisper calls whisper-ctranslate2
# stderr suppressed: 2>$null or 2>&1
```

**Solution:**

**Step 1:** Detect when model is downloading
```powershell
# whisper-ctranslate2 outputs to stderr:
# "Downloading model..."
# Progress bar on stderr
# "Model loaded"

# Capture stderr, check for download
$stderr = & whisper-ctranslate2 --help 2>&1
```

**Step 2:** Show progress during download
```powershell
function Invoke-Whisper {
    # ... existing params ...
    
    # First run: Check if model exists
    $modelCheck = & whisper-ctranslate2 --model $Model --help 2>&1
    $downloading = $modelCheck | Where-Object { $_ -match 'Downloading' }
    
    if ($downloading) {
        Write-Host "Downloading Whisper model: $Model" -ForegroundColor Yellow
        # Don't suppress stderr during download
        & whisper-ctranslate2 --model $Model --language $Language $Path
    } else {
        # Model exists, normal transcription (suppress warnings)
        & whisper-ctranslate2 --model $Model --language $Language $Path 2>&1 |
            Where-Object { $_ -notmatch 'FutureWarning|DeprecationWarning' }
    }
}
```

**Files to Edit:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1` (Invoke-Whisper, Invoke-WhisperJapanese, Invoke-WhisperFolder)

**Testing:**
1. Delete whisper model cache: `Remove-Item ~\.cache\huggingface -Recurse`
2. Run Invoke-Whisper
3. Verify download progress shows
4. Run again
5. Verify no download, transcription starts immediately

---

### PWS-015: Improve Whisper progress display
**Complexity:** ⭐⭐⭐ High | **Duration:** 3-4 hours | **Priority:** P3

**Current Output:**
```
12%|███▋| 478.3/3986.2826875 [02:48<22:43, 2.57seconds/s]
```

**Target Output:**
```
12%|███▋| 478s / 3986s [Elapsed: 02:48 | ETA: 22:43 | Speed: 2.6s/s]
```

**Solution:**

**Step 1:** Capture and parse progress
```powershell
$progressRegex = [regex]'(\d+)%\|([^\|]+)\|\s+([\d.]+)/([\d.]+)\s+\[([^\]]+)<([^\]]+),\s+([\d.]+)seconds/s\]'

& whisper-ctranslate2 --model $Model --language $Language $Path 2>&1 |
    ForEach-Object {
        if ($_ -match $progressRegex) {
            $percent = $matches[1]
            $bar = $matches[2]
            $current = [math]::Round([double]$matches[3])
            $total = [math]::Round([double]$matches[4])
            $elapsed = $matches[5]
            $eta = $matches[6]
            $speed = [math]::Round([double]$matches[7], 1)
            
            # Reformat
            $reformatted = "$percent%|$bar| ${current}s / ${total}s [Elapsed: $elapsed | ETA: $eta | Speed: ${speed}s/s]"
            Write-Host "`r$reformatted" -NoNewline
        } else {
            Write-Host $_
        }
    }
```

**Step 2:** Handle line endings
```powershell
# Clear line before writing
Write-Host "`r" -NoNewline
Write-Host (" " * 120) -NoNewline
Write-Host "`r$reformatted" -NoNewline
```

**Files to Edit:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`

**Testing:**
1. Run Invoke-Whisper on long video
2. Verify progress shows reformatted
3. Verify labels clear
4. Check final output

---

### PWS-016: Suppress Python warnings
**Complexity:** ⭐⭐ Medium | **Duration:** 30 min | **Priority:** P2

**Solution:**

**In Invoke-ToolkitPython:**
```powershell
function Invoke-ToolkitPython {
    param([string[]]$Arguments)
    
    # Suppress warnings
    $env:PYTHONWARNINGS = 'ignore::DeprecationWarning,ignore::FutureWarning'
    $env:PYTHONDONTWRITEBYTECODE = '1'
    
    $python = (Get-Command -Name python).Source
    & $python $Script:PythonToolkit @Arguments
}
```

**In Invoke-Whisper:**
```powershell
function Invoke-Whisper {
    # ... params ...
    
    # Suppress warnings
    $env:PYTHONWARNINGS = 'ignore::DeprecationWarning,ignore::FutureWarning'
    
    & whisper-ctranslate2 --model $Model --language $Language $Path 2>&1 |
        Where-Object { $_ -notmatch 'FutureWarning|DeprecationWarning|UserWarning' }
}
```

**Files to Edit:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`

**Testing:**
1. Run Invoke-Whisper
2. Verify no DeprecationWarning shown
3. Run Invoke-ToolkitPython
4. Verify no warnings

---

### PWS-017: Add whisper autocomplete
**Complexity:** ⭐⭐ Medium | **Duration:** 1-2 hours | **Priority:** P3

**Research:**

**Step 1:** Check argc manifest
```powershell
Get-ArgcManifest | Where-Object { $_ -like '*whisper*' }
```

**Step 2:** Check carapace
```bash
carapace --list | grep whisper
```

**Step 3:** If not available, create custom completion
```powershell
Register-ArgumentCompleter -CommandName 'whisper-ctranslate2' -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)
    
    $completions = @(
        '--model', '--language', '--device', '--compute_type',
        '--beam_size', '--best_of', '--patience', '--length_penalty',
        '--temperature', '--compression_ratio_threshold',
        '--log_prob_threshold', '--no_speech_threshold',
        '--condition_on_previous_text', '--initial_prompt',
        '--word_timestamps', '--prepend_punctuations',
        '--append_punctuations', '--max_line_width',
        '--max_line_count', '--highlight_words', '--threads'
    )
    
    $completions | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
        [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterName', $_)
    }
}

# Model completions
Register-ArgumentCompleter -CommandName 'whisper-ctranslate2' -ParameterName 'model' -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)
    
    $models = @(
        'tiny', 'tiny.en', 'base', 'base.en',
        'small', 'small.en', 'medium', 'medium.en',
        'large-v1', 'large-v2', 'large-v3',
        'distil-large-v2', 'distil-large-v3', 'distil-large-v3.5'
    )
    
    $models | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
        [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
    }
}
```

**Files to Edit:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`

**Testing:**
1. Type `whisper-ctranslate2 --mod<tab>`
2. Should complete to `--model`
3. Type `whisper-ctranslate2 --model dist<tab>`
4. Should show distil models

---

### PWS-018: Auto-invoke whisp after YouTube download
**Complexity:** ⭐⭐ Medium | **Duration:** 45 min | **Priority:** P2

**Dependencies:** PWS-012 (whisp alias)

**Solution:**

**Update Save-YouTubeVideo:**
```powershell
function Save-YouTubeVideo {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string]$Url,
        
        [Parameter()]
        [switch]$Transcribe = $true,  # Default: auto-transcribe
        
        [Parameter()]
        [string]$Model = 'distil-large-v3.5',
        
        [Parameter()]
        [string]$Language = 'en'
    )
    
    # Download video
    Write-Host "Downloading: $Url"
    $output = & yt-dlp $Url -o '%(title)s.%(ext)s'
    
    # Get downloaded file path
    $videoFile = Get-ChildItem -Path . -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    Write-Host "Downloaded: $($videoFile.Name)"
    
    # Auto-transcribe if enabled
    if ($Transcribe) {
        $whisperAvailable = $null -ne (Get-Command -Name whisper-ctranslate2 -ErrorAction SilentlyContinue)
        
        if ($whisperAvailable) {
            Write-Host "Transcribing with whisp ($Model, $Language)..."
            Invoke-WhisperEnglishFast -Path $videoFile.FullName
        } else {
            Write-Warning "whisper-ctranslate2 not found. Skipping transcription."
            Write-Warning "Install: pip install whisper-ctranslate2"
        }
    }
}
```

**Files to Edit:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`

**Testing:**
1. Run `Save-YouTubeVideo 'https://youtube.com/watch?v=...'`
2. Verify video downloads
3. Verify whisp runs automatically
4. Test `-Transcribe:$false` to skip
5. Verify transcription file created

---

## Phase 6: File System Operations (Priority: MEDIUM)

### PWS-019: Fix file extension repair command
**Complexity:** ⭐⭐⭐ High | **Duration:** 2-3 hours | **Priority:** P2

**Problem:** Current one-liner fails on:
1. Wildcard characters in filenames (`[`, `]`)
2. Invalid format names from ffprobe

**Solution:**

```powershell
function Repair-FileExtensions {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$Path,
        
        [Parameter()]
        [switch]$Recurse,
        
        [Parameter()]
        [switch]$WhatIf
    )
    
    # Format name to extension mapping
    $formatMap = @{
        'matroska,webm' = 'mkv'
        'matroska' = 'mkv'
        'webm' = 'webm'
        'mov,mp4,m4a,3gp,3g2,mj2' = 'mp4'
        'mp4' = 'mp4'
        'avi' = 'avi'
        'flv' = 'flv'
        'mpegts' = 'ts'
        'mpeg' = 'mpg'
        'asf' = 'wmv'
        'wav' = 'wav'
        'mp3' = 'mp3'
        'flac' = 'flac'
        'ogg' = 'ogg'
        'm4a' = 'm4a'
        'aac' = 'aac'
    }
    
    $files = Get-ChildItem -LiteralPath $Path -Recurse:$Recurse -File | 
        Where-Object { -not $_.Extension }
    
    foreach ($file in $files) {
        Write-Host "Processing: $($file.Name)"
        
        try {
            # Get format using ffprobe
            $format = & ffprobe -v error -show_entries format=format_name -of csv=p=0 $file.FullName 2>$null
            
            if (-not $format) {
                Write-Warning "  Could not detect format"
                continue
            }
            
            # Map to extension
            $extension = $null
            foreach ($key in $formatMap.Keys) {
                if ($format -match $key) {
                    $extension = $formatMap[$key]
                    break
                }
            }
            
            if (-not $extension) {
                Write-Warning "  Unknown format: $format"
                continue
            }
            
            # Rename file
            $newName = "$($file.Name).$extension"
            $newPath = Join-Path $file.DirectoryName $newName
            
            if ($WhatIf) {
                Write-Host "  Would rename to: $newName" -ForegroundColor Yellow
            } else {
                Rename-Item -LiteralPath $file.FullName -NewName $newName -ErrorAction Stop
                Write-Host "  ✓ Renamed to: $newName" -ForegroundColor Green
            }
        }
        catch {
            Write-Warning "  Failed: $_"
        }
    }
}

# Usage:
# Repair-FileExtensions -Path 'D:\Google Drive\Games\Others' -Recurse -WhatIf
```

**Files:** None (function, not saved to module per requirement)

**Testing:**
1. Test on small directory first
2. Use -WhatIf to preview changes
3. Verify renames work
4. Test on files with `[`, `]` in names
5. Run on full directory

---

### PWS-020: Segregate files by extension
**Complexity:** ⭐⭐ Medium | **Duration:** 30 min | **Priority:** P3

**Solution:**

```powershell
function Group-FilesByExtension {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$Path,
        
        [Parameter()]
        [switch]$WhatIf
    )
    
    $files = Get-ChildItem -LiteralPath $Path -File
    $grouped = $files | Group-Object Extension
    
    foreach ($group in $grouped) {
        $dirName = if ($group.Name) { 
            $group.Name.TrimStart('.') 
        } else { 
            'NoExtension' 
        }
        
        $targetDir = Join-Path $Path $dirName
        
        if ($WhatIf) {
            Write-Host "Would create directory: $targetDir"
            Write-Host "Would move $($group.Count) files"
        } else {
            New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
            Write-Host "Moving $($group.Count) $dirName files..."
            
            $group.Group | ForEach-Object {
                Move-Item -LiteralPath $_.FullName -Destination $targetDir -Force
                Write-Host "  ✓ $($_.Name)"
            }
        }
    }
}

# Usage:
# Group-FilesByExtension -Path 'D:\Google Drive\Games\Others\Miscellaneous' -WhatIf
```

**Files:** None (one-liner)

**Testing:**
1. Test with -WhatIf
2. Verify directories created
3. Verify files moved correctly
4. Check no files lost

---

## Phase 7: Task Scheduling (Priority: LOW)

### PWS-021: Fix auto-close terminal on sync success
**Complexity:** ⭐⭐ Medium | **Duration:** 2 hours | **Priority:** P3

**Problem:** Terminals don't close after successful sync

**Root Cause Analysis:**

**Step 1:** Check C# CLI exit codes
```csharp
// In SyncCommands.cs
public class SyncYouTubeCommand : AsyncCommand<SyncSettings>
{
    public override async Task<int> ExecuteAsync(...)
    {
        // Should return 0 on success
        return 0;  // Success
        return 1;  // Failure
    }
}
```

**Step 2:** Check PowerShell wrapper
```powershell
function Invoke-YouTubeSync {
    # Current:
    $exe = Join-Path $Script:CSharpRoot 'bin\Debug\net10.0\CSharpScripts.exe'
    & $exe sync yt
    
    # Should be:
    & $exe sync yt
    exit $LASTEXITCODE  # Exit with C# return code
}
```

**Step 3:** Check scheduled task
```powershell
function Register-ScheduledSyncTask {
    # Current action:
    $action = New-ScheduledTaskAction -Execute 'pwsh.exe' -Argument "-NoProfile -Command `"Invoke-YouTubeSync`""
    
    # Should be:
    $argument = "-NoProfile -Command `"Invoke-YouTubeSync; exit `$LASTEXITCODE`""
    $action = New-ScheduledTaskAction -Execute 'pwsh.exe' -Argument $argument
}
```

**Solution:**

**File 1: C# Commands (csharp\src\CLI\SyncCommands.cs)**
```csharp
// Ensure all commands return proper exit codes
public override async Task<int> ExecuteAsync(...)
{
    try
    {
        await SyncLogic();
        return 0;  // Success
    }
    catch (Exception ex)
    {
        Logger.Error("Sync failed", ex);
        return 1;  // Failure
    }
}
```

**File 2: PowerShell Wrappers (ScriptsToolkit.psm1)**
```powershell
function Invoke-YouTubeSync {
    $exe = Join-Path $Script:CSharpRoot 'bin\Debug\net10.0\CSharpScripts.exe'
    & $exe sync yt
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ YouTube sync completed successfully" -ForegroundColor Green
    } else {
        Write-Warning "YouTube sync failed with exit code $LASTEXITCODE"
    }
    
    exit $LASTEXITCODE
}

function Invoke-LastFmSync {
    $exe = Join-Path $Script:CSharpRoot 'bin\Debug\net10.0\CSharpScripts.exe'
    & $exe sync lastfm
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Last.fm sync completed successfully" -ForegroundColor Green
    } else {
        Write-Warning "Last.fm sync failed with exit code $LASTEXITCODE"
    }
    
    exit $LASTEXITCODE
}
```

**File 3: Scheduled Task (ScriptsToolkit.psm1)**
```powershell
function Register-ScheduledSyncTask {
    param(
        [string]$TaskName,
        [string]$Command,
        [string]$DailyTime,
        [string]$Description
    )
    
    # Use .exe directly instead of PowerShell wrapper
    $executable = Join-Path $Script:CSharpRoot 'bin\Debug\net10.0\CSharpScripts.exe'
    $argument = $Command  # e.g., "sync yt"
    
    $action = New-ScheduledTaskAction -Execute $executable -Argument $argument -WorkingDirectory $Script:CSharpRoot
    
    # ... rest of function
}
```

**Files to Edit:**
- `C:\Users\Lance\Dev\Scripts\csharp\src\CLI\SyncCommands.cs`
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`

**Testing:**
1. Run `Invoke-YouTubeSync` manually - verify terminal closes on success
2. Create test scheduled task
3. Run task manually
4. Verify terminal closes on success
5. Verify terminal stays open on failure

---

## Phase 8: Documentation (Priority: LOW)

### PWS-022: Explain OmniSharp purpose
**Complexity:** ⭐ Low | **Duration:** 30 min | **Priority:** P3

**Solution:** Create documentation file

**File: markdown/development_environment.md**
```markdown
# Development Environment

## Language Servers

### OmniSharp (C#)
**Purpose:** C# language server providing IntelliSense, code navigation, refactoring

**Features:**
- Syntax highlighting
- IntelliSense (autocomplete)
- Go to definition
- Find references
- Refactoring
- Code fixes

**Used By:** VS Code C# extension

**Configuration:** `.vscode/settings.json` → `"omnisharp.path"`

### basedpyright (Python)
**Purpose:** Python type checker (fork of Pyright with additional features)

**Features:**
- Static type checking
- Type inference
- Error detection
- Import resolution

**Configuration:** `python/pyproject.toml` → `[tool.basedpyright]`

### PowerShell Language Server
**Purpose:** PowerShell IntelliSense and analysis

**Features:**
- PSScriptAnalyzer integration
- Debugging support
- Code formatting

**Used By:** VS Code PowerShell extension

## Formatters

### CSharpier (C#)
**Purpose:** Opinionated C# code formatter

**Configuration:** `.editorconfig`

### Black (Python)
**Purpose:** Opinionated Python code formatter

**Configuration:** `python/pyproject.toml` → `[tool.black]`

### PowerShell Script Analyzer
**Purpose:** PowerShell linter

**Configuration:** `powershell/ScriptsToolkit/PSScriptAnalyzerSettings.psd1`

## Completion Tools

### argc
**Purpose:** Command-line completion for 500+ tools

**Location:** `C:\Users\Lance\Dev\Scripts\tools\bin\argc.exe`

**Usage:** `Import-ArgcCompletions git, docker, kubectl`

### carapace
**Purpose:** Multi-shell completion bridge (~500 commands)

**Location:** `C:\Users\Lance\Dev\Scripts\tools\bin\carapace.exe`

**Bridges:** zsh, fish, bash, inshellisense

### fzf
**Purpose:** Fuzzy finder for files, history, completions

**Location:** WinGet install (via PATH)

**Usage:** Ctrl+T (files), Ctrl+R (history), Ctrl+Space (completion)
```

**Files to Create:**
- `markdown/development_environment.md`

**Testing:**
1. Review documentation
2. Verify all tool paths correct
3. Add any missing tools

---

## Implementation Timeline

### Week 1: Foundation (P0 - Critical)
- [ ] PWS-002: Fix Set-Item error (30 min)
- [ ] PWS-003: Eliminate profile duplication (15 min)
- [ ] PWS-001: Force UTF-8 encoding (10 min)
- [ ] PWS-004: Add differential timing (45 min)

**Total: ~2 hours**  
**Goal:** Stable, error-free profile with timing

### Week 2: Performance (P1 - High)
- [ ] PWS-005: Implement lazy module loading (6 hours)
- [ ] PWS-006: Configure argc lazy loading (1 hour)
- [ ] PWS-007: Retain PSFzf keybinding (15 min)

**Total: ~7 hours**  
**Goal:** Profile loads in <100ms

### Week 3: Organization (P2 - Medium)
- [ ] PWS-008: Centralize tool paths (2 hours)
- [ ] PWS-009: Update all path references (2 hours)
- [ ] PWS-012: Create whisp alias (30 min)
- [ ] PWS-016: Suppress Python warnings (30 min)
- [ ] PWS-018: Auto-invoke whisp after YouTube download (45 min)

**Total: ~6 hours**  
**Goal:** Organized, clean paths

### Week 4: Enhancements (P3 - Low)
- [ ] PWS-010: Find unapproved verbs (15 min)
- [ ] PWS-011: Create verb migration plan (2 hours)
- [ ] PWS-013-015: Whisper improvements (5 hours)
- [ ] PWS-019-020: File operations (3 hours)
- [ ] PWS-021: Fix auto-close terminal (2 hours)
- [ ] PWS-022: Documentation (30 min)

**Total: ~13 hours**  
**Goal:** Polished, feature-complete

---

## Testing Checklist

### Phase 1: Profile Foundation
- [ ] No "Set-Item" error on startup
- [ ] Profile loads without warnings
- [ ] UTF-8 encoding works for unicode
- [ ] Timing shows for each module
- [ ] Differential (Δ) calculations correct
- [ ] Total load time displays

### Phase 2: Lazy Loading
- [ ] Profile loads in <100ms
- [ ] ScriptsToolkit functions work on first call
- [ ] PSFzf loads on Ctrl+Space
- [ ] carapace loads on idle
- [ ] argc completions load on-demand
- [ ] All 56 ScriptsToolkit functions available

### Phase 3: Paths
- [ ] All tools in centralized location
- [ ] $env:PATH includes tools\bin
- [ ] argc/carapace/fzf found via Get-Command
- [ ] All PowerShell paths relative
- [ ] All C# paths use Paths.cs

### Phase 4: Whisper
- [ ] whisp alias works
- [ ] Model download shows progress
- [ ] Transcription progress clear
- [ ] No Python warnings
- [ ] YouTube auto-transcribes

### Phase 5: File Operations
- [ ] Extension repair handles wildcards
- [ ] Files segregate by extension
- [ ] No data loss during operations

### Phase 6: Tasks
- [ ] Sync tasks close on success
- [ ] Terminal stays open on failure
- [ ] Exit codes correct

---

## Success Criteria

1. **Profile Load Time:** <100ms (down from 937ms)
2. **Zero Errors:** No Set-Item, no module load failures
3. **Lazy Loading:** All modules load on-demand
4. **Paths:** All tools centralized, references updated
5. **Whisper:** Full integration with YouTube downloads
6. **Documentation:** Complete environment docs

---

## Rollback Plan

If lazy loading breaks functionality:

1. **Backup Current Profile:**
   ```powershell
   Copy-Item C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1 `
             C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1.backup
   ```

2. **Restore From Backup:**
   ```powershell
   Copy-Item C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1.backup `
             C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1 -Force
   ```

3. **Test Incrementally:** Enable one lazy load at a time, test, commit before next
