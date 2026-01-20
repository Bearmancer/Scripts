#region ScriptsToolkit
# ScriptsToolkit

class WhisperLanguageCompleter : System.Management.Automation.IValidateSetValuesGenerator {
    [string[]] GetValidValues() { return $Script:WhisperLanguages }
}

class WhisperModelCompleter : System.Management.Automation.IValidateSetValuesGenerator {
    [string[]] GetValidValues() { return $Script:WhisperModels }
}

$Script:RepositoryRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Script:CSharpRoot = Join-Path $Script:RepositoryRoot 'csharp'
$Script:PythonRoot = Join-Path $Script:RepositoryRoot 'python'
$Script:LogDirectory = Join-Path $Script:RepositoryRoot 'logs'

. "$PSScriptRoot\ScriptsToolkit.Data.ps1"

function Invoke-Scripts {
    <#
    .SYNOPSIS
    Invoke the C# CLI for sync, music metadata, and utilities.

    .DESCRIPTION
    Runs the C# dotnet project with the specified arguments. This is the main entry point
    for all C# CLI commands including sync operations, music metadata queries, and utilities.

    .PARAMETER Arguments
    Arguments to pass to the C# CLI.

    .EXAMPLE
    Invoke-Scripts sync yt
    Syncs YouTube playlists to Google Sheets.

    .EXAMPLE
    scripts music search "Beethoven Symphony 5"
    Searches for music metadata using the 'scripts' alias.

    .EXAMPLE
    Invoke-Scripts --help
    Shows available CLI commands.
    #>
    [Alias('scripts')]
    [CmdletBinding()]
    param([Parameter(ValueFromRemainingArguments)][string[]]$Arguments)

    dotnet run --project $Script:CSharpRoot -- @Arguments
}

function Sync-YouTube {
    <#
    .SYNOPSIS
    Sync YouTube playlists to Google Sheets.

    .DESCRIPTION
    Fetches video metadata from YouTube playlists and syncs them to a configured Google Sheets
    spreadsheet. Tracks changes, handles rate limiting, and maintains local state cache.

    .EXAMPLE
    Sync-YouTube
    Runs a full YouTube playlist sync.

    .EXAMPLE
    syncyt --dry-run
    Preview sync without making changes.
    #>
    [Alias('syncyt')]
    [CmdletBinding()]
    param()

    Invoke-Scripts sync yt @args
}

function Sync-LastFm {
    <#
    .SYNOPSIS
    Sync Last.fm scrobbles to Google Sheets.

    .DESCRIPTION
    Fetches scrobble history from Last.fm API and syncs to a configured Google Sheets spreadsheet.
    Supports incremental sync from last known timestamp.

    .EXAMPLE
    Sync-LastFm
    Runs a full Last.fm scrobble sync.

    .EXAMPLE
    synclf --dry-run
    Preview sync without making changes.
    #>
    [Alias('synclf')]
    [CmdletBinding()]
    param()

    Invoke-Scripts sync lastfm @args
}

function Sync-All {
    <#
    .SYNOPSIS
    Sync both YouTube playlists and Last.fm scrobbles.

    .DESCRIPTION
    Runs YouTube sync followed by Last.fm sync in sequence. Useful for scheduled tasks
    or manual full syncs.

    .EXAMPLE
    Sync-All
    Syncs YouTube then Last.fm.

    .EXAMPLE
    syncall --dry-run
    Preview both syncs without making changes.
    #>
    [Alias('syncall')]
    [CmdletBinding()]
    param()

    Invoke-Scripts sync all @args
}

function Invoke-Whisper {
    <#
    .SYNOPSIS
    Transcribe audio/video files using whisper-ctranslate2.

    .DESCRIPTION
    Transcribes audio or video files to SRT subtitle format using whisper-ctranslate2.
    Supports automatic language detection, multiple models, translation, and batch processing
    of directories. Skips files that already have corresponding SRT files.

    .PARAMETER InputPath
    Path to the audio/video file or directory to transcribe.

    .PARAMETER Language
    Language code for transcription (e.g., 'en', 'ja', 'de'). Auto-detected if not specified.

    .PARAMETER Model
    Whisper model to use. Default is 'medium'. Options include tiny, base, small, medium,
    large-v3, distil-large-v3.5, etc.

    .PARAMETER Translate
    Translate output to English instead of transcribing in original language.

    .PARAMETER Quiet
    Suppress verbose whisper output.

    .PARAMETER ExtraArgs
    Additional arguments to pass to whisper-ctranslate2.

    .EXAMPLE
    Invoke-Whisper video.mp4
    Transcribes video.mp4 with auto language detection using medium model.

    .EXAMPLE
    whisper . -Language ja -Model large-v3
    Transcribes all media files in current directory as Japanese using large-v3.

    .EXAMPLE
    Invoke-Whisper interview.mp3 -Translate
    Transcribes and translates to English.
    #>
    [Alias('whisper')]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('FilePath', 'FullName', 'Path')]
        [string]$InputPath,

        [Alias('l')]
        [ValidateSet([WhisperLanguageCompleter])]
        [string]$Language,

        [Alias('m')]
        [ValidateSet([WhisperModelCompleter])]
        [string]$Model = 'medium',

        [Alias('t')]
        [switch]$Translate,

        [Alias('q')]
        [switch]$Quiet,

        [Parameter(ValueFromRemainingArguments)]
        [string[]]$ExtraArgs
    )

    begin {
        Assert-CommandExists 'whisper-ctranslate2'
        $toProcess = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
        $skipped = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
    }

    process {
        $item = Get-Item $InputPath
        $files = if ($item.PSIsContainer) { Get-MediaFiles $item } else { @($item) }

        foreach ($file in $files) {
            if (Test-SrtExists $file.FullName) { $skipped.Add($file) }
            else { $toProcess.Add($file) }
        }
    }

    end {
        if ($skipped.Count -gt 0) {
            Write-Information "$(Get-Timestamp) Skipped $($skipped.Count) (SRT exists)" -InformationAction Continue
        }

        if ($toProcess.Count -eq 0) {
            Write-Information "$(Get-Timestamp) Nothing to transcribe" -InformationAction Continue
            return
        }

        $langDisplay = if ($Language) { $Language } else { 'auto' }
        Write-Information "$(Get-Timestamp) Transcribing $($toProcess.Count) file(s) | Model: $Model | Language: $langDisplay" -InformationAction Continue

        $i = 0
        foreach ($file in $toProcess) {
            $i++
            Write-Information "$(Get-Timestamp) [$i/$($toProcess.Count)] $($file.Name)" -InformationAction Continue

            $whisperArgs = @('--model', $Model, '--output_format', 'srt', '--verbose', $(if ($Quiet) { 'False' } else { 'True' }))
            if ($Language) { $whisperArgs += '--language', $Language }
            if ($Translate) { $whisperArgs += '--task', 'translate' }
            if ($ExtraArgs) { $whisperArgs += $ExtraArgs }
            $whisperArgs += $file.FullName

            $env:PYTHONWARNINGS = 'ignore::DeprecationWarning,ignore::UserWarning'
            try { & whisper-ctranslate2 @whisperArgs }
            finally { $env:PYTHONWARNINGS = $null }
        }

        Write-Information "$(Get-Timestamp) Completed $i file(s)" -InformationAction Continue
    }
}

function Invoke-WhisperEnglish {
    <#
    .SYNOPSIS
    Transcribe audio/video optimized for English content.

    .DESCRIPTION
    Wrapper for Invoke-Whisper with English language and distil-large-v3.5 model pre-configured.
    The distilled model is faster while maintaining high accuracy for English content.

    .PARAMETER InputPath
    Path to the audio/video file or directory to transcribe.

    .PARAMETER Translate
    Translate non-English audio to English (useful for multilingual content).

    .PARAMETER Quiet
    Suppress verbose whisper output.

    .PARAMETER ExtraArgs
    Additional arguments to pass to whisper-ctranslate2.

    .EXAMPLE
    Invoke-WhisperEnglish podcast.mp3
    Transcribes English podcast using the fast distilled model.

    .EXAMPLE
    whisp *.mp4
    Transcribes all MP4 files using the 'whisp' alias.
    #>
    [Alias('whisp')]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('FilePath', 'FullName', 'Path')]
        [string]$InputPath,

        [Alias('t')]
        [switch]$Translate,

        [Alias('q')]
        [switch]$Quiet,

        [Parameter(ValueFromRemainingArguments)]
        [string[]]$ExtraArgs
    )

    process {
        Invoke-Whisper $InputPath -Language en -Model 'distil-large-v3.5' -Translate:$Translate -Quiet:$Quiet @ExtraArgs
    }
}

function Invoke-WhisperJapanese {
    <#
    .SYNOPSIS
    Transcribe Japanese audio/video content.

    .DESCRIPTION
    Wrapper for Invoke-Whisper with Japanese language pre-configured. Uses the default
    medium model which provides good accuracy for Japanese.

    .PARAMETER InputPath
    Path to the audio/video file or directory to transcribe.

    .PARAMETER Translate
    Translate Japanese audio to English subtitles.

    .PARAMETER Quiet
    Suppress verbose whisper output.

    .PARAMETER ExtraArgs
    Additional arguments to pass to whisper-ctranslate2.

    .EXAMPLE
    Invoke-WhisperJapanese anime.mkv
    Transcribes Japanese audio to Japanese subtitles.

    .EXAMPLE
    wpj video.mp4 -Translate
    Transcribes Japanese and translates to English using 'wpj' alias.
    #>
    [Alias('wpj')]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('FilePath', 'FullName', 'Path')]
        [string]$InputPath,

        [Alias('t')]
        [switch]$Translate,

        [Alias('q')]
        [switch]$Quiet,

        [Parameter(ValueFromRemainingArguments)]
        [string[]]$ExtraArgs
    )

    process {
        Invoke-Whisper $InputPath -Language ja -Translate:$Translate -Quiet:$Quiet @ExtraArgs
    }
}

function Save-YouTubeDownload {
    <#
    .SYNOPSIS
    Download YouTube videos and optionally transcribe them.

    .DESCRIPTION
    Downloads YouTube videos using yt-dlp and optionally transcribes them using Whisper.
    By default, downloads are transcribed with Invoke-WhisperEnglish unless -NoTranscribe is specified.

    .PARAMETER Urls
    One or more YouTube URLs to download.

    .PARAMETER NoTranscribe
    Skip automatic transcription after download.

    .PARAMETER Translate
    Translate transcription to English (for non-English content).

    .EXAMPLE
    Save-YouTubeDownload 'https://youtube.com/watch?v=dQw4w9WgXcQ'
    Downloads and transcribes the video.

    .EXAMPLE
    ytdl 'https://youtube.com/watch?v=abc123' -NoTranscribe
    Downloads without transcription.

    .EXAMPLE
    Save-YouTubeDownload $url1, $url2 -Translate
    Downloads multiple videos and translates non-English audio.
    #>
    [Alias('ytdl')]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string[]]$Urls,

        [Alias('n')]
        [switch]$NoTranscribe,

        [Alias('t')]
        [switch]$Translate
    )

    foreach ($url in $Urls) {
        Write-Information "$(Get-Timestamp) Downloading: $url" -InformationAction Continue

        $filePath = & yt-dlp --print filename $url --windows-filenames -o '%(title)s.%(ext)s'
        if (Test-Path $filePath) { Remove-Item $filePath -Force }
        & yt-dlp $url --windows-filenames -o '%(title)s.%(ext)s'

        if ($NoTranscribe -or -not (Test-Path $filePath)) { continue }

        if ($Translate) { Invoke-Whisper $filePath -Translate }
        else { Invoke-WhisperEnglish $filePath }
    }
}

function Register-SyncTask {
    <#
    .SYNOPSIS
    Register a Windows scheduled task for sync operations.

    .DESCRIPTION
    Creates a Windows Scheduled Task that runs a sync command daily and at logon.
    Requires Administrator privileges. Validates the command before registering.
    Replaces existing task with the same name.

    .PARAMETER TaskName
    Name for the scheduled task (e.g., 'YouTubeSync', 'LastFmSync').

    .PARAMETER Command
    The sync command to run (e.g., 'sync yt', 'sync lastfm').

    .PARAMETER DailyTime
    Time of day to run the task. Default is 09:00.

    .PARAMETER Description
    Description for the scheduled task.

    .EXAMPLE
    Register-SyncTask -TaskName 'YouTubeSync' -Command 'sync yt'
    Registers YouTube sync to run daily at 9 AM and at logon.

    .EXAMPLE
    regtask -TaskName 'LastFmSync' -Command 'sync lastfm' -DailyTime '22:00'
    Registers Last.fm sync to run at 10 PM daily.
    #>
    [Alias('regtask')]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$TaskName,

        [Parameter(Mandatory)]
        [string]$Command,

        [TimeSpan]$DailyTime = '09:00',

        [string]$Description = 'Scheduled sync task'
    )

    $principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Requires Administrator privileges.'
    }

    if (-not (Test-Path $Script:CSharpRoot)) { throw "Project not found: $Script:CSharpRoot" }
    Assert-NetworkAvailable

    Write-Information "$(Get-Timestamp) Validating: $Command" -InformationAction Continue
    Push-Location $Script:CSharpRoot
    try {
        $result = dotnet run -- $Command --dry-run 2>&1
        if ($LASTEXITCODE -ne 0) { throw "Validation failed: $result" }
    }
    finally { Pop-Location }

    $existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction Ignore
    if ($existing) {
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
        Write-Information "$(Get-Timestamp) Removed existing: $TaskName" -InformationAction Continue
    }

    $taskScript = @"
Set-Location '$Script:CSharpRoot'
dotnet run -- $Command
if (`$LASTEXITCODE -ne 0) { Read-Host 'Press Enter' }
"@
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($taskScript))
    $pwsh = (Get-Command pwsh).Source

    $action = New-ScheduledTaskAction -Execute $pwsh -Argument "-NoProfile -EncodedCommand $encoded" -WorkingDirectory $Script:CSharpRoot
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -RunOnlyIfNetworkAvailable -WakeToRun -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Hours 2)

    $start = [datetime]::Today.Add($DailyTime)
    if ($start -le (Get-Date)) { $start = $start.AddDays(1) }

    $triggers = @((New-ScheduledTaskTrigger -Daily -At $start), (New-ScheduledTaskTrigger -AtLogOn))
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $triggers -Settings $settings -Description $Description | Out-Null

    Write-Information "$(Get-Timestamp) Registered: $TaskName (Daily: $($start.ToString('HH:mm')), Logon)" -InformationAction Continue
}

function Register-AllSyncTasks {
    <#
    .SYNOPSIS
    Register all default sync scheduled tasks.

    .DESCRIPTION
    Convenience function to register both LastFmSync (9 AM) and YouTubeSync (10 AM)
    scheduled tasks. Requires Administrator privileges.

    .EXAMPLE
    Register-AllSyncTasks
    Registers both sync tasks with default schedules.

    .EXAMPLE
    regall
    Same as above using the alias.
    #>
    [Alias('regall')]
    [CmdletBinding()]
    param()

    Assert-NetworkAvailable
    Register-SyncTask -TaskName 'LastFmSync' -Command 'sync lastfm' -Description 'Sync Last.fm scrobbles'
    Register-SyncTask -TaskName 'YouTubeSync' -Command 'sync yt' -DailyTime '10:00' -Description 'Sync YouTube playlists'
}

#region SyncLog
function Get-SyncLog {
    <#
    .SYNOPSIS
    Get sync session logs in a formatted table.

    .DESCRIPTION
    Displays sync session summaries from JSONL log files. Use -Verbose for entry-level
    detail showing individual log events.

    .PARAMETER Service
    Filter by service: 'youtube', 'lastfm', or 'all' (default).

    .PARAMETER Size
    Number of rows to display. Default is 10.

    .PARAMETER Level
    Filter entries by log level. Applies to both entry and session aggregation.

    .PARAMETER SessionId
    Filter to a specific session by ID prefix (first 8 characters).

    .EXAMPLE
    Get-SyncLog
    Shows the 10 most recent sync sessions for all services.

    .EXAMPLE
    viewlog -Service youtube -Size 20
    Shows the 20 most recent YouTube sync sessions.

    .EXAMPLE
    Get-SyncLog -SessionId 'a1b2c3d4'
    Shows details for a specific session.

    .EXAMPLE
    viewlog -Verbose -Size 50
    Shows 50 recent entries in table format with wrapped details.

    .EXAMPLE
    viewlog -Verbose -Format List -Level Error
    Shows error-level entries in detailed list format.

    .EXAMPLE
    viewlog -Verbose -Service lastfm
    Shows detailed Last.fm sync entries.
    #>
    [Alias('viewlog')]
    [CmdletBinding()]
    param(
        [ValidateSet('youtube', 'lastfm', 'all')]
        [string]$Service = 'all',

        [Alias('n')]
        [int]$Size = 10,

        [ValidateSet('Table', 'List')]
        [string]$Format = 'Table',

        [switch]$Descending,

        [string]$SessionId,

        [ValidateSet('All', 'Debug', 'Info', 'Success', 'Warning', 'Error', 'Fatal')]
        [string]$Level = 'All'
    )

    $logFiles = @{
        youtube = Join-Path $Script:LogDirectory 'youtube.jsonl'
        lastfm  = Join-Path $Script:LogDirectory 'lastfm.jsonl'
    }
    $services = if ($Service -eq 'all') { @('youtube', 'lastfm') } else { @($Service) }

    $allEntries = Get-SyncLogEntriesFromFiles $services $logFiles

    if (-not $allEntries) {
        Write-Information "$(Get-Timestamp) No log entries found" -InformationAction Continue
        return
    }
    if ($SessionId) { $allEntries = @($allEntries | Where-Object { $_.SessionId -and $_.SessionId -like "$SessionId*" }) }

    $allEntries = Add-SyncLogParsedTimestamp $allEntries

    if ($Level -ne 'All') {
        $allEntries = @($allEntries | Where-Object { $_.Level -eq $Level })
    }

    $sortDescending = $PSBoundParameters['Descending'] ?? $true
    $verbose = $VerbosePreference -eq 'Continue' -or $PSBoundParameters.ContainsKey('Verbose')

    if ($verbose) {
        $entriesSorted = Select-SyncLogRows -Items $allEntries -Size $Size -Descending:$sortDescending -SortProperty 'ParsedTimestamp'
        $entriesOutput = ConvertTo-SyncLogEntryOutput $entriesSorted

        if ($Format -eq 'List') {
            $entriesOutput | Format-List -Property Time, Service, Level, Event, Session, Details
        }
        else {
            $entriesOutput | Format-Table -Property Time, Service, Level, Event, Session, Details -AutoSize -Wrap
        }
        return
    }

    $sessions = ConvertTo-SyncLogSessionOutput $allEntries

    if (-not $sessions) {
        Write-Error "$(Get-Timestamp) No sessions found"
        return
    }

    $display = Select-SyncLogRows -Items $sessions -Size $Size -Descending:$sortDescending -SortProperty '_Sort'
    $display | Format-Table -Property Session, Service, Time, Status, Summary -AutoSize -Wrap
}

#region SyncLog Helpers
function Get-SyncLogEntriesFromFiles {
    param(
        [string[]]$Services,
        [hashtable]$LogFiles
    )

    foreach ($svc in $Services) {
        $path = $LogFiles[$svc]
        if (Test-Path $path) {
            Get-Content $path | ForEach-Object {
                $entry = $_ | ConvertFrom-Json
                $entry | Add-Member -NotePropertyName 'Service' -NotePropertyValue $svc -Force -PassThru
            }
        }
    }
}

function Add-SyncLogParsedTimestamp {
    param([object[]]$Entries)

    $Entries | Where-Object { $_.SessionId -and $_.Timestamp } | ForEach-Object {
        $parsed = try { [datetime]::ParseExact($_.Timestamp, 'yyyy-MM-dd HH:mm:ss', [System.Globalization.CultureInfo]::InvariantCulture) } catch { try { [datetime]$_.Timestamp } catch { $null } }
        $_ | Add-Member -NotePropertyName 'ParsedTimestamp' -NotePropertyValue $parsed -Force -PassThru
    } | Where-Object ParsedTimestamp
}

function Get-SyncLogEntryDetails {
    param($Entry)

    $entryData = $Entry.Data
    if (-not $entryData) { return '' }

    $text = $entryData.PSObject?.Properties['Text']?.Value
    if ($text) { return $text }

    $parts = $entryData.PSObject.Properties | Where-Object { $_.Name -notin 'Service', 'ProcessId' } | ForEach-Object {
        $val = if ($_.Value -is [array]) { $_.Value -join ', ' } else { $_.Value }
        "$($_.Name): $val"
    }

    $parts -join "`n"
}

function ConvertTo-SyncLogEntryOutput {
    param([object[]]$Entries)

    $Entries | ForEach-Object {
        [PSCustomObject]@{
            Time    = $_.ParsedTimestamp.ToString('yyyy/MM/dd HH:mm:ss')
            Service = $_.Service
            Level   = $_.Level
            Event   = $_.Event
            Session = $_.SessionId?.Substring(0, 8) ?? ''
            Details = Get-SyncLogEntryDetails $_
        }
    }
}

function ConvertTo-SyncLogSessionOutput {
    param([object[]]$Entries)

    $Entries | Group-Object SessionId | ForEach-Object {
        $group = @($_.Group | Sort-Object ParsedTimestamp)
        $first = $group[0]
        if (-not $first -or -not $first.SessionId) { return }

        $startTime = $first.ParsedTimestamp
        # PSScriptAnalyzer doesn't recognize usage in if-expressions
        [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'endEvent')]
        [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'endData')]
        $endEvent = @($group | Where-Object { $_.Event -eq 'SessionEnd' })[0]
        $endData = if ($endEvent) { $endEvent.Data } else { $null }
        $endTime = if ($endEvent) { $endEvent.ParsedTimestamp } else { $null }
        $summary = if ($endData) { $endData.Summary } else { '-' }

        $hasError = [bool]($group | Where-Object { $_.Level -eq 'Error' })
        $interrupted = [bool]($group | Where-Object { $_.Event -eq 'SessionInterrupted' })
        $crashed = [bool]($group | Where-Object { $_.Event -eq 'SessionCrashed' })
        $endStatus = if ($endData) { $endData.Status } else { $null }

        $status = switch ($true) {
            { -not $endTime -and ((Get-Date) - $startTime).TotalHours -lt 2 } { 'Running'; break }
            { -not $endTime } { 'Crashed'; break }
            { $crashed } { 'Crashed'; break }
            { $interrupted } { 'Interrupted'; break }
            { $hasError } { 'Failed'; break }
            { $endStatus } { $endStatus; break }
            default { 'Completed' }
        }

        [PSCustomObject]@{
            Session   = $first.SessionId.Substring(0, 8)
            Service   = $first.Service
            Time      = $startTime.ToString('yyyy/MM/dd HH:mm:ss')
            Status    = $status
            Summary   = $summary
            StartTime = $startTime
            EndTime   = $endTime
            Duration  = if ($endTime) { ($endTime - $startTime).ToString() } else { '-' }
            Events    = $group.Count
            HasError  = $hasError
            _Sort     = $startTime
        }
    }
}

function Select-SyncLogRows {
    param(
        [object[]]$Items,
        [int]$Size,
        [switch]$Descending,
        [string]$SortProperty
    )

    $sorted = $Items | Sort-Object -Property $SortProperty -Descending:$Descending
    $sorted | Select-Object -First $Size
}
#endregion SyncLog Helpers
#endregion SyncLog

function Invoke-Propolis {
    <#
    .SYNOPSIS
    Optimize images using Propolis.

    .DESCRIPTION
    Runs the Propolis image optimizer on a directory. Propolis compresses images
    while maintaining quality.

    .PARAMETER Directory
    Directory containing images to optimize. Defaults to current directory.

    .EXAMPLE
    Invoke-Propolis
    Optimizes images in the current directory.

    .EXAMPLE
    propolis C:\Photos\Vacation
    Optimizes images in the specified directory.
    #>
    [Alias('propolis')]
    [CmdletBinding()]
    param([System.IO.DirectoryInfo]$Directory = (Get-Item .))

    & "$env:LOCALAPPDATA\Personal\Propolis\propolis_windows.exe" --no-specs $Directory.FullName
}

function Get-ScriptsToolkitCommand {
    <#
    .SYNOPSIS
    List all available ScriptsToolkit commands and their aliases.

    .DESCRIPTION
    Displays a formatted table of all exported functions, their aliases, and descriptions
    from the ScriptsToolkit module.

    .EXAMPLE
    Get-ScriptsToolkitCommand
    Lists all commands in the toolkit.

    .EXAMPLE
    stk
    Same as above using the alias.
    #>
    [Alias('stk')]
    [CmdletBinding()]
    param()

    $module = Get-Module ScriptsToolkit
    $module.ExportedFunctions.Keys | Sort-Object | ForEach-Object {
        $func = $_
        $aliases = ($module.ExportedAliases.Values | Where-Object { $_.Definition -eq $func }).Name -join ', '
        $help = Get-Help $func -ErrorAction Ignore
        $synopsis = if ($help.Synopsis) { $help.Synopsis.Trim() } else { '' }
        [PSCustomObject]@{ Alias = $aliases; Function = $func; Description = $synopsis }
    } | Format-Table -AutoSize
}