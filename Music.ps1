function Propolis {
    C:\Users\Lance\AppData\Local\Personal\Propolis\propolis_windows.exe --no-specs .
}

function SoxDownsample {
    $folders = @(Get-Location) + @(Get-ChildItem -Directory -Recurse)

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

function RenameFileRed {
    $rootDirectory = (Get-Item ..)

    Get-ChildItem -Recurse -File | ForEach-Object {
        $relativePath = $_.FullName.Substring($rootDirectory.FullName.Length)

        if ($relativePath.Length -gt 180) {
            $newLength = 180 - ($relativePath.Length - $_.BaseName.Length)
            $newName = $_.BaseName.Substring(0, $newLength) + $_.Extension
            $newFullName = Join-Path $_.Directory $newName

            Rename-Item -LiteralPath $_.FullName -NewName $newName
            Write-Host "Old Name: $($_.FullName)`nNew Name: $newFullName`n-----------------------------"
        }
    }
}

function MakeMP3Torrents {
    Get-ChildItem -Directory -Filter "*MP3*" -Recurse | ForEach-Object {
        Set-Location $_.FullName
        rfr
        py -m py3createtorrent $_.FullName
        Get-ChildItem *.torrent | ForEach-Object { Move-Item $_ "D:\Dropbox\Lance" }
    }
}

function ConvertToMP3 {
    $currentPath = (Resolve-Path .).Path
    $newFolder = "$((Split-Path $currentPath -Parent))\$((Split-Path $currentPath -Leaf)) (MP3)"

    New-Item -ItemType Directory -Force -Path $newFolder | Out-Null

    $flacFiles = Get-ChildItem -File -Recurse | Where-Object { $_.Extension -eq ".flac" }

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
        $message = "-----------------`n`n`n`nThe following FLAC files were not converted to MP3:"
        Write-Host $message
        $message | Out-File -FilePath "Conversion.log" -Encoding UTF8 -Append
        
        $missingMp3s | ForEach-Object { 
            Write-Host $_ 
            $_ | Out-File -FilePath "Conversion.log" -Encoding UTF8 -Append
        }
    }
    else {
        Write-Host "-----------------`n`n`n`nAll FLAC Files Were Successfully Converted To MP3."
        $successMessage | Out-File -FilePath "Conversion.log" -Encoding UTF8 -Append
    }
}

Set-Alias -Name rfr -Value renameFileRed