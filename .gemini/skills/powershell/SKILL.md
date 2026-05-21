---
name: powershell
description: Use when writing or debugging PowerShell scripts. Focuses on shell stability, advanced functions, and command orchestration.
---

# PowerShell Shell & Orchestration

## Skill Handoff Logic
- **ACTIVATE `dotnet`** for universal .NET syntax (`??`, `? :`), runtime behavior, `System.Text.Json` logic, and **ALL Regex Engine behavior** (like syntax, anchors, multiline, timeouts, and `$$` in replacements).
- **ACTIVATE `csharp-regex`** for C# language regex syntax and source generation.

## Core Reference
- [PowerShell Shell Playbook](playbooks/modern-patterns.md)
- [Advanced Function Authoring](playbooks/advanced-functions.md)
- [Error Handling Patterns](playbooks/error-handling.md)
- [Terminal Invocation Edge-Cases](playbooks/terminal-invocation-edge-cases.md)
- [Quoting & Regex Anti-Patterns](playbooks/quoting.md)
- **REQUIRED SKILL:** Use `dotnet` for universal .NET syntax, core types, and core Regular Expression engine rules.

## Cross-Cutting Rules
- **Splatting:** `@variable` splats; `$variable` passes the object as-is. Never mix them up.
- **Error Action:** Set `$ErrorActionPreference = 'Stop'` at function scope, not globally.
- **Terminating Errors:** Use `$PSCmdlet.ThrowTerminatingError` for structured errors, not bare `throw`.
- **Module Visibility:** Export only public API surface from modules; internal helpers stay private.

## PowerShell Implementation (Quick Reference)
- **Pipeline Chain:** `Step1 && Step2 || HandleError`
- **PS Class:** `class T { [string]$N; T([string]$n) { $this.N = $n } }`
- **Custom Object:** `[PSCustomObject]@{ Key = 'Val' }`
- **Error Stop:** `$ErrorActionPreference = 'Stop'`
- **Splatting:** `Get-Command @params`

## PowerShell Quoting & Regex
- **The Gold Standard:** Always use **Single Quotes** (`'...'`) for literal strings and regex patterns.
- **Literal Safety:** Single quotes prevent `$` (variable) or `` ` `` (escape) expansion by the shell.
- **Replace Operator:** `'string' -replace 'pattern', 'replacement'` (Use single quotes).
- **Match Operator:** `'string' -match 'pattern'` (Populates `$matches`).
- **Nightmare Zone:** If you MUST use double quotes, escape `$` with `` ` `` (e.g., `` "`$var" ``).

## Advanced Functions
Mandatory use of `[CmdletBinding()]` and `[OutputType()]`.
```powershell
function Get-Resource {
    [CmdletBinding(DefaultParameterSetName = 'Name')]
    [OutputType([PSCustomObject])]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string]$Name
    )
    process {
        try { /* logic */ }
        catch { $PSCmdlet.WriteError($_) }
    }
}
```

## Shell Stability
- **Paths:** Always use `Join-Path` for cross-platform safety.
- **Execution:** Prefer `-EncodedCommand` for passing complex logic to background processes.
- **Data Flow:** Always use `Write-Output` or `[PSCustomObject]` for returning data; avoid `Write-Host`.

## Red Flags
- Using `Double Quotes` for literal strings or regex patterns.
- Missing `[CmdletBinding()]` in public-facing functions.
- Manual string concatenation for file paths.
- Silent `catch` blocks without error logging.
