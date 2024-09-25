param (
    [Parameter(Mandatory = $true)]
    [System.IO.DirectoryInfo]$directory
)

$outputFile = "$env:USERPROFILE\Desktop\$($directory.BaseName).txt"

Get-ChildItem $directory -Recurse -Include *.flac, *.mp3 | ForEach-Object {
    $ffprobeOutput = & ffprobe -v quiet -print_format json -show_format -show_streams $_

    $metadata = $ffprobeOutput | ConvertFrom-Json
    $content = @(
        "File name: $($_.BaseName)"
    )

    $composer = if ($metadata.format.tags.PSObject.Properties['composer']) { $metadata.format.tags.composer } else { "Unknown" }
    $artist = if ($metadata.format.tags.PSObject.Properties['artist']) { $metadata.format.tags.artist } else { "Unknown" }
    $disc = if ($metadata.format.tags.PSObject.Properties['disc']) { $metadata.format.tags.disc } else { "Unknown" }

    $content = @(
        "File name: $($_.BaseName)"
        "Composer: $composer"
        "Artist: $artist"
        "Disc Number: $disc`n"
    )

    $content | Out-File -FilePath $outputFile -Append
}

RemoveDuplicateEntries $outputFile

function RemoveDuplicateEntries([String]$inputFile) {
    $lines = Get-Content $inputFile
    $uniqueLines = @()
    $lastFileNamePrefix = ""
    $i = 0

    for ($i = 0; $i -lt $lines.Length; $i += 5) {
        $fileName = $lines[$i]
        $fileNamePrefix = $fileName -replace '^File name\:\s*\d*\.*\[*\(*\s*', ''
    
        $parts = $fileNamePrefix.Split(' ')

        if ($parts.Length -ge 3) { $fileNamePrefix = $parts[0..2] -join ' ' }
        elseif ($parts.Length -ge 2) { $fileNamePrefix = $parts[0..1] -join ' ' }
        else { $uniqueLines += $lines[$i..[math]::Min($i + 3, $lines.Length - 1)]; continue }

        if ($fileNamePrefix -ne $lastFileNamePrefix) {
            $uniqueLines += $lines[$i..[math]::Min($i + 3, $lines.Length - 1)] + "`n"
            $lastFileNamePrefix = $fileNamePrefix
        }
    }

    Set-Content $inputFile $uniqueLines
}