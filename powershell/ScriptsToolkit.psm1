Set-StrictMode -Version Latest

#region Configuration

$Script:RepositoryRoot = Split-Path -Path $PSScriptRoot -Parent
$Script:PythonToolkit = Join-Path -Path $RepositoryRoot -ChildPath 'python\toolkit\cli.py'
$Script:CSharpRoot = Join-Path -Path $RepositoryRoot -ChildPath 'csharp'
$Script:LogDirectory = Join-Path -Path $RepositoryRoot -ChildPath 'logs'

#endregion

#region Core

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

#endregion

#region Utilities

<#
.SYNOPSIS
    List all toolkit functions.

.DESCRIPTION
    Displays all functions grouped by category with aliases and descriptions.

.EXAMPLE
    Get-ToolkitFunctions
#>
function Get-ToolkitFunctions {
    [CmdletBinding()]
    [Alias('tkfn')]
    param()

    $functions = @(
        @{ Category = 'Utilities'; Name = 'Get-ToolkitFunctions'; Alias = 'tkfn'; Description = 'List all toolkit functions' }
        @{ Category = 'Utilities'; Name = 'Open-CommandHistory'; Alias = 'hist'; Description = 'Open PowerShell history file' }
        @{ Category = 'Utilities'; Name = 'Show-ToolkitHelp'; Alias = 'tkhelp'; Description = 'Open toolkit documentation' }
        @{ Category = 'Utilities'; Name = 'Invoke-ToolkitAnalyzer'; Alias = 'tklint'; Description = 'Run PSScriptAnalyzer' }
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
        @{ Category = 'Transcription'; Name = 'Invoke-Whisper'; Alias = 'whisper'; Description = 'Transcribe file or folder' }
        @{ Category = 'Transcription'; Name = 'Invoke-WhisperFolder'; Alias = 'whisperf'; Description = 'Transcribe folder (explicit)' }
        @{ Category = 'Transcription'; Name = 'Invoke-WhisperJapanese'; Alias = 'whisperj'; Description = 'Transcribe Japanese file/folder' }
        @{ Category = 'Transcription'; Name = 'Invoke-WhisperJapaneseFolder'; Alias = 'whisperjf'; Description = 'Transcribe Japanese folder (explicit)' }
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

<#
.SYNOPSIS
    Open PowerShell history file in VS Code.

.DESCRIPTION
    Opens PSReadLine console history file for review.

.EXAMPLE
    Open-CommandHistory
#>
function Open-CommandHistory {
    [CmdletBinding()]
    [Alias('hist')]
    param()

    & code "$env:APPDATA\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt"
}

<#
.SYNOPSIS
    Open toolkit documentation.

.EXAMPLE
    Show-ToolkitHelp
#>
function Show-ToolkitHelp {
    [CmdletBinding()]
    [Alias('tkhelp')]
    param()

    $helpFile = Join-Path -Path $PSScriptRoot -ChildPath 'ScriptsToolkit.Help.md'
    & code $helpFile
}

<#
.SYNOPSIS
    Run PSScriptAnalyzer on scripts.

.DESCRIPTION
    Invokes PSScriptAnalyzer using the module's settings file.

.PARAMETER Path
    Directory to analyze.

.EXAMPLE
    Invoke-ToolkitAnalyzer

.EXAMPLE
    Invoke-ToolkitAnalyzer -Path C:\MyProject\src
#>
function Invoke-ToolkitAnalyzer {
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

#endregion

#region Logs

<#
.SYNOPSIS
    View JSONL sync logs.

.DESCRIPTION
    Reads and displays JSONL log entries as table or list.

.PARAMETER Service
    Filter: youtube, lastfm, or all.

.PARAMETER Level
    Filter: Debug, Info, Success, Warning, Error, Fatal.

.PARAMETER Event
    Filter by event type.

.PARAMETER SessionId
    Filter by session ID.

.PARAMETER Search
    Search text in log data.

.PARAMETER Tail
    Number of entries from end (newest). Alias: -Last

.PARAMETER Head
    Number of entries from start (oldest). Alias: -First

.PARAMETER SortBy
    Sort by: date, level, event, session. Default: date

.PARAMETER Chronological
    Show oldest first (ascending date order).

.PARAMETER List
    Display as vertical list.

.EXAMPLE
    Show-SyncLog

.EXAMPLE
    Show-SyncLog -Service youtube -Level Error

.EXAMPLE
    Show-SyncLog -Search Comedy

.EXAMPLE
    Show-SyncLog -Head 10 -Chronological

.EXAMPLE
    Show-SyncLog -SortBy level
#>
function Show-SyncLog {
    [CmdletBinding(DefaultParameterSetName = 'Tail')]
    [Alias('viewlog', 'synclog')]
    param(
        [Parameter()]
        [ValidateSet('youtube', 'lastfm', 'all')]
        [string]$Service = 'all',

        [Parameter()]
        [ValidateSet('Debug', 'Info', 'Success', 'Warning', 'Error', 'Fatal')]
        [string]$Level,

        [Parameter()]
        [string]$Event,

        [Parameter()]
        [string]$SessionId,

        [Parameter()]
        [string]$Search,

        [Parameter(ParameterSetName = 'Tail')]
        [Alias('Last')]
        [int]$Tail = 100,

        [Parameter(ParameterSetName = 'Head')]
        [Alias('First')]
        [int]$Head,

        [Parameter()]
        [ValidateSet('date', 'level', 'event', 'session')]
        [string]$SortBy = 'date',

        [Parameter()]
        [switch]$Chronological,

        [Parameter()]
        [switch]$List
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

    $entries = @()
    foreach ($logFile in $logFiles) {
        $serviceName = [System.IO.Path]::GetFileNameWithoutExtension($logFile)
        foreach ($line in [System.IO.File]::ReadLines($logFile)) {
            if ( [string]::IsNullOrWhiteSpace($line)) {
                continue
            }
            try {
                $obj = $line | ConvertFrom-Json
                $obj | Add-Member -NotePropertyName 'Source' -NotePropertyValue $serviceName -Force
                $parsedTimestamp = [datetime]::ParseExact(
                    $obj.Timestamp,
                    'yyyy-MM-dd HH:mm:ss',
                    [System.Globalization.CultureInfo]::InvariantCulture
                )
                $obj | Add-Member -NotePropertyName 'ParsedTimestamp' -NotePropertyValue $parsedTimestamp -Force
                $entries += $obj
            }
            catch {
                Write-Warning "Failed to parse log line in ${logFile}: $_"
            }
        }
    }

    if ($Level) {
        $entries = $entries | Where-Object { $_.Level -eq $Level }
    }
    if ($Event) {
        $entries = $entries | Where-Object { $_.Event -like "*$Event*" }
    }
    if ($SessionId) {
        $entries = $entries | Where-Object { $_.SessionId -eq $SessionId }
    }
    if ($Search) {
        $entries = $entries | Where-Object {
            $json = $_.Data | ConvertTo-Json -Compress -Depth 5
            $json -like "*$Search*"
        }
    }

    # Determine sort property
    $sortProperty = switch ($SortBy) {
        'date' { 'ParsedTimestamp' }
        'level' { 'Level' }
        'event' { 'Event' }
        'session' { 'SessionId' }
        default { 'ParsedTimestamp' }
    }

    # Sort entries
    $sorted = if ($Chronological) {
        $entries | Sort-Object -Property $sortProperty
    }
    else {
        $entries | Sort-Object -Property $sortProperty -Descending
    }

    # Apply Head or Tail limit
    $result = if ($Head) {
        $sorted | Select-Object -First $Head
    }
    else {
        $sorted | Select-Object -First $Tail
    }

    # Color palette for alternating rows
    $colors = @('Cyan', 'Green', 'Yellow', 'Magenta', 'Blue', 'White')
    $colorIndex = 0

    $displayObjects = $result | ForEach-Object {
        $details = if ($_.Data) {
            if ($_.Data.PSObject.Properties['Text']) {
                $_.Data.Text
            }
            else {
                $separator = $List ? "`n" : " | "
                ($_.Data.PSObject.Properties | ForEach-Object {
                    $val = ($_.Value -is [array]) ? ($_.Value -join ", "): $_.Value
                    "$($_.Name): $val"
                }) -join $separator
            }
        }
        else {
            ''
        }

        $obj = [PSCustomObject]@{
            Timestamp = $_.Timestamp
            Level     = $_.Level
            Event     = $_.Event
            SessionId = $_.SessionId
            Details   = $details
            Color     = $colors[$colorIndex % $colors.Count]
        }
        $colorIndex++
        $obj
    }

    if ($List) {
        $displayObjects | ForEach-Object {
            Write-Host "─────────────────────────────────────────────────" -ForegroundColor DarkGray
            Write-Host "Timestamp: $($_.Timestamp)" -ForegroundColor $_.Color
            Write-Host "Level:     $($_.Level)" -ForegroundColor $_.Color
            Write-Host "Event:     $($_.Event)" -ForegroundColor $_.Color
            Write-Host "SessionId: $($_.SessionId)" -ForegroundColor $_.Color
            Write-Host "Details:   $($_.Details)" -ForegroundColor $_.Color
        }
        Write-Host "─────────────────────────────────────────────────" -ForegroundColor DarkGray
    }
    else {
        # Header
        Write-Host ""
        Write-Host ("Timestamp".PadRight(20) + "Level".PadRight(10) + "Event".PadRight(20) + "SessionId".PadRight(12) + "Details") -ForegroundColor White
        Write-Host ("─" * 100) -ForegroundColor DarkGray

        $displayObjects | ForEach-Object {
            Write-Host ($_.Timestamp.PadRight(20)) -ForegroundColor $_.Color -NoNewline
            Write-Host ($_.Level.PadRight(10)) -ForegroundColor $_.Color -NoNewline
            Write-Host ($_.Event.PadRight(20)) -ForegroundColor $_.Color -NoNewline
            Write-Host ($_.SessionId.PadRight(12)) -ForegroundColor $_.Color -NoNewline
            Write-Host $_.Details -ForegroundColor $_.Color
            Write-Host ("─" * 100) -ForegroundColor DarkGray
        }
    }
}

<#
.SYNOPSIS
    List directories with sizes.

.PARAMETER Directory
    Directory to scan.

.PARAMETER SortBy
    Sort by size or name.

.EXAMPLE
    Get-Directories

.EXAMPLE
    Get-Directories -Directory C:\Music -SortBy name
#>
function Get-Directories {
    [CmdletBinding()]
    [Alias('dirs')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [Parameter()]
        [Alias('s')]
        [ValidateSet('size', 'name')]
        [string]$SortBy = 'size'
    )

    Invoke-ToolkitPython -ArgumentList @('filesystem', 'tree', '--directory', $Directory.FullName, '--sort', $SortBy)
}

<#
.SYNOPSIS
    List all items with sizes.

.PARAMETER Directory
    Directory to scan.

.PARAMETER SortBy
    Sort by size or name.

.EXAMPLE
    Get-FilesAndDirectories -SortBy name
#>
function Get-FilesAndDirectories {
    [CmdletBinding()]
    [Alias('tree')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [Parameter()]
        [Alias('s')]
        [ValidateSet('size', 'name')]
        [string]$SortBy = 'size'
    )

    Invoke-ToolkitPython -ArgumentList @('filesystem', 'tree', '--directory', $Directory.FullName, '--sort', $SortBy, '--include-files')
}

<#
.SYNOPSIS
    Create .torrent files.

.PARAMETER Directory
    Directory to scan.

.PARAMETER IncludeSubdirectories
    Include subdirectories.

.EXAMPLE
    New-Torrents -Directory C:\Music\Album -IncludeSubdirectories
#>
function New-Torrents {
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

#endregion

#region Video

<#
.SYNOPSIS
    Remux video discs to MKV.

.PARAMETER Directory
    Directory to scan.

.PARAMETER SkipMediaInfo
    Skip MediaInfo analysis.

.EXAMPLE
    Start-DiscRemux -Directory D:\BluRay\Movie -SkipMediaInfo
#>
function Start-DiscRemux {
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

<#
.SYNOPSIS
    Compress videos in batch.

.PARAMETER Directory
    Directory to scan.

.EXAMPLE
    Start-BatchCompression -Directory D:\Videos\Raw
#>
function Start-BatchCompression {
    [CmdletBinding()]
    [Alias('compress')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('video', 'compress', '--directory', $Directory.FullName)
}

<#
.SYNOPSIS
    Extract chapter timestamps.

.PARAMETER Directory
    Directory to scan.

.EXAMPLE
    Get-VideoChapters -Directory D:\Movies\Film
#>
function Get-VideoChapters {
    [CmdletBinding()]
    [Alias('chapters')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('video', 'chapters', '--path', $Directory.FullName)
}

<#
.SYNOPSIS
    Report video resolutions.

.PARAMETER Directory
    Directory to scan.

.EXAMPLE
    Get-VideoResolution -Directory D:\Videos
#>
function Get-VideoResolution {
    [CmdletBinding()]
    [Alias('res')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('video', 'resolutions', '--path', $Directory.FullName)
}

#endregion

#region Audio

<#
.SYNOPSIS
    Convert audio files.

.PARAMETER Directory
    Directory to scan.

.PARAMETER Format
    Target: 24-bit, flac, mp3, or all.

.EXAMPLE
    Convert-Audio -Directory C:\Music\Album

.EXAMPLE
    Convert-Audio -Directory C:\Music -Format mp3
#>
function Convert-Audio {
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

<#
.SYNOPSIS
    Convert to MP3.

.PARAMETER Directory
    Directory to scan.

.EXAMPLE
    Convert-ToMP3 -Directory C:\Music\Album
#>
function Convert-ToMP3 {
    [CmdletBinding()]
    [Alias('tomp3')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Convert-Audio -Directory $Directory -Format 'mp3'
}

<#
.SYNOPSIS
    Convert to FLAC.

.PARAMETER Directory
    Directory to scan.

.EXAMPLE
    Convert-ToFLAC -Directory C:\Music\Album
#>
function Convert-ToFLAC {
    [CmdletBinding()]
    [Alias('toflac')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Convert-Audio -Directory $Directory -Format 'flac'
}

<#
.SYNOPSIS
    Extract SACD ISO files.

.PARAMETER Directory
    Directory to scan.

.PARAMETER Format
    Target: 24-bit, flac, mp3, or all.

.EXAMPLE
    Convert-SACD -Directory D:\SACD
#>
function Convert-SACD {
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

<#
.SYNOPSIS
    Rename files using RED naming.

.PARAMETER Directory
    Directory to scan.

.EXAMPLE
    Rename-MusicFiles -Directory C:\Music\Album
#>
function Rename-MusicFiles {
    [CmdletBinding()]
    [Alias('rename')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('audio', 'rename', '--directory', $Directory.FullName)
}

<#
.SYNOPSIS
    Report embedded art sizes.

.PARAMETER Directory
    Directory to scan.

.EXAMPLE
    Get-EmbeddedImageSize -Directory C:\Music
#>
function Get-EmbeddedImageSize {
    [CmdletBinding()]
    [Alias('artsize')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('audio', 'art-report', '--directory', $Directory.FullName)
}

<#
.SYNOPSIS
    Run Propolis analyzer.

.PARAMETER Directory
    Directory to scan.

.EXAMPLE
    Invoke-Propolis -Directory C:\Music\Album
#>
function Invoke-Propolis {
    [CmdletBinding()]
    [Alias('propolis')]
    param(
        [Parameter(Position = 0)]
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    & "$env:LOCALAPPDATA\Personal\Propolis\propolis_windows.exe" --no-specs $Directory.FullName
}

#endregion

#region Transcription

<#
.SYNOPSIS
    Transcribe audio/video using whisper-ctranslate2.

.DESCRIPTION
    Transcribes to SRT subtitles. Accepts file or folder path.
    Pass extra whisper-ctranslate2 arguments via -ExtraArgs.

.PARAMETER Path
    Audio/video file or folder path.

.PARAMETER Language
    Language code (en, ja, es, fr, de, zh, ko). Auto-detects if omitted.

.PARAMETER Model
    Whisper model. Auto-selects distil-large-v3.5 for English, large-v3 otherwise.

.PARAMETER Translate
    Translate non-English to English.

.PARAMETER Force
    Overwrite existing SRT files.

.PARAMETER OutputDir
    Output directory.

.PARAMETER Batched
    Enable batched transcription (faster, may reduce accuracy).

.PARAMETER BatchSize
    Batch size for batched mode.

.PARAMETER NoVadFilter
    Disable Voice Activity Detection.

.PARAMETER RepetitionPenalty
    Penalty for repeated tokens (>1.0 penalizes).

.PARAMETER ExtraArgs
    Additional whisper-ctranslate2 arguments.

.EXAMPLE
    Invoke-Whisper video.mp4

.EXAMPLE
    Invoke-Whisper video.mp4 -Language en

.EXAMPLE
    Invoke-Whisper video.mp4 -Batched -BatchSize 8

.EXAMPLE
    Invoke-Whisper lecture.mp3 -Language ja -Translate

.EXAMPLE
    Invoke-Whisper video.mp4 --word_timestamps True --no_repeat_ngram_size 3
#>
function Invoke-Whisper {
    [CmdletBinding()]
    [Alias('whisper')]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('FilePath', 'FullName')]
        [string]$Path,

        [Parameter()]
        [Alias('l')]
        [string]$Language,

        [Parameter()]
        [Alias('m')]
        [ValidateSet('tiny', 'tiny.en', 'base', 'base.en', 'small', 'small.en', 'medium', 'medium.en', 'large-v1', 'large-v2', 'large-v3', 'large-v3-turbo', 'turbo', 'distil-large-v2', 'distil-large-v3', 'distil-large-v3.5', 'distil-medium.en', 'distil-small.en')]
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

    $effectiveModel = $Model ? $Model : ($Language -eq 'en' ? 'distil-large-v3.5' : 'large-v3')
    $languageDisplay = $Language ? $Language : '(auto-detect)'

    Write-Host "[$( Get-Date -Format 'HH:mm:ss' )] Transcribing: $( $item.Name )" -ForegroundColor Cyan
    Write-Host "             Model: $effectiveModel | Language: $languageDisplay" -ForegroundColor DarkGray

    $whisperArgs = @(
        '--model', $effectiveModel
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

    if ($Language) {
        $whisperArgs += '--language', $Language
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

<#
.SYNOPSIS
    Transcribe all media files in a folder.

.DESCRIPTION
    Batch transcribes all media in a directory. Skips files with existing SRT.
    See Invoke-Whisper for parameter details.

.PARAMETER Directory
    Directory to scan.

.PARAMETER Language
    Language code.

.EXAMPLE
    Invoke-WhisperFolder

.EXAMPLE
    Invoke-WhisperFolder -Directory C:\Videos -Force
#>
function Invoke-WhisperFolder {
    [CmdletBinding()]
    [Alias('whisperf')]
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

<#
.SYNOPSIS
    Transcribe Japanese audio/video.

.DESCRIPTION
    Invoke-Whisper with Language=ja preset.
    See Invoke-Whisper for parameter details.

.EXAMPLE
    Invoke-WhisperJapanese anime.mkv

.EXAMPLE
    Invoke-WhisperJapanese anime.mkv -Translate
#>
function Invoke-WhisperJapanese {
    [CmdletBinding()]
    [Alias('whisperj')]
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

<#
.SYNOPSIS
    Transcribe all Japanese media in a folder.

.DESCRIPTION
    Invoke-WhisperFolder with Language=ja preset.
    See Invoke-Whisper for parameter details.

.EXAMPLE
    Invoke-WhisperJapaneseFolder -Directory C:\Anime

.EXAMPLE
    Invoke-WhisperJapaneseFolder -Translate
#>
function Invoke-WhisperJapaneseFolder {
    [CmdletBinding()]
    [Alias('whisperjf')]
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

#endregion

#region YouTube

<#
.SYNOPSIS
    Download YouTube videos.

.PARAMETER Urls
    URLs to download.

.PARAMETER Transcribe
    Transcribe after download.

.PARAMETER Language
    Transcription language.

.PARAMETER Model
    Whisper model.

.PARAMETER Translate
    Translate to English.

.PARAMETER OutputDir
    Output directory.

.EXAMPLE
    Save-YouTubeVideo https://youtube.com/watch?v=xxx

.EXAMPLE
    Save-YouTubeVideo https://youtube.com/watch?v=xxx -Transcribe
#>
function Save-YouTubeVideo {
    [CmdletBinding()]
    [Alias('ytdl')]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromRemainingArguments)]
        [string[]]$Urls,

        [Parameter()]
        [switch]$Transcribe,

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

        if ($Transcribe -and (Test-Path $filePath)) {
            Invoke-Whisper -Path $filePath -Language $Language -Model $Model -Translate:$Translate -OutputDir $OutputDir.FullName
        }
    }
    Pop-Location
}

#endregion

#region Sync Shortcuts

<#
.SYNOPSIS
    Sync YouTube playlists to Google Sheets.

.DESCRIPTION
    Runs the C# CLI 'sync yt' command to sync YouTube playlists.

.PARAMETER Force
    Clear cache and re-fetch all data.

.PARAMETER Verbose
    Enable verbose/debug logging.

.EXAMPLE
    Invoke-YouTubeSync

.EXAMPLE
    syncyt -Force
#>
function Invoke-YouTubeSync {
    [CmdletBinding()]
    [Alias('syncyt')]
    param(
        [Parameter()]
        [switch]$Force,

        [Parameter()]
        [switch]$Verbose
    )

    Push-Location $Script:CSharpRoot
    try {
        $args = @('run', '--', 'sync', 'yt')
        if ($Force) { $args += '--force' }
        if ($Verbose) { $args += '--verbose' }
        & dotnet @args
    }
    finally {
        Pop-Location
    }
}

<#
.SYNOPSIS
    Sync Last.fm scrobbles to Google Sheets.

.DESCRIPTION
    Runs the C# CLI 'sync lastfm' command to sync scrobbles.

.PARAMETER Since
    Re-sync from date (yyyy/MM/dd). Deletes existing data on/after this date.

.PARAMETER Verbose
    Enable verbose/debug logging.

.EXAMPLE
    Invoke-LastFmSync

.EXAMPLE
    synclf -Since '2024/01/01'
#>
function Invoke-LastFmSync {
    [CmdletBinding()]
    [Alias('synclf')]
    param(
        [Parameter()]
        [string]$Since,

        [Parameter()]
        [switch]$Verbose
    )

    Push-Location $Script:CSharpRoot
    try {
        $args = @('run', '--', 'sync', 'lastfm')
        if ($Since) { $args += '--since', $Since }
        if ($Verbose) { $args += '--verbose' }
        & dotnet @args
    }
    finally {
        Pop-Location
    }
}

<#
.SYNOPSIS
    Run all daily syncs (YouTube + Last.fm).

.DESCRIPTION
    Runs both sync commands sequentially.

.EXAMPLE
    Invoke-AllSyncs

.EXAMPLE
    syncall
#>
function Invoke-AllSyncs {
    [CmdletBinding()]
    [Alias('syncall')]
    param()

    Write-Host "`n[YouTube Sync]" -ForegroundColor Cyan
    Invoke-YouTubeSync

    Write-Host "`n[Last.fm Sync]" -ForegroundColor Cyan
    Invoke-LastFmSync

    Write-Host "`nAll syncs complete!" -ForegroundColor Green
}

#endregion

#region Scheduled Tasks

<#
.SYNOPSIS
    Create scheduled task.

.PARAMETER TaskName
    Task name.

.PARAMETER Command
    Command to run.

.PARAMETER DailyTime
    Time to run.

.PARAMETER Description
    Task description.

.EXAMPLE
    Register-ScheduledSyncTask -TaskName MyTask -Command 'sync yt'
#>
function Register-ScheduledSyncTask {
    [CmdletBinding()]
    [Alias('regtask')]
    param(
        [Parameter(Mandatory)]
        [Alias('n')]
        [string]$TaskName,

        [Parameter(Mandatory)]
        [Alias('c')]
        [string]$Command,

        [Parameter()]
        [TimeSpan]$DailyTime = '09:00:00',

        [Parameter()]
        [string]$Description = 'Scheduled task'
    )

    $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        throw "This function requires administrator privileges. Run PowerShell as Administrator."
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
    $noBuildFlag = if (Test-Path -Path $dllPath) {
        '--no-build '
    }
    else {
        ''
    }

    $pwsh = (Get-Command -Name pwsh).Source
    $argument = "-NoProfile -NoLogo -WindowStyle Hidden -WorkingDirectory `"$Script:CSharpRoot`" -Command `"dotnet run $( $noBuildFlag )$Command; exit `$LASTEXITCODE`""

    $action = New-ScheduledTaskAction -Execute $pwsh -Argument $argument
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -RunOnlyIfNetworkAvailable -WakeToRun

    $start = [datetime]::Today.Add($DailyTime)
    if ($start -le (Get-Date)) {
        $start = $start.AddDays(1)
    }

    $trigger = New-ScheduledTaskTrigger -Daily -At $start

    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings -Description $Description | Out-Null

    Write-Host "Registered '$TaskName' for $($start.ToString('HH:mm') ) daily" -ForegroundColor Green
}

<#
.SYNOPSIS
    Register all sync tasks.

.EXAMPLE
    Register-AllSyncTasks
#>
function Register-AllSyncTasks {
    [CmdletBinding()]
    [Alias('regall')]
    param()

    Register-ScheduledSyncTask -TaskName 'LastFmSync' -Command 'sync lastfm' -DailyTime '09:00:00' -Description 'Syncs Last.fm scrobbles to Google Sheets'
    Register-ScheduledSyncTask -TaskName 'YouTubeSync' -Command 'sync yt' -DailyTime '10:00:00' -Description 'Syncs YouTube playlists to Google Sheets'
}

#endregion

#endregion

Export-ModuleMember -Function * -Alias *
