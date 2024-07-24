function whisperLogic([string]$model, [string]$language, [System.IO.FileInfo]$file) {
    if ($fileExtensions -notcontains $file.Extension) {
        return
    }
    
    $subtitleFile = Join-Path $file.Directory.FullName ($file.BaseName + ".srt")

    if (Test-Path -LiteralPath $subtitleFile) { 
        Write-Output "Subtitle for $($file.BaseName) already exists. Skipping..."
        return 
    }
    
    Write-Output "Now transcribing: $($file.Name)"
    
    whisper --fp16 False --output_format "srt" --model $model --language $language $file
    
    RemoveSubtitleDuplication $subtitleFile
}


function whisp($file) {
    whisperLogic small.en English $file
}

$fileExtensions = '.mkv', '.mp4', '.flac', '.m4a', '.ogg', '.opus', '.wmv', '.ts', '.flv', '.avi'

function whisperPath {
    Get-ChildItem -File | ForEach-Object {
        & whisp($_); 
    }
}

function whisperPathRecursive {
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
        Write-Output "$($file) not found."
    }
}

function whisperFile {
    ccaf whisp
}

function whisperJapaneseFile {
    ccaf wpj
}

function whisperJapanese ([System.IO.FileInfo] $file) {
    $subtitleFile = Join-Path $file.Directory.FullName ($file.BaseName + ".srt")

    if ($fileExtensions -notcontains $file.Extension -or (Test-Path -LiteralPath $subtitleFile)) {
        Write-Output "Skipping $($file.Name)"
        return
    }
    
    whisperLogic small Japanese $file

    SRTtoWord $subtitleFile
}

function SRTtoWord([System.IO.FileInfo] $file) {
    if (!(Test-Path -LiteralPath $file) -or $file.Extension -ne ".srt") {
        Write-Output "Skipping $($file)"
        return 
    }

    $srt = New-Object -ComObject Word.Application
    $document = $srt.Documents.Open($file.FullName)
    $outputFilePath = [System.IO.Path]::ChangeExtension($file.FullName, ".docx")
    $document.SaveAs($outputFilePath, 16); $document.Close(); $srt.Quit()
    
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($document) | Out-Null
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($srt) | Out-Null
    [System.GC]::Collect(); [System.GC]::WaitForPendingFinalizers()
    
    Remove-Item -LiteralPath $file
}

function WordToSRT([String] $filePath) {
    $file = Get-Item $filePath
  
    if (!(Test-Path -LiteralPath $file) -or $file.Extension -ne ".docx") {
        Write-Output "Skipping $($file)"
        return 
    }

    $word = New-Object -ComObject Word.Application
    $document = $word.Documents.Open($file.FullName)
    
    $newName = $file.BaseName -replace " en$", ""
    $newFilePath = Join-Path $file.Directory.FullName "$newName.srt"
    $document.SaveAs($newFilePath, 2); $document.Close(); $word.Quit()

    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($document) | Out-Null
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($word) | Out-Null
    [System.GC]::Collect(); [System.GC]::WaitForPendingFinalizers()

    Remove-Item -LiteralPath $file.FullName
  
}

function SplitWhisperLines {
    param(
        [Parameter(Mandatory = $true)]
        [string] $InputPath
    )

    $chunkLength = 1500

    if (Test-Path -Path $InputPath) {
        $content = Get-Content $InputPath -Raw
    }
    else {
        $content = $InputPath
    }

    $chunks = @()

    while ($content.Length -gt $chunkLength) {
        $chunk = $content.Substring(0, [Math]::Min($chunkLength, $content.Length))

        if ($chunk.EndsWith("`n")) {
            $chunks += $chunk
            $content = $content.Substring($chunk.Length)
        }
        else {
            $lastNewlineIndex = $chunk.LastIndexOf("`n", [Math]::Min($chunk.Length - 1, $chunkLength - 1))
            if ($lastNewlineIndex -ne -1) {
                $chunk = $content.Substring(0, $lastNewlineIndex + 1)
                $content = $content.Substring($chunk.Length)
            }
            else {
                $content = $content.Substring($chunk.Length)
            }

            $chunks += $chunk
        }
    }

    $chunks += $content

    $result = $chunks -join "`n`n_______________________________________________________`n`n"

    Write-Output $result
}

function MergeWhisperLines([System.IO.FileInfo]$Path) {

    $content = Get-Content -LiteralPath $Path -Raw
    $pattern = "(?ms)\d+\r?\n.*?\r?\n(.*?)(\r?\n)+"
    $replacement = '$1 '
  
    $newContent = $content -replace $pattern, $replacement
  
    $newFilePath = $Path.FullName -replace '.srt', ' (Merged).srt'
  
    Set-Content -Path $newFilePath -Value $newContent
  
    Write-Host "Replacement created in new file: $newFilePath"
}
  

# function WordToSRT([System.IO.FileInfo] $file) {
#     if (!(Test-Path -LiteralPath $file) -or ($file.Extension -ne ".docx")) {
#         Write-Output "Skipping $($file.Name)"
#         return 
#     }

#     $word = New-Object -ComObject Word.Application
#     $document = $word.Documents.Open($file.FullName)
#     $newName = $file.BaseName -replace " en$", ""
#     $newFilePath = Join-Path $file.Directory.FullName "$newName.srt"
#     $document.SaveAs($newFilePath, 2); $document.Close(); $word.Quit()

#     [System.Runtime.Interopservices.Marshal]::ReleaseComObject($document) | Out-Null
#     [System.Runtime.Interopservices.Marshal]::ReleaseComObject($word) | Out-Null
#     [System.GC]::Collect(); [System.GC]::WaitForPendingFinalizers()

#     Remove-Item -LiteralPath $file.FullName
# }

Set-Alias -Name wp -Value whisperPath
Set-Alias -Name wpf -Value whisperFile
Set-Alias -Name wpj -Value whisperJapanese
Set-Alias -Name wpjf -Value whisperJapaneseFile
Set-Alias -Name wpr -Value whisperPathRecursive
Set-Alias -Name wts -Value WordToSRT
Set-Alias -Name stw -Value SRTtoWord
Set-Alias -Name rsd -Value RemoveSubtitleDuplication