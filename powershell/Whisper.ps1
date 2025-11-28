function Invoke-Whisper {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('Path', 'FullName')]
        [string]$FilePath,

        [string]$Language = 'en',
        [string]$Model,
        [switch]$Translate
    )

    if (-not (Test-Path -Path $FilePath)) {
        throw "File not found: $FilePath"
    }

    $file = Get-Item -Path $FilePath
    $Model ??= $Language -eq 'en' ? 'distil-large-v3.5' : 'medium'

    $env:PYTHONWARNINGS = 'ignore'

    $whisperArgs = @(
        '--model', $Model
        '--compute_type', 'int8'
        '--output_format', 'srt'
        '--output_dir', $outDir
        '--batched', 'True'
        '--batch_size', '8'
        '--language', $Language
    )

    if ($Translate) { $whisperArgs += '--task', 'translate' }
    $whisperArgs += $FilePath

    & whisper-ctranslate2 @whisperArgs

    [PSCustomObject]@{
        File     = $file.Name
        Model    = $Model
        Language = $Language
        Output   = Join-Path $outDir ($file.BaseName + '.srt')
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
        [switch]$Force
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

        Invoke-Whisper -FilePath $file.FullName -Language $Language -Model $Model -Translate:$Translate
    }

    Write-Host "`nCompleted: $processed/$total files" -ForegroundColor Green
}

function Invoke-WhisperJapanese {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('Path', 'FullName')]
        [string]$FilePath,
        [string]$Model,
        [switch]$Translate
    )

    Invoke-Whisper -FilePath $FilePath -Language ja -Model $Model -Translate:$Translate
}

function Invoke-WhisperJapaneseFolder {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .),
        [string]$Model,
        [switch]$Translate,
        [switch]$Force
    )

    Invoke-WhisperFolder -Directory $Directory -Language ja -Model $Model -Translate:$Translate -Force:$Force
}

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
            Invoke-Whisper -FilePath $filePath -Language $Language -Model $Model -Translate:$Translate
        }
    }
    Pop-Location
}

Set-Alias -Name whisper -Value Invoke-Whisper
Set-Alias -Name whispdir -Value Invoke-WhisperFolder
Set-Alias -Name whispjp -Value Invoke-WhisperJapanese
Set-Alias -Name whispjpdir -Value Invoke-WhisperJapaneseFolder
Set-Alias -Name yt -Value Save-YouTubeVideo
