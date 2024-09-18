function Propolis {
    C:\Users\Lance\AppData\Local\Personal\Propolis\propolis_windows.exe --no-specs .
}

function SoxDownsample([String]$directory) {
    $folders = @(Get-Location $directory) + @(Get-ChildItem -Directory -Recurse)

    foreach ($folder in $folders) {
        Push-Location $folder

        $currentPath = (Resolve-Path -LiteralPath .).Path
        $original = "$currentPath\original"
        $converted = "$currentPath\converted"
        $problemFiles = @()

        New-Item -ItemType Directory -Force -Path $original
        New-Item -ItemType Directory -Force -Path $converted

        $files = Get-ChildItem *.flac

        foreach ($file in $files) {
            $flacInfo = $(sox --i $file.FullName 2>&1)

            if ($flacInfo -match "Precision\s*:\s*24-bit") {
                if ($flacInfo -match "Sample Rate\s*:\s*96000" -or $flacInfo -match "Sample Rate\s*:\s*192000") {
                    sox -S $file.FullName -R -G -b 16 "converted\$($file.Name)" rate -v -L 48000 dither
                }
                elseif ($flacInfo -match "Sample Rate\s*:\s*88200" -or $flacInfo -match "Sample Rate\s*:\s*176400") {
                    sox -S $file.FullName -R -G -b 16 "converted\$($file.Name)" rate -v -L 44100 dither
                }

                elseif ($flacInfo -match "Sample Rate\s*:\s*44100" -or $flacInfo -match "Sample Rate\s*:\s*48000") {
                    sox -S $file.FullName -R -G -b 16 "converted\$($file.Name)" dither
                }

                Move-Item -LiteralPath $file.FullName -Destination "$original"
            }
            elseif ($flacInfo -match "Precision\s*:\s*16-bit" -and (($flacInfo -match "Sample Rate\s*:\s*44100") -or ($flacInfo -match "Sample Rate\s*:\s*48000"))) {
                Write-Host "File is already 16-bit."
            }
            else {
                $problemFiles += $file.BaseName
            }
        }

        if ($problemFiles.Count -gt 0) {
            Write-Host "The following file's bit-depth and sample rate could not be determined:"
            foreach ($problemFile in $problemFiles) {
                Write-Host "`n$($problemFile.BaseName)"
            }
        }
    
        if ((Test-Path -LiteralPath $converted) -or (Test-Path -LiteralPath $original)) {
            while (Get-ChildItem -LiteralPath $converted) {
                Get-ChildItem -LiteralPath $converted | Move-Item -Destination $currentPath
            }

            while ((Test-Path -LiteralPath $converted) -or (Test-Path -LiteralPath $original)) {
                Remove-Item -Recurse -LiteralPath $converted
                Remove-Item -Recurse -LiteralPath $original 
            }
        }

        Pop-Location
    }
}

function RenameFileRed([String]$directory) {
    $rootDirectory = ((Get-Item $directory).Parent)
    $fileList = ""
    $oldFileNames = "Old File Names:`n"

    Get-ChildItem -Recurse -File | ForEach-Object {
        $relativePath = $_.FullName.Substring($rootDirectory.FullName.Length)
        Write-Host $relativePath

        if ($relativePath.Length -gt 180) {
            $oldFileNames += $.FullName

            $newLength = 180 - ($relativePath.Length - $_.BaseName.Length)
            $newName = $_.BaseName.Substring(0, $newLength) + $_.Extension
            Rename-Item -LiteralPath $_.FullName -NewName $newName

            if ($fileList) {
                $fileList += "|$newName"
            }
            else {
                $fileList = "filelist:`"$newName"
            }
        }
    }

    $output += $oldFileNames + "----------------------------------`n`nNew Files:---------------------------------" + $fileList + "`""

    $desktopPath = [System.IO.Path]::Combine([Environment]::GetFolderPath('Desktop'), "Files Renamed - $($rootDirectory.BaseName).txt")

    $output | Out-File $desktopPath
}

function ConvertToMP3([String]$directory) {
    $currentPath = (Resolve-Path $directory).Path
    $newFolder = "$((Split-Path $currentPath -Parent))\$((Split-Path $currentPath -Leaf)) (MP3)"
    
    $flacFiles = Get-ChildItem -LiteralPath $currentPath -Recurse 
    
    foreach ($file in $flacFiles) {
        $relativePath = $file.FullName.Substring($currentPath.Length).TrimStart('\')
        $destinationPath = Join-Path $newFolder $relativePath
        $destinationFolder = Split-Path $destinationPath -Parent
        $mp3Path = [System.IO.Path]::ChangeExtension($destinationPath, "mp3")

        if (-not (Test-Path $destinationFolder)) {
            New-Item -ItemType Directory -Force -Path $destinationFolder | Out-Null
        }

        try {
            & ffmpeg -i $file.FullName -codec:a libmp3lame -map_metadata 0 -id3v2_version 3 -b:a 320k $mp3Path -y
        }
        catch {
            $errorMsg = "Exception while converting: $($file.FullName)"
            Write-Host $errorMsg
            $errorMsg | Out-File -FilePath "Error.log" -Encoding UTF8 -Append
        }
    }

    $mp3Files = Get-ChildItem -Path $newFolder -Filter *.mp3 -Recurse | Select-Object -ExpandProperty FullName
    $mp3Set = $mp3Files | ForEach-Object { 
        $_.Substring($newFolder.Length).TrimStart('\') 
    }

    $missingMp3s = @($flacFiles | Where-Object {
            $relativePath = $_.FullName.Substring($currentPath.Length).TrimStart('\')
            $mp3Path = [System.IO.Path]::ChangeExtension($relativePath, "mp3")
            -not $mp3Set.Contains($mp3Path)
        } | ForEach-Object { $_.FullName.Substring($currentPath.Length).TrimStart('\') })

    if ($missingMp3s.Count -gt 0) {
        $message = "-----------------`n`n`n`nThe following FLAC files for $currentPath were not converted to MP3:"
        Write-Host $message
        $message | Out-File -FilePath "Conversion.log" -Encoding UTF8 -Append
        
        $missingMp3s | ForEach-Object { 
            Write-Host $_ 
            $_ | Out-File -FilePath "Conversion.log" -Encoding UTF8 -Append
        }
    }
    else {
        $successMessage = "-----------------`n`n`n`nAll FLAC Files for $currentPath were successfully converted to MP3."
        Write-Host ""
        $successMessage | Out-File -FilePath "C:\Users\Lance\Desktop\Conversion - $($currentPath.BaseName)).log" -Encoding UTF8 -Append
        Set-Location $destinationFolder
        MakeTorrents
    }
    
    & robocopy $currentPath $newFolder /E /XF *.log *.cue *.md5 *.flac
}

function Remove-DuplicateEntries([string]$inputFile) {
    $lines = $inputFile
    $uniqueLines = @()
    $lastFileNamePrefix = ""
    $i = 0

    while ($i -lt $lines.Length) {
        if ($lines[$i] -match "^File name:") {
            $fileName = $lines[$i] -replace "^File name: \d+\.\s*", ""

            if ($fileName -match "^(\w+ \w+)") {
                $fileNamePrefix = $matches[1]
            }

            if ($fileNamePrefix -ne $lastFileNamePrefix) {
                $uniqueLines += $lines[$i]
                if ($i + 1 -lt $lines.Length) { $uniqueLines += $lines[$i + 1] }
                if ($i + 2 -lt $lines.Length) { $uniqueLines += $lines[$i + 2] }
                $uniqueLines += ""

                $lastFileNamePrefix = $fileNamePrefix
                $i += 3
            }
            else {
                $i += 3
            }
        }
    }

    Set-Content -Path $inputFile -Value $uniqueLines
}

function Get-FlacMetadata([System.IO.DirectoryInfo]$directory) {
    $outputFile = "$env:USERPROFILE\Desktop\$($directory.BaseName).txt"

    Get-ChildItem -Path $directory -Recurse -Filter *.flac | ForEach-Object {
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
            "Disc Number: $disc"
        )

        $content | Out-File -FilePath $outputFile -Append
    }

    Remove-DuplicateEntries $outputFile
}

function MakeTorrents($directory) {
    Get-ChildItem $directory -Directory | ForEach-Object {
        Set-Location $_.FullName
        rfr
        py -m py3createtorrent $_.FullName
        Get-ChildItem *.torrent | ForEach-Object { Move-Item $_ $env:USERPROFILE\Desktop }
    }
}

Set-Alias -Name rfr -Value renameFileRed