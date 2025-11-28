function Whisper([string]$FilePath, [string]$Model = 'large-v3', [string]$Language = $null, [switch]$Translate) {
    if (-not (Test-Path -Path $FilePath)) { Write-Error "File not found: $FilePath"; return }

    $env:PYTHONWARNINGS = "ignore"
    
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Processing: $(Split-Path -Leaf -Path $FilePath)"
    
    $arguments = @(
        '--model', $Model,
        '--compute_type', 'int8',
        '--output_format', 'srt',
        '--output_dir', (Get-Item -Path $FilePath).Directory.FullName,
        $FilePath
    )

    if ($Language) { $arguments += @('--language', $Language) }
    if ($Translate) { $arguments += @('--task', 'translate') }

    & 'whisper-ctranslate2' @arguments
}

function Whisp([string] $FilePath) {
    Whisper $FilePath -Model 'distil-large-v3.5' -Language 'en'
}

function WhisperJapanese([string] $FilePath) {
    Whisper $FilePath -Language 'ja'
}

function WhisperJapaneseFolder([System.IO.DirectoryInfo] $FolderPath) {
    $files = Get-ChildItem $FolderPath

    foreach ($file in $files) {
        WhisperJapanese $file.FullName
    }
}

function Save-YouTubeVideo {
    param(
        [Parameter(ValueFromRemainingArguments)]
        [string[]]$Links,
        [switch]$Foreign
    )

    foreach ($link in $Links) {
        $filePath = & 'yt-dlp' --print filename $link --windows-filenames
        $fileExists = Test-Path -Path $filePath

        if ($fileExists) {
            Remove-Item $filePath
            Write-Host "Deleted pre-existing file at: $filePath"
        }

        & 'yt-dlp' $link --windows-filenames

        if ($Foreign) {
            Whisper $filePath -Translate
        }
        else {
            Whisp $filePath
        }
        
    }
}

Set-Alias -Name wp -Value WhisperFolder
Set-Alias -Name wj -Value WhisperJapanese
Set-Alias -Name wpj -Value WhisperJapaneseFolder
Set-Alias -Name yt -Value Save-YouTubeVideo