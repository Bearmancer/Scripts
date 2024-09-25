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
        Remove-Item -Recurse -LiteralPath $original 
    }

    Pop-Location
}