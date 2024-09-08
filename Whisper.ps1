$fileExtensions = '.mkv', '.mp4', '.mp3', '.flac', '.m4a', '.ogg', '.opus', '.wmv', '.ts', '.flv', '.avi'

function WhisperLogic([string]$model, [string]$language, [System.IO.FileInfo]$file) {
    if ($fileExtensions -notcontains $file.Extension) {
        return
    }
    
    $subtitleFile = Join-Path $file.Directory.FullName ($file.BaseName + ".srt")

    if (Test-Path -LiteralPath $subtitleFile) { 
        Write-Host "Subtitle for $($file.BaseName) already exists. Skipping..."
        return 
    }
    
    Write-Host "Now transcribing: $($file.Name)"
    
    whisper --fp16 False --output_format "srt" --model $model --language $language $file
    
    RemoveSubtitleDuplication (Get-Item $subtitleFile)

    # if ($language -ne "English" -or $language -ne "Hindi") {
    #     TranslateFile $subtitleFile
        
    #     $translatedFile = "$($(Get-Item $subtitleFile).BaseName) - Translated.srt"

    #     if (Test-Path $translatedFile) {
    #         Remove-Item $subtitleFile
    #         Rename-Item $translatedFile $subtitleFile
    #     }
    # }
}

function Whisp($file) {
    whisperLogic small.en English $file
}

function WhisperPath {
    Get-ChildItem -File | ForEach-Object {
        & whisp $_
    }
}

function WhisperPathRecursive {
    Get-ChildItem -Directory -Recurse | ForEach-Object { 
        Start-Process -FilePath "pwsh.exe" -ArgumentList "-NoExit -Command Set-Location -LiteralPath '$($_.FullName)'; whisperPath" 
    }
    
    whisperPath
}

function RemoveSubtitleDuplication([System.IO.FileInfo]$file) {
    $oldText = '(\d+\r?\n\d+.*?\r?\n(.*?))(?:\r?\n)+(?:\d+\r?\n\d+.*?\r?\n\2(?:\r?\n)+)+'
    $newText = "`$1`r`n`r`n"
    
    if (Test-Path -LiteralPath $file) {
        $NewContent = [System.IO.File]::ReadAllText($file) -replace $oldText, $newText
        [System.IO.File]::WriteAllText($file, $NewContent)
    }
    else {
        Write-Host "$($file) not found."
    }
}

function WhisperJapanese ([System.IO.FileInfo] $file) {
    whisperLogic small Japanese $file
}

function WhisperPathJapanese {
    Get-ChildItem -File | ForEach-Object {
        & wj $_
    }
}

function WhisperJapaneseFile {
    ccaf wj
}

function SRTtoWord($file) {
    py 'C:\Users\Lance\Documents\Powershell\Python Scripts\Word and SRT Conversions.py' $file srt
}

function WordToSRT($file) {
    py 'C:\Users\Lance\Documents\Powershell\Python Scripts\Word and SRT Conversions.py' $file docx
}

Set-Alias -Name wp -Value whisperPath
Set-Alias -Name wj -Value whisperJapanese
Set-Alias -Name wpj -Value whisperPathJapanese
Set-Alias -Name wpf -Value whisperJapaneseFile
Set-Alias -Name wpr -Value whisperPathRecursive
Set-Alias -Name rsd -Value RemoveSubtitleDuplication