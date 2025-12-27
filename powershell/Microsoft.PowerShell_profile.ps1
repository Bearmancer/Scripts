Set-StrictMode -Version Latest

#region Timing Infrastructure (uncomment Show-ProfileSummary at end to enable)
$Script:ProfileStart = [System.Diagnostics.Stopwatch]::StartNew()
$Script:ModuleTimes = [ordered]@{}

function Write-ProfileTiming {
    param([string]$Component)
    $Script:ModuleTimes[$Component] = $Script:ProfileStart.ElapsedMilliseconds
}

function Show-ProfileSummary {
    $prev = 0
    Write-Host "`nProfile timing:" -ForegroundColor Cyan
    foreach ($item in $Script:ModuleTimes.GetEnumerator()) {
        $delta = $item.Value - $prev
        Write-Host ("  {0,-20} {1,4}ms (+{2})" -f $item.Key, $item.Value, $delta) -ForegroundColor DarkGray
        $prev = $item.Value
    }
    Write-Host ("  {0,-20} {1,4}ms" -f 'TOTAL', $Script:ProfileStart.ElapsedMilliseconds) -ForegroundColor Green
}
#endregion

#region Module Path & UTF-8 Console
# ScriptsToolkit is lazy-loaded on first use of its functions
# Only essential setup done here: module path + UTF-8 encoding
$env:PSModulePath = 'C:\Users\Lance\Dev\Scripts\powershell' + [IO.Path]::PathSeparator + $env:PSModulePath

# Inline UTF-8 setup (was Set-Utf8Console, saves ~165ms module load)
[Console]::InputEncoding = [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Global:OutputEncoding = [System.Text.Encoding]::UTF8
$env:PYTHONIOENCODING = 'utf-8'
chcp 65001 | Out-Null
Write-ProfileTiming 'UTF8-Setup'
#endregion

#region PSCompletions - Completion menu provider
# PSCompletions provides GUI menu for Tab completions
# Carapace provides the actual completion data for 670+ commands
Import-Module PSCompletions -ErrorAction SilentlyContinue
Write-ProfileTiming 'PSCompletions'
#endregion

#region PSReadLine - Basic line editing (Tab handled by PSCompletions)
# PSCompletions menu_enhance uses Set-PSReadLineKeyHandler for Tab
# We only configure prediction and colors here
Set-PSReadLineOption -PredictionSource History
Set-PSReadLineOption -PredictionViewStyle InlineView
Set-PSReadLineOption -Colors @{ "Selection" = "`e[7m" }
Write-ProfileTiming 'PSReadLine'
#endregion

#region Carapace - Multi-shell completion provider
# Carapace registers completers via Register-ArgumentCompleter
# PSCompletions menu displays these completions
$env:CARAPACE_BRIDGES = 'zsh,fish,bash,inshellisense'
carapace _carapace | Out-String | Invoke-Expression
Write-ProfileTiming 'Carapace'
#endregion

#region Argc-completions - Static completions for dotnet, whisper-ctranslate2
# argc-completions provides static completions for commands not covered by carapace
# Note: winget uses native completion for DYNAMIC package search
$argcRoot = Join-Path $env:USERPROFILE 'argc-completions'
if ((Test-Path $argcRoot) -and (Get-Command -Name argc -CommandType Application -ErrorAction SilentlyContinue)) {
    $env:ARGC_COMPLETIONS_ROOT = $argcRoot
    $env:ARGC_COMPLETIONS_PATH = "$argcRoot\completions\windows;$argcRoot\completions"
    # Only load specific commands (winget excluded - uses native for dynamic search)
    $argcCmds = @('dotnet', 'whisper-ctranslate2') | Where-Object {
        Get-Command -Name $_ -CommandType Application -ErrorAction SilentlyContinue
    }
    if ($argcCmds) {
        argc --argc-completions powershell @argcCmds | Out-String | Invoke-Expression
    }
}
Write-ProfileTiming 'Argc'
#endregion

#region PSFzf - Lazy loaded fuzzy finder
# Ctrl+R = Fuzzy search command HISTORY (most recent first)
# Ctrl+T = Fuzzy search FILES/directories in current tree
# Ctrl+Space = Fuzzy completion of ALL completions for current command
# PSFzf is NOT imported at startup - only when shortcuts are pressed
# This saves ~400ms startup time while preserving fuzzy search capability
Set-PSReadLineKeyHandler -Key 'Ctrl+r' -BriefDescription 'FzfHistory' -ScriptBlock {
    if (-not (Get-Module PSFzf)) {
        Import-Module PSFzf -ErrorAction SilentlyContinue
        Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t' -PSReadlineChordReverseHistory 'Ctrl+r'
    }
    Invoke-FzfPsReadlineHandlerHistory
}
Set-PSReadLineKeyHandler -Key 'Ctrl+t' -BriefDescription 'FzfProvider' -ScriptBlock {
    if (-not (Get-Module PSFzf)) {
        Import-Module PSFzf -ErrorAction SilentlyContinue
        Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t' -PSReadlineChordReverseHistory 'Ctrl+r'
    }
    Invoke-FzfPsReadlineHandlerProvider
}
Set-PSReadLineKeyHandler -Key 'Ctrl+Spacebar' -BriefDescription 'FzfTabCompletion' -ScriptBlock {
    if (-not (Get-Module PSFzf)) {
        Import-Module PSFzf -ErrorAction SilentlyContinue
    }
    Invoke-FzfTabCompletion
}
Write-ProfileTiming 'PSFzf-Lazy'
#endregion

#region Native Completions (winget, gh, dotnet)
# These use built-in completion APIs from the tools themselves
# Native completions provide better tooltips and dynamic data

# winget: DYNAMIC completion (live package search from repository)
Register-ArgumentCompleter -Native -CommandName winget -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)
    $line = $commandAst.ToString()
    winget complete --word="$wordToComplete" --commandline="$line" --position $cursorPosition | ForEach-Object {
        [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
    }
}

# gh: LAZY-LOADED native completion with tooltips
# Saves ~278ms startup - calls gh __complete directly (mirrors native script logic)
Register-ArgumentCompleter -Native -CommandName gh -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)

    # Reconstruct command - AST strips trailing space, so check cursor vs AST length
    $command = $commandAst.ToString()
    $hasTrailingSpace = $cursorPosition -gt $command.Length

    # Truncate to cursor if cursor is within command
    if ($command.Length -gt $cursorPosition) {
        $command = $command.Substring(0, $cursorPosition)
    }

    # Split program from arguments
    $program, $arguments = $command.Split(' ', 2)
    $requestComp = "$program __complete $arguments"

    # If completing a new word (cursor past command = trailing space), add empty arg
    if ($hasTrailingSpace) {
        $requestComp += ' ""'
    }

    # Call gh __complete
    Invoke-Expression $requestComp 2>$null | ForEach-Object {
        if ($_ -match '^:(\d+)$') { return }  # Skip directive lines
        if ($_ -match 'Completion ended') { return }  # Skip debug output
        $parts = $_ -split "`t", 2
        $text = $parts[0]
        $tooltip = if ($parts.Count -gt 1) { $parts[1] } else { $text }
        [System.Management.Automation.CompletionResult]::new($text, $text, 'ParameterValue', $tooltip)
    }
}

# dotnet: Native completion (subcommands/options, uses dotnet complete)
# Only register if argc-completions isn't providing it
if (-not $env:ARGC_COMPLETIONS_ROOT) {
    Register-ArgumentCompleter -Native -CommandName dotnet -ScriptBlock {
        param($wordToComplete, $commandAst, $cursorPosition)
        dotnet complete --position $cursorPosition "$commandAst" | ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
        }
    }
}
Write-ProfileTiming 'Native-Completions'
#endregion

#region Diagnostic Functions
function Get-CompletionSource {
    <#
    .SYNOPSIS
        Identifies which completer handles a command using DYNAMIC detection.
    .DESCRIPTION
        Uses reflection to access PowerShell's internal ArgumentCompleter registry,
        then cross-references with known completion providers.
    .EXAMPLE
        Get-CompletionSource dotnet
        Get-CompletionSource winget
        Get-CompletionSource git
    #>
    param([Parameter(Mandatory)][string]$Command)

    $sources = [System.Collections.Generic.List[string]]::new()

    # Access internal ArgumentCompleter registry via reflection (DYNAMIC)
    $ec = [System.Management.Automation.Runspaces.Runspace]::DefaultRunspace.GetType().GetProperty(
        'ExecutionContext', [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance
    ).GetValue([System.Management.Automation.Runspaces.Runspace]::DefaultRunspace)

    $nativeCompleters = $ec.GetType().GetProperty(
        'NativeArgumentCompleters', [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance
    ).GetValue($ec)

    $hasNativeCompleter = $nativeCompleters.ContainsKey($Command)

    # Check PSCompletions (DYNAMIC - queries module state)
    if ($PSCompletions -and $PSCompletions.data.list -contains $Command) {
        $sources.Add("PSCompletions")
    }

    # Check Carapace (DYNAMIC - queries binary)
    $carapaceList = carapace --list 2>$null
    if ($carapaceList -match "(?m)^$Command\s") {
        $sources.Add("Carapace")
    }

    # Check argc-completions (DYNAMIC - checks filesystem)
    if ($env:ARGC_COMPLETIONS_ROOT) {
        $argcScript = Join-Path $env:ARGC_COMPLETIONS_ROOT "completions\$Command.sh"
        $argcScriptWin = Join-Path $env:ARGC_COMPLETIONS_ROOT "completions\windows\$Command.sh"
        if ((Test-Path $argcScript) -or (Test-Path $argcScriptWin)) {
            $sources.Add("argc-completions")
        }
    }

    # Check for native tool completion (winget, dotnet have built-in completion)
    $nativeTools = @{
        'winget' = 'winget complete'
        'dotnet' = 'dotnet complete'
        'gh' = 'gh completion'
        'kubectl' = 'kubectl completion'
        'rustup' = 'rustup completions'
    }
    if ($nativeTools.ContainsKey($Command)) {
        $sources.Add("Native ($($nativeTools[$Command]))")
    }

    # Determine if completer is registered
    $completionCount = 0
    if ($hasNativeCompleter) {
        $completers = [System.Management.Automation.CommandCompletion]::CompleteInput("$Command ", "$Command ".Length, $null)
        $completionCount = $completers.CompletionMatches.Count
    }

    if ($sources.Count -eq 0 -and $hasNativeCompleter) {
        $sources.Add("Unknown (registered but source unclear)")
    } elseif ($sources.Count -eq 0) {
        $sources.Add("None (fallback to file completion)")
    }

    [PSCustomObject]@{
        Command = $Command
        Sources = $sources -join ', '
        RegisteredCompleter = $hasNativeCompleter
        CompletionCount = $completionCount
    }
}

function Find-Command {
    <#
    .SYNOPSIS
        Fuzzy search for commands, aliases, and functions.
    .EXAMPLE
        Find-Command git      # Find all commands containing 'git'
        Find-Command *clean*  # Wildcard search
    #>
    param([string]$Pattern = '*')

    $results = @()
    $results += Get-Alias -Name $Pattern -ErrorAction SilentlyContinue |
        Select-Object @{N='Name';E={$_.Name}}, @{N='Type';E={'Alias'}}, @{N='Definition';E={$_.Definition}}
    $results += Get-Command -Name $Pattern -CommandType Function -ErrorAction SilentlyContinue |
        Select-Object @{N='Name';E={$_.Name}}, @{N='Type';E={'Function'}}, @{N='Definition';E={$_.Source}}
    $results += Get-Command -Name $Pattern -CommandType Cmdlet -ErrorAction SilentlyContinue |
        Select-Object @{N='Name';E={$_.Name}}, @{N='Type';E={'Cmdlet'}}, @{N='Definition';E={$_.Source}}
    $results += Get-Command -Name $Pattern -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object @{N='Name';E={$_.Name}}, @{N='Type';E={'Application'}}, @{N='Definition';E={$_.Source}}

    $results | Sort-Object Name
}

function Get-RegisteredCompleters {
    <#
    .SYNOPSIS
        Lists all registered argument completers (DYNAMIC via reflection).
    .EXAMPLE
        Get-RegisteredCompleters | Measure-Object  # Total count
        Get-RegisteredCompleters | Where-Object { $_ -like 'dotnet*' }
    #>
    $ec = [System.Management.Automation.Runspaces.Runspace]::DefaultRunspace.GetType().GetProperty(
        'ExecutionContext', [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance
    ).GetValue([System.Management.Automation.Runspaces.Runspace]::DefaultRunspace)

    $nativeCompleters = $ec.GetType().GetProperty(
        'NativeArgumentCompleters', [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Instance
    ).GetValue($ec)

    $nativeCompleters.Keys | Sort-Object
}
#endregion

#region Summary
$Script:ProfileStart.Stop()
# Show-ProfileSummary  # Uncomment to see timing breakdown
# Write-Host "(Tab=psc, Ctrl+Space=fzf complete, Ctrl+R=fzf history, Ctrl+T=fzf files)" -ForegroundColor DarkGray
#endregion
