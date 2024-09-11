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

    New-Item -ItemType Directory -Force -Path $newFolder

    Get-ChildItem -File -Recurse | ForEach-Object {
        $relativePath = $_.FullName.Substring($currentPath.Length).TrimStart("\")
        $destinationPath = Join-Path $newFolder $relativePath
        $destinationFolder = Split-Path $destinationPath -Parent

        if (-not (Test-Path $destinationFolder)) {
            New-Item -ItemType Directory -Force -Path $destinationFolder
        }

        if ($_.Extension -eq ".flac") {
            $flacInfo = sox --i $_.FullName 2>&1

            if ($flacInfo -match "Precision\s*:\s*16-bit") {
                $mp3Path = Join-Path $destinationFolder "$($_.BaseName).mp3"
                ffmpeg -i $_.FullName -codec:a libmp3lame -map_metadata -1 -b:a 320k $mp3Path
            }
            else {
                Write-Host "$($_.FullName) is not a 16-bit FLAC file."
            }
        }
        elseif ($_.Extension -notin ".cue", ".m3u", ".md5", ".accurip", ".log") {
            Copy-Item -Path $_.FullName -Destination $destinationPath
        }
    }
}

Set-Alias -Name rfr -Value renameFileRed