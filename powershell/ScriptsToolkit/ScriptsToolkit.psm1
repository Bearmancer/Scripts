# ScriptsToolkit Module File

class WhisperLanguageCompleter : System.Management.Automation.IValidateSetValuesGenerator {
    [string[]] GetValidValues() { return $Script:WhisperLanguages }
}

class WhisperModelCompleter : System.Management.Automation.IValidateSetValuesGenerator {
    [string[]] GetValidValues() { return $Script:WhisperModels }
}

$Script:RepositoryRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Script:CSharpRoot = Join-Path $Script:RepositoryRoot 'csharp'
$Script:CSharpPublish = Join-Path $Script:CSharpRoot 'publish'
$Script:ToolsExe = Join-Path $Script:CSharpPublish 'tools.exe'
$Script:PythonRoot = Join-Path $Script:RepositoryRoot 'python'
$Script:PythonCli = Join-Path $Script:RepositoryRoot 'python' 'toolkit' 'cli.py'
$Script:LogDirectory = Join-Path $Script:RepositoryRoot 'logs'
$Script:SyncTime = [TimeSpan]'09:00'

. "$PSScriptRoot\ScriptsToolkit.Data.ps1"

#region CLI Wrappers
function Invoke-Tools {
    <#
    .SYNOPSIS
    Invoke the C# CLI for sync, music metadata, and utilities.

    .DESCRIPTION
    Runs the compiled tools.exe with the specified arguments. This is the main entry point
    for all C# CLI commands including sync operations, music metadata queries, and utilities.
    Falls back to dotnet run if the compiled exe is not found.

    .PARAMETER Arguments
    Arguments to pass to the C# CLI.

    .EXAMPLE
    Invoke-Tools sync yt
    Syncs YouTube playlists to Google Sheets.

    .EXAMPLE
    tools music search "Beethoven Symphony 5"
    Searches for music metadata using the 'tools' alias.

    .EXAMPLE
    Invoke-Tools --help
    Shows available CLI commands.
    #>
    [Alias('tools')]
    [CmdletBinding()]
    param([Parameter(ValueFromRemainingArguments)][string[]]$Arguments)

    if (Test-Path $Script:ToolsExe) {
        & $Script:ToolsExe @Arguments
    }
    else {
        Write-Warning "tools.exe not found. Run 'regall' to compile. Falling back to dotnet run..."
        dotnet run --project $Script:CSharpRoot -- @Arguments
    }
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

    Invoke-Tools sync yt @args
}

function Sync-LastFm {
    <#
    .SYNOPSIS
    Sync Last.fm scrobbles to Google Sheets.

    .DESCRIPTION
    Fetches scrobble history from Last.fm API and syncs to a configured Google Sheets spreadsheet.
    Supports incremental sync from last known timestamp and optional date filtering.
    Always rebuilds from source to ensure latest changes are reflected.

    .PARAMETER Date
    Sync scrobbles from a specific date onwards. Format: dd/MM/yyyy (e.g., 20/01/2026).
    This will delete existing scrobbles from that date forward and re-sync them.

    .EXAMPLE
    Sync-LastFm
    Runs full Last.fm scrobble sync from last known timestamp.

    .EXAMPLE
    music 20/01/2026
    Sync scrobbles from January 20, 2026 onwards using the 'music' alias.

    .EXAMPLE
    Sync-LastFm -Date 01/12/2024
    Re-sync scrobbles from December 1, 2024.
    #>
    [Alias('music')]
    [CmdletBinding()]
    param(
        [Parameter(Position = 0, ValueFromPipeline)]
        [string]$Date
    )

    $cliArgs = @('sync', 'lastfm')

    if ($Date) {
        try {
            $dateObj = [datetime]::ParseExact($Date, 'dd/MM/yyyy', [System.Globalization.CultureInfo]::InvariantCulture)
            $formattedDate = $dateObj.ToString('yyyy/MM/dd', [System.Globalization.CultureInfo]::InvariantCulture)
            $cliArgs += '--since', $formattedDate
        }
        catch {
            Write-Error -Message "Invalid date format. Expected dd/MM/yyyy (e.g., 20/01/2026). Received: '$Date'" -ErrorAction Stop
            return
        }
    }
    Invoke-Tools @cliArgs @args
}

function Sync-All {
    <#
    .SYNOPSIS
    Sync both YouTube playlists and Last.fm scrobbles.

    .DESCRIPTION
    Runs YouTube sync followed by Last.fm sync in sequence. Useful for scheduled tasks
    or manual full syncs. Always builds from source.

    .EXAMPLE
    Sync-All
    Syncs YouTube then Last.fm.

    .EXAMPLE
    sync
    Same as above using the 'sync' alias.
    #>
    [Alias('sync')]
    [CmdletBinding()]
    param()

    $cliArgs = @('sync', 'all')

    Invoke-Tools @cliArgs @args
}
#endregion CLI Wrappers

#region Python Toolkit Wrappers
function Invoke-Toolkit {
    <#
    .SYNOPSIS
    Invoke the Python toolkit CLI for audio, video, and filesystem operations.

    .DESCRIPTION
    Runs the Python toolkit CLI with the specified arguments. This is the main entry point
    for all Python toolkit commands including audio conversion, video processing, and filesystem tools.

    .PARAMETER Arguments
    Arguments to pass to the Python toolkit CLI.

    .EXAMPLE
    Invoke-Toolkit audio convert -d C:\Music -f flac
    Converts audio files in the specified directory to FLAC.

    .EXAMPLE
    Invoke-Toolkit video remux -p C:\Disc
    Remuxes a disc to MKV.
    #>
    [CmdletBinding()]
    param([Parameter(ValueFromRemainingArguments)][string[]]$Arguments)

    & uv run --directory $Script:PythonRoot toolkit @Arguments
}

function Convert-Audio {
    <#
    .SYNOPSIS
    Convert audio files to various formats or extract SACD ISOs.

    .DESCRIPTION
    Converts audio files in a directory. Supports FLAC downsampling and SACD ISO extraction.

    .PARAMETER Directory
    Directory containing audio files. Defaults to current directory.

    .PARAMETER Mode
    Mode: 'convert' for FLAC conversion or 'extract' for SACD extraction.

    .PARAMETER Format
    Output format (e.g., 'all', '24-bit', 'mp3'). Default is 'all'.

    .EXAMPLE
    Convert-Audio -Directory C:\Music\Album -Format flac
    Converts audio files to FLAC.

    .EXAMPLE
    sacd -Directory C:\SACD -Mode extract
    Extracts SACD ISO files.
    #>
    [Alias('sacd')]
    [CmdletBinding()]
    param(
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .),

        [Alias('m')]
        [ValidateSet('convert', 'extract')]
        [string]$Mode = 'convert',

        [Alias('f')]
        [string]$Format = 'all'
    )

    Invoke-Toolkit audio convert -d $Directory.FullName -m $Mode -f $Format
}

function Rename-AudioRed {
    <#
    .SYNOPSIS
    Rename files with excessively long paths for RED compatibility.

    .DESCRIPTION
    Renames files whose paths exceed 180 characters for compatibility with RED/OPS uploading.

    .PARAMETER Directory
    Directory containing audio files. Defaults to current directory.

    .EXAMPLE
    Rename-AudioRed -Directory C:\Music\LongAlbumName
    Renames files with long paths.
    #>
    [Alias('renred')]
    [CmdletBinding()]
    param(
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .)
    )

    Invoke-Toolkit audio rename -d $Directory.FullName
}

function Get-AudioArtReport {
    <#
    .SYNOPSIS
    Report embedded artwork sizes in FLAC files.

    .DESCRIPTION
    Scans FLAC files and reports any with embedded artwork larger than 1MB.

    .PARAMETER Directory
    Directory containing FLAC files. Defaults to current directory.

    .EXAMPLE
    Get-AudioArtReport -Directory C:\Music\Album
    Reports artwork sizes in the specified directory.
    #>
    [Alias('artreport')]
    [CmdletBinding()]
    param(
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .)
    )

    Invoke-Toolkit audio art-report -d $Directory.FullName
}

function Invoke-Remux {
    <#
    .SYNOPSIS
    Remux DVD/Blu-ray discs to MKV.

    .DESCRIPTION
    Remuxes DVD or Blu-ray disc structures to MKV files using MakeMKV.

    .PARAMETER Path
    Path to disc folder (containing VIDEO_TS or BDMV). Defaults to current directory.

    .PARAMETER SkipMediaInfo
    Skip MediaInfo generation after remuxing.

    .EXAMPLE
    Invoke-Remux -Path C:\Disc\BDMV
    Remuxes a Blu-ray disc to MKV.

    .EXAMPLE
    remux
    Remuxes disc in current directory.
    #>
    [Alias('remux')]
    [CmdletBinding()]
    param(
        [Alias('p')]
        [System.IO.DirectoryInfo]$Path = (Get-Item .),

        [switch]$SkipMediaInfo
    )

    $tkArgs = @('video', 'remux', '-p', $Path.FullName)
    if ($SkipMediaInfo) { $tkArgs += '--skip-mediainfo' }
    Invoke-Toolkit @tkArgs
}

function Compress-Video {
    <#
    .SYNOPSIS
    Batch compress MKV files using HandBrake.

    .DESCRIPTION
    Compresses all MKV files in a directory to MP4 using HandBrake presets.

    .PARAMETER Directory
    Directory containing MKV files. Defaults to current directory.

    .EXAMPLE
    Compress-Video -Directory C:\Videos
    Compresses all MKV files in the directory.
    #>
    [Alias('hb')]
    [CmdletBinding()]
    param(
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .)
    )

    Invoke-Toolkit video compress -d $Directory.FullName
}

function Get-VideoChapters {
    <#
    .SYNOPSIS
    Extract chapters from video files.

    .DESCRIPTION
    Extracts individual chapters from video files as separate files.

    .PARAMETER Path
    Video file or directory containing video files. Defaults to current directory.

    .EXAMPLE
    Get-VideoChapters -Path C:\Videos\movie.mkv
    Extracts chapters from the video file.
    #>
    [CmdletBinding()]
    param(
        [Alias('p')]
        [string]$Path = '.'
    )

    Invoke-Toolkit video chapters -p $Path
}

function Get-VideoResolutions {
    <#
    .SYNOPSIS
    Print resolution information for video files.

    .DESCRIPTION
    Displays width, height, and resolution for video files.

    .PARAMETER Path
    Video file or directory containing video files. Defaults to current directory.

    .EXAMPLE
    Get-VideoResolutions -Path C:\Videos
    Prints resolution info for all videos in the directory.
    #>
    [CmdletBinding()]
    param(
        [Alias('p')]
        [string]$Path = '.'
    )

    Invoke-Toolkit video resolutions -p $Path
}

function New-Gif {
    <#
    .SYNOPSIS
    Create optimized GIF from video file.

    .DESCRIPTION
    Creates an optimized GIF from a video file with configurable start time, duration, and size.

    .PARAMETER InputFile
    Input video file.

    .PARAMETER Start
    Start time in mm:ss format. Default is '00:00'.

    .PARAMETER Duration
    Duration in seconds. Default is 30.

    .PARAMETER MaxSize
    Maximum GIF size in MiB. Default is 300.

    .PARAMETER OutputDirectory
    Output directory. Defaults to Desktop.

    .EXAMPLE
    New-Gif -InputFile movie.mkv -Start 1:23 -Duration 10
    Creates a 10-second GIF starting at 1:23.
    #>
    [Alias('gif')]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [Alias('i')]
        [string]$InputFile,

        [Alias('s')]
        [string]$Start = '00:00',

        [Alias('d')]
        [int]$Duration = 30,

        [Alias('m')]
        [int]$MaxSize = 300,

        [Alias('o')]
        [string]$OutputDirectory = [System.IO.Path]::Combine([System.Environment]::GetFolderPath('Desktop'))
    )

    Invoke-Toolkit video gif -i $InputFile -s $Start -d $Duration -m $MaxSize -o $OutputDirectory
}

function Get-VideoThumbnails {
    <#
    .SYNOPSIS
    Extract thumbnail grid and full-size images from video.

    .DESCRIPTION
    Creates a thumbnail grid and individual full-size screenshots from a video file.

    .PARAMETER Path
    Video file to extract thumbnails from.

    .EXAMPLE
    Get-VideoThumbnails -Path movie.mkv
    Extracts thumbnails from the video.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [Alias('p')]
        [string]$Path
    )

    Invoke-Toolkit video thumbnails -p $Path
}

function New-Torrent {
    <#
    .SYNOPSIS
    Create RED and OPS torrents for a directory.

    .DESCRIPTION
    Creates torrent files for uploading to RED and OPS music trackers.

    .PARAMETER Directory
    Directory to create torrent for. Defaults to current directory.

    .PARAMETER IncludeSubdirectories
    Create torrents for each subdirectory instead of the root.

    .EXAMPLE
    New-Torrent -Directory C:\Music\Album
    Creates torrents for the album directory.

    .EXAMPLE
    mktor -IncludeSubdirectories
    Creates torrents for each subdirectory.
    #>
    [Alias('mktor')]
    [CmdletBinding()]
    param(
        [Alias('d')]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .),

        [switch]$IncludeSubdirectories
    )

    $tkArgs = @('filesystem', 'torrents', '-d', $Directory.FullName)
    if ($IncludeSubdirectories) { $tkArgs += '--include-subdirectories' }
    Invoke-Toolkit @tkArgs
}

#endregion Python Toolkit Wrappers

#region Whisper Transcription
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

function Invoke-WhisperEnglish {
    <#
    .SYNOPSIS
    Transcribe English audio/video using the fast distilled model.

    .DESCRIPTION
    Wrapper for Invoke-Whisper optimized for English content using the distil-large-v3.5 model,
    which is faster while maintaining high accuracy for English.

    .PARAMETER InputPath
    Path to the audio/video file or directory to transcribe.

    .PARAMETER Quiet
    Suppress verbose whisper output.

    .PARAMETER ExtraArgs
    Additional arguments to pass to whisper-ctranslate2.

    .EXAMPLE
    Invoke-WhisperEnglish podcast.mp3
    Transcribes English podcast using the fast distilled model.

    .EXAMPLE
    whisp lecture.mp4
    Transcribes using the 'whisp' alias.
    #>
    [Alias('whisp')]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('FilePath', 'FullName', 'Path')]
        [string]$InputPath,

        [Alias('q')]
        [switch]$Quiet,

        [Parameter(ValueFromRemainingArguments)]
        [string[]]$ExtraArgs
    )

    process {
        Invoke-Whisper $InputPath -Language en -Model 'distil-large-v3.5' -Quiet:$Quiet @ExtraArgs
    }
}
#endregion Whisper Transcription

#region YouTube
function Save-YouTubeDownload {
    <#
    .SYNOPSIS
    Download YouTube videos and optionally transcribe them.

    .DESCRIPTION
    Downloads YouTube videos using yt-dlp and optionally transcribes them using Whisper.
    By default, downloads are transcribed with Invoke-Whisper unless -NoTranscribe is specified.

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
        else { Invoke-Whisper $filePath -Language en -Model 'distil-large-v3.5' }
    }
}
#endregion YouTube

#region Scheduled Tasks
function Register-LastFmSyncTask {
    <#
    .SYNOPSIS
    Register Last.fm sync scheduled task.

    .DESCRIPTION
    Creates a Windows Scheduled Task for Last.fm sync at 09:00 daily.
    Catches up missed runs automatically.

    .EXAMPLE
    Register-LastFmSyncTask
    Registers Last.fm sync at 09:00 AM.
    #>
    [Alias('reglfm')]
    [CmdletBinding()]
    param()

    Register-SyncTaskInternal -TaskName 'LastFmSync' -Command 'sync lastfm' -DailyTime $Script:SyncTime -Description "Sync Last.fm scrobbles ($Script:SyncTime daily)"
}

function Register-YouTubeSyncTask {
    <#
    .SYNOPSIS
    Register YouTube sync scheduled task.

    .DESCRIPTION
    Creates a Windows Scheduled Task for YouTube sync at 09:00 daily.
    Catches up missed runs automatically.

    .EXAMPLE
    Register-YouTubeSyncTask
    Registers YouTube sync at 09:00 AM.
    #>
    [Alias('regyt')]
    [CmdletBinding()]
    param()

    Register-SyncTaskInternal -TaskName 'YouTubeSync' -Command 'sync yt' -DailyTime $Script:SyncTime -Description "Sync YouTube playlists ($Script:SyncTime daily)"
}


function Register-AllSyncTasks {
    <#
    .SYNOPSIS
    Compile tools.exe and register all sync scheduled tasks.

    .DESCRIPTION
    Compiles the C# project to tools.exe, then registers:
    - LastFmSync:  09:00
    - YouTubeSync: 09:00

    Tasks use StartWhenAvailable to catch up missed runs.
    #>
    [Alias('regall')]
    [CmdletBinding()]
    param()

    Assert-NetworkAvailable
    Build-Tools
    Register-LastFmSyncTask
    Register-YouTubeSyncTask
}

function Build-Tools {
    <#
    .SYNOPSIS
    Compile the C# project to tools.exe.

    .DESCRIPTION
    Runs dotnet publish to create a single-file executable at csharp/publish/tools.exe.

    .EXAMPLE
    Build-Tools
    Compiles the C# project.
    #>
    [CmdletBinding()]
    param()

    Write-Information "$(Get-Timestamp) Compiling tools.exe..." -InformationAction Continue
    $result = dotnet publish $Script:CSharpRoot -c Release -r win-x64 -o $Script:CSharpPublish 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Compilation failed: $result"
    }
    Write-Information "$(Get-Timestamp) Compiled: $Script:ToolsExe" -InformationAction Continue
}

function Unregister-AllSyncTasks {
    <#
    .SYNOPSIS
    Unregister all sync scheduled tasks.

    .DESCRIPTION
    Removes LastFmSync and YouTubeSync scheduled tasks.
    #>
    [Alias('unreg')]
    [CmdletBinding()]
    param()

    @('LastFmSync', 'YouTubeSync') | ForEach-Object {
        $task = Get-ScheduledTask -TaskName $_ -ErrorAction Ignore
        if ($task) {
            Unregister-ScheduledTask -TaskName $_ -Confirm:$false
            Write-Information "$(Get-Timestamp) Removed: $_" -InformationAction Continue
        }
    }
}

function Register-SyncTaskInternal {
    <#
    .SYNOPSIS
    Internal helper to register or update a sync scheduled task.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$TaskName,
        [Parameter(Mandatory)][string]$Command,
        [Parameter(Mandatory)][TimeSpan]$DailyTime,
        [Parameter(Mandatory)][string]$Description
    )

    if (-not (Test-Path $Script:ToolsExe)) {
        throw "tools.exe not found: $Script:ToolsExe. Run Build-Tools or regall first."
    }

    $taskScript = @"
`$ErrorActionPreference = 'Continue'
try {
    & '$Script:ToolsExe' $Command
    if (`$LASTEXITCODE -ne 0) {
        Write-Information "Sync failed with exit code: `$LASTEXITCODE"
        Read-Host 'Press Enter to close'
    }
}
catch {
    Write-Information "ERROR: `$_"
    Read-Host 'Press Enter to close'
}
"@
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($taskScript))
    $pwsh = (Get-Command pwsh).Source

    $action = New-ScheduledTaskAction -Execute $pwsh -Argument "-NoProfile -EncodedCommand $encoded" -WorkingDirectory $Script:RepositoryRoot
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -RunOnlyIfNetworkAvailable -WakeToRun -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Hours 2)

    $start = [datetime]::Today.Add($DailyTime)
    if ($start -le (Get-Date)) { $start = $start.AddDays(1) }

    $dailyTrigger = New-ScheduledTaskTrigger -Daily -At $start

    $existing = Get-ScheduledTask -TaskName $TaskName -ErrorAction Ignore
    if ($existing) {
        Set-ScheduledTask -TaskName $TaskName -Action $action -Trigger $dailyTrigger -Settings $settings | Out-Null
        Write-Information "$(Get-Timestamp) Updated: $TaskName (Daily: $($start.ToString('HH:mm')))" -InformationAction Continue
    }
    else {
        Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $dailyTrigger -Settings $settings -Description $Description | Out-Null
        Write-Information "$(Get-Timestamp) Registered: $TaskName (Daily: $($start.ToString('HH:mm')))" -InformationAction Continue
    }
}
#endregion Scheduled Tasks

#region Log Files
function Get-SyncLog {
    <#
    .SYNOPSIS
    View sync session logs.

    .DESCRIPTION
    Displays sync logs from JSONL log files.

    Default view shows sync runs (sessions). Use -Raw to see individual log events.

    For YouTube playlist changes with video titles, use Get-YouTubePlaylistLog (alias: ytlog).

    .PARAMETER Raw
    Show individual log events (SessionStart, PlaylistUpdated, Exception, etc.) with full JSON data.

    .PARAMETER Service
    Filter by service: 'yt', 'lfm', or 'all' (default).

    .PARAMETER Size
    Number of rows to display. Default is 10. Alias: -n

    .PARAMETER Session
    Filter to a specific session by ID prefix (first 8 chars).

    .PARAMETER Errors
    Show only error-level events. Implies -Raw view.

    .PARAMETER Asc
    Sort ascending (oldest first). Default is newest first.

    .PARAMETER Full
    Show extended columns (Session ID, Duration, Event count). Alias: -v

    .EXAMPLE
    viewlog
    Shows the 10 most recent sync runs.

    .EXAMPLE
    viewlog -Raw -n 20
    Shows 20 most recent raw log events with full JSON data.

    .EXAMPLE
    viewlog -Errors
    Shows error-level log entries.

    .EXAMPLE
    viewlog -Raw -Session a1b2c3d4
    Shows log entries for a specific session.

    .EXAMPLE
    viewlog -Full
    Shows sync runs with session ID, duration, and event count.

    .LINK
    Get-YouTubePlaylistLog
    #>
    [Alias('viewlog')]
    [CmdletBinding()]
    param(
        # View switches
        [Alias('r')]
        [switch]$Raw,

        [ValidateSet('yt', 'lfm', 'music', 'all')]
        [string]$Service = 'all',

        [Alias('n')]
        [int]$Size = 10,

        [string]$Session,

        [switch]$Errors,

        [switch]$Asc,

        [Alias('v')]
        [switch]$Full
    )

    $view = if ($Raw -or $Errors) { 'Raw' } else { 'Runs' }

    $serviceMap = @{ 'yt' = 'youtube'; 'lfm' = 'lastfm'; 'music' = 'music'; 'all' = 'all' }
    $mappedService = $serviceMap[$Service]

    $logFiles = @{
        youtube = Join-Path $Script:LogDirectory 'youtube.jsonl'
        lastfm  = Join-Path $Script:LogDirectory 'lastfm.jsonl'
        music   = Join-Path $Script:LogDirectory 'music.jsonl'
    }
    $services = if ($mappedService -eq 'all') { @('youtube', 'lastfm', 'music') } else { @($mappedService) }

    $allEntries = Get-SyncLogEntriesFromFiles $services $logFiles

    if (-not $allEntries) {
        Write-Information "$(Get-Timestamp) No log entries found" -InformationAction Continue
        return
    }

    if ($Session) {
        $allEntries = @($allEntries | Where-Object { $_.SessionId -and $_.SessionId -like "$Session*" })
    }

    $allEntries = Add-SyncLogParsedTimestamp $allEntries

    if ($Errors) {
        $allEntries = @($allEntries | Where-Object { $_.Level -eq 'Error' })
    }

    $descending = -not $Asc

    switch ($view) {
        'Raw' {
            $sorted = Select-SyncLogRows -Items $allEntries -Size $Size -Descending:$descending -SortProperty 'ParsedTimestamp'
            $output = ConvertTo-SyncLogEntryOutput $sorted
            $props = if ($Full) { @('Timestamp', 'Session', 'Service', 'Level', 'Event', 'Details') } else { @('Timestamp', 'Service', 'Level', 'Event', 'Details') }
            $output | Format-Table -Property $props -AutoSize -Wrap | Out-Host -Paging
        }
        default {
            $sessions = ConvertTo-SyncLogSessionOutput $allEntries
            if (-not $sessions) {
                Write-Information "$(Get-Timestamp) No sessions found" -InformationAction Continue
                return
            }
            $display = Select-SyncLogRows -Items $sessions -Size $Size -Descending:$descending -SortProperty '_Sort'
            $props = if ($Full) { @('Timestamp', 'Session', 'Service', 'Duration', 'Events', 'Status', 'Summary') } else { @('Timestamp', 'Service', 'Status', 'Summary') }
            $display | Format-Table -Property $props -AutoSize -Wrap
        }
    }
}

#region SyncLog Helpers
function ConvertTo-NormalizedLogEntry {
    param($Entry, [string]$ServiceName)
    
    if ($Entry.'@t') {
        # CLEF format — normalize
        $eventName = ($Entry.'@mt' -split '\s')[0]  # Extract first word from template
        $level = if ($Entry.'@l') { $Entry.'@l' } else { 'Information' }
        # Map Serilog levels to short display names
        $levelMap = @{ 'Verbose' = 'Debug'; 'Debug' = 'Debug'; 'Information' = 'Info'; 'Warning' = 'Warning'; 'Error' = 'Error'; 'Fatal' = 'Fatal' }
        $displayLevel = if ($levelMap[$level]) { $levelMap[$level] } else { $level }
        
        # Build Data object from all non-CLEF properties
        $dataProps = @{}
        foreach ($prop in $Entry.PSObject.Properties) {
            if ($prop.Name -notmatch '^@' -and $prop.Name -ne 'Service') {
                $dataProps[$prop.Name] = $prop.Value
            }
        }

        $normalized = [PSCustomObject]@{
            Timestamp = $Entry.'@t'
            Level     = $displayLevel
            Event     = $eventName
            SessionId = $Entry.SessionId
            Data      = [PSCustomObject]$dataProps
            Service   = $ServiceName
        }
        $normalized
    }
    else {
        # Legacy format — passthrough with Service added
        $Entry | Add-Member -NotePropertyName 'Service' -NotePropertyValue $ServiceName -Force -PassThru
    }
}

function Get-SyncLogEntriesFromFiles {
    param(
        [string[]]$Services,
        [hashtable]$LogFiles
    )

    foreach ($svc in $Services) {
        $path = $LogFiles[$svc]
        if (Test-Path $path) {
            Get-Content $path | ForEach-Object {
                $raw = $_ | ConvertFrom-Json
                ConvertTo-NormalizedLogEntry -Entry $raw -ServiceName $svc
            }
        }
    }
}

function Add-SyncLogParsedTimestamp {
    param([object[]]$Entries)

    $Entries | Where-Object { $_.SessionId -and $_.Timestamp } | ForEach-Object {
        $parsed = $null
        $ts = $_.Timestamp
        
        # Try ISO 8601 first (CLEF format)
        if ($ts -is [datetime]) {
            $parsed = $ts
        }
        elseif ($ts -match 'T.*Z$') {
            $parsed = try { [datetime]::Parse($ts, [System.Globalization.CultureInfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::RoundtripKind) } catch { $null }
        }
        
        # Fallback: legacy format
        if (-not $parsed) {
            $parsed = try { [datetime]::ParseExact($ts, $Script:DATETIME_FORMAT, [System.Globalization.CultureInfo]::InvariantCulture) } catch { try { [datetime]$ts } catch { $null } }
        }
        
        if ($parsed) {
            # Convert UTC to local time for display
            if ($parsed.Kind -eq [System.DateTimeKind]::Utc) {
                $parsed = $parsed.ToLocalTime()
            }
            $_ | Add-Member -NotePropertyName 'ParsedTimestamp' -NotePropertyValue $parsed -Force -PassThru
        }
    } | Where-Object ParsedTimestamp
}

function Get-SyncLogEntryDetails {
    param($Entry)

    $entryData = $Entry.Data
    if (-not $entryData) { return '' }

    $parts = $entryData.PSObject.Properties | Where-Object { $_.Name -notin 'Service', 'ProcessId' } | ForEach-Object {
        $val = if ($_.Value -is [array]) { $_.Value -join ', ' } else { $_.Value }
        "$($_.Name): $val"
    }

    $parts -join "`n"
}

function ConvertTo-SyncLogEntryOutput {
    param([object[]]$Entries)

    $svcMap = @{ 'youtube' = 'YouTube'; 'lastfm' = 'LastFM' }
    $Entries | ForEach-Object {
        [PSCustomObject]@{
            Timestamp = $_.ParsedTimestamp.ToString('yyyy\/MM\/dd HH:mm')
            Session   = $_.SessionId?.Substring(0, 8) ?? ''
            Service   = $svcMap[$_.Service] ?? $_.Service
            Level     = $_.Level
            Event     = $_.Event
            Details   = Get-SyncLogEntryDetails $_
        }
    }
}

function Format-YouTubePlaylistSimple {
    <#
    .SYNOPSIS
    Simple compact view of YouTube playlist changes with per-playlist tables.
    #>
    param([object[]]$Entries)

    foreach ($entry in $Entries) {
        $data = $entry.Data
        $timestamp = $entry.ParsedTimestamp.ToString('yyyy\/MM\/dd HH:mm')
        $playlist = $data.Title
        $added = if ($data.Added) { $data.Added } elseif ($entry.Event -eq 'PlaylistCreated' -and $data.Videos) { $data.Videos } else { 0 }
        $removed = if ($data.Removed) { $data.Removed } else { 0 }

        Write-Host ""
        Write-Host "$playlist " -NoNewline -ForegroundColor Cyan
        Write-Host "($timestamp) " -NoNewline -ForegroundColor DarkGray
        Write-Host "[+$added/-$removed]" -ForegroundColor DarkGray

        if ($data.AddedVideos -and $data.AddedVideos.Count -gt 0) {
            foreach ($video in $data.AddedVideos) {
                Write-Host "  + " -NoNewline -ForegroundColor Green
                Write-Host $video
            }
        }

        if ($data.RemovedVideos -and $data.RemovedVideos.Count -gt 0) {
            foreach ($video in $data.RemovedVideos) {
                Write-Host "  - " -NoNewline -ForegroundColor Red
                Write-Host $video -ForegroundColor DarkGray
            }
        }
    }
    Write-Host ""
}

function Format-YouTubePlaylistDetailed {
    <#
    .SYNOPSIS
    Detailed view of YouTube playlist changes with session IDs and full video lists.
    #>
    param([object[]]$Entries)

    foreach ($entry in $Entries) {
        $data = $entry.Data
        $timestamp = $entry.ParsedTimestamp.ToString('yyyy\/MM\/dd HH:mm')
        $playlist = $data.Title
        $added = if ($data.Added) { $data.Added } elseif ($entry.Event -eq 'PlaylistCreated' -and $data.Videos) { $data.Videos } else { 0 }
        $removed = if ($data.Removed) { $data.Removed } else { 0 }
        $session = $entry.SessionId.Substring(0, 8)

        Write-Host ""
        Write-Host "$timestamp  " -NoNewline -ForegroundColor DarkGray
        Write-Host "$session  " -NoNewline -ForegroundColor DarkYellow
        Write-Host $playlist -NoNewline -ForegroundColor Cyan
        Write-Host "  [" -NoNewline -ForegroundColor DarkGray
        if ($added -gt 0) { Write-Host "+$added" -NoNewline -ForegroundColor Green }
        if ($added -gt 0 -and $removed -gt 0) { Write-Host " / " -NoNewline -ForegroundColor DarkGray }
        if ($removed -gt 0) { Write-Host "-$removed" -NoNewline -ForegroundColor Red }
        Write-Host "]" -ForegroundColor DarkGray

        if ($data.AddedVideos) {
            foreach ($video in $data.AddedVideos) {
                Write-Host "        + " -NoNewline -ForegroundColor Green
                Write-Host $video -ForegroundColor White
            }
        }

        if ($data.RemovedVideos) {
            foreach ($video in $data.RemovedVideos) {
                Write-Host "        - " -NoNewline -ForegroundColor Red
                Write-Host $video -ForegroundColor DarkGray
            }
        }
    }
    Write-Host ""
}

function ConvertTo-SyncLogSessionOutput {
    param([object[]]$Entries)

    $Entries | Group-Object SessionId | ForEach-Object {
        $group = @($_.Group | Sort-Object ParsedTimestamp)
        $first = $group[0]
        if (-not $first -or -not $first.SessionId) { return }

        $startTime = $first.ParsedTimestamp
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

        $dur = if ($endTime) { $endTime - $startTime } else { $null }
        $durStr = if ($dur) { '{0:mm\:ss}' -f $dur } else { '-' }
        $svcMap = @{ 'youtube' = 'YouTube'; 'lastfm' = 'LastFM' }

        [PSCustomObject]@{
            Timestamp = $startTime.ToString('yyyy\/MM\/dd HH:mm')
            Session   = $first.SessionId.Substring(0, 8)
            Service   = $svcMap[$first.Service] ?? $first.Service
            Duration  = $durStr
            Events    = $group.Count
            Status    = $status
            Summary   = $summary
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

    $selected = $Items | Sort-Object -Property $SortProperty -Descending | Select-Object -First $Size

    if ($Descending) {
        $selected
    }
    else {
        $selected | Sort-Object -Property $SortProperty
    }
}
#endregion SyncLog Helpers

function Get-SyncStatus {
    <#
    .SYNOPSIS
    Quick health check for sync services.

    .DESCRIPTION
    Shows the last sync time and status for YouTube and Last.fm services.

    .EXAMPLE
    Get-SyncStatus
    Shows sync health for all services.

    .EXAMPLE
    syncstatus
    Same as above using the alias.
    #>
    [Alias('syncstatus')]
    [CmdletBinding()]
    param()

    $logFiles = @{
        youtube = Join-Path $Script:LogDirectory 'youtube.jsonl'
        lastfm  = Join-Path $Script:LogDirectory 'lastfm.jsonl'
    }

    $results = foreach ($svc in @('youtube', 'lastfm')) {
        $path = $logFiles[$svc]
        if (-not (Test-Path $path)) {
            [PSCustomObject]@{ Service = $svc; LastSync = '-'; Status = 'No logs'; Age = '-' }
            continue
        }

        $entries = Get-Content $path | ForEach-Object { $_ | ConvertFrom-Json }
        $sessions = $entries | Where-Object { $_.Event -eq 'SessionEnd' -or $_.Event -eq 'SessionInterrupted' } |
        Sort-Object { try { [datetime]::ParseExact($_.Timestamp, $Script:DATETIME_FORMAT, $null) } catch { [datetime]$_.Timestamp } } -Descending |
        Select-Object -First 1

        if (-not $sessions) {
            [PSCustomObject]@{ Service = $svc; LastSync = '-'; Status = 'No completed sessions'; Age = '-' }
            continue
        }

        $ts = try { [datetime]::ParseExact($sessions.Timestamp, $Script:DATETIME_FORMAT, $null) } catch { [datetime]$sessions.Timestamp }
        $age = (Get-Date) - $ts
        $ageStr = if ($age.TotalDays -ge 1) { "$([int]$age.TotalDays)d ago" } elseif ($age.TotalHours -ge 1) { "$([int]$age.TotalHours)h ago" } else { "$([int]$age.TotalMinutes)m ago" }
        $status = if ($sessions.Data.Status) { $sessions.Data.Status } else { 'Unknown' }

        [PSCustomObject]@{
            Service  = $svc
            LastSync = $ts.ToString('yyyy\/MM\/dd HH:mm')
            Status   = $status
            Age      = $ageStr
        }
    }

    $results | Format-Table -AutoSize
}

function Get-YouTubePlaylistLog {
    <#
    .SYNOPSIS
    View YouTube playlist changes.

    .DESCRIPTION
    Shows YouTube playlist updates with added/removed video titles.
    
    Default view: compact summary with playlist name and change counts.
    Detailed view (-Full): includes session ID, timestamps, and full video titles.

    .PARAMETER Size
    Number of playlists to display. Default is 10. Alias: -n

    .PARAMETER Session
    Filter to a specific session by ID prefix (first 8 chars).

    .PARAMETER Changes
    Only show playlists with actual adds/removes (excludes no-change syncs).

    .PARAMETER Asc
    Sort ascending (oldest first). Default is newest first.

    .PARAMETER Full
    Show detailed view with session IDs and expanded video info. Alias: -v

    .EXAMPLE
    ytlog
    Shows the 10 most recent YouTube playlist updates (compact view).

    .EXAMPLE
    ytlog -n 20
    Shows 20 most recent playlist updates.

    .EXAMPLE
    ytlog -Changes
    Shows only playlists with actual video changes.

    .EXAMPLE
    ytlog -Full
    Shows detailed view with session IDs and full descriptions.

    .LINK
    Get-SyncLog
    #>
    [Alias('ytlog')]
    [CmdletBinding()]
    param(
        [Alias('n')]
        [int]$Size = 10,

        [string]$Session,

        [switch]$Changes,

        [switch]$Asc,

        [Alias('v')]
        [switch]$Full
    )

    $logPath = Join-Path $Script:LogDirectory 'youtube.jsonl'
    if (-not (Test-Path $logPath)) {
        Write-Information "$(Get-Timestamp) No YouTube log found" -InformationAction Continue
        return
    }

    $allEntries = Get-Content $logPath | ForEach-Object {
        $raw = $_ | ConvertFrom-Json
        ConvertTo-NormalizedLogEntry -Entry $raw -ServiceName 'youtube'
    }

    if ($Session) {
        $allEntries = @($allEntries | Where-Object { $_.SessionId -and $_.SessionId -like "$Session*" })
    }

    $allEntries = Add-SyncLogParsedTimestamp $allEntries

    $playlistEntries = @($allEntries | Where-Object { $_.Event -eq 'PlaylistUpdated' -or $_.Event -eq 'PlaylistCreated' })
    if ($Changes) {
        $playlistEntries = @($playlistEntries | Where-Object { $_.Data.Added -gt 0 -or $_.Data.Removed -gt 0 })
    }

    if (-not $playlistEntries) {
        Write-Information "$(Get-Timestamp) No playlist entries found" -InformationAction Continue
        return
    }

    $descending = -not $Asc
    $sorted = Select-SyncLogRows -Items $playlistEntries -Size $Size -Descending:$descending -SortProperty 'ParsedTimestamp'
    
    if ($Full) {
        Format-YouTubePlaylistDetailed $sorted
    }
    else {
        Format-YouTubePlaylistSimple $sorted
    }
}

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

function Get-ToolkitCommand {
    <#
    .SYNOPSIS
    List all available ScriptsToolkit commands and their aliases.

    .DESCRIPTION
    Displays a formatted table of all exported functions, their aliases, and descriptions
    from the ScriptsToolkit module.

    .EXAMPLE
    Get-ToolkitCommand
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