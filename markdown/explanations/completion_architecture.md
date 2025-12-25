# PowerShell Completion Architecture
*Created: January 2025*
*Updated: December 2025 - Benchmarks verified*

## Performance Summary

| Scenario | Time | Delta |
|----------|------|-------|
| pwsh -NoProfile | ~273ms | baseline |
| PSCompletions alone (fresh) | ~1537ms | +1264ms |
| Carapace alone (fresh) | ~1245ms | +972ms |
| **Current Profile** | **~1839ms** | **+1567ms** |
| With PSFzf (eager) | ~2200ms | +1927ms |
| With PSFzf (lazy) | ~1839ms | +0ms startup |

**Key Optimization Applied:** PSFzf lazy loading saves ~400ms at startup.

## Overview

The completion system consists of three layers working together:

```
┌─────────────────────────────────────────────────────────────┐
│                     TAB KEY PRESS                           │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│              PSCompletions (Menu UI Layer)                  │
│  • Intercepts Tab via Set-PSReadLineKeyHandler              │
│  • Calls TabExpansion2 for completion data                  │
│  • Renders TUI menu with enable_menu_enhance=1              │
│  • Owns: psc, winget completions                            │
└──────────────────────────┬──────────────────────────────────┘
                           │ TabExpansion2
                           ▼
┌─────────────────────────────────────────────────────────────┐
│           Register-ArgumentCompleter Sources                │
├─────────────────────────────────────────────────────────────┤
│  Carapace (670 commands)     │  argc (2 commands)           │
│  • git, gh, docker, kubectl  │  • dotnet                    │
│  • npm, cargo, go, az        │  • whisper-ctranslate2       │
│  • Custom: whisper-ctranslate2.yaml                         │
└─────────────────────────────────────────────────────────────┘
```

## Component Details

### 1. PSCompletions v6.2.2 (Menu UI)

**Role:** Tab key handler and TUI menu renderer

**Current Completions (2):**
- `psc` - PSCompletions management
- `winget` - Windows Package Manager (Carapace lacks this)

**How it Works:**
1. `Import-Module PSCompletions` registers Tab key handler
2. On Tab press, calls `TabExpansion2` for completion data
3. Aggregates results from ALL registered completers
4. Renders results in TUI menu (when >1 match)

**Configuration:**
```powershell
$PSCompletions.config.enable_menu          # 1 = TUI menu enabled
$PSCompletions.config.enable_menu_enhance  # 1 = Enhanced tooltips
$PSCompletions.config.trigger_key          # Tab
$PSCompletions.config.completion_suffix    # "" (empty)
```

### 2. Carapace v1.5.7 (Completion Data Provider)

**Role:** Primary source of completion data for 670+ commands

**How it Works:**
```powershell
carapace _carapace | Out-String | Invoke-Expression
```
This registers `Register-ArgumentCompleter` for each supported command.

**Environment:**
```powershell
$env:CARAPACE_BRIDGES = 'zsh,fish,bash,inshellisense'
```

**Not Covered:** `winget` (use PSCompletions or argc)

### 3. argc (Additional Completions)

**Role:** Fills gaps not covered by Carapace

**Currently Registered:**
- `dotnet` - .NET CLI (better than Carapace)
- `whisper-ctranslate2` - Custom spec

**How it Works:**
```powershell
argc --argc-completions powershell dotnet | Out-String | Invoke-Expression
```

## Data Flow: Tab Completion

```
User types: git checkout ma<Tab>

1. PSCompletions intercepts Tab
2. Calls TabExpansion2('git checkout ma', cursorColumn=16)
3. PowerShell finds ArgumentCompleter for 'git' (Carapace)
4. Carapace spawns: carapace git powershell 'checkout' 'ma'
5. Carapace returns: [main, master, maintenance]
6. PSCompletions renders menu with options
7. User selects, PSCompletions inserts selection
```

## PSReadLine Integration

PSReadLine handles:
- **Inline Prediction:** `PredictionSource History` shows gray inline suggestions
- **History Navigation:** UpArrow/DownArrow cycle through history
- **Native History Search:** Ctrl+R (ReverseSearchHistory), F8 (HistorySearchBackward)

PSCompletions overrides:
- **Tab:** Custom handler for TUI menu

## PSFzf (Optional - NOT in current profile)

**Role:** Fuzzy finder integration

**Key Bindings (when enabled):**
- `Ctrl+R` - Fuzzy history search
- `Ctrl+T` - Fuzzy file/directory search
- `Alt+C` - Fuzzy cd to directory
- `Ctrl+Space` - Fuzzy tab completion

**Impact:** ~421ms import time

**Lazy Loading Option:**
```powershell
#region PSFzf - Lazy loaded on first Ctrl+R
Set-PSReadLineKeyHandler -Key 'Ctrl+r' -ScriptBlock {
    if (-not (Get-Module PSFzf)) {
        Import-Module PSFzf
        Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t'
        Set-PsFzfOption -PSReadlineChordReverseHistory 'Ctrl+r'
    }
    Invoke-FzfPsReadlineHandlerHistory
}
Set-PSReadLineKeyHandler -Key 'Ctrl+t' -ScriptBlock {
    if (-not (Get-Module PSFzf)) {
        Import-Module PSFzf
        Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t'
        Set-PsFzfOption -PSReadlineChordReverseHistory 'Ctrl+r'
    }
    Invoke-FzfPsReadlineHandlerProvider
}
#endregion
```

## History List Alternatives to PSFzf

### Option 1: PSReadLine Native (No module needed)
| Key | Function | Description |
|-----|----------|-------------|
| `Ctrl+R` | ReverseSearchHistory | Incremental text search |
| `F8` | HistorySearchBackward | Match by prefix |
| `UpArrow` | PreviousHistory | Cycle through history |

### Option 2: Out-GridView F7 Handler (No module needed)
```powershell
Set-PSReadLineKeyHandler -Key F7 -BriefDescription 'ShowHistory' -ScriptBlock {
    $line = $null
    $cursor = $null
    [Microsoft.PowerShell.PSConsoleReadLine]::GetBufferState([ref]$line, [ref]$cursor)
    $selection = Get-History | Out-GridView -Title 'Select Command' -PassThru
    if ($selection) {
        [Microsoft.PowerShell.PSConsoleReadLine]::ReplaceLine($selection.CommandLine)
        [Microsoft.PowerShell.PSConsoleReadLine]::SetCursorPosition($selection.CommandLine.Length)
    }
}
```

### Option 3: F7History Module (~471ms)
```powershell
Install-Module F7History -Scope CurrentUser
Import-Module F7History
```
Provides console-based GUI popup on F7.

### Recommendation
- **If you rarely use Ctrl+R:** Use PSReadLine native Ctrl+R
- **If you want visual list:** Use Out-GridView F7 handler (zero overhead)
- **If you want fuzzy search:** Use PSFzf with lazy loading pattern

## Benchmark Results

| Scenario | Time (ms) | Notes |
|----------|-----------|-------|
| pwsh -NoProfile | 273 | Baseline |
| PSCompletions alone (fresh) | 1,537 | +1264ms (includes PSReadLine) |
| Carapace alone (fresh) | 1,245 | +972ms (registers 670 completers) |
| **Current Profile** | **1,887** | **+1614ms** |
| Profile + PSFzf (eager) | ~2,300 | +~400ms additional |
| Profile + PSFzf (lazy) | 1,887 | 0ms startup, ~180ms first use |

**Verified December 2025** - Fresh shell measurements with 5-iteration averages.

## Known Issues

### COMP-001: Tab Double Space
**Symptom:** Tab inserts completion + extra space
**Cause:** PSCompletions may add space AND completer adds space
**Fix:** Set `$PSCompletions.config.completion_suffix = ""`

### COMP-002: Tab Selects Instead of Filling ✅ FIXED
**Symptom:** Tab navigates menu instead of inserting selection
**Expected:** Single match should auto-insert
**Fix Applied:** `psc menu config enable_enter_when_single 1`

### COMP-003: winget Dynamic Search Broken
**Symptom:** `winget install je--<Tab>` doesn't search packages
**Cause:** PSCompletions winget doesn't support dynamic API search
**Fix:** Use `winget search` then copy package name

## Current Profile Order

```powershell
1. UTF-8 + Module Path      # Essential config
2. ScriptsToolkit           # Custom functions (~49ms)
3. PSCompletions            # Tab handler + menu (~350ms)
4. PSReadLine               # Prediction + colors (~5ms)
5. Carapace                 # 670 completions (~168ms)
6. argc                     # dotnet, whisper (~117ms)
7. PSFzf (LAZY)             # Ctrl+R/Ctrl+T (0ms startup)
```

This order ensures:
- PSCompletions gets Tab before PSReadLine's default
- Carapace/argc completers registered before any Tab press
- All completers available to PSCompletions TabExpansion2
