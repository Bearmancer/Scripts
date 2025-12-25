Set-StrictMode -Version Latest


$Script:RepositoryRoot = Split-Path -Path $PSScriptRoot -Parent
$Script:PythonToolkit = Join-Path -Path $RepositoryRoot -ChildPath 'python\toolkit\cli.py'
$Script:CSharpRoot = Join-Path -Path $RepositoryRoot -ChildPath 'csharp'
$Script:LogDirectory = Join-Path -Path $RepositoryRoot -ChildPath 'logs'

$Script:LoadedArgcCommands = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$Script:AvailableArgcCommands = $null
$Script:ArgcCompletionsRoot = if ($env:ARGC_COMPLETIONS_ROOT) {
    $env:ARGC_COMPLETIONS_ROOT
}
else {
    Join-Path -Path $env:USERPROFILE -ChildPath 'argc-completions'
}
$Script:ProfileStartTime = $null

function Write-Timing {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string]$Component
    )

    if ($null -eq $Script:ProfileStartTime) {
        $Script:ProfileStartTime = [System.Diagnostics.Stopwatch]::StartNew()
    }

    $elapsed = $Script:ProfileStartTime.ElapsedMilliseconds
    Write-Host "[$('{0,5}' -f $elapsed)ms] $Component" -ForegroundColor DarkGray
}

function Set-Utf8Console {
    [CmdletBinding()]
    param()

    [Console]::InputEncoding = [System.Text.Encoding]::UTF8
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    $Global:OutputEncoding = [System.Text.Encoding]::UTF8
    $Global:PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'
    $Global:PSDefaultParameterValues['*:Encoding'] = 'utf8'
    $env:PYTHONIOENCODING = 'utf-8'
    chcp 65001 | Out-Null
}

function Initialize-Completions {
    [CmdletBinding()]
    param(
        [Parameter()]
        [switch]$ShowTiming,

        [Parameter()]
        [string[]]$ArgcCommands = @('dotnet', 'winget')
    )

    if ($ShowTiming) {
        $Script:ProfileStartTime = [System.Diagnostics.Stopwatch]::StartNew()
        Write-Timing "Completion init start"
    }

    Set-PSReadLineOption -PredictionSource HistoryAndPlugin
    Set-PSReadLineOption -PredictionViewStyle ListView
    Set-PSReadLineOption -Colors @{ "Selection" = "`e[7m" }
    if ($ShowTiming) { Write-Timing "PSReadLine configured" }

    if (Get-Module -ListAvailable -Name PSFzf) {
        Import-Module PSFzf
        Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t'
        Set-PsFzfOption -PSReadlineChordReverseHistory 'Ctrl+r'
        Set-PSReadLineKeyHandler -Key 'Ctrl+Spacebar' -ScriptBlock { Invoke-FzfTabCompletion }
        if ($ShowTiming) { Write-Timing "PSFzf loaded" }
    }

    if (Get-Command -Name carapace -CommandType Application -ErrorAction SilentlyContinue) {
        $env:CARAPACE_BRIDGES = 'zsh,fish,bash,inshellisense'
        carapace _carapace | Out-String | Invoke-Expression
        if ($ShowTiming) { Write-Timing "Carapace loaded" }
    }

    if (Get-Command -Name argc -CommandType Application -ErrorAction SilentlyContinue) {
        $availableArgc = $ArgcCommands | Where-Object {
            Get-Command -Name $_ -CommandType Application -ErrorAction SilentlyContinue
        }
        if ($availableArgc) {
            argc --argc-completions powershell @availableArgc | Out-String | Invoke-Expression
            foreach ($cmd in $availableArgc) {
                $null = $Script:LoadedArgcCommands.Add($cmd)
            }
            if ($ShowTiming) { Write-Timing "argc loaded ($($availableArgc -join ', '))" }
        }
    }

    if ($ShowTiming) {
        $Script:ProfileStartTime.Stop()
        Write-Host "Completions initialized in $($Script:ProfileStartTime.ElapsedMilliseconds)ms" -ForegroundColor Cyan
    }
}

function Find-UnapprovedVerbs {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$ModuleName = 'ScriptsToolkit'
    )

    $approved = Get-Verb | Select-Object -ExpandProperty Verb
    $unapproved = Get-Command -Module $ModuleName | Where-Object {
        $_.CommandType -eq 'Function' -and $_.Verb -and $_.Verb -notin $approved
    }

    if ($unapproved) {
        Write-Host "`nUnapproved verbs in $ModuleName :" -ForegroundColor Yellow
        foreach ($cmd in $unapproved) {
            $suggestion = switch ($cmd.Verb) {
                'Create' { 'New' }
                'Delete' { 'Remove' }
                'Make' { 'New' }
                'Change' { 'Set' }
                'List' { 'Get' }
                'Print' { 'Write' }
                'Build' { 'New' }
                'Execute' { 'Invoke' }
                'Load' { 'Import' }
                default { '?' }
            }
            Write-Host "  $($cmd.Name) -> $suggestion-$($cmd.Noun)" -ForegroundColor Red
        }
    }
    else {
        Write-Host "All verbs in $ModuleName are approved" -ForegroundColor Green
    }

    return $unapproved
}

function Show-BinaryLocations {
    [CmdletBinding()]
    param()

    $binaries = @(
        @{ Name = 'argc'; Package = 'cargo install argc' }
        @{ Name = 'carapace'; Package = 'winget install carapace-sh.carapace-bin' }
        @{ Name = 'fzf'; Package = 'winget install junegunn.fzf' }
        @{ Name = 'dotnet'; Package = 'winget install Microsoft.DotNet.SDK.9' }
        @{ Name = 'python'; Package = 'winget install Python.Python.3.12' }
        @{ Name = 'ffprobe'; Package = 'winget install Gyan.FFmpeg' }
        @{ Name = 'yt-dlp'; Package = 'winget install yt-dlp.yt-dlp' }
    )

    Write-Host "`nBinary Locations" -ForegroundColor Cyan
    Write-Host ("─" * 80) -ForegroundColor DarkGray

    foreach ($bin in $binaries) {
        $cmd = Get-Command -Name $bin.Name -CommandType Application -ErrorAction SilentlyContinue
        if ($cmd) {
            Write-Host "  [✓] " -ForegroundColor Green -NoNewline
            Write-Host "$($bin.Name.PadRight(12))" -ForegroundColor White -NoNewline
            Write-Host $cmd.Source -ForegroundColor DarkGray
        }
        else {
            Write-Host "  [✗] " -ForegroundColor Red -NoNewline
            Write-Host "$($bin.Name.PadRight(12))" -ForegroundColor White -NoNewline
            Write-Host "Not found. Install: $($bin.Package)" -ForegroundColor Yellow
        }
    }

    Write-Host "`nEnvironment Paths:" -ForegroundColor Cyan
    Write-Host "  ARGC_COMPLETIONS_ROOT: $(if ($env:ARGC_COMPLETIONS_ROOT) { $env:ARGC_COMPLETIONS_ROOT } else { '[not set]' })" -ForegroundColor DarkGray
    Write-Host ("─" * 80) -ForegroundColor DarkGray
}

function Set-CompletionPaths {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$ArgcRoot = $env:ARGC_COMPLETIONS_ROOT
    )

    if (-not $ArgcRoot) {
        Write-Warning "ArgcRoot not specified and ARGC_COMPLETIONS_ROOT not set. Provide -ArgcRoot parameter."
        return
    }

    $env:ARGC_COMPLETIONS_ROOT = $ArgcRoot
    $env:ARGC_COMPLETIONS_PATH = "$ArgcRoot\completions\windows;$ArgcRoot\completions"

    $binPath = Join-Path $ArgcRoot 'bin'
    if ((Test-Path -LiteralPath $binPath) -and $env:PATH -notlike "*$binPath*") {
        $env:PATH = $binPath + [IO.Path]::PathSeparator + $env:PATH
    }

    Write-Host "[✓] Completion paths configured" -ForegroundColor Green
    Write-Host "    ARGC_COMPLETIONS_ROOT: $env:ARGC_COMPLETIONS_ROOT" -ForegroundColor DarkGray
}


function Get-ArgcManifest {
    [CmdletBinding()]
    [OutputType([string[]])]
    param()

    if ($null -ne $Script:AvailableArgcCommands) {
        return $Script:AvailableArgcCommands
    }

    $manifestUrl = 'https://raw.githubusercontent.com/sigoden/argc-completions/main/MANIFEST.md'
    $cacheFile = Join-Path -Path $Script:ArgcCompletionsRoot -ChildPath '.manifest-cache.txt'
    $cacheMaxAge = [TimeSpan]::FromHours(24)

    if ((Test-Path -LiteralPath $cacheFile) -and ((Get-Date) - (Get-Item -LiteralPath $cacheFile).LastWriteTime) -lt $cacheMaxAge) {
        $Script:AvailableArgcCommands = [System.IO.File]::ReadAllLines($cacheFile)
        return $Script:AvailableArgcCommands
    }

    try {
        Write-Verbose "Fetching argc manifest from GitHub..."
        $manifest = Invoke-RestMethod -Uri $manifestUrl -TimeoutSec 10
        $commands = [System.Collections.Generic.List[string]]::new()
        $regex = [regex]::new('^\- \[([^\]]+)\]', [System.Text.RegularExpressions.RegexOptions]::Multiline)
        foreach ($match in $regex.Matches($manifest)) {
            $commands.Add($match.Groups[1].Value)
        }
        $Script:AvailableArgcCommands = [string[]]$commands

        $cacheDir = Split-Path -Path $cacheFile -Parent
        if (-not (Test-Path -LiteralPath $cacheDir)) {
            New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null
        }
        [System.IO.File]::WriteAllLines($cacheFile, $Script:AvailableArgcCommands)
        Write-Verbose "Cached $($commands.Count) argc-supported commands"
    }
    catch {
        Write-Warning "Failed to fetch argc manifest: $_"
        $windowsDir = Join-Path $Script:ArgcCompletionsRoot 'completions\windows'
        $commonDir = Join-Path $Script:ArgcCompletionsRoot 'completions'
        $set = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)

        foreach ($dir in @($windowsDir, $commonDir)) {
            if ([System.IO.Directory]::Exists($dir)) {
                foreach ($file in [System.IO.Directory]::GetFiles($dir, '*.sh')) {
                    $null = $set.Add([System.IO.Path]::GetFileNameWithoutExtension($file))
                }
            }
        }
        $Script:AvailableArgcCommands = [string[]]$set
    }

    return $Script:AvailableArgcCommands
}

function Get-ArgcLoadedCommands {
    [CmdletBinding()]
    [OutputType([string[]])]
    param()

    return [string[]]$Script:LoadedArgcCommands
}

function Import-ArgcCompletions {
    [CmdletBinding()]
    [Alias('argc-load')]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline)]
        [string[]]$Commands
    )

    begin {
        $allCommands = [System.Collections.Generic.List[string]]::new()
    }

    process {
        $allCommands.AddRange($Commands)
    }

    end {
        $toLoad = [System.Collections.Generic.List[string]]::new()

        foreach ($cmd in $allCommands) {
            if ($Script:LoadedArgcCommands.Contains($cmd)) {
                Write-Verbose "Skipping $cmd - already loaded"
                continue
            }
            if (Get-Command -Name $cmd -CommandType Application -ErrorAction SilentlyContinue) {
                $toLoad.Add($cmd)
            }
            else {
                Write-Warning "Command not found: $cmd"
            }
        }

        if ($toLoad.Count -eq 0) {
            Write-Host "No new commands to load" -ForegroundColor Yellow
            return
        }

        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        argc --argc-completions powershell @toLoad | Out-String | Invoke-Expression

        foreach ($cmd in $toLoad) {
            $null = $Script:LoadedArgcCommands.Add($cmd)
        }

        $sw.Stop()
        Write-Host "[✓] Loaded $($toLoad.Count) argc completions in $($sw.ElapsedMilliseconds)ms: $($toLoad -join ', ')" -ForegroundColor Green
    }
}

function Select-ArgcCompletions {
    [CmdletBinding()]
    [Alias('argc-select')]
    param()

    $manifest = Get-ArgcManifest
    if (-not $manifest -or $manifest.Count -eq 0) {
        Write-Warning "No argc commands available"
        return
    }

    $selected = $manifest | fzf --multi --preview "argc --argc-completions powershell {} 2>&1 | Select-Object -First 20"
    if ($selected) {
        Import-ArgcCompletions -Commands $selected
    }
}

Register-ArgumentCompleter -CommandName 'Import-ArgcCompletions' -ParameterName 'Commands' -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)

    $manifest = Get-ArgcManifest
    $loaded = $Script:LoadedArgcCommands
    foreach ($cmd in $manifest) {
        if ($cmd -like "$wordToComplete*" -and -not $loaded.Contains($cmd)) {
            [System.Management.Automation.CompletionResult]::new($cmd, $cmd, 'ParameterValue', $cmd)
        }
    }
}


function Invoke-ToolkitPython {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string[]]$ArgumentList
    )

    $python = (Get-Command -Name python).Source
    $arguments = @($Script:PythonToolkit) + $ArgumentList
    & $python @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Python toolkit exited with code $LASTEXITCODE"
    }
}



function Get-ToolkitFunctions {
    <#
    .SYNOPSIS
        Lists all available functions in the ScriptsToolkit module.

    .DESCRIPTION
        Displays a categorized list of all functions exported by the ScriptsToolkit module,
        including their aliases and brief descriptions. Functions are grouped by category
        (Utilities, Logging, Filesystem, Video, Audio, Transcription, YouTube, Sync, Tasks).

    .EXAMPLE
        Get-ToolkitFunctions

        Lists all functions organized by category.

    .EXAMPLE
        tkfn

        Same as above using the alias.

    .OUTPUTS
        None. Writes formatted output to the console.

    .NOTES
        Alias: tkfn
    #>
    [CmdletBinding()]
    [Alias('tkfn')]
    param()

    $functions = @(
        @{ Category = 'Utilities'; Name = 'Get-ToolkitFunctions'; Alias = 'tkfn'; Description = 'List all toolkit functions' }
        @{ Category = 'Utilities'; Name = 'Open-CommandHistory'; Alias = 'hist'; Description = 'Open PowerShell history file' }
        @{ Category = 'Utilities'; Name = 'Show-ToolkitHelp'; Alias = 'tkhelp'; Description = 'Open toolkit documentation' }
        @{ Category = 'Utilities'; Name = 'Invoke-ToolkitAnalyzer'; Alias = 'tklint'; Description = 'Run PSScriptAnalyzer' }
        @{ Category = 'Completion'; Name = 'Get-ArgcManifest'; Alias = '-'; Description = 'Get all argc-supported commands' }
        @{ Category = 'Completion'; Name = 'Get-ArgcLoadedCommands'; Alias = '-'; Description = 'List loaded argc completions' }
        @{ Category = 'Completion'; Name = 'Import-ArgcCompletions'; Alias = 'argc-load'; Description = 'Lazy-load argc completions' }
        @{ Category = 'Completion'; Name = 'Select-ArgcCompletions'; Alias = 'argc-select'; Description = 'Select argc completions via fzf' }
        @{ Category = 'Completion'; Name = 'Initialize-Completions'; Alias = '-'; Description = 'Set up full completion stack' }
        @{ Category = 'Completion'; Name = 'Write-Timing'; Alias = '-'; Description = 'Log profile timing' }
        @{ Category = 'Completion'; Name = 'Set-Utf8Console'; Alias = '-'; Description = 'Force UTF-8 encoding' }
        @{ Category = 'Logs'; Name = 'Show-SyncLog'; Alias = 'synclog'; Description = 'View JSONL sync logs as table' }
        @{ Category = 'Sync'; Name = 'Invoke-YouTubeSync'; Alias = 'syncyt'; Description = 'Sync YouTube playlists' }
        @{ Category = 'Sync'; Name = 'Invoke-LastFmSync'; Alias = 'synclf'; Description = 'Sync Last.fm scrobbles' }
        @{ Category = 'Sync'; Name = 'Invoke-AllSyncs'; Alias = 'syncall'; Description = 'Run all daily syncs' }
        @{ Category = 'Filesystem'; Name = 'Get-Directories'; Alias = 'dirs'; Description = 'List directories with sizes' }
        @{ Category = 'Filesystem'; Name = 'Get-FilesAndDirectories'; Alias = 'tree'; Description = 'List all items with sizes' }
        @{ Category = 'Filesystem'; Name = 'New-Torrents'; Alias = 'torrent'; Description = 'Create .torrent files' }
        @{ Category = 'Video'; Name = 'Start-DiscRemux'; Alias = 'remux'; Description = 'Remux video discs to MKV' }
        @{ Category = 'Video'; Name = 'Start-BatchCompression'; Alias = 'compress'; Description = 'Compress videos in batch' }
        @{ Category = 'Video'; Name = 'Get-VideoChapters'; Alias = 'chapters'; Description = 'Extract chapter timestamps' }
        @{ Category = 'Video'; Name = 'Get-VideoResolution'; Alias = 'res'; Description = 'Report video resolutions' }
        @{ Category = 'Audio'; Name = 'Convert-Audio'; Alias = 'audio'; Description = 'Convert audio files' }
        @{ Category = 'Audio'; Name = 'Convert-ToMP3'; Alias = 'tomp3'; Description = 'Convert to MP3' }
        @{ Category = 'Audio'; Name = 'Convert-ToFLAC'; Alias = 'toflac'; Description = 'Convert to FLAC' }
        @{ Category = 'Audio'; Name = 'Convert-SACD'; Alias = 'sacd'; Description = 'Extract SACD ISO files' }
        @{ Category = 'Audio'; Name = 'Rename-MusicFiles'; Alias = 'rename'; Description = 'Rename files using RED naming' }
        @{ Category = 'Audio'; Name = 'Get-EmbeddedImageSize'; Alias = 'artsize'; Description = 'Report embedded art sizes' }
        @{ Category = 'Audio'; Name = 'Invoke-Propolis'; Alias = 'propolis'; Description = 'Run Propolis analyzer' }
        @{ Category = 'Transcription'; Name = 'Invoke-Whisper'; Alias = 'whisp'; Description = 'Transcribe file or folder' }
        @{ Category = 'Transcription'; Name = 'Invoke-WhisperFolder'; Alias = 'wpf'; Description = 'Transcribe folder (explicit)' }
        @{ Category = 'Transcription'; Name = 'Invoke-WhisperJapanese'; Alias = 'wpj'; Description = 'Transcribe Japanese file/folder' }
        @{ Category = 'Transcription'; Name = 'Invoke-WhisperJapaneseFolder'; Alias = 'wpjf'; Description = 'Transcribe Japanese folder (explicit)' }
        @{ Category = 'YouTube'; Name = 'Save-YouTubeVideo'; Alias = 'ytdl'; Description = 'Download YouTube videos' }
        @{ Category = 'Tasks'; Name = 'Register-ScheduledSyncTask'; Alias = 'regtask'; Description = 'Create scheduled task' }
        @{ Category = 'Tasks'; Name = 'Register-AllSyncTasks'; Alias = 'regall'; Description = 'Register all sync tasks' }
    )

    Write-Host "`nScriptsToolkit Functions" -ForegroundColor Cyan
    Write-Host "========================`n" -ForegroundColor Cyan

    $functions | Group-Object -Property Category | ForEach-Object {
        Write-Host "$( $_.Name )" -ForegroundColor Yellow
        $_.Group | ForEach-Object {
            Write-Host "  $($_.Alias.PadRight(10) )" -ForegroundColor Green -NoNewline
            Write-Host "$($_.Name.PadRight(30) )" -ForegroundColor White -NoNewline
            Write-Host "$( $_.Description )" -ForegroundColor DarkGray
        }
        Write-Host ""
    }
}

function Open-CommandHistory {
    <#
    .SYNOPSIS
        Opens the PowerShell command history file in VS Code.

    .DESCRIPTION
        Opens the PSReadLine command history file located at
        %APPDATA%\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt
        in Visual Studio Code for viewing or searching past commands.

    .EXAMPLE
        Open-CommandHistory

        Opens the history file in VS Code.

    .EXAMPLE
        hist

        Same as above using the alias.

    .NOTES
        Alias: hist
        Requires VS Code to be installed and available in PATH.
    #>
    [CmdletBinding()]
    [Alias('hist')]
    param()

    & code "$env:APPDATA\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt"
}

function Show-ToolkitHelp {
    <#
    .SYNOPSIS
        Opens the ScriptsToolkit help documentation file.

    .DESCRIPTION
        Opens the ScriptsToolkit.Help.md file in Visual Studio Code, which contains
        comprehensive documentation for all toolkit functions and usage examples.

    .EXAMPLE
        Show-ToolkitHelp

        Opens the help documentation in VS Code.

    .EXAMPLE
        tkhelp

        Same as above using the alias.

    .NOTES
        Alias: tkhelp
        Requires VS Code to be installed and available in PATH.
    #>
    [CmdletBinding()]
    [Alias('tkhelp')]
    param()

    $helpFile = Join-Path -Path $PSScriptRoot -ChildPath 'ScriptsToolkit.Help.md'
    & code $helpFile
}

function Invoke-ToolkitAnalyzer {
    <#
    .SYNOPSIS
        Runs PSScriptAnalyzer on toolkit scripts.

    .DESCRIPTION
        Invokes PSScriptAnalyzer with the toolkit's custom settings file to check
        for errors and warnings in PowerShell scripts. Uses the settings defined
        in PSScriptAnalyzerSettings.psd1.

    .PARAMETER Path
        Path to the file or directory to analyze. Defaults to the toolkit directory.

    .EXAMPLE
        Invoke-ToolkitAnalyzer

        Analyzes all scripts in the toolkit directory.

    .EXAMPLE
        tklint -Path .\MyScript.ps1

        Analyzes a specific script file.

    .EXAMPLE
        Invoke-ToolkitAnalyzer -Path C:\Scripts

        Analyzes all scripts in the specified directory.

    .NOTES
        Alias: tklint
        Requires PSScriptAnalyzer module to be installed.
    #>
    [CmdletBinding()]
    [Alias('tklint')]
    param(
        [Parameter(Position = 0)]
        [Alias('p')]
        [string]$Path = (Join-Path -Path $PSScriptRoot -ChildPath '.')
    )

    $settings = Join-Path -Path $PSScriptRoot -ChildPath 'PSScriptAnalyzerSettings.psd1'
    Invoke-ScriptAnalyzer -Path $Path -Settings $settings -Recurse -Severity Error, Warning
}



function Show-SyncLog {
    <#
    .SYNOPSIS
        View sync session logs with session-based grouping and status summaries.

    .DESCRIPTION
        Displays sync log entries from YouTube and LastFm services.
        Default view shows session summaries (last N sessions).
        Use -Entries to see individual log entries.
        Use -Detailed to include verbose events like PlaylistUpdated/ScrobblesProcessed.

    .PARAMETER Service
        Filter by service: 'youtube', 'lastfm', or 'all' (default).

    .PARAMETER Sessions
        Number of recent sessions to display (default: 10).

    .PARAMETER Entries
        Switch to entry-based view instead of session summaries.

    .PARAMETER Tail
        Number of entries to show when using -Entries (default: 20).

    .PARAMETER SessionId
        Filter to a specific session ID. Use this to investigate a specific sync run.

    .PARAMETER Detailed
        Include verbose events (PlaylistUpdated, PlaylistCreated, ScrobblesProcessed).
        By default these are hidden to reduce noise.

    .PARAMETER Full
        Show full details without truncation.

    .EXAMPLE
        synclog
        Shows last 10 session summaries for all services.

    .EXAMPLE
        Show-SyncLog -Service youtube -Sessions 5
        Shows last 5 YouTube sync sessions.

    .EXAMPLE
        synclog -Entries -Tail 50
        Shows last 50 individual log entries across all services.

    .EXAMPLE
        synclog -SessionId '2fd1b74d' -Detailed
        Shows all entries for a specific session, including verbose events.

    .EXAMPLE
        synclog -Entries -Detailed -Full
        Shows detailed log entries without truncation - useful for debugging.

    .NOTES
        Log files: logs/youtube.jsonl, logs/lastfm.jsonl

        Default view filters out noise (PlaylistUpdated with 0 changes, etc.)
        Use -Detailed to see everything for investigating issues.

        Session statuses:
        - Completed: Normal finish
        - Failed: Ended with exception
        - Interrupted: User cancelled (Ctrl+C)
        - Crashed: Session never closed (detected on next run)
        - Running: Started recently, no end event yet
    #>
    [CmdletBinding(DefaultParameterSetName = 'Sessions')]
    [Alias('viewlog', 'synclog')]
    param(
        [Parameter()]
        [ValidateSet('youtube', 'lastfm', 'all')]
        [string]$Service = 'all',

        [Parameter(ParameterSetName = 'Sessions')]
        [Alias('n')]
        [int]$Sessions = 10,

        [Parameter(ParameterSetName = 'Entries')]
        [Alias('e')]
        [switch]$Entries,

        [Parameter(ParameterSetName = 'Entries')]
        [Alias('Last')]
        [int]$Tail = 20,

        [Parameter()]
        [string]$SessionId,

        [Parameter()]
        [Alias('d')]
        [switch]$Detailed,

        [Parameter()]
        [Alias('f')]
        [switch]$Full
    )

    $logFiles = @()
    $ytLog = Join-Path -Path $Script:LogDirectory -ChildPath 'youtube.jsonl'
    $lfmLog = Join-Path -Path $Script:LogDirectory -ChildPath 'lastfm.jsonl'

    if ($Service -in 'all', 'youtube' -and (Test-Path $ytLog)) {
        $logFiles += $ytLog
    }
    if ($Service -in 'all', 'lastfm' -and (Test-Path $lfmLog)) {
        $logFiles += $lfmLog
    }

    if ($logFiles.Count -eq 0) {
        Write-Warning "No log files found in $Script:LogDirectory"
        return
    }

    $allEntries = @()
    foreach ($logFile in $logFiles) {
        $serviceName = [System.IO.Path]::GetFileNameWithoutExtension($logFile)
        foreach ($line in [System.IO.File]::ReadLines($logFile)) {
            if ( [string]::IsNullOrWhiteSpace($line)) {
                continue
            }
            try {
                $obj = $line | ConvertFrom-Json
                $obj | Add-Member -NotePropertyName 'Source' -NotePropertyValue $serviceName -Force
                $parsed = [datetime]::ParseExact($obj.Timestamp, 'yyyy/MM/dd HH:mm:ss', [System.Globalization.CultureInfo]::InvariantCulture)
                $obj | Add-Member -NotePropertyName 'ParsedTimestamp' -NotePropertyValue $parsed -Force
                $allEntries += $obj
            }
            catch {
                Write-Warning "Failed to parse log line in ${logFile}: $_"
            }
        }
    }

    if ($SessionId) {
        $allEntries = $allEntries | Where-Object { $_.SessionId -eq $SessionId }
    }

    if ($PSCmdlet.ParameterSetName -eq 'Entries' -or $Entries) {
        Show-SyncLogEntries -Entries $allEntries -Tail $Tail -Full:$Full -Detailed:$Detailed
    }
    else {
        Show-SyncLogSessions -Entries $allEntries -MaxSessions $Sessions -Full:$Full
    }
}

function Show-SyncLogSessions {
    param(
        [Parameter(Mandatory)]
        [array]$Entries,
        [int]$MaxSessions = 10,
        [switch]$Full
    )

    $sessionData = @{ }
    foreach ($entry in $Entries) {
        $sid = $entry.SessionId
        if (-not $sid) {
            continue
        }

        if (-not $sessionData.ContainsKey($sid)) {
            $sessionData[$sid] = @{
                SessionId  = $sid
                Source     = $entry.Source
                StartTime  = $null
                EndTime    = $null
                Status     = 'Unknown'
                EventCount = 0
                Summary    = ''
                HasError   = $false
            }
        }

        $sessionData[$sid].EventCount++

        switch ($entry.Event) {
            'SessionStart' {
                $sessionData[$sid].StartTime = $entry.ParsedTimestamp
            }
            'SessionEnd' {
                $sessionData[$sid].EndTime = $entry.ParsedTimestamp
                $sessionData[$sid].Status = $entry.Data.Status ?? 'Completed'
                $sessionData[$sid].Summary = $entry.Data.Summary ?? ''
            }
            'SessionInterrupted' {
                $sessionData[$sid].EndTime = $entry.ParsedTimestamp
                $sessionData[$sid].Status = 'Interrupted'
                $sessionData[$sid].Summary = $entry.Data.Progress ?? ''
            }
            'SessionCrashed' {
                $sessionData[$sid].Status = 'Crashed'
                $sessionData[$sid].Summary = "Detected at $( $entry.Data.DetectedAt )"
            }
            'Exception' {
                $sessionData[$sid].HasError = $true
            }
        }
    }

    $sessionList = $sessionData.Values | ForEach-Object { [PSCustomObject]$_ } |
    Where-Object { $_.StartTime } |
    Sort-Object -Property StartTime -Descending |
    Select-Object -First $MaxSessions |
    Sort-Object -Property StartTime

    if (-not $sessionList -or @($sessionList).Count -eq 0) {
        Write-Host "`nNo sessions found." -ForegroundColor Yellow
        return
    }

    $statusColors = @{
        'Completed'   = 'Green'
        'Failed'      = 'Red'
        'Interrupted' = 'Yellow'
        'Crashed'     = 'Magenta'
        'Running'     = 'Cyan'
        'Unknown'     = 'DarkGray'
    }

    $terminalWidth = $Host.UI.RawUI.WindowSize.Width
    $now = Get-Date

    $serviceNames = @{
        'youtube' = 'YouTube'
        'lastfm'  = 'Last.fm'
    }

    Write-Host ""
    Write-Host "Session History" -ForegroundColor Cyan
    Write-Host ("─" * $terminalWidth) -ForegroundColor DarkGray

    $headerFormat = "{0,-19} {1,-8} {2,-8} {3,-12} {4,-6} {5}"
    Write-Host ($headerFormat -f "DateTime", "Service", "Duration", "Status", "Events", "Summary") -ForegroundColor White
    Write-Host ("─" * $terminalWidth) -ForegroundColor DarkGray

    foreach ($s in $sessionList) {
        $dateTime = $s.StartTime.ToString('yyyy/MM/dd HH:mm:ss')
        $serviceName = $serviceNames[$s.Source] ?? $s.Source

        $duration = if ($s.EndTime) {
            $span = $s.EndTime - $s.StartTime
            "{0}s" -f [int]$span.TotalSeconds
        }
        else {
            $span = $now - $s.StartTime
            "~{0}s" -f [int]$span.TotalSeconds
        }

        $status = if (-not $s.EndTime -and ($now - $s.StartTime).TotalHours -lt 2) {
            'Running'
        }
        elseif (-not $s.EndTime) {
            'Crashed'
        }
        elseif ($s.HasError) {
            'Failed'
        }
        else {
            $s.Status
        }

        $color = $statusColors[$status] ?? 'White'
        $summary = if ($Full) {
            $s.Summary
        }
        else {
            if ($s.Summary.Length -gt 40) {
                $s.Summary.Substring(0, 37) + "..."
            }
            else {
                $s.Summary
            }
        }

        Write-Host ($headerFormat -f $dateTime, $serviceName, $duration, $status, $s.EventCount, $summary) -ForegroundColor $color
    }

    Write-Host ("─" * $terminalWidth) -ForegroundColor DarkGray

    $taskInfo = Get-ScheduledTask -TaskName "*Sync" -ErrorAction SilentlyContinue |
    Where-Object { $_.TaskName -in 'YouTubeSync', 'LastFmSync' } |
    Get-ScheduledTaskInfo -ErrorAction SilentlyContinue

    if ($taskInfo) {
        Write-Host ""
        Write-Host "Scheduled Tasks" -ForegroundColor Cyan
        Write-Host ("─" * $terminalWidth) -ForegroundColor DarkGray
        foreach ($task in $taskInfo) {
            $lastRun = if ($task.LastRunTime -and $task.LastRunTime.Year -gt 2000) {
                $task.LastRunTime.ToString('yyyy/MM/dd HH:mm:ss')
            }
            else {
                'Never'
            }
            $nextRun = if ($task.NextRunTime) {
                $task.NextRunTime.ToString('yyyy/MM/dd HH:mm:ss')
            }
            else {
                'Not scheduled'
            }
            $resultCode = switch ($task.LastTaskResult) {
                0 {
                    'Success'
                }
                1 {
                    'Incorrect function'
                }
                267009 {
                    'Task running'
                }
                267011 {
                    'Task never run'
                }
                default {
                    "Code: $( $task.LastTaskResult )"
                }
            }
            $resultColor = if ($task.LastTaskResult -eq 0) {
                'Green'
            }
            elseif ($task.LastTaskResult -eq 267009) {
                'Cyan'
            }
            else {
                'Yellow'
            }

            Write-Host "  $($task.TaskName.PadRight(15) )" -ForegroundColor White -NoNewline
            Write-Host "Last: $lastRun  " -ForegroundColor DarkGray -NoNewline
            Write-Host "Next: $nextRun  " -ForegroundColor DarkGray -NoNewline
            Write-Host "Result: $resultCode" -ForegroundColor $resultColor
        }
        Write-Host ("─" * $terminalWidth) -ForegroundColor DarkGray
    }
}

function Show-SyncLogEntries {
    param(
        [Parameter(Mandatory)]
        [array]$Entries,
        [int]$Tail = 20,
        [switch]$Full,
        [switch]$Detailed
    )

    if (-not $Detailed) {
        # Filter out verbose events that create noise in default view
        $verboseEvents = @('PlaylistCreated', 'ScrobblesProcessed')
        $Entries = $Entries | Where-Object { $_.Event -notin $verboseEvents }

        # Also filter out PlaylistUpdated/PlaylistDeleted/PlaylistRenamed with no actual changes
        $Entries = $Entries | Where-Object {
            if ($_.Event -eq 'PlaylistUpdated') {
                # Only show if something actually changed
                $added = $_.Data.Added ?? 0
                $removed = $_.Data.Removed ?? 0
                return ($added -gt 0 -or $removed -gt 0)
            }
            return $true
        }
    }

    $result = $Entries | Sort-Object -Property ParsedTimestamp | Select-Object -Last $Tail

    $levelColors = @{
        'Debug'   = 'DarkGray'
        'Info'    = 'Cyan'
        'Success' = 'Green'
        'Warning' = 'Yellow'
        'Error'   = 'Red'
        'Fatal'   = 'Magenta'
    }

    $terminalWidth = $Host.UI.RawUI.WindowSize.Width

    Write-Host ""
    $headerFormat = "{0,-20} {1,-8} {2,-8} {3,-20} {4,-10} {5}"
    Write-Host ($headerFormat -f "Timestamp", "Service", "Level", "Event", "Session", "Details") -ForegroundColor White
    Write-Host ("─" * $terminalWidth) -ForegroundColor DarkGray

    foreach ($entry in $result) {
        $color = $levelColors[$entry.Level] ?? 'White'

        $details = if ($entry.Data) {
            if ($entry.Data.PSObject.Properties['Text']) {
                $entry.Data.Text
            }
            else {
                ($entry.Data.PSObject.Properties | Where-Object { $_.Name -notin 'Service', 'ProcessId' } | ForEach-Object {
                    $val = if ($_.Value -is [array]) {
                        $_.Value -join ", "
                    }
                    else {
                        $_.Value
                    }
                    "$( $_.Name ): $val"
                }) -join " | "
            }
        }
        else {
            ''
        }

        $maxDetails = if ($Full) {
            $terminalWidth - 70
        }
        else {
            50
        }
        if ($details.Length -gt $maxDetails) {
            $details = $details.Substring(0, $maxDetails - 3) + "..."
        }

        $sid = if ($entry.SessionId) {
            $entry.SessionId.Substring(0, [Math]::Min(8, $entry.SessionId.Length))
        }
        else {
            ''
        }

        Write-Host ($headerFormat -f $entry.Timestamp, $entry.Source, $entry.Level, $entry.Event, $sid, $details) -ForegroundColor $color
    }

    Write-Host ("─" * $terminalWidth) -ForegroundColor DarkGray
}

function Get-Directories {
    <#
    .SYNOPSIS
        Displays directory tree with sizes.

    .DESCRIPTION
        Shows a tree view of subdirectories with their sizes.
        Useful for finding large folders.

    .PARAMETER Directory
        Root directory to analyze. Defaults to current directory.

    .PARAMETER Sort
        Sort order: 'size' (largest first) or 'name' (alphabetical).

    .EXAMPLE
        dirs
        Shows directory tree for current folder sorted by size.

    .EXAMPLE
        Get-Directories -Directory D:\Media -Sort name
        Shows directory tree for D:\Media sorted alphabetically.
    #>
    [CmdletBinding()]
    [Alias('dirs')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [Parameter()]
        [Alias('s')]
        [ValidateSet('size', 'name')]
        [string]$Sort = 'size'
    )

    Invoke-ToolkitPython -ArgumentList @('filesystem', 'tree', '--directory', $Directory.FullName, '--sort', $Sort)
}

function Get-FilesAndDirectories {
    <#
    .SYNOPSIS
        Displays full file and directory tree with sizes.

    .DESCRIPTION
        Shows a tree view of all files and subdirectories with their sizes.
        More detailed than Get-Directories.

    .PARAMETER Directory
        Root directory to analyze. Defaults to current directory.

    .PARAMETER Sort
        Sort order: 'size' (largest first) or 'name' (alphabetical).

    .EXAMPLE
        tree
        Shows full tree for current folder sorted by size.

    .EXAMPLE
        Get-FilesAndDirectories -Directory D:\Projects -Sort name
        Shows full tree for D:\Projects sorted alphabetically.
    #>
    [CmdletBinding()]
    [Alias('tree')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [Parameter()]
        [Alias('s')]
        [ValidateSet('size', 'name')]
        [string]$Sort = 'size'
    )

    Invoke-ToolkitPython -ArgumentList @('filesystem', 'tree', '--directory', $Directory.FullName, '--sort', $Sort, '--include-files')
}

function New-Torrents {
    <#
    .SYNOPSIS
        Creates .torrent files for directories.

    .DESCRIPTION
        Generates .torrent files for each subdirectory in the specified path.
        Useful for batch torrent creation.

    .PARAMETER Directory
        Directory containing folders to create torrents for. Defaults to current directory.

    .PARAMETER IncludeSubdirectories
        If specified, recursively processes subdirectories.

    .EXAMPLE
        torrent
        Creates torrents for each folder in current directory.

    .EXAMPLE
        New-Torrents -Directory D:\Uploads -IncludeSubdirectories
        Creates torrents recursively for all folders in D:\Uploads.
    #>
    [CmdletBinding()]
    [Alias('torrent')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [Parameter()]
        [Alias('r')]
        [switch]$IncludeSubdirectories
    )

    $arguments = @('filesystem', 'torrents', '--directory', $Directory.FullName)
    if ($IncludeSubdirectories.IsPresent) {
        $arguments += '--include-subdirectories'
    }
    Invoke-ToolkitPython -ArgumentList $arguments
}



function Start-DiscRemux {
    <#
    .SYNOPSIS
        Remuxes video disc folders to MKV files.

    .DESCRIPTION
        Converts Blu-ray/DVD disc folder structures to single MKV files.
        Preserves all streams and metadata.

    .PARAMETER Directory
        Directory containing disc folders to remux. Defaults to current directory.

    .PARAMETER SkipMediaInfo
        If specified, skips generating MediaInfo reports.

    .EXAMPLE
        remux
        Remuxes all disc folders in current directory.

    .EXAMPLE
        Start-DiscRemux -Directory D:\Rips -SkipMediaInfo
        Remuxes disc folders in D:\Rips without MediaInfo.
    #>
    [CmdletBinding()]
    [Alias('remux')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [Parameter()]
        [switch]$SkipMediaInfo
    )

    $arguments = @('video', 'remux', '--path', $Directory.FullName)
    if ($SkipMediaInfo.IsPresent) {
        $arguments += '--skip-mediainfo'
    }
    Invoke-ToolkitPython -ArgumentList $arguments
}

function Start-BatchCompression {
    <#
    .SYNOPSIS
        Compresses video files in batch.

    .DESCRIPTION
        Re-encodes video files to reduce file size while maintaining quality.
        Uses optimized encoding settings.

    .PARAMETER Directory
        Directory containing videos to compress. Defaults to current directory.

    .EXAMPLE
        compress
        Compresses all videos in current directory.

    .EXAMPLE
        Start-BatchCompression -Directory D:\Videos
        Compresses all videos in D:\Videos.
    #>
    [CmdletBinding()]
    [Alias('compress')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('video', 'compress', '--directory', $Directory.FullName)
}

function Get-VideoChapters {
    <#
    .SYNOPSIS
        Extracts chapter timestamps from video files.

    .DESCRIPTION
        Reads chapter markers from MKV/MP4 files and displays them.
        Useful for verifying chapter accuracy.

    .PARAMETER Directory
        Directory containing video files. Defaults to current directory.

    .EXAMPLE
        chapters
        Shows chapters for videos in current directory.

    .EXAMPLE
        Get-VideoChapters -Directory D:\Movies
        Shows chapters for videos in D:\Movies.
    #>
    [CmdletBinding()]
    [Alias('chapters')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('video', 'chapters', '--path', $Directory.FullName)
}

function Get-VideoResolution {
    <#
    .SYNOPSIS
        Reports resolution of video files.

    .DESCRIPTION
        Scans video files and displays their resolution (width x height).
        Helps identify SD/HD/4K content.

    .PARAMETER Directory
        Directory containing video files. Defaults to current directory.

    .EXAMPLE
        res
        Shows resolution for videos in current directory.

    .EXAMPLE
        Get-VideoResolution -Directory D:\Movies
        Shows resolution for videos in D:\Movies.
    #>
    [CmdletBinding()]
    [Alias('res')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('video', 'resolutions', '--path', $Directory.FullName)
}



function Convert-Audio {
    <#
    .SYNOPSIS
        Converts audio files to various formats.

    .DESCRIPTION
        Converts audio files between formats (FLAC, MP3, 24-bit).
        Processes all audio files in the specified directory.

    .PARAMETER Directory
        Directory containing audio files. Defaults to current directory.

    .PARAMETER Format
        Target format: '24-bit', 'flac', 'mp3', or 'all'.

    .EXAMPLE
        audio
        Converts audio in current directory to all formats.

    .EXAMPLE
        Convert-Audio -Directory D:\Music -Format mp3
        Converts all audio in D:\Music to MP3.
    #>
    [CmdletBinding()]
    [Alias('audio')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [Parameter()]
        [Alias('fmt')]
        [ValidateSet('24-bit', 'flac', 'mp3', 'all')]
        [string]$Format = 'all'
    )

    Invoke-ToolkitPython -ArgumentList @('audio', 'convert', '--directory', $Directory.FullName, '--mode', 'convert', '--format', $Format)
}

function Convert-ToMP3 {
    <#
    .SYNOPSIS
        Converts audio files to MP3 format.

    .DESCRIPTION
        Shortcut for Convert-Audio -Format mp3.

    .PARAMETER Directory
        Directory containing audio files. Defaults to current directory.

    .EXAMPLE
        tomp3
        Converts all audio in current directory to MP3.
    #>
    [CmdletBinding()]
    [Alias('tomp3')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Convert-Audio -Directory $Directory -Format 'mp3'
}

function Convert-ToFLAC {
    <#
    .SYNOPSIS
        Converts audio files to FLAC format.

    .DESCRIPTION
        Shortcut for Convert-Audio -Format flac.

    .PARAMETER Directory
        Directory containing audio files. Defaults to current directory.

    .EXAMPLE
        toflac
        Converts all audio in current directory to FLAC.
    #>
    [CmdletBinding()]
    [Alias('toflac')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Convert-Audio -Directory $Directory -Format 'flac'
}

function Convert-SACD {
    <#
    .SYNOPSIS
        Extracts audio from SACD ISO files.

    .DESCRIPTION
        Extracts DSD audio from SACD ISO images and converts to specified format.

    .PARAMETER Directory
        Directory containing SACD ISO files. Defaults to current directory.

    .PARAMETER Format
        Output format: '24-bit', 'flac', 'mp3', or 'all'.

    .EXAMPLE
        sacd
        Extracts SACD content in current directory to all formats.

    .EXAMPLE
        Convert-SACD -Directory D:\SACDs -Format flac
        Extracts SACD ISOs in D:\SACDs to FLAC.
    #>
    [CmdletBinding()]
    [Alias('sacd')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [Parameter()]
        [Alias('fmt')]
        [ValidateSet('24-bit', 'flac', 'mp3', 'all')]
        [string]$Format = 'all'
    )

    Invoke-ToolkitPython -ArgumentList @('audio', 'convert', '--directory', $Directory.FullName, '--mode', 'extract', '--format', $Format)
}

function Rename-MusicFiles {
    <#
    .SYNOPSIS
        Renames music files based on metadata tags.

    .DESCRIPTION
        Renames audio files using embedded metadata (artist, title, track number).
        Standardizes file naming across a music collection.

    .PARAMETER Directory
        Directory containing music files. Defaults to current directory.

    .EXAMPLE
        rename
        Renames music files in current directory.

    .EXAMPLE
        Rename-MusicFiles -Directory D:\Music\Unsorted
        Renames music files in D:\Music\Unsorted.
    #>
    [CmdletBinding()]
    [Alias('rename')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('audio', 'rename', '--directory', $Directory.FullName)
}

function Get-EmbeddedImageSize {
    <#
    .SYNOPSIS
        Reports embedded album art sizes in audio files.

    .DESCRIPTION
        Scans audio files and reports the resolution of embedded cover art.
        Helps identify files with missing or low-quality artwork.

    .PARAMETER Directory
        Directory containing audio files. Defaults to current directory.

    .EXAMPLE
        artsize
        Shows album art sizes for audio in current directory.

    .EXAMPLE
        Get-EmbeddedImageSize -Directory D:\Music\FLAC
        Shows album art sizes for audio in D:\Music\FLAC.
    #>
    [CmdletBinding()]
    [Alias('artsize')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('audio', 'art-report', '--directory', $Directory.FullName)
}

function Invoke-Propolis {
    <#
    .SYNOPSIS
        Runs Propolis audio file analyzer.

    .DESCRIPTION
        Executes Propolis to analyze audio files for quality issues.
        Uses the --no-specs flag for streamlined output.

    .PARAMETER Directory
        Directory to analyze. Defaults to current directory.

    .EXAMPLE
        propolis
        Analyzes audio in current directory.

    .EXAMPLE
        Invoke-Propolis -Directory D:\Music\NewRips
        Analyzes audio in D:\Music\NewRips.
    #>
    [CmdletBinding()]
    [Alias('propolis')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    & "$env:LOCALAPPDATA\Personal\Propolis\propolis_windows.exe" --no-specs $Directory.FullName
}



function Invoke-Whisper {
    <#
    .SYNOPSIS
        Transcribes audio/video files using Whisper AI.

    .DESCRIPTION
        Uses whisper-ctranslate2 to generate SRT subtitles from media files.
        Supports multiple models, languages, and translation.

    .PARAMETER Path
        Path to audio/video file or directory to transcribe.

    .PARAMETER Language
        Source language code (e.g., 'en', 'ja'). Auto-detected if not specified.

    .PARAMETER Model
        Whisper model to use. Options include tiny, base, small, medium, large-v3, turbo, distil variants.

    .PARAMETER Translate
        If specified, translates non-English audio to English.

    .PARAMETER Force
        If specified, overwrites existing SRT files.

    .PARAMETER OutputDir
        Directory for output files. Defaults to current directory.

    .PARAMETER Batched
        If specified, uses batched inference for faster processing.

    .PARAMETER BatchSize
        Batch size for batched inference. Default: 4.

    .PARAMETER NoVadFilter
        If specified, disables Voice Activity Detection filter.

    .PARAMETER RepetitionPenalty
        Penalty for repeated tokens. Default: 1.1.

    .PARAMETER ExtraArgs
        Additional arguments to pass to whisper-ctranslate2.

    .EXAMPLE
        whisp video.mp4
        Transcribes video.mp4 with auto-detected language.

    .EXAMPLE
        Invoke-Whisper -Path lecture.mp3 -Language en -Model turbo
        Transcribes lecture.mp3 in English using turbo model.

    .EXAMPLE
        whisp interview.mp4 -Translate
        Transcribes and translates non-English audio to English.
    #>
    [CmdletBinding()]
    [Alias('whisp')]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('FilePath', 'FullName')]
        [string]$Path,

        [Parameter()]
        [Alias('l')]
        [string]$Language = 'en',

        [Parameter()]
        [Alias('m')]
        [ValidateSet('tiny', 'tiny.en', 'base', 'base.en', 'small', 'small.en', 'medium', 'medium.en', 'large-v1', 'large-v2', 'large-v3', 'large-v3-turbo', 'turbo', 'distil-large-v2', 'distil-large-v3', 'distil-large-v3.5', 'distil-medium.en', 'distil-small.en')]
        [string]$Model = 'distil-large-v3.5',

        [Parameter()]
        [Alias('t')]
        [switch]$Translate,

        [Parameter()]
        [Alias('f')]
        [switch]$Force,

        [Parameter()]
        [Alias('o')]
        [string]$OutputDir = (Get-Location).Path,

        [Parameter()]
        [Alias('b')]
        [switch]$Batched,

        [Parameter()]
        [int]$BatchSize = 4,

        [Parameter()]
        [switch]$NoVadFilter,

        [Parameter()]
        [Alias('rp')]
        [double]$RepetitionPenalty = 1.1,

        [Parameter(ValueFromRemainingArguments)]
        [string[]]$ExtraArgs
    )

    try {
        $null = Get-Command -Name whisper-ctranslate2 -ErrorAction Stop
    }
    catch {
        throw "whisper-ctranslate2 is not installed or not in PATH. Install it before running whisp."
    }

    $item = Get-Item -Path $Path
    if ($item.PSIsContainer) {
        Invoke-WhisperFolder -Directory $item -Language $Language -Model $Model -Translate:$Translate -Force:$Force -OutputDir $OutputDir -Batched:$Batched -BatchSize $BatchSize -NoVadFilter:$NoVadFilter -RepetitionPenalty $RepetitionPenalty -ExtraArgs $ExtraArgs
        return
    }

    $srtPath = Join-Path $OutputDir ($item.BaseName + '.srt')

    if ((Test-Path $srtPath) -and -not $Force) {
        Write-Host "[$( Get-Date -Format 'HH:mm:ss' )] Skipped: $( $item.Name ) (SRT exists)" -ForegroundColor Yellow
        return
    }

    $effectiveLanguage = if ($Language -eq 'auto') {
        $null
    }
    else {
        $Language
    }
    $languageDisplay = $effectiveLanguage ? $effectiveLanguage : '(auto-detect)'

    Write-Host "[$( Get-Date -Format 'HH:mm:ss' )] Transcribing: $( $item.Name )" -ForegroundColor Cyan
    Write-Host "             Model: $Model | Language: $languageDisplay" -ForegroundColor DarkGray
    Write-Host "             Legend: % | Bar | Processed/Total Audio [Elapsed<Remaining, Rate]" -ForegroundColor DarkGray
    Write-Host "             Note: If the model is missing, a download will begin (no output is suppressed)." -ForegroundColor DarkGray

    $whisperArgs = @(
        '--model', $Model
        '--compute_type', 'auto'
        '--output_format', 'srt'
        '--output_dir', $OutputDir
        '--verbose', 'False'
        '--repetition_penalty', $RepetitionPenalty.ToString()
    )

    if (-not $NoVadFilter) {
        $whisperArgs += '--vad_filter', 'True'
        $whisperArgs += '--vad_min_silence_duration_ms', '500'
    }

    if ($Batched) {
        $whisperArgs += '--batched', 'True'
        $whisperArgs += '--batch_size', $BatchSize.ToString()
    }

    if ($effectiveLanguage) {
        $whisperArgs += '--language', $effectiveLanguage
    }

    if ($Translate) {
        $whisperArgs += '--task', 'translate'
    }

    if ($ExtraArgs) {
        $whisperArgs += $ExtraArgs
    }

    $whisperArgs += $item.FullName

    & whisper-ctranslate2 @whisperArgs

    Write-Host "[$( Get-Date -Format 'HH:mm:ss' )] Completed: $( $item.Name )" -ForegroundColor Green
}

function Invoke-WhisperFolder {
    <#
    .SYNOPSIS
        Transcribes all audio/video files in a folder.

    .DESCRIPTION
        Batch transcribes media files using Whisper AI.
        Skips files that already have SRT subtitles unless -Force is used.

    .PARAMETER Directory
        Directory containing media files. Defaults to current directory.

    .PARAMETER Language
        Source language code. Default: 'en'.

    .PARAMETER Model
        Whisper model to use.

    .PARAMETER Translate
        If specified, translates to English.

    .PARAMETER Force
        If specified, overwrites existing SRT files.

    .PARAMETER OutputDir
        Directory for output files.

    .EXAMPLE
        wpf
        Transcribes all media in current directory.

    .EXAMPLE
        Invoke-WhisperFolder -Directory D:\Lectures -Language en -Force
        Transcribes all media in D:\Lectures, overwriting existing SRTs.
    #>
    [CmdletBinding()]
    [Alias('wpf')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .),

        [Parameter()]
        [Alias('l')]
        [string]$Language = 'en',

        [Parameter()]
        [Alias('m')]
        [string]$Model,

        [Parameter()]
        [Alias('t')]
        [switch]$Translate,

        [Parameter()]
        [Alias('f')]
        [switch]$Force,

        [Parameter()]
        [Alias('o')]
        [string]$OutputDir = (Get-Location).Path,

        [Parameter()]
        [Alias('b')]
        [switch]$Batched,

        [Parameter()]
        [int]$BatchSize = 4,

        [Parameter()]
        [switch]$NoVadFilter,

        [Parameter()]
        [Alias('rp')]
        [double]$RepetitionPenalty = 1.1,

        [Parameter(ValueFromRemainingArguments)]
        [string[]]$ExtraArgs
    )

    try {
        $null = Get-Command -Name whisper-ctranslate2 -ErrorAction Stop
    }
    catch {
        throw "whisper-ctranslate2 is not installed or not in PATH."
    }

    $extensions = @('.mp4', '.mkv', '.avi', '.mp3', '.flac', '.wav', '.webm', '.m4a', '.opus', '.ogg')
    $files = Get-ChildItem $Directory -Recurse -File | Where-Object { $_.Extension.ToLower() -in $extensions }

    $skipped = @()
    $toProcess = @()

    foreach ($file in $files) {
        $srtPath = [System.IO.Path]::ChangeExtension($file.FullName, '.srt')
        if ((Test-Path $srtPath) -and -not $Force) {
            $skipped += $file
        }
        else {
            $toProcess += $file
        }
    }

    if ($skipped.Count -gt 0) {
        Write-Host "`nSkipped $( $skipped.Count ) files (SRT exists):" -ForegroundColor Yellow
        $skipped | ForEach-Object { Write-Host "  $( $_.Name )" -ForegroundColor DarkGray }
        Write-Host ""
    }

    if ($toProcess.Count -eq 0) {
        Write-Host "Nothing to transcribe. Use -Force to overwrite existing files." -ForegroundColor Yellow
        return
    }

    Write-Host "Transcribing $( $toProcess.Count ) files:`n" -ForegroundColor Cyan

    $current = 0
    foreach ($file in $toProcess) {
        $current++
        Write-Host "[$current/$( $toProcess.Count )] " -ForegroundColor DarkGray -NoNewline
        Invoke-Whisper -Path $file.FullName -Language $Language -Model $Model -Translate:$Translate -Force:$Force -OutputDir $OutputDir -Batched:$Batched -BatchSize $BatchSize -NoVadFilter:$NoVadFilter -RepetitionPenalty $RepetitionPenalty -ExtraArgs $ExtraArgs
        Write-Host ""
    }

    Write-Host "Completed: $current/$( $toProcess.Count ) transcribed" -ForegroundColor Green
    if ($skipped.Count -gt 0) {
        Write-Host "           $( $skipped.Count ) skipped" -ForegroundColor DarkGray
    }
}

function Invoke-WhisperJapanese {
    <#
    .SYNOPSIS
        Transcribes Japanese audio/video files.

    .DESCRIPTION
        Shortcut for Invoke-Whisper with Japanese language preset.
        Uses large-v3 model by default for best Japanese accuracy.

    .PARAMETER Path
        Path to Japanese audio/video file.

    .PARAMETER Translate
        If specified, translates Japanese to English.

    .EXAMPLE
        wpj anime.mkv
        Transcribes Japanese video.

    .EXAMPLE
        wpj interview.mp4 -Translate
        Transcribes and translates Japanese to English.
    #>
    [CmdletBinding()]
    [Alias('wpj')]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('FilePath', 'FullName')]
        [string]$Path,

        [Parameter()]
        [Alias('m')]
        [string]$Model,

        [Parameter()]
        [Alias('t')]
        [switch]$Translate,

        [Parameter()]
        [Alias('f')]
        [switch]$Force,

        [Parameter()]
        [Alias('o')]
        [string]$OutputDir = (Get-Location).Path,

        [Parameter(ValueFromRemainingArguments)]
        [string[]]$ExtraArgs
    )

    Invoke-Whisper -Path $Path -Language ja -Model $Model -Translate:$Translate -Force:$Force -OutputDir $OutputDir -ExtraArgs $ExtraArgs
}

function Invoke-WhisperJapaneseFolder {
    <#
    .SYNOPSIS
        Transcribes all Japanese audio/video files in a folder.

    .DESCRIPTION
        Batch transcribes Japanese media using Whisper AI.
        Shortcut for Invoke-WhisperFolder with Japanese preset.

    .PARAMETER Directory
        Directory containing Japanese media files.

    .PARAMETER Translate
        If specified, translates to English.

    .EXAMPLE
        wpjf
        Transcribes all Japanese media in current directory.

    .EXAMPLE
        Invoke-WhisperJapaneseFolder -Directory D:\Anime -Translate
        Transcribes and translates all Japanese media in D:\Anime.
    #>
    [CmdletBinding()]
    [Alias('wpjf')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .),

        [Parameter()]
        [Alias('m')]
        [string]$Model,

        [Parameter()]
        [Alias('t')]
        [switch]$Translate,

        [Parameter()]
        [Alias('f')]
        [switch]$Force,

        [Parameter()]
        [Alias('o')]
        [string]$OutputDir = (Get-Location).Path,

        [Parameter(ValueFromRemainingArguments)]
        [string[]]$ExtraArgs
    )

    Invoke-WhisperFolder -Directory $Directory -Language ja -Model $Model -Translate:$Translate -Force:$Force -OutputDir $OutputDir -ExtraArgs $ExtraArgs
}



function Save-YouTubeVideo {
    <#
    .SYNOPSIS
        Downloads YouTube videos with optional transcription.

    .DESCRIPTION
        Uses yt-dlp to download YouTube videos in best quality.
        Automatically transcribes the downloaded video using Whisper (defaults to true).

    .PARAMETER Urls
        One or more YouTube URLs to download.

    .PARAMETER NoTranscribe
        If specified, skips the automatic transcription.

    .PARAMETER Language
        Language for transcription.

    .PARAMETER Model
        Whisper model for transcription.

    .PARAMETER Translate
        If specified, translates to English.

    .PARAMETER OutputDir
        Directory for downloaded files. Defaults to current directory.

    .EXAMPLE
        ytdl https://youtube.com/watch?v=abc123
        Downloads the video and transcribes it (default behavior).

    .EXAMPLE
        Save-YouTubeVideo -Urls $url -NoTranscribe
        Downloads video without transcription.
    #>
    [CmdletBinding()]
    [Alias('ytdl')]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string[]]$Urls,

        [Parameter()]
        [switch]$NoTranscribe,

        [Parameter()]
        [Alias('l')]
        [string]$Language = 'en',

        [Parameter()]
        [Alias('m')]
        [string]$Model,

        [Parameter()]
        [Alias('t')]
        [switch]$Translate,

        [Parameter()]
        [Alias('o')]
        [System.IO.DirectoryInfo]$OutputDir = (Get-Item .)
    )

    if (-not $NoTranscribe) {
        try {
            $null = Get-Command -Name whisper-ctranslate2 -ErrorAction Stop
        }
        catch {
            Write-Warning "whisper-ctranslate2 is not installed or not in PATH. Transcription will be skipped."
        }
    }

    Push-Location $OutputDir

    foreach ($url in $Urls) {
        Write-Host "[$( Get-Date -Format 'HH:mm:ss' )] " -NoNewline
        Write-Host 'Downloading: ' -ForegroundColor Cyan -NoNewline
        Write-Host $url

        $filePath = & yt-dlp --print filename $url --windows-filenames -o '%(title)s.%(ext)s'

        if (Test-Path -Path $filePath) {
            Remove-Item $filePath -Force
            Write-Host '  Replaced existing file' -ForegroundColor Yellow
        }

        & yt-dlp $url --windows-filenames -o '%(title)s.%(ext)s'

        if (-not $NoTranscribe -and (Test-Path $filePath)) {
            Invoke-Whisper -Path $filePath -Language $Language -Model $Model -Translate:$Translate -OutputDir $OutputDir.FullName
        }
    }
    Pop-Location
}



function Invoke-YouTubeSync {
    <#
    .SYNOPSIS
        Synchronizes YouTube playlists to a Google Sheets spreadsheet.

    .DESCRIPTION
        Fetches all playlists from your YouTube account and syncs them to Google Sheets.
        Uses ETag-based change detection to minimize API calls on subsequent runs.
        Supports resumable syncs - if interrupted, will continue from where it left off.

    .PARAMETER Force
        Forces a complete re-sync by clearing the local cache before starting.
        Use this if you suspect the cache is corrupted or out of sync.

    .EXAMPLE
        syncyt
        Runs an optimized sync, only fetching changed playlists based on ETag comparison.

    .EXAMPLE
        Invoke-YouTubeSync -Force
        Clears all cached state and performs a complete re-sync of all playlists.

    .EXAMPLE
        syncyt -Verbose
        Runs sync with debug-level logging to see detailed progress and API calls.

    .NOTES
        Required environment variables:
        - YOUTUBE_GOOGLE_CLIENT_ID
        - YOUTUBE_GOOGLE_CLIENT_SECRET
        - YOUTUBE_SPREADSHEET_ID (optional - will create new if not set)

        Log file: logs/youtube.jsonl
        State file: state/youtube/sync.json
    #>
    [CmdletBinding()]
    [Alias('syncyt')]
    param(
        [Parameter()]
        [Alias('f')]
        [switch]$Force
    )

    Push-Location $Script:CSharpRoot
    try {
        $arguments = @('run', '--', 'sync', 'yt')
        if ($Force) {
            $arguments += '--force'
        }
        if ($VerbosePreference -eq 'Continue') {
            $arguments += '--verbose'
        }
        & dotnet @arguments
    }
    finally {
        Pop-Location
    }
}

function Invoke-LastFmSync {
    <#
    .SYNOPSIS
        Synchronizes Last.fm scrobbles to a Google Sheets spreadsheet.

    .DESCRIPTION
        Fetches scrobble history from Last.fm and syncs to Google Sheets.
        Uses incremental sync by default, only fetching scrobbles newer than the latest in cache.
        Supports resumable syncs - if interrupted mid-page, will resume from last completed page.

    .PARAMETER Since
        Force re-sync from a specific date. Format: yyyy/MM/dd
        Deletes existing data from that date forward and re-fetches from Last.fm.
        Use with caution - this WILL delete data from the spreadsheet.

    .EXAMPLE
        synclf
        Runs an incremental sync, fetching only new scrobbles since last sync.

    .EXAMPLE
        Invoke-LastFmSync -Since '2024/01/01'
        Deletes all scrobbles from 2024/01/01 onwards and re-fetches from Last.fm.

    .EXAMPLE
        synclf -Verbose
        Runs sync with debug-level logging to see each page fetch and API call.

    .NOTES
        Required environment variables:
        - LASTFM_API_KEY
        - LASTFM_USERNAME
        - LASTFM_SPREADSHEET_ID (optional - will create new if not set)

        Log file: logs/lastfm.jsonl
        State file: state/lastfm/sync.json
        Cache file: state/lastfm/scrobbles.json
    #>
    [CmdletBinding()]
    [Alias('synclf')]
    param(
        [Parameter()]
        [Alias('s')]
        [string]$Since
    )

    Push-Location $Script:CSharpRoot
    try {
        $arguments = @('run', '--', 'sync', 'lastfm')
        if ($Since) {
            $arguments += '--since', $Since
        }
        if ($VerbosePreference -eq 'Continue') {
            $arguments += '--verbose'
        }
        & dotnet @arguments
    }
    finally {
        Pop-Location
    }
}

function Invoke-AllSyncs {
    <#
    .SYNOPSIS
        Runs all sync operations in sequence.

    .DESCRIPTION
        Executes YouTube sync followed by Last.fm sync.
        Useful for daily scheduled syncs or manual full sync runs.

    .EXAMPLE
        syncall
        Runs YouTube sync then Last.fm sync.

    .EXAMPLE
        Invoke-AllSyncs -Verbose
        Runs all syncs with debug logging enabled.

    .NOTES
        This runs syncs sequentially, not in parallel.
        If YouTube sync fails, Last.fm sync will still run.
    #>
    [CmdletBinding()]
    [Alias('syncall')]
    param()

    Write-Host "`n[YouTube Sync]" -ForegroundColor Cyan
    if ($VerbosePreference -eq 'Continue') {
        Invoke-YouTubeSync -Verbose
    }
    else {
        Invoke-YouTubeSync
    }

    Write-Host "`n[Last.fm Sync]" -ForegroundColor Cyan
    if ($VerbosePreference -eq 'Continue') {
        Invoke-LastFmSync -Verbose
    }
    else {
        Invoke-LastFmSync
    }

    Write-Host "`nAll syncs complete!" -ForegroundColor Green
}



function Register-ScheduledSyncTask {
    <#
    .SYNOPSIS
        Creates a Windows scheduled task for running sync operations.

    .DESCRIPTION
        Registers a scheduled task that runs a sync command daily at the specified time.
        Also triggers at user logon if the scheduled time was missed.
        Window stays open on failure so user can see error message.

    .PARAMETER TaskName
        Name for the scheduled task. Must be unique.

    .PARAMETER Command
        The dotnet CLI command to run (e.g., 'sync yt' or 'sync lastfm').

    .PARAMETER DailyTime
        Time of day to run the task. Default: 09:00:00

    .PARAMETER Description
        Description for the scheduled task.

    .EXAMPLE
        Register-ScheduledSyncTask -TaskName 'YouTubeSync' -Command 'sync yt' -DailyTime '10:00:00'

    .EXAMPLE
        regtask -TaskName 'LastFmSync' -Command 'sync lastfm'

    .NOTES
        Must be run as Administrator.
        Triggers: Daily at specified time + At logon (2 min delay).
        StartWhenAvailable: Runs ASAP if scheduled time was missed.
        Logs: C# handles all logging to logs/*.jsonl
    #>
    [CmdletBinding()]
    [Alias('regtask')]
    param(
        [Parameter(Mandatory)]
        [string]$TaskName,

        [Parameter(Mandatory)]
        [string]$Command,

        [Parameter()]
        [TimeSpan]$DailyTime = '09:00:00',

        [Parameter()]
        [string]$Description = 'Scheduled sync task'
    )

    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        throw "Run PowerShell as Administrator."
    }

    if (-not (Test-Path -Path $Script:CSharpRoot)) {
        throw "Project path not found: $Script:CSharpRoot"
    }

    $existingTask = Get-ScheduledTask -TaskName $TaskName -ErrorAction Ignore
    if ($existingTask) {
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
        Write-Host "Removed existing task '$TaskName'" -ForegroundColor Yellow
    }

    $dllPath = Join-Path -Path $Script:CSharpRoot -ChildPath 'bin\Debug\net10.0\CSharpScripts.dll'

    $script = @"
Set-Location '$Script:CSharpRoot'
`$noBuild = if (Test-Path '$dllPath') { '--no-build' } else { `$null }
if (`$noBuild) { dotnet run `$noBuild -- $Command } else { dotnet run -- $Command }
if (`$LASTEXITCODE -ne 0) { Read-Host 'Press Enter' }
"@

    $bytes = [Text.Encoding]::Unicode.GetBytes($script)
    $encoded = [Convert]::ToBase64String($bytes)

    $executable = (Get-Command -Name pwsh).Source
    $argument = "-NoProfile -EncodedCommand $encoded"

    $action = New-ScheduledTaskAction -Execute $executable -Argument $argument -WorkingDirectory $Script:CSharpRoot
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -RunOnlyIfNetworkAvailable -WakeToRun -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Hours 2)

    $start = [datetime]::Today.Add($DailyTime)
    if ($start -le (Get-Date)) {
        $start = $start.AddDays(1)
    }

    $dailyTrigger = New-ScheduledTaskTrigger -Daily -At $start
    $logonTrigger = New-ScheduledTaskTrigger -AtLogOn
    $logonTrigger.Delay = 'PT2M'

    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger @($dailyTrigger, $logonTrigger) -Settings $settings -Description $Description | Out-Null

    Write-Host "Registered '$TaskName'" -ForegroundColor Green
    Write-Host "  Daily:  $($start.ToString('HH:mm') )" -ForegroundColor DarkGray
    Write-Host "  Logon:  2 min after login" -ForegroundColor DarkGray
}

function Register-AllSyncTasks {
    <#
    .SYNOPSIS
        Registers all sync scheduled tasks with default settings.

    .DESCRIPTION
        Creates scheduled tasks for both Last.fm and YouTube syncs.
        Last.fm runs at 9 AM, YouTube at 10 AM (staggered to avoid overlap).

    .EXAMPLE
        regall
        Creates 'LastFmSync' task at 9 AM and 'YouTubeSync' task at 10 AM.

    .NOTES
        Must be run as Administrator.
        Removes existing tasks with the same names before creating.
    #>
    [CmdletBinding()]
    [Alias('regall')]
    param()

    Register-ScheduledSyncTask -TaskName 'LastFmSync' -Command 'sync lastfm' -DailyTime '09:00:00' -Description 'Syncs Last.fm scrobbles to Google Sheets'
    Register-ScheduledSyncTask -TaskName 'YouTubeSync' -Command 'sync yt' -DailyTime '10:00:00' -Description 'Syncs YouTube playlists to Google Sheets'
}
