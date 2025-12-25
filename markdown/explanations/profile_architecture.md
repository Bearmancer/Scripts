# PowerShell Profile Architecture

*Last Updated: January 2025*

## Current State

**Profile Load Time:** ~885ms (with timing enabled)
**Root Cause:** Eager loading of completion systems (PSCompletions ~373ms, Carapace ~173ms)

**Profile Consolidation:** ✅ COMPLETE
- System profile dot-sources workspace profile
- Single source of truth at `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`

---

## Profile Files Explained

### System Profile (Automatic)
**Location:** `C:\Users\Lance\Documents\PowerShell\Microsoft.PowerShell_profile.ps1`
**Loaded By:** PowerShell automatically when starting any session
**Access Variable:** `$PROFILE`
**Purpose:** User-specific profile for all PowerShell sessions

### Workspace Profile (Custom)
**Location:** `C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1`
**Loaded By:** Manually (currently duplicated in system profile)
**Purpose:** Version-controlled, portable, can be shared across machines

---

## Profile Duplication - RESOLVED ✅

**Solution Applied:** System profile dot-sources workspace profile:

```powershell
# C:\Users\Lance\Documents\PowerShell\Microsoft.PowerShell_profile.ps1
. 'C:\Users\Lance\Dev\Scripts\powershell\Microsoft.PowerShell_profile.ps1'
```

**Benefits:**
- Single source of truth (workspace profile)
- Git-trackable (in Scripts repo)
- Easy to restore on new machine
- No sync issues

---

## Why UTF-8 Encoding Should Move to ScriptsToolkit

### Current State (Workspace Profile)
```powershell
[Console]::InputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Global:OutputEncoding = [System.Text.Encoding]::UTF8
$PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'
$PSDefaultParameterValues['*:Encoding'] = 'utf8'
$env:PYTHONIOENCODING = 'utf-8'
```

6 lines of setup code in profile!

### ScriptsToolkit.psm1 Already Has This
```powershell
function Set-Utf8Console {
    [CmdletBinding()]
    param()

    [Console]::InputEncoding = [System.Text.Encoding]::UTF8
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    $Global:OutputEncoding = [System.Text.Encoding]::UTF8
    $Global:PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'
    $Global:PSDefaultParameterValues['*:Encoding'] = 'utf8'
    $env:PYTHONIOENCODING = 'utf-8'
    chcp 65001 | Out-Null  # ← Missing from current profile!
}
```

### Why Move to Module?

1. **DRY Principle** - One function, not duplicated code
2. **Better Scope** - Uses `$Global:` explicitly (current profile doesn't!)
3. **Code Page** - Includes `chcp 65001` (fixes console display issues)
4. **Reusability** - Other scripts can call if needed
5. **Testable** - Can verify UTF-8 setup in tests
6. **Profile Simplification** - One line: `Set-Utf8Console`

### Lazy Loading Consideration

**Question:** But if ScriptsToolkit is lazy-loaded, how to call `Set-Utf8Console` in profile?

**Answer:** UTF-8 setup must happen BEFORE lazy loading. Two options:

#### Option 1: Inline UTF-8 in Profile (Keep Current)
```powershell
# Profile runs immediately, before lazy load
[Console]::InputEncoding = [System.Text.Encoding]::UTF8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
# ... rest of encoding setup
```

**Pros:** No module dependency
**Cons:** Duplicated code, missing chcp 65001

#### Option 2: Load ScriptsToolkit Early (Just for UTF-8)
```powershell
# Load module minimally for Set-Utf8Console
Import-Module "$ModulePath\ScriptsToolkit" -Function Set-Utf8Console
Set-Utf8Console

# Then set up lazy loading for everything else
```

**Pros:** Reuses function, includes chcp
**Cons:** Partial module load (defeats lazy loading)

#### Option 3: Extract UTF-8 to Separate Tiny Module
```powershell
# Create: powershell/Utf8Setup/Utf8Setup.psm1
# Just one function, loads instantly (<5ms)

Import-Module Utf8Setup
Set-Utf8Console

# Then lazy-load ScriptsToolkit
```

**Pros:** Reusable, fast, no duplication
**Cons:** Extra module file

**Recommendation:** **Option 3** - Extract to tiny module
- Profile cleanliness
- Reusable across projects
- Minimal load time impact (~5ms)
- Includes chcp 65001

---

## Profile Load Sequence (Current - 937ms)

```
[0ms]     PowerShell starts
[10ms]    Profile execution begins
[15ms]    Set-StrictMode
[20ms]    Add ScriptsToolkit to PSModulePath
[90ms]    UTF-8 encoding setup (6 statements)
[100ms]   Import-Module PSCompletions  ← 200ms+ (SLOW!)
[320ms]   Set-PSReadLineOption (2 calls)
[350ms]   Import-Module PSFzf  ← 150ms+ (SLOW!)
[500ms]   Configure PSFzf (3 calls)
[550ms]   carapace | Invoke-Expression  ← 200ms+ (SLOW!)
[750ms]   argc | Invoke-Expression  ← 150ms+ (SLOW!)
[937ms]   Profile load complete
```

**Bottlenecks:**
1. PSCompletions: ~220ms
2. carapace: ~200ms
3. argc: ~187ms
4. PSFzf: ~150ms

**Total wasted time:** ~757ms (80% of load time!)

---

## Target Architecture (Lazy Loading - <100ms)

### Phase 1: Profile Startup (Target: 60ms)

```
[0ms]     PowerShell starts
[5ms]     Profile execution begins
[10ms]    Set-StrictMode -Version Latest
[15ms]    Add ScriptsToolkit to PSModulePath
[20ms]    Import-Module Utf8Setup  (tiny module)
[25ms]    Set-Utf8Console
[35ms]    Set-PSReadLineOption (basic config only)
[50ms]    Register proxy functions (56 functions)
[60ms]    Display load summary
```

**What's NOT loaded:**
- ScriptsToolkit module (proxies registered instead)
- PSCompletions (deferred to first Tab)
- PSFzf (deferred to first Ctrl+T/R/Space)
- carapace (deferred to first external command)
- argc completions (on-demand via argc-load)

### Phase 2: First Function Call (e.g., Get-Directories)

```
User types: Get-Directories
  ↓
Proxy function intercepts
  ↓
Remove proxy: Remove-Item Function:\Get-Directories
  ↓
Load module: Import-Module ScriptsToolkit -Global
  ↓
Call real function: & Get-Directories @args
  ↓
(~200ms first call, instant thereafter)
```

### Phase 3: First Tab Completion

```
User presses: Tab
  ↓
PSReadLine Tab handler intercepts
  ↓
Check: if (-not $global:__PSCompletionsLoaded)
  ↓
Load: Import-Module PSCompletions
  ↓
Set flag: $global:__PSCompletionsLoaded = $true
  ↓
Invoke: [Microsoft.PowerShell.PSConsoleReadLine]::TabCompleteNext()
  ↓
(~200ms first tab, instant thereafter)
```

### Phase 4: First Fuzzy Find (Ctrl+T)

```
User presses: Ctrl+T
  ↓
PSReadLine key handler intercepts
  ↓
Check: if (-not (Get-Module PSFzf))
  ↓
Load: Import-Module PSFzf
  ↓
Configure: Set-PsFzfOption calls
  ↓
Replace handler: Set-PSReadLineKeyHandler with real Invoke-FuzzySetLocation
  ↓
Invoke: Invoke-FuzzySetLocation
  ↓
(~150ms first Ctrl+T, instant thereafter)
```

---

## Lazy Loading Implementation Patterns

### Pattern 1: Proxy Functions

**Use Case:** ScriptsToolkit module (56 functions)

```powershell
function Register-LazyModule {
    param(
        [string]$ModuleName,
        [string]$ModulePath,
        [string[]]$Functions
    )

    foreach ($funcName in $Functions) {
        $scriptBlock = {
            # Capture variables in closure
            $modulePath = $using:ModulePath
            $funcName = $using:funcName

            # Remove this proxy
            Remove-Item "Function:\$funcName" -Force

            # Load the real module globally
            Import-Module $modulePath -Global -Force

            # Call the real function with original arguments
            & $funcName @args
        }.GetNewClosure()

        # Create proxy function
        New-Item -Path "Function:\$funcName" -Value $scriptBlock -Force | Out-Null
    }
}

# Usage:
$toolkitFunctions = @('Get-Directories', 'Show-SyncLog', ... ) # All 56
Register-LazyModule -ModuleName 'ScriptsToolkit' `
    -ModulePath "$env:PSModulePath\ScriptsToolkit\ScriptsToolkit.psm1" `
    -Functions $toolkitFunctions
```

**How It Works:**
1. Profile creates 56 "fake" functions (proxies)
2. Proxies are lightweight (just a script block)
3. First call to ANY function triggers module load
4. Proxy removes itself, real function takes over
5. Subsequent calls are instant (no proxy overhead)

**Cost:**
- Creating proxies: ~15ms for 56 functions
- First call overhead: ~200ms (one-time)
- Memory: Minimal (proxies are tiny)

### Pattern 2: Deferred Module Loading

**Use Case:** PSCompletions, PSFzf

```powershell
# Instead of: Import-Module PSCompletions

# Create deferred loader
$global:__DeferredModules = @{}

function Register-DeferredModule {
    param([string]$ModuleName, [scriptblock]$LoadAction)
    $global:__DeferredModules[$ModuleName] = $LoadAction
}

function Load-DeferredModule {
    param([string]$ModuleName)

    if ($global:__DeferredModules.ContainsKey($ModuleName)) {
        & $global:__DeferredModules[$ModuleName]
        $global:__DeferredModules.Remove($ModuleName)
    }
}

# Register PSCompletions
Register-DeferredModule -ModuleName 'PSCompletions' -LoadAction {
    Import-Module PSCompletions -ErrorAction SilentlyContinue
}

# Hook into Tab key
$originalTabHandler = (Get-PSReadLineKeyHandler -Key Tab)[0].Function
Set-PSReadLineKeyHandler -Key Tab -ScriptBlock {
    Load-DeferredModule 'PSCompletions'
    # Now invoke actual tab completion
    & $originalTabHandler
}
```

### Pattern 3: On-Demand Loading

**Use Case:** argc completions (~500 available)

```powershell
# Don't load any argc completions in profile

# User runs when needed:
argc-load git docker kubectl  # Loads only these 3

# Tab completion for argc-load shows available commands:
Register-ArgumentCompleter -CommandName 'argc-load' -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete)

    Get-ArgcManifest | Where-Object { $_ -like "$wordToComplete*" } | ForEach-Object {
        [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
    }
}
```

---

## Profile vs Module Design Decision

### When to Put Code in Profile

**Use Profile For:**
1. Environment setup (PSModulePath, $env vars)
2. Encoding configuration (UTF-8)
3. Lazy loading registration (proxies, hooks)
4. Timing/diagnostics (Write-Timing)

**Profile Should Be:**
- Small (<100 lines)
- Fast (<100ms)
- Infrastructure-focused

### When to Put Code in Module

**Use Module For:**
1. Actual functionality (commands, utilities)
2. Reusable functions (Set-Utf8Console)
3. Business logic
4. Complex initialization

**Module Should Be:**
- Lazy-loadable
- Testable
- Version-controlled
- Documented

### Anti-Pattern: Bloated Profile

**Bad Profile (Current State):**
```powershell
# 40+ lines of encoding setup
[Console]::InputEncoding = ...
[Console]::OutputEncoding = ...
# ...

# Eager module loading
Import-Module PSCompletions
Import-Module PSFzf
# ...

# Inline functions
function My-CustomCommand { ... }
function Another-Command { ... }
# Total: 200+ lines, 937ms load time
```

**Good Profile (Target):**
```powershell
# 60 lines total, <100ms load time

Set-StrictMode -Version Latest
$sw = [Diagnostics.Stopwatch]::StartNew()

# Add module path
$ModulePath = 'C:\Users\Lance\Dev\Scripts\powershell'
$env:PSModulePath = $ModulePath + [IO.Path]::PathSeparator + $env:PSModulePath

# UTF-8 setup (tiny module)
Import-Module Utf8Setup
Set-Utf8Console
Write-Timing "UTF-8 configured"

# Basic PSReadLine
Set-PSReadLineOption -PredictionSource HistoryAndPlugin
Set-PSReadLineOption -PredictionViewStyle ListView
Write-Timing "PSReadLine configured"

# Lazy load ScriptsToolkit (56 functions)
Register-LazyModule -ModuleName 'ScriptsToolkit' -Functions $toolkitFunctions
Write-Timing "ScriptsToolkit proxies registered"

# Lazy load completions
Register-DeferredCompletions
Write-Timing "Completions deferred"

$sw.Stop()
Write-Host "Profile loaded in $($sw.ElapsedMilliseconds)ms" -ForegroundColor Cyan
```

---

## Module Loading Best Practices

### 1. Minimize Profile Scope

**Profile's Job:**
- Set up environment
- Register lazy loaders
- Exit quickly

**NOT Profile's Job:**
- Implement commands
- Load heavy dependencies
- Parse data files

### 2. Defer Everything Possible

**Always Defer:**
- Third-party modules (PSCompletions, PSFzf)
- Completion systems (carapace, argc)
- Custom modules (ScriptsToolkit)

**Never Defer:**
- PSReadLine (needed for interactive experience)
- Critical environment setup (PSModulePath, encoding)

### 3. Measure Everything

```powershell
$sw = [Diagnostics.Stopwatch]::StartNew()
$lastCheckpoint = 0

function Write-Timing {
    param([string]$Component)
    $elapsed = $sw.ElapsedMilliseconds
    $delta = $elapsed - $script:lastCheckpoint
    Write-Host "[$('{0,5}' -f $elapsed)ms] $Component (Δ${delta}ms)" -ForegroundColor DarkGray
    $script:lastCheckpoint = $elapsed
}
```

**Track:**
- Total load time
- Per-module load time (differential)
- Memory usage (if needed)

### 4. Optimize Loading Order

**Load in This Order:**
1. Critical setup (StrictMode, PSModulePath)
2. Encoding (UTF-8)
3. PSReadLine basic config
4. Lazy loaders registration
5. Display summary

**Why This Order:**
- Failures fail fast (StrictMode first)
- PSReadLine needed for key handlers
- Lazy loaders registered before use
- Summary shows actual load time

---

## Error Handling Strategy

### Profile Errors Are Critical

If profile fails to load, shell is broken!

**Always:**
- Use try-catch around risky operations
- Provide fallbacks for missing modules
- Log errors clearly
- Continue loading even after errors

```powershell
# Bad:
Import-Module PSCompletions  # Error stops entire profile!

# Good:
try {
    Import-Module PSCompletions -ErrorAction Stop
}
catch {
    Write-Warning "PSCompletions failed to load: $_"
    # Profile continues loading
}
```

### Module Errors Are Recoverable

If module fails to load, user can reload or skip.

```powershell
# In ScriptsToolkit.psm1
function Invoke-YouTubeSync {
    try {
        # ... implementation
    }
    catch {
        Write-Error "YouTube sync failed: $_"
        return 1  # Non-zero exit code
    }
}
```

---

## Testing Strategy

### Profile Testing

**Cannot use Pester** (profiles don't export functions)

**Manual Testing:**
```powershell
# Measure load time
Measure-Command { . $PROFILE }

# Verify no errors
. $PROFILE
$Error.Count  # Should be 0

# Check module loaded
Get-Module ScriptsToolkit  # Should be empty (lazy-loaded)

# Trigger lazy load
Get-Directories  # Should work and load module

# Verify module now loaded
Get-Module ScriptsToolkit  # Should show module
```

### Module Testing

**Use Pester:**
```powershell
Describe 'ScriptsToolkit' {
    BeforeAll {
        Import-Module ScriptsToolkit -Force
    }

    It 'Exports Get-Directories' {
        Get-Command Get-Directories -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
    }

    It 'Set-Utf8Console sets encoding' {
        Set-Utf8Console
        [Console]::OutputEncoding.WebName | Should -Be 'utf-8'
    }
}
```

---

## Summary

### Current Problems
1. ❌ 937ms load time (unacceptable)
2. ❌ Duplicate profile files (sync issues)
3. ❌ Eager loading (wastes time on unused modules)
4. ❌ No error handling (profile failures break shell)
5. ❌ No timing visibility (can't identify bottlenecks)

### Target Solutions
1. ✅ <100ms load time (90% improvement)
2. ✅ Single source of truth (dot-source pattern)
3. ✅ Lazy loading (load only when used)
4. ✅ Graceful error handling (try-catch)
5. ✅ Differential timing (Write-Timing function)

### Implementation Priority
1. **Phase 1:** Add timing to current profile (PWS-004)
2. **Phase 2:** Fix duplication (PWS-003)
3. **Phase 3:** Implement lazy loading (PWS-005)
4. **Phase 4:** Extract UTF-8 to tiny module (PWS-001)
5. **Phase 5:** Add error handling (PWS-002)

### Expected Results
- **Load time:** 937ms → 60ms (94% faster)
- **First function call:** +200ms (one-time cost)
- **Memory:** ~50% reduction (modules not loaded)
- **Startup experience:** Instant, responsive shell
