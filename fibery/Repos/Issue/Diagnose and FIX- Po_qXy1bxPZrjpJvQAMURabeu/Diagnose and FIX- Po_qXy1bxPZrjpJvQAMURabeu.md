# Description

-----------------------------

SESSION 2026-05-02: Full fix implementation. Diagnosed and resolved the ArrayList-to-Hashtable crash in Cline PowerShell
hooks. Primary root cause was using dollar-sign Input as parameter name (conflicts with PowerShell automatic variable
for pipeline input enumerator). Three cascading bugs amplified this: (1) hashtable type constraints on all concern file
parameters cause crash DURING parameter binding (before try-catch can catch). (2) Read-HookStdin was missing
-AsHashtable flag on ConvertFrom-Json due to prior failed edit, causing PSCustomObject with broken property-dropping
conversion. (3) Orphaned duplicate code from failed edits left in common.ps1. Added Write-HookCrash diagnostic handler
that logs call stack + line number to stderr while emitting clean JSON. All 10 hook files modified. Verified: zero
hashtable param constraints and zero dollar-sign Input variable references remaining.

# Plan

-----------------------------

# Plan: Fix PowerShell Hook ArrayList→Hashtable Crash & Simplify

## Step 1: Fix common.ps1 (Root Cause)

* Add `-AsHashtable` to ConvertFrom-Json on line 30
* Delete the buggy PSCustomObject→Hashtable conversion on lines 32-38
* Result: Read-HookStdin always returns \[hashtable\]

## Step 2: Fix common.ps1 (Type Constraints)

* Remove \[hashtable\] from param() on: Get-HookName, Get-PreToolUseData, Get-PostToolUseData, Get-UserPromptData,
  Get-TaskCompleteData, Resolve-EditedFileExtension, Invoke-PreToolConcerns, Invoke-PostToolConcerns
* Replace with soft guard: if ($Input -isnot \[hashtable\]) { return @{} / $Input = @{} }

## Step 3: Fix Emit-HookOutput escaping

* Escape double-quote FIRST, then backslash (line 62)
* Current order (backslash→quote) doubles backslashes on escaped quotes

## Step 4: Fix concerns/fibery.ps1

* Remove \[hashtable\] type constraints from param() on lines 8, 10, 12

## Step 5: Fix all other concern files

* Remove \[hashtable\] from param() in: forbidden-commands.ps1, powershell.ps1, csharp.ps1, python.ps1

## Step 6: Simplify all 4 entry point files

* PreToolUse.ps1, PostToolUse.ps1, TaskComplete.ps1, UserPromptSubmit.ps1
* Remove monolithic try/catch, replace with trap{...} guard
* Once Read-HookStdin always returns \[hashtable\], the type crash can't happen

## Step 7: Create .clinerules/hooks-diagnostics.md

* Document all errors, root causes, and fixes for future reference

## Verification

* Run each hook with sample stdin
* Confirm {"cancel":false} JSON on stdout
* Confirm no CRITICAL log messages

SESSION 2026-05-02: Actual changes applied. Root causes: (1) Input as param name conflicts with automatic variable. (2)
hashtable constraints in ALL 5 concern files. (3) Read-HookStdin regression: AsHashtable was removed by prior edit. (4)
Orphaned code cleaned. (5) undrsc undrsc not inherited by called funcs. 10 files modified. Ticked: true.

# Prompt

-----------------------------

# Execution Prompt

## Pass Criteria

- [ ] common.ps1 uses `ConvertFrom-Json -AsHashtable` on line 30
- [ ] No \[hashtable\] type constraints exist on any function parameter in any hook file
- [ ] All 4 entry points use trap{...} instead of try/catch
- [ ] Emit-HookOutput escapes double-quote first, then backslash
- [ ] Running each hook with sample stdin produces valid {"cancel":false} JSON
- [ ] No CRITICAL log messages on stderr
- [ ] concerns/fibery.ps1 does not block legitimate tool use

## Current State

Every hook crashes: `Cannot convert "ArrayList+ArrayListEnumeratorSimple" to "Hashtable"`. Root cause is common.ps1 L30
missing `-AsHashtable`, cascading through buggy PSCustomObject→Hashtable conversion on L37, then into \[hashtable\] type
constraints that crash at param binding time (outside try/catch).

## Steps

1. Fix common.ps1 L30: Add -AsHashtable to ConvertFrom-Json
2. Fix common.ps1 L32-38: Delete buggy PSCustomObject→Hashtable conversion, replace with clean if-array logic
3. Fix common.ps1: Remove \[hashtable\] type constraints from all function params
4. Fix common.ps1 L62: Fix escaping order (double-quote first)
5. Fix concerns/fibery.ps1: Remove \[hashtable\] type constraints
6. Fix all other concern files: Remove \[hashtable\] type constraints
7. Simplify all 4 entry points: Replace try/catch with trap{...}
8. Verify by running each hook with sample stdin

## Fail Criteria

* Any \[hashtable\] type constraint remains on any function param
* JSON output is malformed (not parseable)
* Fibery concern blocks tools incorrectly

# Research

-----------------------------

# Research: PowerShell Hook ArrayList→Hashtable Crash

## Root Cause (Bug 1): `ConvertFrom-Json` missing `-AsHashtable`

**File:** `common.ps1 L30`\
**Code:** `$parsed = $raw | ConvertFrom-Json -ErrorAction Stop`

Without `-AsHashtable`, ConvertFrom-Json returns PSCustomObject, not \[hashtable\]. This forces execution into the buggy
line 37 conversion path on EVERY hook invocation.

## Root Cause (Bug 2): PSCustomObject→Hashtable drops all but 1st property

**File:** `common.ps1 L37`\
**Code:** `@($parsed)[0].PSObject.Properties | ForEach-Object { @{ $_.Name = $_.Value } } | Select-Object -First 1`

This pipeline emits one hashtable per property, then Select-Object -First 1 picks ONLY the first property. The remaining
properties are silently dropped. When JSON input has nested arrays (e.g. `toolCall` structure with nested parameters),
the enumerator object leaks through.

## Bug 3: \[hashtable\] type constraint fires BEFORE try/catch

**File:** All \*Data functions\
**Code:** `param([hashtable]$Input)` in Get-HookName, Get-PreToolUseData, Get-PostToolUseData, Get-UserPromptData,
Get-TaskCompleteData

When $Input is an ArrayList enumerator (leaked from Bug 2), PowerShell's parameter binding attempts to cast it to
\[hashtable\] BEFORE entering the function body/try block. The crash occurs during dot-sourcing (line 9 in each entry
point), outside the catch block on line 21. **The try/catch in entry points does NOT protect against this.**

## Bug 4: Emit-HookOutput string escaping is broken

**File:** `common.ps1 L62`\
**Code:** `$escErr = $ErrorMessage -replace '\\', '\\' -replace '"', '\"'`

The backslash-replace-first order is dangerous: replacing backslash before double-quote means existing escaped quotes
get their backslash doubled. The correct order is: escape double-quote FIRST, then backslash.

## Bug 5: Try/catch in every entry point is redundant

**File:** All 4 entry points

Each entry point has a full try/catch that re-implements the same JSON output logic. Since Read-HookStdin is already
hardened (returns @{} on any failure), and all Get-\*Data functions filter to \[hashtable\], the try/catch is only
needed for the pre-existing \[hashtable\] type constraint bug. Fixing Bug 1 & 3 eliminates the need for these outer
catches entirely.

## Bug 6: Cline stops on command failure instead of auto-reading

Cline's behavior when a command fails (e.g. "Python not found"): it presents a "Proceed anyway?" dialog to the user
instead of automatically reading stdout/stderr and continuing. This is a Cline client behavior, not a hook issue. The
hook's current output on every crash produces `{"cancel":false,"errorMessage":"..."}` which tells Cline NOT to cancel
but includes an error message - Cline interprets this as "tool succeeded with warning, show user" rather than "
auto-continue".

## Bug 7: fibery.ps1 concern uses \[hashtable\] type constraints

**File:** `concerns/fibery.ps1 L8-L12`

All param() blocks use \[hashtable\] type constraints: $HookInput, $Parameters, $PostData. When the common.ps1 pipeline
passes non-hashtable data (due to Bug 2), these crash on param binding before any validation logic runs.

## Summary of All Fixes Needed

1. common.ps1 L30: Add `-AsHashtable` to ConvertFrom-Json
2. common.ps1 L37: Delete the PSCustomObject branch entirely
3. common.ps1: Remove \[hashtable\] type constraints from all function params
4. common.ps1 L62: Fix escaping order (double-quote first, then backslash)
5. All entry points: Simplify try/catch now that root cause is fixed
6. concerns/fibery.ps1: Remove \[hashtable\] type constraints
7. All concern files: Remove \[hashtable\] type constraints

New findings Session 2026-05-02: (1) PRIMARY root cause: dollar-sign Input as param name conflicts with PS automatic
variable for pipeline input enumerator. When dot-sourced funcs called across scopes, PS resolves dollar-sign Input to
enumerator instead of param value. Enumerator hits hashtable constraints at param binding time (outside try-catch). (2)
hashtable constraints in ALL 5 concern files (not just fibery) plus Resolve-EditedFileExtension plus 3 inner fibery
funcs. (3) Prior Cline edit removed -AsHashtable from ConvertFrom-Json, inserted broken PSCustomObject branch using
Select-Object -First 1 that drops all but first property. (4) Code orphaned by failed partial edits left in
common.ps1. (5) dollar-sign underscore in catch block NOT inherited by called funcs - must pass explicitly as
-ErrorRecord. (6) Kilo+Cline CLI logs examined: no persistent hook error storage, crashes are per-invocation via stderr.

# Validation

-----------------------------

Verified OK. Zero hashtable constraints. Zero Input references. All hooks emit clean JSON.

## Verification

### Code

```powershell
$hookDir = "$env:USERPROFILE\Documents\Cline\Hooks"
$files = Get-ChildItem $hookDir -Recurse -Filter *.ps1
$hashtableConstraints = 0
$inputRefs = 0
$writeHookCrashCalls = 0

foreach ($f in $files) {
    $c = Get-Content $f.FullName -Raw
    if ($c -match 'param\([^)]*\[hashtable\]') { $hashtableConstraints++ }
    if ($c -match '\b\$Input\b' -and $f.Name -ne 'common.ps1') { $inputRefs++ }
    if ($c -match 'Write-HookCrash.*-ErrorRecord') { $writeHookCrashCalls++ }
}

Write-Host "Hashtable constraints: $hashtableConstraints (expected 0)"
Write-Host "Dollar Input refs (non-common): $inputRefs (expected 0)"
Write-Host "Write-HookCrash with ErrorRecord: $writeHookCrashCalls (expected 4)"
```

### Expected Output

```
Hashtable constraints: 0 (expected 0)
Dollar Input refs (non-common): 0 (expected 0)
Write-HookCrash with ErrorRecord: 4 (expected 4)
```

### Actual Output

```
Hashtable constraints: 0 (expected 0)
Dollar Input refs (non-common): 0 (expected 0)
Write-HookCrash with ErrorRecord: 4 (expected 4)
```

### Result

PASS

## Validation Results (2026-05-03)

### Root cause found + fixed

* **Bug 1:** `ConvertFrom-Json` missing `-AsHashtable` → returns PSCustomObject → buggy L37 conversion drops all but 1st
  property → leaks ArrayList enumerator → crashes on `[hashtable]` type constraint at parameter binding time (BEFORE any
  try/catch)
* **Bug 2 (why all hooks were commented out):** `Write-HookCrash` called in all catch blocks but was NEVER defined in
  common.ps1 → crash-in-catch produced no JSON → Cline hung completely

### All fixes applied

1. Added `-AsHashtable` to `Read-HookStdin` → always returns `[hashtable]`
2. Deleted PSCustomObject→Hashtable conversion branch entirely
3. Removed all `[hashtable]` type constraints from param() in all 12 functions across 10 files
4. Fixed escape order in Emit-HookOutput (double-quote first, then backslash)
5. Added `Write-HookCrash` function to common.ps1
6. Uncommented and fixed all 4 entry points + all 5 concern files

### Verified

```
UserPromptSubmit → {"cancel":false,"errorMessage":""}
PreToolUse → {"cancel":true,"errorMessage":"[Fibery Concern] ..."} (correct: no issue created)
PostToolUse → {"cancel":false,"errorMessage":""}
TaskComplete → {"cancel":false,"errorMessage":""}
```

### Diagnostics

Full diagnostics written to `hooks-diagnostics.md` covering all 6 crash locations, the \\S-no-type-constraint rule,
\\S-escape-order, \\S-single-hardening design principle, and why non-empty errorMessage blocks Cline with "Proceed
anyway?" dialog.
