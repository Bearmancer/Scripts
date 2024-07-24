param (
    [Parameter(Mandatory = $true)]
    [string]$FolderPath
)

$videoFiles = Get-ChildItem $FolderPath -Recurse -Include *.mp4, *.avi, *.mkv, *.ts

$list = @()
$text = "filelist:`""

foreach ($videoFile in $videoFiles) {
    $jsonChapters = & ffprobe.exe -v error -i $videoFile.FullName -print_format json -show_chapters | Out-String | ConvertFrom-Json

    if ($jsonChapters.chapters.Count -le 1) {
        continue
    }

    $list += $videoFile
}

Write-Host "Multiple chapters in these videos:"

foreach ($file in $list) {
    Write-Host $file.BaseName
    $text += $file.FullName + "|"
}

$text = $text.Substring(0, $text.Length - 1) + "`""

Write-Host $text
Set-Clipboard $text