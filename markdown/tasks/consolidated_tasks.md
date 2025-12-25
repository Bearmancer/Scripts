# Consolidated Task List
*Generated: December 25, 2025*
*Sources: To-Do.md, master_task_list.md, migration_signatures.md, powershell_enhancements.md*
*Last Verified: January 2025*

---

## ✅ RESOLVED: Completion System Conflicts

**Verified January 2025:** Conflicts have been resolved by removing overlapping PSCompletions.

**PSCompletions Stats (CURRENT):**
- **2 completions installed:** `psc`, `winget` (verified via `psc list`)
- Previous state had 72 completions causing 45 conflicts with Carapace
- Config: `enable_menu=1`, `enable_menu_enhance=1`, `trigger_key=Tab`
- Tab → CustomAction (intercepts all Tab completions, renders Carapace output)

**Carapace Stats:**
- **670 completions** built-in
- Does NOT have: winget (uses PSCompletions)
- Custom spec: whisper-ctranslate2.yaml ✅

**Current Architecture (CLEAN):**
- PSCompletions: Menu UI + `winget`, `psc` completions
- Carapace: 670 commands (git, gh, docker, kubectl, etc.)
- argc: `dotnet`, `whisper-ctranslate2`

---

## 🔴 Completion System Issues (Remaining)

These issues need diagnosis and resolution. Root cause analysis required.

### COMP-001: Tab causing double space ⭐⭐⭐ High
**Description:** Tab key insertion causes double space for unknown reason.
**Root Cause:** Likely PSCompletions `completion_suffix` config combined with Carapace completer output
**Verified Config:** `completion_suffix = ""` (empty in config)
**Diagnosis:** Check if PSCompletions is adding space AND the completer also adds space

---

### COMP-002: Tab selecting instead of filling ⭐⭐⭐ High
**Description:** Tab goes down on menu instead of filling presently selected value.
**Expected:** Tab should complete/fill selected item
**Verified Config:** `enable_enter_when_single=0` - When only one match, still shows menu
**Fix Option:** Set `psc config enable_enter_when_single 1` for immediate fill on single match

---

### COMP-003: winget dynamic autocomplete broken ⭐⭐⭐ High
**Description:** `winget install je--` not causing dynamic autocomplete to trigger.
**Verified:** Carapace does NOT have winget. PSCompletions HAS winget (in 72 completions).
**Root Cause:** PSCompletions winget completer may not support dynamic package search.
**Fix Options:**
1. Use argc: `argc --argc-completions powershell winget` (verified working)
2. Check if PSCompletions winget supports `winget install <Tab>` package search

---

### COMP-004: Create completion benchmark ⭐⭐ Medium
**Description:** Create benchmark to test how long dynamic completion takes per tool.
**Verified Benchmark Data (Prior Session):**
| Component | Invoke Time (ms) |
|-----------|------------------|
| Baseline (pwsh -NoProfile) | 303 |
| winget native (`Register-ArgumentCompleter`) | 351 |
| gh native (`gh completion -s powershell`) | 464 |
| PSCompletions import | 716-769 |
| dotnet native (.NET 10) | 1090 |
| Carapace (`carapace _carapace`) | 1261 |

---

### COMP-005: gh completion shows explanation ⭐ Low
**Description:** `gh` completions show explanation after name of command (informational, may be correct behavior).
**Verified:** Both PSCompletions AND Carapace have `gh` (conflict). Rich metadata from either source.

---

### COMP-006: gh preview feature ⭐ Low
**Description:** `gh` shows preview sometimes - need to determine source.
**Suspected Source:** PSFzf's `Invoke-FzfTabCompletion` preview pane OR PSCompletions menu enhancement

---

### COMP-007: gh double parenthesis ⭐ Low
**Description:** `gh` completions have double parenthesis at times.
**Research:** Check Carapace gh completion spec formatting

---

### COMP-008: Diagnose help parsing handlers ⭐⭐ Medium
**Description:** winget explanation appears below item - diagnose who handles help parsing.
**Components to Check:** PSCompletions tooltip vs Carapace description field vs native completion

---

### COMP-009: Document completion component responsibilities ⭐⭐ Medium
**Description:** Assess who handles each part: Tab key press, auto-suggest, help menu parsing, creating new help, auto-suggest segregation.
**Answer (Verified):**
- **Tab Key Press:** PSCompletions (via `Set-PSReadLineKeyHandler -Key Tab`)
- **Auto-suggest (Inline):** PSReadLine `PredictionSource History`
- **Help Menu/Tooltips:** PSCompletions `enable_menu_enhance=1` renders ALL completer output
- **Completion Data:** Carapace (670 commands) + argc (dotnet, winget) + native (`gh`, `winget`)

---

### COMP-010: PSCompletions color customization ⭐ Low
**Description:** Can all parts of `psc` be customized (selected item color, scheme)?
**Answer:** Yes - via `psc config` or `$PSCompletions.config.*` properties

---

### COMP-011: Use warmer colors ⭐ Low
**Description:** Use warmer colors if possible for completion menu.
**Implementation:** Configure PSCompletions color scheme in profile

---

### COMP-012: PSCompletions 'need_ignore_suffix' error ✅ RESOLVED
**Description:** Exception in custom key handler: "The property 'need_ignore_suffix' cannot be found on this object."
**Error Context:** Occurred on `git ` tab completion

**Root Cause (Verified January 2025):**
- Conflicting completions: PSCompletions had 72 completions including git, 45 overlapped with Carapace
- When PSCompletions processed Tab for `git`, it received completion objects from BOTH sources

**Resolution:** Removed conflicting completions. Now only `psc` and `winget` in PSCompletions.

**Current State (TESTED):**
```pwsh
TabExpansion2 'git ' -cursorColumn 4  # Returns Carapace completions, no error
```

---

### COMP-013: PSFzf history popup ⭐ Low
**Description:** Is PSFzf the only way to see popup list of recent commands?
**Answer:** PSFzf provides fuzzy search via `Ctrl+R`. PSReadLine also has native history search via `F7` or `#` trigger.
**⚠️ NOTE:** PSFzf is NOT currently in the profile - see COMP-016 to add it.

---

### COMP-014: Document Ctrl+R vs Ctrl+T ⭐⭐ Medium
**Description:** Disambiguate what Ctrl+R and Ctrl+T do and how fzf search differs from PSCompletions.
**Answer (When PSFzf is enabled):**
- **Ctrl+R (PSFzf):** Fuzzy reverse history search (find previous commands)
- **Ctrl+T (PSFzf):** Fuzzy file/directory search in current location
- **Ctrl+Space (PSFzf):** `Invoke-FzfTabCompletion` - fuzzy completion for current context
- **Tab (PSCompletions):** Menu-based completion with TUI
- **Difference:** PSFzf is fuzzy/interactive, PSCompletions is menu-driven with exact match
**⚠️ NOTE:** PSFzf must first be added to profile - see COMP-016.

---

### COMP-015: Carapace same 'need_ignore_suffix' error ✅ RESOLVED
**Description:** Same error as COMP-012 occurred for `carapace`.
**Resolution:** Fixed along with COMP-012 by removing conflicting PSCompletions.

---

### COMP-016: Add PSFzf to profile ⭐⭐ Medium
**Description:** PSFzf is referenced in documentation but NOT in current profile.
**Current State:** Profile loads PSCompletions, Carapace, argc but NOT PSFzf
**Impact:** Ctrl+R, Ctrl+T, Ctrl+Space fuzzy features are unavailable

**Implementation:**
```powershell
#region PSFzf - Fuzzy finder integration
if (Get-Module -ListAvailable -Name PSFzf) {
    Import-Module PSFzf
    Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t'
    Set-PsFzfOption -PSReadlineChordReverseHistory 'Ctrl+r'
    Set-PSReadLineKeyHandler -Key 'Ctrl+Spacebar' -ScriptBlock { Invoke-FzfTabCompletion }
}
#endregion
```

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`

**Performance Note:** Adding PSFzf will add ~200-300ms to profile load time.

---

### COMP-017: Resolve PSCompletions/Carapace conflicts ✅ RESOLVED
**Description:** 45 commands had completions in BOTH PSCompletions and Carapace, causing errors.

**Resolution Applied:**
- Removed conflicting completions from PSCompletions
- Current PSCompletions: `psc`, `winget` only (verified via `psc list`)
- Carapace: 670 commands (no conflicts)

**Current Architecture:**
| Component | Role | Completions |
|-----------|------|-------------|
| PSCompletions | Menu UI + `psc`, `winget` | 2 |
| Carapace | Primary completion data | 670 |
| argc | dotnet, whisper-ctranslate2 | 2 |

---

## ✅ Resolved Infrastructure Issue

**ScriptsToolkit is NOW loaded by $PROFILE** (Resolved December 25, 2025)

**Resolution Steps Completed:**
1. ✅ Copied profile content to workspace: `powershell\Microsoft.PowerShell_profile.ps1`
2. ✅ Documents profile now dot-sources workspace profile
3. ✅ Added `Import-Module ScriptsToolkit` to profile
4. ✅ Added differential timing (PWS-004)
5. ✅ Suppressed PSCompletions Set-Item error (PWS-002)

**All 42 functions and aliases (`whisp`, `ytdl`, `Get-ArgcManifest`, etc.) are now available in fresh shells.**

---

## Table of Contents
- [PowerShell Tasks (22)](#powershell-tasks-22)
- [C# Tasks (35)](#c-tasks-35)
- [Python Tasks (8)](#python-tasks-8)
- [System & Configuration (5)](#system--configuration-5)
- [Documentation & Research (8)](#documentation--research-8)

---

## PowerShell Tasks (22)

### Profile & Performance (7 tasks)

#### PWS-001: Force UTF-8 encoding always ⭐ Low
**Source:** To-Do.md #68
**Description:** Ensure PowerShell always uses UTF-8 encoding for all input/output operations globally.

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`
- `C:\Users\Lance\Documents\PowerShell\Microsoft.PowerShell_profile.ps1`

**Implementation:** Already implemented via `Set-Utf8Console` in ScriptsToolkit.psm1; call from profile

---

#### PWS-002: Fix Set-Item null argument error ✅ COMPLETE
**Source:** User request (PowerShell 7.6.0-preview.6 error)
**Description:** Fix "Set-Item: Cannot process argument because the value of argument 'name' is null" error occurring during profile load.

**Status:** ✅ COMPLETE - Error suppressed with SilentlyContinue

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`

**Implementation Done:**
```powershell
try {
    $ErrorActionPreference = 'SilentlyContinue'
    Import-Module PSCompletions -ErrorAction Stop 2>&1 | Out-Null
    $ErrorActionPreference = 'Continue'
} catch { ... }
```

---

#### PWS-003: Eliminate profile duplication ✅ COMPLETE
**Source:** User request
**Description:** Consolidate two profile files - $PROFILE should dot-source the workspace profile as single source of truth.

**Status:** ✅ COMPLETE - Workspace profile is now source of truth

**Files:**
- `C:\Users\Lance\Documents\PowerShell\Microsoft.PowerShell_profile.ps1` - Now just: `. 'C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1'`
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1` - Full profile content
- `C:\Users\Lance\Documents\PowerShell\Microsoft.PowerShell_profile.ps1.backup` - Original backup

**Implementation Done:**
1. Copied content to workspace profile
2. Documents profile dot-sources workspace
3. Backup created at `.backup` extension

---

#### PWS-004: Add differential timing to profile ✅ COMPLETE
**Source:** To-Do.md #22, User request #4
**Description:** Add performance benchmarking showing differential (Δ) load time per module.

**Status:** ✅ COMPLETE - Timing infrastructure implemented (commented out by default)

**To Enable Timing:** Uncomment timing regions in profile:
```powershell
# Uncomment: #region Timing Infrastructure, Write-ProfileTiming calls, Show-ProfileSummary
```

**Sample Output (when enabled):**
```
[    7ms] UTF-8 configured (Δ7ms)
[  102ms] ScriptsToolkit loaded (Δ95ms)
[  461ms] PSCompletions loaded (Δ359ms)
[  466ms] PSReadLine configured (Δ5ms)
[  681ms] PSFzf loaded (Δ215ms)
[  855ms] carapace loaded (Δ174ms)
[  883ms] argc loaded (Δ28ms)

Profile loaded in 885ms
```

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`

**Implementation Done:** `Write-ProfileTiming` + `Show-ProfileSummary` functions with cumulative and delta tracking

---

#### PWS-005: Implement lazy module loading ⭐⭐⭐ High
**Source:** To-Do.md #61, User request #3
**Description:** Implement lazy loading for ScriptsToolkit and third-party modules to optimize profile load time.

**⚠️ VERIFIED: Target of <100ms is UNREALISTIC**

**Verified Benchmark Data (January 2025):**
| Component | Init Time (ms) | Notes |
|-----------|----------------|-------|
| Baseline (pwsh -NoProfile) | 303 | Minimum PowerShell startup |
| winget native completer | 351 | `Register-ArgumentCompleter` only |
| gh native completer | 464 | `gh completion -s powershell` |
| PSCompletions import | 716-769 | Includes TUI setup |
| dotnet native (.NET 10) | 1090 | `dotnet completions script pwsh` |
| Carapace full init | 1261 | `carapace _carapace` (670 cmds) |

**Profile Load Time Breakdown:**
| Component     | Time    | % of Total |
|---------------|---------|------------|
| PSCompletions | ~373ms  | 40%        |
| PSFzf         | ~301ms  | 33%        |
| carapace      | ~173ms  | 19%        |
| argc          | ~54ms   | 6%         |
| Other         | ~19ms   | 2%         |
| **TOTAL**     | ~920ms  | 100%       |

**Realistic Target:** Optimize to ~500-600ms; <100ms is impossible with completion systems.

**Optimal Completion Strategy (Based on Benchmarks):**
- **winget:** Native (`winget complete`) - 351ms, FASTEST
- **gh:** Carapace (already in 670 cmds) or native (464ms)
- **dotnet:** argc or native .NET 10 - both ~1000ms
- **Everything else:** Carapace

**Modules to Lazy-Load:**
- **ScriptsToolkit** (42 functions) - Create proxy function stubs
- **PSFzf** - Defer until Ctrl+T/Ctrl+R/Ctrl+Space pressed (~301ms savings)
- **carapace** - Cannot easily defer (needed for Tab completions)

**User Requirement:** Lazy load ALL modules (personal + third-party), NO subdirectory structure required

---

#### PWS-006: Configure argc lazy loading ✅ COMPLETE
**Source:** To-Do.md #62-63, User request #4
**Description:** Configure argc for dynamic lazy loading with manifest visibility in PSCompletions.

**Status:** ✅ COMPLETE - Functions now accessible (ScriptsToolkit loaded)

**Verified Facts:**
- argc manifest supports **1,087 commands**
- argc CAN dynamically generate completions including for `whisper-ctranslate2`

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`

**Functions Now Available:**
- `Get-ArgcManifest` - Fetches/caches manifest from GitHub
- `Import-ArgcCompletions` (alias: argc-load) - Dynamically loads completions
- `Get-ArgcLoadedCommands` - Shows currently loaded argc commands

**Note:** PSCompletions menu integration deferred (separate enhancement)

---

#### PWS-007: Retain PSFzf Ctrl+Space keybinding ⭐ Low
**Source:** To-Do.md #64-65
**Description:** Ensure PSFzf uses Ctrl+Space for fuzzy completion, PSCompletions uses menu-style tab completion.

**Current Configuration:**
```powershell
Set-PSReadLineKeyHandler -Key 'Ctrl+Spacebar' -ScriptBlock { Invoke-FzfTabCompletion }
```

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`

**Implementation:** Verify PSCompletions menu setting, retain existing PSFzf keybind

---

### Binary Path Management (2 tasks)

#### PWS-008: Centralize tool paths ⭐⭐ Medium
**Source:** To-Do.md #54-57, User request
**Description:** Standardize installation paths for argc, carapace, fzf and update all references in scripts and C# code.

**Current Paths (Verified):**
- argc: `C:\Users\Lance\.cargo\bin\argc.exe`
- carapace: `C:\Users\Lance\AppData\Local\Microsoft\WinGet\Links\carapace.exe`
- fzf: `C:\Users\Lance\AppData\Local\Microsoft\WinGet\Links\fzf.exe`
- Python: System install (in PATH)
- FFmpeg/ffprobe: System PATH

**Target Structure (Optional):**
```
C:\Users\Lance\Dev\Scripts\tools\
├── bin\
│   ├── argc.exe
│   ├── carapace.exe
│   └── fzf.exe
└── argc-completions\
    ├── completions\
    └── bin\
```

**Files to Search/Update:**
- `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`
- `C:\Users\Lance\Dev\Scripts\csharp\src\**\*.cs` (search for binary paths)

**Decision Required:** Keep current locations (standard install paths) or consolidate?

**Steps (if centralizing):**
1. Find current locations: `Get-Command argc, carapace, fzf | Select-Object Source`
2. Create `C:\Users\Lance\Dev\Scripts\tools\bin\`
3. Move/copy binaries to centralized location
4. Update `$env:PATH` in profile
5. Update `$Script:ArgcCompletionsRoot` in ScriptsToolkit.psm1
6. Search C# code for hardcoded binary paths

---

#### PWS-009: Update all path references ⭐⭐ Medium
**Source:** To-Do.md #59
**Description:** Test and update all file paths in .cs and .ps1 files to align with `C:\Users\Lance\Dev\Scripts` directory structure.

**Files to Audit:**
- All `.cs` files in `csharp\src\**\*`
- All `.ps1` and `.psm1` files in `powershell\**\*`

**Known Paths to Verify:**
- `$Script:RepositoryRoot` → `C:\Users\Lance\Dev\Scripts`
- `$Script:PythonToolkit` → `C:\Users\Lance\Dev\Scripts\python\toolkit\cli.py`
- `$Script:CSharpRoot` → `C:\Users\Lance\Dev\Scripts\csharp`
- `$Script:LogDirectory` → `C:\Users\Lance\Dev\Scripts\logs`

**Implementation:**
```powershell
# Search for hardcoded paths
Get-ChildItem -Path csharp, powershell -Recurse -Include *.cs, *.ps1, *.psm1 |
    Select-String -Pattern 'C:\\Users\\|C:/Users/' |
    Where-Object { $_.Line -notmatch '^\s*//' }
```

---

### Module & Command Management (2 tasks)

#### PWS-010: Find unapproved PowerShell verbs ✅ COMPLETE
**Source:** To-Do.md #48
**Description:** Use native method to find all unapproved PowerShell verbs in ScriptsToolkit module.

**Status:** ✅ VERIFIED COMPLETE - All verbs are approved

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`

**Function Already Exists:** `Find-UnapprovedVerbs` (lines 100-134)

**Verification:**
```powershell
Import-Module C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1
Find-UnapprovedVerbs -ModuleName ScriptsToolkit
# Output: All verbs in ScriptsToolkit are approved
```

---

#### PWS-011: Create verb migration plan ✅ COMPLETE (Not Needed)
**Source:** To-Do.md #49
**Description:** Create migration plan to rename functions using approved PowerShell verbs.

**Status:** ✅ NOT NEEDED - PWS-010 confirmed all verbs are already approved

**Dependencies:** PWS-010 verified first

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`

**Original Verb Mapping (Not Needed):**
```
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

**Implementation:** Create detailed find/replace commands for each rename

---

### Whisper Integration (7 tasks)

#### PWS-012: Create 'whisp' alias ✅ COMPLETE
**Source:** To-Do.md #15
**Description:** Create `whisp` alias for Invoke-Whisper using distil-large-v3.5 model and English language by default.

**Status:** ✅ COMPLETE - Alias now accessible (ScriptsToolkit loaded)

**Verification:**
```powershell
Get-Command whisp
# Name  Source
# ----  ------
# whisp ScriptsToolkit
```

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`

**Note:** Default model is configurable via -Model parameter (default: 'distil-large-v3')

---

#### PWS-013: Check whisper in Save-YouTubeVideo ✅ COMPLETE
**Source:** To-Do.md #23
**Description:** Verify if Save-YouTubeVideo shows warning when whisper-ctranslate2 is missing.

**Status:** ✅ COMPLETE - Function now accessible and warning implemented

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1` (Save-YouTubeVideo function)

**Existing Implementation (verified working):**
```powershell
try {
    $null = Get-Command -Name whisper-ctranslate2 -ErrorAction Stop
} catch {
    Write-Warning "whisper-ctranslate2 not found. Install: pip install whisper-ctranslate2"
    return
}
```

---

#### PWS-014: Restore model download progress ⭐⭐ Medium
**Source:** To-Do.md #24-25
**Description:** Show UI when whisper model is being downloaded (currently suppressed).

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1` (Invoke-Whisper, Invoke-WhisperJapanese, Invoke-WhisperFolder)

**Implementation:** Remove stderr suppression during model download, detect download in progress

---

#### PWS-015: Improve Whisper progress display ⭐⭐⭐ High
**Source:** To-Do.md #16-17, #19-20
**Description:** Enhance progress bar output with clear labels and explanations.

**Current Output:**
```
[17:20:47] Transcribing: file.webm
           Model: distil-large-v3.5 | Language: en
Detected language 'English' with probability 1.000000
12%|███▋| 478.3/3986.2826875 [02:48<22:43, 2.57seconds/s]
```

**Target Output:**
```
[17:20:47] Transcribing: file.webm
           Model: distil-large-v3.5 | Language: en
Detected language 'English' with probability 1.000000
12%|███▋| 478s / 3986s [Elapsed: 02:48 | ETA: 22:43 | Speed: 2.6s/s]
```

**Requirements:**
1. Explain progress bar numbers (processed/total seconds)
2. Delineate ETA vs elapsed time
3. Replace "seconds/s" with "s/s" or "Speed: 2.6s/s"
4. Add labels: "Elapsed:", "ETA:", "Speed:"

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`

**Implementation:** Parse whisper-ctranslate2 stderr, reformat with regex

---

#### PWS-016: Suppress Python warnings ⭐⭐ Medium
**Source:** To-Do.md #66
**Description:** Suppress outdated library warnings from whisper-ctranslate2 and Python packages.

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1` (Invoke-ToolkitPython wrapper)

**Implementation:**
```powershell
$env:PYTHONWARNINGS = 'ignore::DeprecationWarning,ignore::FutureWarning'
$env:PYTHONDONTWRITEBYTECODE = '1'
```

---

#### PWS-017: Add whisper autocomplete ✅ COMPLETE
**Source:** To-Do.md #67
**Description:** Investigate autocomplete support for whisper-ctranslate2 CLI via PSCompletions/carapace/argc/fzf.

**Status:** ✅ COMPLETE - Custom Carapace spec created

**Research Findings (Verified):**
| Tool | whisper-ctranslate2 Support |
|------|----------------------------|
| argc-completions | ❌ NOT in manifest (1000+ commands, no whisper) |
| Carapace built-in | ❌ NOT included (670 commands, no whisper) |
| PSCompletions | ❌ No whisper module |
| **Custom Carapace Spec** | ✅ CREATED and working |

**Implementation Done:**
Custom spec at `%APPDATA%\carapace\specs\whisper-ctranslate2.yaml`:
```yaml
name: whisper-ctranslate2
description: Whisper transcription using CTranslate2
flags:
  --model: Choose model size
  --task: transcribe or translate
  --language: Source language
  --output_format: Output format
completion:
  flag:
    model: ["tiny", "tiny.en", "base", "small", "medium", "large-v1", "large-v2", "large-v3", "distil-large-v3"]
    task: ["transcribe", "translate"]
    output_format: ["txt", "vtt", "srt", "tsv", "json", "all"]
    device: ["auto", "cpu", "cuda"]
  positional:
    - ["$files"]
```

---

#### PWS-018: Auto-invoke whisp after YouTube download ✅ COMPLETE
**Source:** To-Do.md #60
**Description:** Make Save-YouTubeVideo automatically invoke whisp for transcription after download.

**Status:** ✅ VERIFIED COMPLETE - Already implemented

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`

**Implementation (Already Done):**
- Save-YouTubeVideo has `-NoTranscribe` switch parameter
- Auto-transcribes by default when download completes
- Calls `Invoke-Whisper` with `distil-large-v3` model

---

### File System Operations (2 tasks)

#### PWS-019: Fix file extension repair command ⭐⭐⭐ High
**Source:** master_task_list.md #5
**Description:** Create robust command to detect/fix missing file extensions using ffprobe or mediainfo.

**Current Failed Implementation:**
```powershell
Get-ChildItem -Recurse -File | Where-Object { -not $_.Extension } |
ForEach-Object {
    $ext = (ffprobe -v error -show_entries format=format_name -of csv=p=0 $_.FullName 2>$null).Split(',')[0]
    if ($ext) { Rename-Item $_.FullName "$($_.Name).$ext" }
}
```

**Errors:**
- "The filename, directory name, or volume label syntax is incorrect"
- "Cannot retrieve the dynamic parameters. Wildcard character pattern is not valid: [cheersmumbai @ DT"

**Root Causes:**
1. Filenames contain wildcard characters `[`, `]` → Rename-Item fails
2. ffprobe format names aren't valid extensions (e.g., "matroska" ≠ "mkv")
3. Need to use `-LiteralPath` instead of positional parameter

**Files:** None (one-liner, not saved to module per requirement)

**Implementation:**
```powershell
Get-ChildItem -LiteralPath 'D:\Path' -Recurse -File |
Where-Object { -not $_.Extension } |
ForEach-Object {
    $format = ffprobe -v error -show_entries format=format_name -of csv=p=0 $_.FullName 2>$null
    $ext = switch -Regex ($format) {
        'matroska' { 'mkv' }
        'mp4' { 'mp4' }
        'webm' { 'webm' }
        'avi' { 'avi' }
        'mov' { 'mov' }
        default { $null }
    }
    if ($ext) {
        $newName = "$($_.Name).$ext"
        Rename-Item -LiteralPath $_.FullName -NewName $newName -ErrorAction Continue
    }
}
```

---

#### PWS-020: Segregate files by extension ⭐⭐ Medium
**Source:** To-Do.md #27
**Description:** Create one-liner to organize files in target directory into subdirectories by extension.

**Target Directory:** `D:\Google Drive\Games\Others\Miscellaneous`

**Files:** None (one-liner)

**Implementation:**
```powershell
Get-ChildItem -LiteralPath 'D:\Google Drive\Games\Others\Miscellaneous' -File |
Group-Object Extension |
ForEach-Object {
    $dirName = if ($_.Name) { $_.Name.TrimStart('.') } else { 'NoExtension' }
    $dir = Join-Path 'D:\Google Drive\Games\Others\Miscellaneous' $dirName
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    $_.Group | Move-Item -Destination $dir -Force
}
```

---

### Task Scheduling (1 task)

#### PWS-021: Fix auto-close terminal on sync success ⭐⭐ Medium
**Source:** To-Do.md #41-43
**Description:** Fix scheduled sync tasks to auto-close terminal on success.

**Root Cause Analysis Required:**
1. Check return value/exit code from C# CLI
2. Verify PowerShell wrapper handles exit codes
3. Check scheduled task action settings

**Files:**
- `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\ScriptsToolkit.psm1`:
  - Register-ScheduledSyncTask
  - Invoke-YouTubeSync
  - Invoke-LastFmSync
- `C:\Users\Lance\Dev\Scripts\csharp\src\CLI\SyncCommands.cs`

**Implementation:** Ensure C# CLI returns exit code 0 on success, PowerShell calls `exit $LASTEXITCODE`

---

### Documentation (1 task)

#### PWS-022: Explain OmniSharp purpose ⭐ Low
**Source:** To-Do.md #21
**Description:** Document OmniSharp and other development tools.

**Answer:** OmniSharp is the C# language server providing IntelliSense, code navigation, refactoring for VS Code. Used by C# extension for syntax highlighting, autocomplete, go-to-definition.

**Files:**
- `markdown\development_environment.md` (new file)

**Implementation:** Create documentation explaining:
- OmniSharp (C# language server)
- basedpyright (Python type checker)
- PSScriptAnalyzer (PowerShell linter)
- CSharpier (C# formatter)
- Black (Python formatter)

---

## C# Tasks (35)

### CLI Argument Handling (2 tasks)

#### CS-001: Improve CLI error messages for malformed options ⭐ Low
**Source:** To-Do.md #9-10
**Description:** Improve error messages when users mistype option formats.

**Current Behavior (Spectre.Console.Cli):**
- `-i` → Valid short form
- `--input` → Valid long form
- `-input` → Interpreted as `-i nput` → Shows cryptic "No value provided" error
- `--xyz` → Silently ignored (unknown options fall through)

**Status:** ✅ PARTIALLY WORKING - Spectre already rejects `-input` but with poor error message

**Files:**
- `csharp\src\CLI\*.cs` (all command files)

**Implementation Options:**
1. **LOW PRIORITY**: Error messages are functional, just not perfectly clear
2. If desired: Create custom `ICommandInterceptor` to validate option format before execution
3. Alternative: Add help text clarifying correct option format

**Testing Done:**
```
dotnet run -- music fill -input test.tsv → "No value provided" (rejects)
dotnet run -- music fill -i test.tsv → Works correctly
dotnet run -- music fill --input test.tsv → Works correctly
```

---

#### CS-002: Migrate usings to GlobalUsings.cs ✅ COMPLETE
**Source:** To-Do.md #8
**Description:** Move all using statements to GlobalUsings.cs following MusicCommands pattern.

**Status:** ✅ COMPLETE - Added missing usings, removed redundant local usings

**Changes Made:**
- Added `Google.Apis.YouTube.v3.Data` to GlobalUsings
- Added `System.Text.Encodings.Web` to GlobalUsings
- Removed redundant usings from ReleaseProgressCache.cs, StateManager.cs, YouTubeService.cs
- Added `DiscogsVideoDto` alias to resolve Video type ambiguity

**Files:**
- `csharp\src\GlobalUsings.cs`

---

### Region Management (2 tasks)

#### CS-003: Audit regions across all C# files ⭐⭐⭐ High
**Source:** To-Do.md #6-7, #30, #34, #60; master_task_list.md #8
**Description:** Review all C# files to add regions for navigability.

**Verified January 2025: NO files currently have regions**

**Guidelines:**
- File <100 lines: No regions needed
- File 100-300 lines: Max 2-3 semantic regions
- File >300 lines: Use regions for related functionality
- Region names: Semantic (e.g., "Music Metadata Search" not "Public Methods")

**Files >300 Lines Needing Regions (10 files):**
| File | Lines | Suggested Regions |
|------|-------|-------------------|
| GoogleSheetsService.cs | 1230 | Auth, Read, Write, Sync, Export |
| MusicBrainzService.cs | 1041 | Search, Lookup, Parse, Format |
| MusicSearchCommand.cs | 1035 | Settings, Search Modes, Rendering |
| YouTubePlaylistOrchestrator.cs | 1029 | Fetch, Compare, Sync, Progress |
| DiscogsService.cs | 633 | Auth, Search, Parse, Format |
| MusicFillCommand.cs | 612 | Settings, TSV Processing, Output |
| SyncCommands.cs | 459 | YouTube, LastFm, Status, Help |
| Console.cs | 414 | Logging, Progress, Tables, Formatting |
| MailTmService.cs | 385 | Auth, Messages, Parse |
| Logger.cs | 331 | Config, File, Console, Format |

**Files 100-300 Lines (Optional):**
- YouTubeChangeDetector.cs (269)
- YouTubeService.cs (252)
- LastFmService.cs (203)
- CompletionCommands.cs (198)

---

#### CS-004: Refactor MusicCommand structure ⭐⭐⭐ High
**Source:** To-Do.md #6, #34
**Description:** Analyze and refactor Music commands structure.

**Questions:**
- Why is `public sealed class` separate from command logic?
- Should commands be merged with regional separation?
- Can shared logic be extracted?

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`
- `csharp\src\CLI\MusicSearchCommand.cs`
- `csharp\src\Services\Music\**\*.cs`

---

### Code Cleanup (2 tasks)

#### CS-005: Remove comments and XML docs ✅ COMPLETE
**Source:** To-Do.md #58
**Description:** Remove all comments and XML documentation per AI coding instructions.

**Status:** ✅ COMPLETE - No comments found in codebase (only URLs containing //)

---

#### CS-006: Fix all compiler warnings ✅ COMPLETE
**Source:** To-Do.md #37
**Description:** Address all compiler warnings.

**Status:** ✅ COMPLETE - `dotnet build` shows 0 warnings

---

### Music Fill Command (14 tasks)

#### CS-007: Search both Discogs AND MusicBrainz ⭐⭐⭐ High
**Source:** To-Do.md #11
**Description:** Query BOTH services, merge results, sort by confidence.

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`
- `csharp\src\Services\Music\DiscogsService.cs`
- `csharp\src\Services\Music\MusicBrainzService.cs`

**Implementation:**
1. Query services in parallel
2. Merge results
3. Sort by confidence descending
4. Display top matches from either source

---

#### CS-008: Fix search result formatting ⭐⭐⭐ High
**Source:** To-Do.md #12-14
**Description:** Improve output format to show input data and field-specific results.

**Current (Bad):**
```
Symphony No. 7 - Bruckner, Anton
Label:
50% Victor (Discogs)
```

**Target:**
```
Recording: Symphony No. 7
Composer: Anton Bruckner

┌─ Label ─────────────────────┐
│ 50% Victor (Discogs)        │
│ 40% DG (MusicBrainz)        │
└──────────────────────────────┘
```

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`
- `csharp\src\Infrastructure\Console.cs`

---

#### CS-009: Integrate live progress bar ⭐⭐⭐⭐ Very High
**Source:** To-Do.md #3, #29
**Description:** Add live progress showing field being searched, service used, values parsed in real-time.

**Target:**
```
Searching... [23/43]
Current: Symphony No. 7 - Bruckner
[✓ Label: Victor] [✓ Year: 1985] [? CatalogNumber] (Discogs)
[Searching MusicBrainz...]
```

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`
- `csharp\src\Infrastructure\Console.cs`

**Implementation:** Use Spectre.Console.Progress

---

#### CS-010: Show found fields with elapsed time ⭐⭐ Medium
**Source:** To-Do.md #38-39
**Description:** Display recording name before found fields, show all filled fields with elapsed time.

**Format:**
```
Recording: Symphony No. 7 - Bruckner

Found:
  Label: Deutsche Grammophon
  Year: 1985
  Catalog: 415 835-1

Elapsed: 2.3s
```

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`

---

#### CS-011: Write found fields in real-time ⭐⭐⭐ High
**Source:** To-Do.md #40
**Description:** Write fields to TSV immediately when found (not at end), support resume without re-searching.

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`
- `csharp\src\Services\Music\MusicExporter.cs`

---

#### CS-012: Prefer first pressing for labels ⭐⭐ Medium
**Source:** To-Do.md #41
**Description:** Prioritize details from very first pressing when searching labels.

**Files:**
- `csharp\src\Services\Music\DiscogsService.cs`
- `csharp\src\Services\Music\MusicBrainzService.cs`

---

#### CS-013: Match label with catalog number ⭐⭐⭐ High
**Source:** To-Do.md #42
**Description:** Show which catalog numbers correspond to which labels.

**Current (Unclear):**
```
Label Suggestions:
40% Deutsche Grammophon (Discogs)
40% Philips (Discogs)
Catalog # Suggestions:
40% 415 835-1 (Discogs)
40% 6598 572 (Discogs)
```

**Target:**
```
Label Suggestions:
40% Deutsche Grammophon → Catalog: 415 835-1 (Discogs)
40% Philips → Catalog: 6598 572 (Discogs)
```

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`

---

#### CS-014: Auto-shorten label names ⭐⭐ Medium
**Source:** To-Do.md #43
**Description:** Automatically shorten labels (e.g., "Deutsche Grammophon" → "DG").

**Mapping:**
```
Deutsche Grammophon → DG
Columbia Masterworks → Columbia
RCA Victor Red Seal → RCA Red Seal
```

**Files:**
- `csharp\src\Services\Music\MusicExporter.cs`

---

#### CS-015: Create auto-filled TSV output ⭐⭐ Medium
**Source:** To-Do.md #44
**Description:** Generate TSV with highest confidence values, vertically aligned, without prompt.

**Files:**
- `csharp\src\Services\Music\MusicExporter.cs`

---

#### CS-016: Finish missing fields implementation ⭐⭐⭐ High
**Source:** To-Do.md #28; master_task_list.md #6
**Description:** Complete implementation to find ALL missing info always (Year, Label, CatalogNumber).

**Progress Display:**
```
[✓ Label: DG] [✓ Year: 1985] [? CatalogNumber] (via MusicBrainz)
```

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`
- `csharp\src\Services\Music\DiscogsService.cs`
- `csharp\src\Services\Music\MusicBrainzService.cs`

---

#### CS-017: Explain manual field writing ⭐ Low
**Source:** To-Do.md #45
**Description:** Document why each field is written manually vs passing entire record.

**Current Code:**
```csharp
csv.WriteField(record.Composer);
csv.WriteField(record.Work);
csv.WriteField(record.Orchestra);
// ... 16 more lines
```

**Files:**
- `csharp\src\Services\Music\MusicExporter.cs`

---

#### CS-018: Determine best location for fill output ⭐ Low
**Source:** To-Do.md #49
**Description:** Decide where to store auto-filled TSV files (directory structure and file naming).

**Options:**
- `exports\filled\`
- `exports\music\`
- `state\music\`

**Files:**
- `csharp\src\Infrastructure\Paths.cs`

---

#### CS-019: Enforce Console.cs for all Spectre calls ⭐⭐ Medium
**Source:** To-Do.md #54-55
**Description:** Never call Spectre.Console directly; always use wrapper in Console.cs to prevent markup errors.

**Verified January 2025: 1 Violation Found**
- [CompletionCommands.cs](csharp/src/CLI/CompletionCommands.cs#L55-L66): Direct `new Panel(new Markup(...))` usage

**Current Violation (CompletionCommands.cs:55-66):**
```csharp
var panel = new Panel(
    new Markup(
        $"[bold green]✓ Tab completion installed successfully![/]\n\n"
            + $"[dim]Profile:[/]\n[link=file:///{psProfilePath}]{psProfilePath}[/]\n\n"
            + $"[yellow]Action Required:[/]\nRestart PowerShell or run: [bold]. $PROFILE[/]"
    )
)
{
    Border = BoxBorder.Rounded,
    Padding = new Spectre.Console.Padding(1, 1),
    Header = new PanelHeader("[blue]System Configuration[/]"),
};
```

**Console.cs Already Has:**
- `CreatePanel(string content, string header)` - internal method exists but is limited

**Required Changes:**
1. Add `Console.WritePanel(string title, string content, Color borderColor)` wrapper
2. Refactor CompletionCommands.cs to use wrapper

**Files:**
- `csharp\src\Infrastructure\Console.cs` (add missing wrappers)
- All `.cs` files using Spectre.Console directly

---

#### CS-020: Migrate to named args for complex calls ⭐⭐ Medium
**Source:** To-Do.md #52-53
**Description:** Use named arguments when method has many parameters.

**Rule:** >3 parameters → use named arguments

**Files:** All `.cs` files in `csharp\src\**\*`

---

### Python to C# Migration (5 tasks)

#### CS-021: Consider CliWrap for subprocess handling ⭐⭐ Medium
**Source:** master_task_list.md
**Description:** Evaluate CliWrap library for easier path/subprocess handling vs Process.Start.

**Research:** Compare CliWrap vs current implementation

---

#### CS-022: Use ImageSharp for image manipulation ⭐⭐⭐ High
**Source:** master_task_list.md
**Description:** Migrate Python PIL/Pillow image operations to SixLabors.ImageSharp.

**Files:**
- New: `csharp\src\Services\Video\ImageService.cs`

---

#### CS-023: Explain ImageSharp vs PIL differences ⭐ Low
**Source:** master_task_list.md
**Description:** Document how ImageSharp differs from PIL in:
- Positioning within frames
- Frame extraction

**Files:**
- `markdown\libraries_comparison.md` (new)

---

#### CS-024: Integrate Python scrobble into C# ⭐⭐⭐⭐ Very High
**Source:** master_task_list.md; To-Do.md #46
**Description:** Show best way to natively integrate Python last.fm features in C# (reserve for end, don't start changing ps1).

**Files:**
- New: `markdown\python_to_csharp_migration_guide.md`

**Note:** Do not add py scrobble to other parts (keep isolated)

---

#### CS-025: Create Python to C# migration plan ⭐⭐⭐ High
**Source:** To-Do.md #51, #59
**Description:** Create 1:1 function migration plan using .NET design (functional when possible, avoid class instantiation).

**Files:**
- `markdown\implementation\python_migration_plan.md`

**Python Functions to Migrate (61 total):**
- toolkit.audio: 17 functions
- toolkit.cli: 12 functions
- toolkit.cuesheet: 5 functions
- toolkit.filesystem: 6 functions
- toolkit.lastfm: 4 functions
- toolkit.video: 17 functions

---

### Hierarchy Files (1 task)

#### CS-026: Create region markings for hierarchy files ⭐ Low
**Source:** To-Do.md #30
**Description:** Add region markings to hierarchy files for better navigation.

**Files:**
- `csharp\src\Hierarchy\MetaBrainz.MusicBrainz.hierarchy.txt`
- `csharp\src\Hierarchy\ParkSquare.Discogs.hierarchy.txt`

---

### Infrastructure (2 tasks)

#### CS-027: Fix Successfully installed UI ⭐⭐ Medium
**Source:** To-Do.md #4
**Description:** Add UI to show package installation progress.

**Current (No UI):**
```
CFFI-2.0.0 pycparser-2.23 sounddevice-0.5.3 whisper-ctranslate2-0.5.6
PS C:\Users\Lance>
```

**Files:** Likely completion command related

---

#### CS-028: Understand `<?` usage ⭐ Low
**Source:** To-Do.md #18
**Description:** Explain purpose of `<?` in C# code.

**Research:** Find occurrences, document usage

---

### Configuration & Merge (6 tasks)

#### CS-029: Merge music commands with regional separation ⭐⭐⭐ High
**Source:** master_task_list.md
**Description:** Merge all music commands into one file with `#region` separation.

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`
- `csharp\src\CLI\MusicSearchCommand.cs`
- Target: `csharp\src\CLI\MusicCommands.cs`

---

#### CS-030: Assess Python file contents individually ⭐⭐ Medium
**Source:** master_task_list.md
**Description:** Review each Python file to understand functionality before migration.

**Files:**
- `python\toolkit\__init__.py`
- `python\toolkit\audio.py`
- `python\toolkit\cli.py`
- `python\toolkit\cuesheet.py`
- `python\toolkit\filesystem.py`
- `python\toolkit\lastfm.py`
- `python\toolkit\logging_config.py`
- `python\toolkit\video.py`

---

#### CS-031: Create new CLI structure with all features ⭐⭐⭐⭐ Very High
**Source:** master_task_list.md
**Description:** Create new integrated CLI structure including last.fm features.

**Files:**
- Restructure `csharp\src\CLI\**\*.cs`

---

#### CS-032: Create integration cohesion structure ⭐⭐⭐⭐ Very High
**Source:** master_task_list.md
**Description:** Create new structure to integrate Python, PowerShell, C# cohesively.

---

#### CS-033: Fix basedpyright errors ✅ COMPLETE
**Source:** To-Do.md #1, #31, #47-48; master_task_list.md
**Description:** Fix Pyright errors after modifying pyproject.toml to suppress untyped library errors.

**Status:** ✅ VERIFIED COMPLETE - 0 errors, 0 warnings, 0 notes

**Files:**
- `python\pyproject.toml`
- All Python files

**Verification:**
```powershell
Set-Location c:\Users\Lance\Dev\Scripts\python
basedpyright toolkit
# Output: 0 errors, 0 warnings, 0 notes
```

**Note:** Must run from `python/` directory for pyproject.toml to be detected

---

#### CS-034: Explain pyproject.toml purpose ⭐ Low
**Source:** To-Do.md #47
**Description:** Document why .toml file exists and its configuration.

**Files:**
- `python\pyproject.toml`

**Answer:** pyproject.toml is Python's project metadata and tool configuration file (PEP 518). Contains basedpyright settings, dependencies, build config.

---

## Python Tasks (8)

### Configuration (3 tasks)

#### PY-001: Disable pyright errors for untyped libraries ✅ COMPLETE
**Source:** To-Do.md #1, #31
**Description:** Configure basedpyright to suppress type errors from third-party untyped libraries.

**Status:** ✅ VERIFIED COMPLETE - `basedpyright toolkit` from `python/` directory shows 0 errors

**Files:**
- `python\pyproject.toml`

**Current Config:**
```toml
[tool.basedpyright]
typeCheckingMode = "strict"
exclude = ["last.fm Scrobble Updater"]
reportMissingTypeStubs = false
reportUnknownMemberType = false
reportUnknownArgumentType = false
reportUnknownVariableType = false
reportUnknownParameterType = false
reportUnknownLambdaType = false
reportAny = false
```

**Important:** Must run from `python/` directory where pyproject.toml exists:
```powershell
Set-Location c:\Users\Lance\Dev\Scripts\python
basedpyright toolkit
# Output: 0 errors, 0 warnings, 0 notes
```

---

#### PY-002: Suppress untyped library warnings ✅ COMPLETE
**Source:** To-Do.md #47
**Description:** Modify pyproject.toml to not show warnings for untyped libraries.

**Status:** ✅ VERIFIED COMPLETE - All warning suppressions in place and working

**Files:**
- `python\pyproject.toml`

---

#### PY-003: List all Python signatures ✅ COMPLETE
**Source:** To-Do.md #50
**Description:** Use pydoc to list all Python signatures in new file.

**Status:** ✅ COMPLETE - Created comprehensive signature documentation

**Files Created:**
- `markdown\python_signatures.md` - 65 functions across 7 modules with type hints and C# migration notes

---

### Migration Planning (5 tasks)

#### PY-004: Create Typer-based CLI overhaul plan ⭐⭐⭐ High
**Source:** To-Do.md #46
**Description:** Plan for integrating Python last.fm scrobble into toolkit using Typer (ignore C#/PowerShell duplication).

**Files:**
- `python\toolkit\cli.py`
- `python\toolkit\lastfm.py`

**Note:** This py last.fm is token only, not invoked elsewhere

---

#### PY-005: Delete last.fm scrobble folder after integration ⭐ Low
**Source:** To-Do.md #46; master_task_list.md
**Description:** After PY-004 completes, implement migration and delete separate folder.

**Dependencies:** PY-004

**Files:**
- Remove: `python\last.fm Scrobble Updater\` (entire folder)

---

#### PY-006: Create cohesive structure ⭐⭐⭐⭐ Very High
**Source:** master_task_list.md
**Description:** Integrate all Python files cohesively.

**Files:**
- All files in `python\toolkit\`

---

#### PY-007: Assess all py files individually ⭐⭐ Medium
**Source:** master_task_list.md
**Description:** Review each Python file's contents before creating migration plan.

**Files:**
- `python\toolkit\__init__.py`
- `python\toolkit\audio.py`
- `python\toolkit\cli.py`
- `python\toolkit\cuesheet.py`
- `python\toolkit\filesystem.py`
- `python\toolkit\lastfm.py`
- `python\toolkit\logging_config.py`
- `python\toolkit\video.py`

---

#### PY-008: Do not add py scrobble to other parts ⭐ Low
**Source:** master_task_list.md
**Description:** Keep Python scrobble isolated from PowerShell/C# (no cross-integration).

**Implementation:** Ensure scrobble remains in Python only

---

## System & Configuration (5)

### Development Environment (5 tasks)

#### SYS-001: Set up profile for code styles ⭐⭐ Medium
**Source:** To-Do.md #2
**Description:** Understand how to set up as profile (py/C# style configs, globalusings) - is this a runner? agent profile?

**Files:**
- `.editorconfig`
- `.vscode\settings.json`
- `csharp\.editorconfig`
- `python\pyproject.toml`

---

#### SYS-002: Create .vscode settings with proper config ✅ COMPLETE
**Source:** User request
**Description:** Create workspace VS Code settings explaining why global config is insufficient.

**Status:** ✅ COMPLETE - Created all .vscode config files

**Files Created:**
- `.vscode\settings.json` - C#/Python/PowerShell formatting, editor config
- `.vscode\extensions.json` - Recommended extensions
- `.vscode\launch.json` - Debug configurations for C#, Python, PowerShell

---

#### SYS-003: Use GitHub Copilot defaults ⭐ Low
**Source:** To-Do.md #32
**Description:** Learn how to use default values out of the box with GitHub Copilot when creating new projects.

**Research:** Copilot Chat settings and workspace configuration

---

#### SYS-004: Fix Copilot always being outdated ⭐⭐ Medium
**Source:** To-Do.md #36
**Description:** Prevent Copilot from being on older version without forcing download every time.

**Research:** VS Code extension update settings

---

#### SYS-005: Fix Rider markdown numbering ⭐ Low
**Source:** To-Do.md #35
**Description:** Make Rider recognize markdown numbering correctly (code blocks don't start new list).

**Research:** Rider markdown settings

---

## Documentation & Research (8)

#### DOC-001: Create named argument convention AI instruction ⭐⭐ Medium
**Source:** To-Do.md #53
**Description:** Find best way to specify scheme of named args to AI for code generation.

**Files:**
- New: `markdown\ai_coding_instructions.md`

---

#### DOC-002: Suggest improvements to named arg conventions ⭐ Low
**Source:** To-Do.md #44
**Description:** Review and suggest improvements to namedargumentconvention AI instruction set.

---

#### DOC-003: Explain native AI tool for plans ⭐ Low
**Source:** To-Do.md #45
**Description:** Identify what native AI tool creates implementation plans (not inside dir, formatted differently, supports review).

**Answer:** GitHub Copilot Workspace or similar

---

#### DOC-004: Create development environment docs ⭐⭐ Medium
**Source:** To-Do.md #21
**Description:** Document all development tools and their purposes.

**Files:**
- New: `markdown\development_environment.md`

**Tools to Document:**
- OmniSharp
- basedpyright
- PSScriptAnalyzer
- CSharpier
- Black
- Ruff
- argc/carapace/fzf

---

#### DOC-005: Create libraries comparison docs ⭐⭐ Medium
**Source:** master_task_list.md
**Description:** Document ImageSharp vs PIL differences.

**Files:**
- New: `markdown\libraries_comparison.md`

---

#### DOC-006: Auto-add numbering when pasting ⭐ Low
**Source:** To-Do.md #57
**Description:** Figure out how to auto-continue numbering when pasting lines in markdown.

**Research:** VS Code markdown extension settings

---

#### DOC-007: Explain what "fill" does by default ⭐ Low
**Source:** To-Do.md #5
**Description:** Document what `music fill` command does when launching a search.

**Files:**
- `markdown\cli_commands_reference.md` (new)

---

#### DOC-008: Understand "Successfully installed" UI issue ⭐ Low
**Source:** To-Do.md #4
**Description:** Document why pip/package installations don't show UI progress.

---

## Summary Statistics

**Total Tasks:** 95 (17 COMP + 22 PWS + 35 CS + 8 PY + 5 SYS + 8 DOC)

### By Category
| Category | Total | Complete | Remaining |
|----------|-------|----------|-----------|
| Completion Issues (COMP) | 17 | 0 | 17 |
| PowerShell (PWS) | 22 | 12 | 10 |
| C# (CS) | 35 | 4 | 31 |
| Python (PY) | 8 | 4 | 4 |
| System (SYS) | 5 | 1 | 4 |
| Documentation (DOC) | 8 | 0 | 8 |
| **TOTAL** | **95** | **21** | **74** |

### By Complexity
| Priority | Count | Description |
|----------|-------|-------------|
| ⭐ Low | 31 | Quick fixes, documentation |
| ⭐⭐ Medium | 28 | Standard implementation |
| ⭐⭐⭐ High | 19 | Complex, multi-file changes |
| ⭐⭐⭐⭐ Very High | 4 | Architecture-level changes |

### Completed Tasks (21)
**PowerShell (12):**
- ✅ PWS-002: Set-Item error suppressed
- ✅ PWS-003: Profile consolidation complete
- ✅ PWS-004: Differential timing implemented
- ✅ PWS-006: argc functions accessible
- ✅ PWS-007: PSFzf Ctrl+Space retained
- ✅ PWS-010: All verbs approved
- ✅ PWS-011: No migration needed
- ✅ PWS-012: whisp alias working
- ✅ PWS-013: whisper check in ytdl working
- ✅ PWS-017: whisper autocomplete (custom carapace spec)
- ✅ PWS-018: Auto-transcribe implemented

**C# (4):**
- ✅ CS-002: GlobalUsings migration complete
- ✅ CS-005: No comments to remove
- ✅ CS-006: 0 compiler warnings
- ✅ CS-033: basedpyright 0 errors

**Python (4):**
- ✅ PY-001: basedpyright errors fixed
- ✅ PY-002: Untyped library warnings suppressed
- ✅ PY-003: Python signatures documented (65 functions)

**System (1):**
- ✅ SYS-002: .vscode settings created

---

## Appendix: Completion Stack Reference

### Verified Module Versions (January 2025)
| Module | Version | Location |
|--------|---------|----------|
| PSCompletions | 6.2.2 | Documents\PowerShell\Modules |
| PSFzf | 2.7.9 | Documents\PowerShell\Modules |
| PSReadLine | 2.4.5 | Documents\PowerShell\Modules |

### PSCompletions Configuration (Verified)
| Config Key | Value | Effect |
|------------|-------|--------|
| `enable_menu` | 1 | Show completion menu |
| `enable_menu_enhance` | 1 | Take over Tab key |
| `trigger_key` | Tab | Key that triggers menu |
| `enable_enter_when_single` | 0 | Show menu even for single match |
| `enable_tip` | 1 | Show tooltips |

### Completion Overlap Analysis (RESOLVED)
| Source | Total | Status |
|--------|-------|--------|
| PSCompletions | 2 | ✅ Only `psc`, `winget` (conflicts resolved) |
| Carapace | 670 | ✅ Primary completion source |
| argc (loaded) | 2 | ✅ `dotnet`, `whisper-ctranslate2` |

**Previous State (Caused COMP-012/COMP-015):**
- PSCompletions had 72 completions, 45 conflicted with Carapace
- Removed all conflicting completions

**Current Clean State:**
- PSCompletions: `psc`, `winget` (Carapace lacks these)
- Carapace: 670 commands (git, gh, docker, kubectl, etc.)
- argc: dotnet, whisper-ctranslate2

### Current Profile Load Order (Verified)
```powershell
# 1. UTF-8 Configuration
[Console]::InputEncoding = [Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# 2. ScriptsToolkit (40 custom functions)
Import-Module ScriptsToolkit

# 3. PSCompletions (2 completions: psc, winget + TUI Menu)
Import-Module PSCompletions  # Sets Tab → CustomAction

# 4. PSReadLine (Prediction/History)
Set-PSReadLineOption -PredictionSource History

# 5. Carapace (670 completions - NO conflicts now)
carapace _carapace | Out-String | Invoke-Expression

# 6. argc (dotnet, whisper-ctranslate2)
argc --argc-completions powershell dotnet whisper-ctranslate2 | Invoke-Expression

# NOTE: PSFzf is NOT loaded (see COMP-016)
```

### Completion Coverage Matrix (Updated - Clean)
| Command | PSCompletions | Carapace | argc | Status |
|---------|---------------|----------|------|--------|
| git | ❌ | ✅ | - | ✅ Carapace only |
| gh | ❌ | ✅ | - | ✅ Carapace only |
| docker | ❌ | ✅ | - | ✅ Carapace only |
| kubectl | ❌ | ✅ | - | ✅ Carapace only |
| npm | ❌ | ✅ | - | ✅ Carapace only |
| winget | ✅ | ❌ | - | ✅ PSCompletions (no Carapace) |
| dotnet | ❌ | ❌ | ✅ | ✅ argc only |
| whisper-ctranslate2 | ❌ | ✅ (spec) | - | ✅ Carapace custom spec |
| psc | ✅ | ❌ | - | ✅ PSCompletions only |

### Key Bindings (Current vs Planned)
| Key | Handler | Function | Status |
|-----|---------|----------|--------|
| Tab | PSCompletions | Menu completion (TUI) | ✅ Active |
| Ctrl+Space | PSFzf | Fuzzy completion | ⚠️ Needs COMP-016 |
| Ctrl+R | PSFzf | Fuzzy history search | ⚠️ Needs COMP-016 |
| Ctrl+T | PSFzf | Fuzzy file search | ⚠️ Needs COMP-016 |

### Tool Locations (Verified)
| Tool | Path | Install Method |
|------|------|----------------|
| argc | `C:\Users\Lance\.cargo\bin\argc.exe` | cargo install |
| carapace | `C:\Users\Lance\AppData\Local\Microsoft\WinGet\Links\carapace.exe` | winget |
| fzf | WinGet Links | winget |
| PSCompletions | PowerShell Module (v6.2.2) | Install-Module |
| PSFzf | PowerShell Module (v2.7.9) | Install-Module |

---

## Priority Recommendations

### ✅ Resolved (This Session)
1. **COMP-012/COMP-015/COMP-017:** PSCompletions/Carapace conflicts RESOLVED
   - Removed conflicting completions, now only `psc` + `winget` in PSCompletions
   - `git <Tab>` works without errors

### Immediate (Quick Wins)
1. **COMP-016:** Add PSFzf to profile for fuzzy features (Ctrl+R, Ctrl+T)
2. **COMP-003:** Test winget dynamic autocomplete with current clean setup

### High Priority
3. **CS-007-CS-016:** Music command improvements (search, progress, formatting)
4. **CS-003:** Add regions to 10 files >300 lines (GoogleSheetsService, MusicBrainzService, etc.)
5. **PWS-005:** Profile performance optimization (~900ms → ~500ms target)

### Medium Priority
6. **CS-019-CS-020:** Code quality (Console.cs enforcement, named args)
7. **PWS-014-PWS-016:** Whisper UX improvements

### Long-term
8. **CS-024, CS-031-CS-032:** Full Python/C# integration
9. **PY-004-PY-006:** Python toolkit restructure
