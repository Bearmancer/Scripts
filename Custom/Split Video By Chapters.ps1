param (
    [Parameter(Mandatory = $true)]
    [string]$FolderPath
)

$videoFiles = Get-ChildItem $FolderPath -Recurse -Include *.mp4, *.avi, *.mkv, *.ts

$i = 1

foreach ($videoFile in $videoFiles) {
    $parentDirectory = $videoFile.Directory
    $jsonChapters = & ffprobe.exe -v error -i $videoFile.FullName -print_format json -show_chapters | Out-String | ConvertFrom-Json

    if ($jsonChapters.chapters.Count -le 1) {
        Write-Host "No chapters found in $($videoFile.Name)."
        continue
    }

    $extension = [System.IO.Path]::GetExtension($videoFile)

    foreach ($chapter in $jsonChapters.chapters) {
        $formattedIndex = "{0:D2}" -f $i
        $outputFileName = Join-Path -Path $parentDirectory.FullName -ChildPath "$($parentDirectory.BaseName) - Chapter $formattedIndex$extension"
        & ffmpeg.exe -i $videoFile -ss $chapter.start_time -to $chapter.end_time -c copy -avoid_negative_ts make_zero -y $outputFileName
        $i++
    }
}