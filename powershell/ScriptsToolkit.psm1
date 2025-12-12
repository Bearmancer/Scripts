Set-StrictMode -Version Latest


$Script:RepositoryRoot = Split-Path -Path $PSScriptRoot -Parent
$Script:PythonToolkit = Join-Path -Path $RepositoryRoot -ChildPath 'python\toolkit\cli.py'
$Script:CSharpRoot = Join-Path -Path $RepositoryRoot -ChildPath 'csharp'
$Script:LogDirectory = Join-Path -Path $RepositoryRoot -ChildPath 'logs'



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
    [CmdletBinding()]
    [Alias('hist')]
    param()

    & code "$env:APPDATA\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt"
}

function Show-ToolkitHelp {
    [CmdletBinding()]
    [Alias('tkhelp')]
    param()

    $helpFile = Join-Path -Path $PSScriptRoot -ChildPath 'ScriptsToolkit.Help.md'
    & code $helpFile
}

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
        [string]$EventType,

        [Parameter()]
        [string]$SessionId,

        [Parameter()]
        [string]$Search,

        [Parameter(ParameterSetName = 'Tail')]
        [Alias('Last')]
        [int]$Tail = 10,

        [Parameter(ParameterSetName = 'Head')]
        [Alias('First')]
        [int]$Head,

        [Parameter()]
        [ValidateSet('Date', 'Level', 'Event', 'Session')]
        [string]$Sort = 'Date',

        [Parameter()]
        [switch]$Descending,

        [Parameter()]
        [switch]$List,

        [Parameter()]
        [Alias('f')]
        [switch]$Full,

        [Parameter()]
        [Alias('a')]
        [switch]$All
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
                    'yyyy/MM/dd HH:mm:ss',
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

    if (-not $All) {
        $verboseEvents = @('PlaylistUpdated', 'PlaylistDeleted', 'PlaylistRenamed', 'PlaylistCreated')
        $entries = $entries | Where-Object { $_.Event -notin $verboseEvents }
    }

    if ($Level) {
        $entries = $entries | Where-Object { $_.Level -eq $Level }
    }
    if ($EventType) {
        $entries = $entries | Where-Object { $_.Event -like "*$EventType*" }
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

    $sortProperty = switch ($Sort) {
        'Date' { 'ParsedTimestamp' }
        'Level' { 'Level' }
        'Event' { 'Event' }
        'Session' { 'SessionId' }
        default { 'ParsedTimestamp' }
    }

    $useDescending = if ($PSBoundParameters.ContainsKey('Descending')) {
        $Descending
    }
    else {
        $false
    }

    $sorted = if ($useDescending) {
        $entries | Sort-Object -Property $sortProperty -Descending
    }
    else {
        $entries | Sort-Object -Property $sortProperty
    }

    $result = if ($Head) {
        $sorted | Select-Object -First $Head
    }
    else {
        $sorted | Select-Object -First $Tail
    }

    $levelColors = @{
        'Debug'   = 'DarkGray'
        'Info'    = 'Cyan'
        'Success' = 'Green'
        'Warning' = 'Yellow'
        'Error'   = 'Red'
        'Fatal'   = 'Magenta'
    }

    $displayObjects = $result | ForEach-Object {
        $details = if ($_.Data) {
            if ($_.Data.PSObject.Properties['Text']) {
                $_.Data.Text
            }
            else {
                $separator = $List ? "`n" : " | "
                ($_.Data.PSObject.Properties | Where-Object { $_.Name -ne 'Service' } | ForEach-Object {
                    $val = ($_.Value -is [array]) ? ($_.Value -join ", "): $_.Value
                    "$($_.Name): $val"
                }) -join $separator
            }
        }
        else {
            ''
        }

        [PSCustomObject]@{
            Timestamp = $_.Timestamp
            Level     = $_.Level
            Event     = $_.Event
            Source    = $_.Source
            SessionId = $_.SessionId
            Details   = $details
            Color     = $levelColors[$_.Level]
        }
    }

    if ($List) {
        $displayObjects | ForEach-Object {
            $divider = "─" * 80
            Write-Host $divider -ForegroundColor $_.Color
            Write-Host "Timestamp: $($_.Timestamp)" -ForegroundColor $_.Color
            Write-Host "Level:     $($_.Level)" -ForegroundColor $_.Color
            Write-Host "Event:     $($_.Event)" -ForegroundColor $_.Color
            Write-Host "Service:   $($_.Source)" -ForegroundColor $_.Color
            Write-Host "SessionId: $($_.SessionId)" -ForegroundColor $_.Color
            Write-Host "Details:   $($_.Details)" -ForegroundColor $_.Color
        }
        Write-Host ("─" * 80) -ForegroundColor DarkGray
    }
    else {
        $terminalWidth = $Host.UI.RawUI.WindowSize.Width
        $timestampWidth = 20
        $levelWidth = 10
        $eventWidth = 20
        $sourceWidth = 8
        $sessionWidth = 10
        $fixedWidth = $timestampWidth + $levelWidth + $eventWidth + $sourceWidth + $sessionWidth
        $detailsWidth = if ($Full) { $terminalWidth - $fixedWidth - 4 } else { [Math]::Min(60, $terminalWidth - $fixedWidth - 4) }

        Write-Host ""
        $header = "Timestamp".PadRight($timestampWidth) + "Service".PadRight($sourceWidth) + "Level".PadRight($levelWidth) + "Event".PadRight($eventWidth) + "Session".PadRight($sessionWidth) + "Details"
        Write-Host $header -ForegroundColor White
        Write-Host ("─" * $terminalWidth) -ForegroundColor DarkGray

        $displayObjects | ForEach-Object {
            $color = $_.Color
            $details = $_.Details

            $detailLines = @()
            if ($Full -and $details.Length -gt $detailsWidth) {
                $remaining = $details
                while ($remaining.Length -gt 0) {
                    if ($remaining.Length -le $detailsWidth) {
                        $detailLines += $remaining
                        $remaining = ''
                    }
                    else {
                        $detailLines += $remaining.Substring(0, $detailsWidth)
                        $remaining = $remaining.Substring($detailsWidth)
                    }
                }
            }
            else {
                if ($details.Length -gt $detailsWidth) {
                    $detailLines += $details.Substring(0, $detailsWidth - 3) + "..."
                }
                else {
                    $detailLines += $details
                }
            }

            Write-Host ($_.Timestamp.PadRight($timestampWidth)) -ForegroundColor $color -NoNewline
            Write-Host ($_.Source.PadRight($sourceWidth)) -ForegroundColor $color -NoNewline
            Write-Host ($_.Level.PadRight($levelWidth)) -ForegroundColor $color -NoNewline
            Write-Host ($_.Event.PadRight($eventWidth)) -ForegroundColor $color -NoNewline
            Write-Host ($_.SessionId.Substring(0, [Math]::Min(8, $_.SessionId.Length)).PadRight($sessionWidth)) -ForegroundColor $color -NoNewline
            Write-Host $detailLines[0] -ForegroundColor $color

            for ($i = 1; $i -lt $detailLines.Count; $i++) {
                Write-Host (' ' * $fixedWidth) -NoNewline
                Write-Host $detailLines[$i] -ForegroundColor $color
            }

            Write-Host ("─" * $terminalWidth) -ForegroundColor $color
        }
    }
}

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
        [string]$Sort = 'size'
    )

    Invoke-ToolkitPython -ArgumentList @('filesystem', 'tree', '--directory', $Directory.FullName, '--sort', $Sort)
}

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
        [string]$Sort = 'size'
    )

    Invoke-ToolkitPython -ArgumentList @('filesystem', 'tree', '--directory', $Directory.FullName, '--sort', $Sort, '--include-files')
}

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



function Invoke-Whisper {
    [CmdletBinding()]
    [Alias('whisp')]
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

function Invoke-WhisperFolder {
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
    [CmdletBinding()]
    [Alias('ytdl')]
    param(
        [Parameter(Mandatory, Position = 0)]
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



function Invoke-YouTubeSync {
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
        if ($Force) { $arguments += '--force' }
        if ($VerbosePreference -eq 'Continue') { $arguments += '--verbose' }
        & dotnet @arguments
    }
    finally {
        Pop-Location
    }
}

function Invoke-LastFmSync {
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
        if ($Since) { $arguments += '--since', $Since }
        if ($VerbosePreference -eq 'Continue') { $arguments += '--verbose' }
        & dotnet @arguments
    }
    finally {
        Pop-Location
    }
}

function Invoke-AllSyncs {
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

    $script = "Set-Location '$Script:CSharpRoot'; dotnet run -- $Command; if (`$LASTEXITCODE -ne 0) { Read-Host 'Press Enter to close' }"

    $terminal = Get-Command -Name 'wt.exe' -ErrorAction Ignore
    if ($terminal) {
        $executable = $terminal.Source
        $argument = "pwsh -Command `"$script`""
    }
    else {
        $executable = (Get-Command -Name pwsh).Source
        $argument = "-Command `"$script`""
    }

    $action = New-ScheduledTaskAction -Execute $executable -Argument $argument
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -RunOnlyIfNetworkAvailable -WakeToRun -MultipleInstances IgnoreNew

    $start = [datetime]::Today.Add($DailyTime)
    if ($start -le (Get-Date)) {
        $start = $start.AddDays(1)
    }

    $trigger = New-ScheduledTaskTrigger -Daily -At $start
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Settings $settings -Description $Description | Out-Null

    Write-Host "Registered '$TaskName' for $($start.ToString('HH:mm')) daily" -ForegroundColor Green
}

function Register-AllSyncTasks {
    [CmdletBinding()]
    [Alias('regall')]
    param()

    Register-ScheduledSyncTask -TaskName 'LastFmSync' -Command 'sync lastfm' -DailyTime '09:00:00' -Description 'Syncs Last.fm scrobbles to Google Sheets'
    Register-ScheduledSyncTask -TaskName 'YouTubeSync' -Command 'sync yt' -DailyTime '10:00:00' -Description 'Syncs YouTube playlists to Google Sheets'
}



Export-ModuleMember -Function * -Alias *
