function ExtractCommentaryAudio() {
    $videoFiles = Get-ChildItem | Where-Object { $_.Extension -in @(".mkv", ".mp4") }

    foreach ($file in $videoFiles) {
        $audioTrackCount = (ffmpeg -i $file.FullName -hide_banner 2>&1 | Select-String "Stream #" -Context 0, 1 | Select-String "Audio" | Measure-Object).Count
        if ($audioTrackCount -gt 1) {
            $outputFile = Join-Path $PWD.Path ("$($file.BaseName) Audio Commentary.flac")
            ffmpeg -i $file.FullName -map 0:a:1 -sample_fmt s16 -acodec flac $outputFile
        }
    }

    Get-ChildItem *.flac | ForEach-Object { Write-Host $_.Name }
}