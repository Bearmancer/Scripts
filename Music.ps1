function Propolis {
    C:\Users\Lance\AppData\Local\Personal\Propolis\propolis_windows.exe --no-specs .
}

function SoxDownsample([System.IO.DirectoryInfo]$directory) {
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

function RenameFileRed([System.IO.DirectoryInfo]$directory) {
    $rootDirectory = ((Get-Item $directory).Parent)
    $fileList = @()
    $oldFileNames = "Old File Names:`n`n"

    Get-ChildItem -Path $directory.FullName -Recurse -File | ForEach-Object {
        $relativePath = $_.FullName.Substring($rootDirectory.FullName.Length)

        if ($relativePath.Length -gt 180) {
            $oldFileNames += "$($_.FullName)`n"

            $newLength = 180 - ($relativePath.Length - $_.BaseName.Length)
            $newName = $_.BaseName.Substring(0, $newLength) + $_.Extension

            Rename-Item $_.FullName -NewName $newName

            $fileList += [System.IO.Path]::Combine($_.DirectoryName, $newName)
        }
    }

    if ($fileList.Count -gt 0) {
        $output += $oldFileNames
        $output += "---------------------------------`nNew File Names:`n`n"
        $output += "filelist:`"" + ($fileList -join "|") + "`""
        $desktopPath = [System.IO.Path]::Combine([Environment]::GetFolderPath('Desktop'), "Files Renamed - $($directory.BaseName).txt")
        $output | Out-File $desktopPath

        Write-Host "Files have been renamed for $directory."
    }
    else {
        Write-Host "No files renamed for $directory."
    }
}

function ConvertToMP3([System.IO.DirectoryInfo]$directory) {
    $currentPath = (Resolve-Path $directory).Path
    $newFolder = "$((Split-Path $currentPath -Parent))\$((Split-Path $currentPath -Leaf)) (MP3)"
    $logPath = "C:\Users\Lance\Desktop\Conversion Log.txt"
    
    $flacFiles = Get-ChildItem -Path $currentPath -Recurse | Where-Object Extension -eq ".flac"
    
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
            $errorMsg | Out-File -FilePath $logPath -Append
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
        $message = "The following FLAC files for $currentPath were not converted to MP3:"
        Write-Host $message
        $message | Out-File -FilePath $logPath -Append
        
        $missingMp3s | ForEach-Object { 
            Write-Host $_ 
            $_ | Out-File -FilePath $logPath -Append
        }
    }
    else {
        $successMessage = "All FLAC Files for $currentPath were successfully converted to MP3.`n`n------------------"
        $successMessage | Out-File -FilePath $logPath -Append

        Set-Location $destinationFolder
        MakeTorrents
    }
    
    & robocopy $currentPath $newFolder /E /XF *.log *.cue *.md5 *.flac
}

function Remove-DuplicateEntries([String]$inputFile) {
    $lines = Get-Content $inputFile
    $uniqueLines = @()
    $lastFileNamePrefix = ""
    $i = 0

    for ($i = 0; $i -lt $lines.Length; $i += 4) {
        if ($lines[$i] -match "^File name:") {
            $fileName = $lines[$i]

            $fileNamePrefix = $fileName -replace '^File name\:\s*\d*\.*\[*\(*\s*', ''
            Write-Host "`$filenameprefix is $fileNamePrefix"

            $parts = $fileNamePrefix.Split(' ')
            elseif ($parts.Length -ge 3) { $fileNamePrefix = $parts[0..2] -join ' ' }
            elseif ($parts.Length -ge 2) { $fileNamePrefix = $parts[0..1] -join ' ' }
            else { $uniqueLines += $lines[$i..[math]::Min($i + 3, $lines.Length - 1)]; continue }

            if ($fileNamePrefix -ne $lastFileNamePrefix) {
                $uniqueLines += $lines[$i..[math]::Min($i + 3, $lines.Length - 1)]

                $lastFileNamePrefix = $fileNamePrefix
            }
            else { Write-Host "Skipping duplicate entry" }
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
        rfr $_.FullName
        py -m py3createtorrent $_.FullName
        Get-ChildItem *.torrent | ForEach-Object { Move-Item $_ $env:USERPROFILE\Desktop }
    }
}

Set-Alias -Name rfr -Value renameFileRed