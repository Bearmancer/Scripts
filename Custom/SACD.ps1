function ExtractSACDs {
    $isoFiles = Get-ChildItem -Recurse "*.iso"
    $totalDiscs = ($isoFiles | Measure-Object).Count
    $sacd_extract = "C:\Users\Lance\AppData\Local\Personal\sacd_extract\sacd_extract.exe"

    Move-ISO

    $discNumber = 0

    foreach ($isoFile in $isoFiles) {
        if ($totalDiscs -gt 1) { ++$discNumber }
        
        $parentFolder = $isoFile.Directory.FullName

        # Set grandparent directory as parent directory for mulit-disc albums
        if ($isoFile.Directory -match "Disc.*" -or $isoFile.Directory -match "CD.*") {
            $parentFolder = $isoFile.Directory.Parent.FullName
        }

        $result = & $sacd_extract -P -i $isoFile

        if ($result -match "Multichannel" -or $result -match "5 Channel" -or $result -match "6 Channel") {
            $multiChannelParentFolder = "{0} [SACD - 5.1 - 24-88.1]" -f $parentFolder

            if (-not (Test-Path -LiteralPath $multiChannelParentFolder)) {
                New-Item -ItemType Directory $multiChannelParentFolder
            }

            #Convert ISO to DFF Files
            & $sacd_extract -m -p -c -i $isoFile.FullName

            #Create Directory to Move DFF Files
            $discLocation = GetDiscLocation $multiChannelParentFolder $discNumber

            # #Move DFF to New Folder
            Get-ChildItem -Recurse *.dff | % { Move-Item $_.FullName $discLocation }
        }

        if ($result -match "Stereo" -or $result -match "2 Channel" -or $result -match "Multichannel" -or $result -match "5 Channel") {
            $stereoParentFolder = "{0} [SACD - 2.0 - 24-88.1]" -f $parentFolder

            if (-not (Test-Path -LiteralPath $stereoParentFolder)) {
                New-Item -ItemType Directory $stereoParentFolder
            }

            #Convert ISO to DFF Files
            & $sacd_extract -2 -p -c -i $isoFile.FullName

            #Create Directory to Move DFF Files
            $discLocation = GetDiscLocation $stereoParentFolder $discNumber

            # #Move DFF to New Folder
            Get-ChildItem -Recurse *.dff | % { Move-Item $_.FullName $discLocation }
            
        }
        else {
            Write-Host "Audio is neither 5ch nor stereo."
            Continue
        }
    }

    Get-ChildItem -LiteralPath $stereoParentFolder -Recurse -Directory | DFFtoFLAC
    if ($multiChannelParentFolder) {
        Get-ChildItem -LiteralPath $multiChannelParentFolder -Recurse -Directory | DFFtoFLAC
    } else {
        Write-Host "Multichannel audio does not exist for this folder"
    }
            
    # Delete empty folders
    Get-ChildItem -Directory -Recurse | % { Write-Host "Now processing: `n $($_.FullName)" } 
    Get-ChildItem -Directory -Recurse | Where-Object { $_.GetFileSystemInfos().Count -eq 0 } | % { Remove-Item -LiteralPath $_.FullName -Force }
}

function GetDiscLocation([System.IO.DirectoryInfo]$parentFolder, $discNumber) {
    $discLocation = $parentFolder
            
    if ($discNumber -gt 0) {
        $subDirectoryPath = "{0}/Disc {1}" -f $parentFolder.FullName, $discNumber
        New-Item -ItemType Directory $subDirectoryPath -Force | Out-Null
        $discLocation = $subDirectoryPath
    }

    return $discLocation
}

function CheckDynamicRange($directory) {
    $dr_gains = @()

    foreach ($file in Get-ChildItem $directory *.dff) {
        $output = ffmpeg -i $file.FullName -af "volumedetect" -f null - 2>&1
        $dr_gain = $output | Select-String -Pattern 'max_volume: (-?\d+(\.\d+)?) dB' | ForEach-Object { $_.Matches.Groups[1].Value }
        $dr_gains += [double]$dr_gain
    }

    $max_dr_gain = ($dr_gains | Measure-Object -Maximum).Maximum

    Return $max_dr_gain
}

function DFFtoFLAC {
    param(
        [Parameter(ValueFromPipeline = $true)]
        [System.IO.DirectoryInfo]$inputFolder
    )

    process {

        $files = Get-ChildItem -LiteralPath $inputFolder *.dff
        
        $dynamicRange = CheckDynamicRange $inputFolder
        $dynamicRange -= 0.5

        foreach ($file in $files) {
            $flacFile = $file.FullName -replace ".dff", ".flac"

            ffmpeg -i $file.FullName -vn -c:a flac -sample_fmt s32 -ar 88200 -af "volume=$($dynamicRange)" -dither_method triangular $flacFile
    
            $trimmedFlacFile = $flacFile -replace '\.flac$', ' - Trimmed.flac'
    
            sox $flacFile $trimmedFlacFile trim 0.0065 reverse silence 1 0 0% trim 0.0065 reverse pad 0.0065 0.2

            if (Test-Path -LiteralPath $trimmedFlacFile) {
                Remove-Item -LiteralPath $flacFile
                Rename-Item -LiteralPath $trimmedFlacFile -NewName $flacFile
            } else {
                Write-Host "$($trimmedFlacFile) not found"
            }
        }

        CheckDFFandFLAC $inputFolder
    }

}

function CheckDFFandFLAC([System.IO.FileSystemInfo] $directory) {
    $flacCount = (Get-ChildItem -LiteralPath $directory *.flac | Measure-Object).Count
    $dffFiles = Get-ChildItem -LiteralPath $directory *.dff
    $dffCount = ($dffFiles | Measure-Object).Count

    Write-Host "FLAC files count: $flacCount"
    Write-Host "DFF files count: $dffCount"

    if ($flacCount -eq $dffCount) {
        Write-Host "Equal number of FLAC and DFF files."
        $dffFiles | Remove-Item -Force
    }
    else {
        Write-Host "Unequal number of FLAC and DFF files in $($directory.FullName)"
    }
}

function Move-ISO {
    $folders = Get-ChildItem -Recurse *.iso |
    Group-Object Directory | 
    Where-Object { $_.Count -gt 1 } |
    Select-Object -ExpandProperty Name

    foreach ($folder in $folders) {
        $isoFiles = Get-ChildItem $folder *.iso

        $index = 1
        foreach ($isoFile in $isoFiles) {
            $newFolder = Join-Path -Path $folder -ChildPath "Disc $index"
            New-Item -ItemType Directory $newFolder -Force
            Move-Item $isoFile.FullName $newFolder
            $index++
        }
    }
}