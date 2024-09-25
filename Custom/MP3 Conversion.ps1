param (
    [System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))
)
function MP3Conversion {
    $currentPath = (Resolve-Path $directory).Path
    $logPath = "C:\Users\Lance\Desktop\Conversion Log.txt"
    $outputPath = [System.IO.Path]::Combine("C:\Users\Lance\Desktop\Torrents\MP3", "$($directory.Name) (Converted)")

    New-Item -ItemType Directory $outputPath -Force

    Get-ChildItem $directory *.flac -Recurse | ForEach-Object {
        $relativePath = $_.FullName.Substring($directory.FullName.Length).TrimStart('\')
        $newFLACPath = [System.IO.Path]::Combine($outputPath, $relativePath)
        $newFLACDirectory = [System.IO.Path]::GetDirectoryName($newFLACPath)

        if (-not (Test-Path $newFLACDirectory)) { New-Item -ItemType Directory $newFLACDirectory -Force }

        Copy-Item $_ $newFLACDirectory

        metaflac --dont-use-padding --remove --block-type=PICTURE,PADDING $newFLACPath
        metaflac --add-padding=8192 $newFLACPath
    
        $mp3Path = [System.IO.Path]::ChangeExtension($newFLACPath, "mp3")
        $mp3Dir = [System.IO.Path]::GetDirectoryName($mp3Path)
    
        if (-not (Test-Path $mp3Dir)) { New-Item -ItemType Directory $mp3Dir -Force }
    
        try {
            & ffmpeg -i $newFLACPath -codec:a libmp3lame -map_metadata 0 -id3v2_version 3 -b:a 320k $mp3Path -y
        }
        catch {
            $errorMsg = "Exception while converting: $newFLACPath"
            Write-Host $errorMsg
            Add-Content $logPath $errorMsg
        }
    
        Remove-Item $newFLACPath
    }

    $flacFiles = Get-ChildItem $currentPath *.flac -Recurse 
    $mp3Files = Get-ChildItem $outputPath *.mp3 -Recurse | Select-Object -ExpandProperty FullName
    
    {
        $mp3Set = $mp3Files | ForEach-Object { 
            $_.Substring($outputPath.Length).TrimStart('\') 
        }

        $missingMp3s = @($flacFiles | Where-Object {
                $relativePath = $_.FullName.Substring($currentPath.Length).TrimStart('\')
                $mp3Path = [System.IO.Path]::ChangeExtension($relativePath, "mp3")
                -not $mp3Set.Contains($mp3Path)
            } | ForEach-Object { $_.FullName.Substring($currentPath.Length).TrimStart('\') })

        if ($missingMp3s.Count -gt 0) {
            $message = "Problematic Files:`nfilelist:`"$($missingMp3s -join "|")`"" 
            Write-Host $message
            Add-Content $logPath $message 
        }
        else {
            $message = "All FLAC Files for $currentPath were successfully converted to MP3.`n------------------`n"
            Write-Host $message
            Add-Content $logPath $message

            RenameFileRed $outputPath
        }
    }
}