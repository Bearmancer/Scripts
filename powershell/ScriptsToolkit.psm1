Set-StrictMode -Version Latest

#region Configuration

$Script:RepositoryRoot = Split-Path -Path $PSScriptRoot -Parent
$Script:PythonToolkit = Join-Path -Path $RepositoryRoot -ChildPath 'python\toolkit\cli.py'
$Script:CSharpRoot = Join-Path -Path $RepositoryRoot -ChildPath 'csharp'

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

function Get-ToolkitFunctions {
    [CmdletBinding()]
    param()

    $functions = @(
        @{ Category = 'Utilities'; Name = 'Get-ToolkitFunctions'; Alias = 'tkfn'; Description = 'List all toolkit functions' }
        @{ Category = 'Utilities'; Name = 'Open-CommandHistory'; Alias = 'hist'; Description = 'Open PowerShell history file' }
        @{ Category = 'Utilities'; Name = 'Show-ToolkitHelp'; Alias = 'tkhelp'; Description = 'Open toolkit documentation' }
        @{ Category = 'Utilities'; Name = 'Invoke-ToolkitAnalyzer'; Alias = 'tklint'; Description = 'Run PSScriptAnalyzer' }
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
        @{ Category = 'Transcription'; Name = 'Invoke-Whisper'; Alias = 'whisper'; Description = 'Transcribe single file' }
        @{ Category = 'Transcription'; Name = 'Invoke-WhisperFolder'; Alias = 'whisperf'; Description = 'Transcribe folder' }
        @{ Category = 'Transcription'; Name = 'Invoke-WhisperJapanese'; Alias = 'whisperj'; Description = 'Transcribe Japanese audio' }
        @{ Category = 'Transcription'; Name = 'Invoke-WhisperJapaneseFolder'; Alias = 'whisperjf'; Description = 'Transcribe Japanese folder' }
        @{ Category = 'YouTube'; Name = 'Save-YouTubeVideo'; Alias = 'ytdl'; Description = 'Download YouTube videos' }
        @{ Category = 'Tasks'; Name = 'Register-ScheduledSyncTask'; Alias = 'regtask'; Description = 'Create scheduled task' }
        @{ Category = 'Tasks'; Name = 'Register-AllSyncTasks'; Alias = 'regall'; Description = 'Register all sync tasks' }
    )

    Write-Host "`nScriptsToolkit Functions" -ForegroundColor Cyan
    Write-Host "========================`n" -ForegroundColor Cyan

    $functions | Group-Object -Property Category | ForEach-Object {
        Write-Host "$($_.Name)" -ForegroundColor Yellow
        $_.Group | ForEach-Object {
            Write-Host "  $($_.Alias.PadRight(10))" -ForegroundColor Green -NoNewline
            Write-Host "$($_.Name.PadRight(30))" -ForegroundColor White -NoNewline
            Write-Host "$($_.Description)" -ForegroundColor DarkGray
        }
        Write-Host ""
    }
}

function Open-CommandHistory {
    [CmdletBinding()]
    param()

    & code "$env:APPDATA\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt"
}

function Show-ToolkitHelp {
    [CmdletBinding()]
    param()

    $helpFile = Join-Path -Path $PSScriptRoot -ChildPath 'ScriptsToolkit.Help.md'
    & code $helpFile
}

function Invoke-ToolkitAnalyzer {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Path = (Join-Path -Path $PSScriptRoot -ChildPath '.')
    )

    $settings = Join-Path -Path $PSScriptRoot -ChildPath 'PSScriptAnalyzerSettings.psd1'
    Invoke-ScriptAnalyzer -Path $Path -Settings $settings -Recurse -Severity Error, Warning
}

#endregion

#region Filesystem

function Get-Directories {
    [CmdletBinding()]
    param(
        [Parameter()]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [ValidateSet('size', 'name')]
        [string]$SortBy = 'size'
    )

    Invoke-ToolkitPython -ArgumentList @('filesystem', 'tree', '--directory', $Directory.FullName, '--sort', $SortBy)
}

function Get-FilesAndDirectories {
    [CmdletBinding()]
    param(
        [Parameter()]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [ValidateSet('size', 'name')]
        [string]$SortBy = 'size'
    )

    Invoke-ToolkitPython -ArgumentList @('filesystem', 'tree', '--directory', $Directory.FullName, '--sort', $SortBy, '--include-files')
}

function New-Torrents {
    [CmdletBinding()]
    param(
        [Parameter()]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

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

function Start-DiscRemux {
    [CmdletBinding()]
    param(
        [Parameter()]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

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
    param(
        [Parameter()]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('video', 'compress', '--directory', $Directory.FullName)
}

function Get-VideoChapters {
    [CmdletBinding()]
    param(
        [Parameter()]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('video', 'chapters', '--path', $Directory.FullName)
}

function Get-VideoResolution {
    [CmdletBinding()]
    param(
        [Parameter()]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('video', 'resolutions', '--path', $Directory.FullName)
}

#endregion

#region Audio

function Convert-Audio {
    [CmdletBinding()]
    param(
        [Parameter()]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [ValidateSet('24-bit', 'flac', 'mp3', 'all')]
        [string]$Format = 'all'
    )

    Invoke-ToolkitPython -ArgumentList @('audio', 'convert', '--directory', $Directory.FullName, '--mode', 'convert', '--format', $Format)
}

function Convert-ToMP3 {
    [CmdletBinding()]
    param(
        [Parameter()]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Convert-Audio -Directory $Directory -Format 'mp3'
}

function Convert-ToFLAC {
    [CmdletBinding()]
    param(
        [Parameter()]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Convert-Audio -Directory $Directory -Format 'flac'
}

function Convert-SACD {
    [CmdletBinding()]
    param(
        [Parameter()]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .),

        [ValidateSet('24-bit', 'flac', 'mp3', 'all')]
        [string]$Format = 'all'
    )

    Invoke-ToolkitPython -ArgumentList @('audio', 'convert', '--directory', $Directory.FullName, '--mode', 'extract', '--format', $Format)
}

function Rename-MusicFiles {
    [CmdletBinding()]
    param(
        [Parameter()]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('audio', 'rename', '--directory', $Directory.FullName)
}

function Get-EmbeddedImageSize {
    [CmdletBinding()]
    param(
        [Parameter()]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    Invoke-ToolkitPython -ArgumentList @('audio', 'art-report', '--directory', $Directory.FullName)
}

function Invoke-Propolis {
    [CmdletBinding()]
    param(
        [Parameter()]
        [System.IO.DirectoryInfo]$Directory = (Get-Item -Path .)
    )

    & "$env:LOCALAPPDATA\Personal\Propolis\propolis_windows.exe" --no-specs $Directory.FullName
}

#endregion

#region Transcription

function Invoke-Whisper {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('Path', 'FullName')]
        [string]$FilePath,

        [string]$Language,
        [string]$Model,
        [switch]$Translate,
        [switch]$Force,
        [string]$OutputDir = (Get-Location).Path
    )

    if (-not (Test-Path -Path $FilePath)) {
        throw "File not found: $FilePath"
    }

    $file = Get-Item -Path $FilePath
    $srtPath = Join-Path $OutputDir ($file.BaseName + '.srt')

    if ((Test-Path $srtPath) -and -not $Force) {
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] " -ForegroundColor DarkGray -NoNewline
        Write-Host "Skipped: " -ForegroundColor Yellow -NoNewline
        Write-Host "$($file.Name) " -NoNewline
        Write-Host "(SRT exists, use -Force to overwrite)" -ForegroundColor DarkGray
        return
    }

    $detectedLanguage = $null
    if (-not $Language) {
        $detectedLanguage = '(auto-detect)'
        $effectiveModel = if ($Model) { $Model } else { 'medium' }
    }
    else {
        $effectiveModel = if ($Model) { $Model } elseif ($Language -eq 'en') { 'distil-large-v3.5' } else { 'medium' }
    }

    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] " -ForegroundColor DarkGray -NoNewline
    Write-Host "Transcribing: " -ForegroundColor Cyan -NoNewline
    Write-Host $file.Name
    Write-Host "             Model: " -ForegroundColor DarkGray -NoNewline
    Write-Host $effectiveModel -ForegroundColor White -NoNewline
    Write-Host " | Language: " -ForegroundColor DarkGray -NoNewline
    if ($detectedLanguage) {
        Write-Host $detectedLanguage -ForegroundColor Yellow
    }
    else {
        Write-Host $Language -ForegroundColor White
    }

    $env:PYTHONWARNINGS = 'ignore'

    $whisperArgs = @(
        '--model', $effectiveModel,
        '--compute_type', 'int8',
        '--output_format', 'srt',
        '--output_dir', $OutputDir,
        '--batched', 'True',
        '--batch_size', '8'
    )

    if ($Language) {
        $whisperArgs += '--language'
        $whisperArgs += $Language
    }

    if ($Translate) {
        $whisperArgs += '--task'
        $whisperArgs += 'translate'
    }

    $whisperArgs += $FilePath

    & whisper-ctranslate2 @whisperArgs

    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] " -ForegroundColor DarkGray -NoNewline
    Write-Host "Completed: " -ForegroundColor Green -NoNewline
    Write-Host $file.Name
}

function Invoke-WhisperFolder {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .),

        [string]$Language = 'en',
        [string]$Model,
        [switch]$Translate,
        [switch]$Force,
        [string]$OutputDir = (Get-Location).Path
    )

    $extensions = '.mp4', '.mkv', '.avi', '.mp3', '.flac', '.wav', '.webm', '.m4a', '.opus', '.ogg'
    $files = Get-ChildItem $Directory -Recurse -File | Where-Object { $_.Extension.ToLower() -in $extensions }
    $total = $files.Count

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
        Write-Host "`nSkipped " -ForegroundColor Yellow -NoNewline
        Write-Host "$($skipped.Count)" -ForegroundColor White -NoNewline
        Write-Host " files (SRT exists):" -ForegroundColor Yellow
        foreach ($file in $skipped) {
            Write-Host "  $($file.Name)" -ForegroundColor DarkGray
        }
        Write-Host ""
    }

    if ($toProcess.Count -eq 0) {
        Write-Host "Nothing to transcribe. Use " -ForegroundColor Yellow -NoNewline
        Write-Host "-Force" -ForegroundColor Cyan -NoNewline
        Write-Host " to overwrite existing files." -ForegroundColor Yellow
        return
    }

    Write-Host "Transcribing " -ForegroundColor Cyan -NoNewline
    Write-Host "$($toProcess.Count)" -ForegroundColor White -NoNewline
    Write-Host " files:" -ForegroundColor Cyan
    Write-Host ""

    $current = 0
    foreach ($file in $toProcess) {
        $current++
        Write-Host "[$current/$($toProcess.Count)] " -ForegroundColor DarkGray -NoNewline
        Invoke-Whisper -FilePath $file.FullName -Language $Language -Model $Model -Translate:$Translate -Force:$Force -OutputDir $OutputDir
        Write-Host ""
    }

    Write-Host "Completed: $current/$($toProcess.Count) transcribed" -ForegroundColor Green
    if ($skipped.Count -gt 0) {
        Write-Host "           $($skipped.Count) skipped (already existed)" -ForegroundColor DarkGray
    }
}

function Invoke-WhisperJapanese {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('Path', 'FullName')]
        [string]$FilePath,
        [string]$Model,
        [switch]$Translate,
        [switch]$Force,
        [string]$OutputDir = (Get-Location).Path
    )

    Invoke-Whisper -FilePath $FilePath -Language ja -Model $Model -Translate:$Translate -Force:$Force -OutputDir $OutputDir
}

function Invoke-WhisperJapaneseFolder {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .),
        [string]$Model,
        [switch]$Translate,
        [switch]$Force,
        [string]$OutputDir = (Get-Location).Path
    )

    Invoke-WhisperFolder -Directory $Directory -Language ja -Model $Model -Translate:$Translate -Force:$Force -OutputDir $OutputDir
}

#endregion

#region YouTube

function Save-YouTubeVideo {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromRemainingArguments)]
        [string[]]$Urls,
        [switch]$Transcribe,
        [string]$Language = 'en',
        [string]$Model,
        [switch]$Translate,
        [System.IO.DirectoryInfo]$OutputDir = (Get-Item .)
    )

    Push-Location $OutputDir

    foreach ($url in $Urls) {
        Write-Host "[$(Get-Date -Format 'HH:mm:ss')] " -NoNewline
        Write-Host 'Downloading: ' -ForegroundColor Cyan -NoNewline
        Write-Host $url

        $filePath = & yt-dlp --print filename $url --windows-filenames -o '%(title)s.%(ext)s'

        if (Test-Path -Path $filePath) {
            Remove-Item $filePath -Force
            Write-Host '  Replaced existing file' -ForegroundColor Yellow
        }

        & yt-dlp $url --windows-filenames -o '%(title)s.%(ext)s'

        if ($Transcribe -and (Test-Path $filePath)) {
            Invoke-Whisper -FilePath $filePath -Language $Language -Model $Model -Translate:$Translate -OutputDir $OutputDir.FullName
        }
    }
    Pop-Location
}

#endregion

#region Scheduled Tasks

function Register-ScheduledSyncTask {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$TaskName,

        [Parameter(Mandatory)]
        [string]$Command,

        [TimeSpan]$DailyTime = '09:00:00',
        [string]$Description = "Scheduled task"
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
    $noBuildFlag = if (Test-Path -Path $dllPath) { '--no-build ' } else { '' }

    $pwsh = (Get-Command -Name pwsh).Source
    $argument = "-NoProfile -NoLogo -WindowStyle Hidden -WorkingDirectory `"$Script:CSharpRoot`" -Command `"dotnet run $($noBuildFlag)$Command; exit `$LASTEXITCODE`""

    $action = New-ScheduledTaskAction -Execute $pwsh -Argument $argument
    $settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -RunOnlyIfNetworkAvailable -WakeToRun

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
    param()

    Register-ScheduledSyncTask -TaskName 'LastFmSync' -Command 'sync lastfm' -DailyTime '09:00:00' -Description 'Syncs Last.fm scrobbles to Google Sheets'
    Register-ScheduledSyncTask -TaskName 'YouTubeSync' -Command 'sync yt' -DailyTime '10:00:00' -Description 'Syncs YouTube playlists to Google Sheets'
}

#endregion

#region Aliases

Set-Alias -Name tkfn -Value Get-ToolkitFunctions
Set-Alias -Name hist -Value Open-CommandHistory
Set-Alias -Name tkhelp -Value Show-ToolkitHelp
Set-Alias -Name tklint -Value Invoke-ToolkitAnalyzer
Set-Alias -Name dirs -Value Get-Directories
Set-Alias -Name tree -Value Get-FilesAndDirectories
Set-Alias -Name torrent -Value New-Torrents
Set-Alias -Name remux -Value Start-DiscRemux
Set-Alias -Name compress -Value Start-BatchCompression
Set-Alias -Name chapters -Value Get-VideoChapters
Set-Alias -Name res -Value Get-VideoResolution
Set-Alias -Name audio -Value Convert-Audio
Set-Alias -Name tomp3 -Value Convert-ToMP3
Set-Alias -Name toflac -Value Convert-ToFLAC
Set-Alias -Name sacd -Value Convert-SACD
Set-Alias -Name rename -Value Rename-MusicFiles
Set-Alias -Name artsize -Value Get-EmbeddedImageSize
Set-Alias -Name propolis -Value Invoke-Propolis
Set-Alias -Name whisper -Value Invoke-Whisper
Set-Alias -Name whisperf -Value Invoke-WhisperFolder
Set-Alias -Name whisperj -Value Invoke-WhisperJapanese
Set-Alias -Name whisperjf -Value Invoke-WhisperJapaneseFolder
Set-Alias -Name ytdl -Value Save-YouTubeVideo
Set-Alias -Name regtask -Value Register-ScheduledSyncTask
Set-Alias -Name regall -Value Register-AllSyncTasks

#endregion

Export-ModuleMember -Function * -Alias *
