param ([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location)))

function RenameFileRed {
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