function Propolis {
    C:\Users\Lance\AppData\Local\Personal\Propolis\propolis_windows.exe --no-specs .
}

function SoxDownsample([System.IO.DirectoryInfo[]]$folders = @($(Get-Item (Get-Location)))) {
    $folders.Add(@(Get-ChildItem -Directory -Recurse))

    foreach ($folder in $folders) {
        Push-Location $folder

        $currentPath = (Resolve-Path -LiteralPath .).Path
        $original = "$currentPath\original"
        $converted = "$currentPath\converted"
        $problemFiles = @()

        New-Item -ItemType Directory $original -Force
        New-Item -ItemType Directory $converted -Force 

        $files = Get-ChildItem *.flac

        foreach ($file in $files) {
            $flacInfo = & sox --i $file.FullName 2>&1 | Out-String

            $precision = if ($flacInfo -match "Precision\s*:\s*(\d+-bit)") { $matches[1] } 
            else { Write-Host "Can't determine bit rate"; $problemFiles.Add($file.BaseName); Continue }

            $sampleRate = @("192000", "176400", "96000", "88200", "48000", "44100") | Where-Object { $flacInfo -match "Sample Rate\s*:\s*$_" } | Select-Object -First 1

            $actions = @{
                "24-bit, 96000"  = { sox -S $file.FullName -R -G -b 16 "$converted\$($file.Name)" rate -v -L 48000 dither }
                "24-bit, 192000" = { sox -S $file.FullName -R -G -b 16 "$converted\$($file.Name)" rate -v -L 48000 dither }
                "24-bit, 88200"  = { sox -S $file.FullName -R -G -b 16 "$converted\$($file.Name)" rate -v -L 44100 dither }
                "24-bit, 176400" = { sox -S $file.FullName -R -G -b 16 "$converted\$($file.Name)" rate -v -L 44100 dither }
                "24-bit, 44100"  = { sox -S $file.FullName -R -G -b 16 "$converted\$($file.Name)" dither }
                "24-bit, 48000"  = { sox -S $file.FullName -R -G -b 16 "$converted\$($file.Name)" dither }
                "16-bit, 44100"  = { Write-Host "File is already 16-bit."; Continue }
                "16-bit, 48000"  = { Write-Host "File is already 16-bit."; Continue }
            }

            if ($precision -and $sampleRate) {
                $actionKey = "$precision, $sampleRate"
                if ($actions.ContainsKey($actionKey)) {
                    & $actions[$actionKey]
                    Move-Item -LiteralPath $file.FullName -Destination "$original"
                }
                else { Write-Host "Precision and bit rate is incompatibile at $precision and $sampleRate." }
            }
            else {
                $problemFiles.Add($file.BaseName)
            }
        }
    }

    if ($problemFiles.Count -gt 0) {
        Write-Host "The following file's bit-depth and sample rate could not be determined:"
        $problemFiles | ForEach-Object { Write-Host "`n$_" }
    }
    
    while (Get-ChildItem -LiteralPath $converted) {
        Get-ChildItem -LiteralPath $converted | Move-Item -Destination $currentPath
    }

    while ((Test-Path -LiteralPath $converted)) {
        Remove-Item -Recurse -LiteralPath $converted
        # Remove-Item -Recurse -LiteralPath $original 
    }

    Pop-Location
}

function RenameFileRed([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    $rootDirectory = $directory.Parent
    $fileList = @()
    $oldFileNames = "Old File Names:`n`n"

    Get-ChildItem $directory.FullName -Recurse -File | ForEach-Object {
        $relativePath = $_.FullName.Substring($rootDirectory.FullName.Length)

        if ($relativePath.Length -gt 180) {
            $oldFileNames.Add("$($_)`n")

            $newLength = 180 - ($relativePath.Length - $_.BaseName.Length)
            $newName = $_.BaseName.Substring(0, $newLength) + $_.Extension

            Rename-Item $_.FullName -NewName $newName

            $fileList.Add([System.IO.Path]::Combine($_.DirectoryName, $newName))
        }
    }

    if ($fileList.Count -gt 0) {
        $output = "$oldFileNames`n-----------------------`nNew File Names:`nfilelist:`"$($fileList -join "|")`""
        $desktopPath = [System.IO.Path]::Combine([Environment]::GetFolderPath('Desktop'), "Files Renamed - $($directory.BaseName).txt")
        Add-Content $desktopPath $output

        Write-Host "Files have been renamed for $directory.`n-----------------------"
    }
    else {
        Write-Host "No files renamed for $directory.`n-----------------------"
    }
}

function MP3Conversion([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\ConvertToMP3.py" $directory.FullName
}

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

function MakeTorrents([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    rfr $directory
    py -m py3createtorrent $directory
    Get-ChildItem *.torrent | ForEach-Object { Move-Item $_ $env:USERPROFILE\Desktop }
}

Set-Alias -Name rfr -Value renameFileRed