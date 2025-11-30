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
        [Alias('Path','FullName')]
        [string]$FilePath,

        [string]$Language = 'en',
        [string]$Model,
        [switch]$Translate,
        [string]$OutputDir = (Get-Location).Path
    )

    if (-not (Test-Path -Path $FilePath)) {
        throw "File not found: $FilePath"
    }

    $file = Get-Item -Path $FilePath
    if (-not $Model) { $Model = if ($Language -eq 'en') { 'distil-large-v3.5' } else { 'medium' } }

    $env:PYTHONWARNINGS = 'ignore'

    $whisperArgs = @(
        '--model', $Model,
        '--compute_type', 'int8',
        '--output_format', 'srt',
        '--output_dir', $OutputDir,
        '--batched', 'True',
        '--batch_size', '8',
        '--language', $Language
    )

    if ($Translate) { $whisperArgs += '--task'; $whisperArgs += 'translate' }
    $whisperArgs += $FilePath

    & whisper-ctranslate2 @whisperArgs

    [PSCustomObject]@{
        File     = $file.Name
        Model    = $Model
        Language = $Language
        Output   = Join-Path $OutputDir ($file.BaseName + '.srt')
    }
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
    $processed = 0

    foreach ($file in $files) {
        $srtPath = [System.IO.Path]::ChangeExtension($file.FullName, '.srt')

        if ((Test-Path $srtPath) -and -not $Force) {
            Write-Host "[$(Get-Date -Format 'HH:mm:ss')] " -NoNewline
            Write-Host 'Skipped: ' -ForegroundColor Yellow -NoNewline
            Write-Host "$($file.Name) (SRT exists)"
            continue
        }

        $processed++
        Write-Host "[$processed/$total] " -ForegroundColor DarkGray -NoNewline

        Invoke-Whisper -FilePath $file.FullName -Language $Language -Model $Model -Translate:$Translate -OutputDir $OutputDir
    }

    Write-Host "`nCompleted: $processed/$total files" -ForegroundColor Green
}

function Invoke-WhisperJapanese {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('Path','FullName')]
        [string]$FilePath,
        [string]$Model,
        [switch]$Translate,
        [string]$OutputDir = (Get-Location).Path
    )

    Invoke-Whisper -FilePath $FilePath -Language ja -Model $Model -Translate:$Translate -OutputDir $OutputDir
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

Export-ModuleMember -Function *
