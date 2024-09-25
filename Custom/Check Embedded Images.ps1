function CheckEmbeddedImage($flacFile) {
    $exifTool = "C:\Users\Lance\Desktop\exiftool-12.96_64\exiftool.exe"
    $output = & $exifTool -PictureWidth -PictureHeight -s -s -s $flacFile 2>&1

    if (!(Test-Path $flacFile) -or ($output -match "Warning")) {
        Write-Host "Could not process: $flacFile"
        return 999
    }

    $dimensions = & $exifTool -PictureWidth -PictureHeight -s -s -s $flacFile
    $imageSize = & $exifTool -PictureLength -s -s -s $flacFile | ForEach-Object { [math]::Round($_ / 1KB, 2) }

    if ($dimensions -and $imageSize -gt 1024) {
        Write-Host "$flacFile has an image greater than 1MB."
    } 

    return $imageSize
}