function propolis {
    C:\Users\Lance\AppData\Local\Personal\Propolis\propolis_windows.exe --no-specs .
}

function soxDownsample {

    $sox = "C:\Program Files (x86)\sox-14-4-2\sox.exe"
    $files = Get-ChildItem | Where-Object { $_.Extension -eq '.flac' -or $_.Extension -eq '.mka' }
    $currentPath = (Resolve-Path .).Path
    $original = "$currentPath\original"
    $converted = "$currentPath\converted"
    $problemFiles = @()

    New-Item -ItemType Directory -Path $original
    New-Item -ItemType Directory -Path $converted

    foreach ($file in $files) {
        $flacInfo = & $sox --i $file.FullName 2>&1
        
        if ($flacInfo -match "Precision\s*:\s*24-bit") {
            if ($flacInfo -match "Sample Rate\s*:\s*96000" -or $flacInfo -match "Sample Rate\s*:\s*192000") {
                & $sox -S $file.FullName -R -G -b 16 "converted\$($file.Name)" rate -v -L 48000 dither
            }
            elseif ($flacInfo -match "Sample Rate\s*:\s*88200" -or $flacInfo -match "Sample Rate\s*:\s*176400") {
                & $sox -S $file.FullName -R -G -b 16 "converted\$($file.Name)" rate -v -L 44100 dither
            }

            elseif ($flacInfo -match "Sample Rate\s*:\s*44100" -or $flacInfo -match "Sample Rate\s*:\s*48000") {
                & $sox -S $file.FullName -R -G -b 16 "converted\$($file.Name)" dither
            }

            Move-Item -LiteralPath $file.FullName -Destination "$original"
        }
        elseif ($flacInfo -match "Precision\s*:\s*16-bit" -and (($flacInfo -match "Sample Rate\s*:\s*44100") -or ($flacInfo -match "Sample Rate\s*:\s*48000"))) {
            Write-Output "$($file.BaseName) is already 16-bit."
        }
        else {
            $problemFiles += $file.BaseName
        }
    }

    if ($elseBlockFiles.Count -gt 0) {
        Write-Output "The following file's bit-depth and sample rate could not be determined:"
        foreach ($problemFile in $problemFiles) {
            Write-Output "`n$($elseFile.BaseName)"
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
}

function renameFileRed {
    $rootDirectory = (Get-Item ..)

    Get-ChildItem -Recurse -File | ForEach-Object {
        $relativePath = $_.FullName.Substring($rootDirectory.FullName.Length)

        if ($relativePath.Length -gt 180) {
            $newLength = 180 - ($relativePath.Length - $_.BaseName.Length)
            $newName = $_.BaseName.Substring(0, $newLength) + $_.Extension
            $newFullName = Join-Path $_.Directory $newName

            Rename-Item -LiteralPath $_.FullName -NewName $newName
            Write-Output "Old Name: $($_.FullName)`nNew Name: $newFullName`n-----------------------------"
        }
    }
}

function MoveEachFolder() {
    $cueFiles = Get-ChildItem -Path Get-Location -Filter "*.cue"

    foreach ($cueFile in $cueFiles) {
        $discNumber = [regex]::Match($cueFile.Name, '\d+').Value
        Write-Host "Processing Disc $discNumber"

        $destinationDirectory = Join-Path -Path $sourceDirectory -ChildPath ("Disc $discNumber")
        if (-not (Test-Path -Path $destinationDirectory)) {
            New-Item -Path $destinationDirectory -ItemType Directory | Out-Null
            Write-Host "Created directory: $destinationDirectory"
        }
        
        $flacFiles = Get-ChildItem -Path $sourceDirectory -Filter "*$discNumber*.flac"

        if ($flacFiles.Count -eq 0) {
            Write-Host "No FLAC files found for Disc $discNumber"
        }
        else {
            foreach ($flacFile in $flacFiles) {
                Write-Host "Moving files..."
                if (Test-Path -Path $cueFile.FullName) {
                    Move-Item -Path $cueFile.FullName, $flacFile.FullName -Destination $destinationDirectory
                }
                else {
                    Write-Host "Cue file not found for Disc $discNumber"
                }
            }
        }
    }
}

Set-Alias -Name rfr -Value renameFileRed
Set-Alias -Name sd -Value soxDownsample