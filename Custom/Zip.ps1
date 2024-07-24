function ZipFolders() {
    ZipFoldersLogic -path (Get-Location)
}

function ZipFoldersLogic($path) {
    $path = Get-Item -LiteralPath $path
    $maxSize = 9500 * 1000 * 1000
    $zipFilePath = "C:\Users\Lance\Desktop\zippedFolders"

    Write-Output "Now zipping up $($path.BaseName)"

    New-Item -ItemType Directory -Path $zipFilePath -Force

    $allItems = Get-ChildItem -LiteralPath $path.FullName -Recurse

    $sortedItems = Get-FolderSize | Sort-Object SizeBytes

    $zipFolder = @()
    $zipFileCounter = 1
    $currentSize = 0
    $currentIndex = 0
    $lastIndex = $sortedItems.Length - 1

    while ($currentIndex -le $lastIndex) {
        $selectedItem = $sortedItems[$currentIndex]

        $itemSize = if ($selectedItem.PSIsContainer) {
        (Get-ChildItem -LiteralPath $selectedItem.FullName -Recurse -File | Measure-Object -Property Length -Sum).Sum
        }
        else {
            ($selectedItem.Length)
        }

        if ($itemSize -gt $maxSize -and $selectedItem.PSIsContainer) {
            Write-Output "$($selectedItem.BaseName) is too big to be zipped up. Now applying code recursively..."

            ZipFoldersLogic $selectedItem.FullName

            $allItems = $allItems | Where-Object { $_.FullName -notlike $selectedItem.FullName }

            $currentIndex++
            continue
        }
        
        if ($currentSize + $itemSize -gt $maxSize -or $currentIndex -eq $lastIndex) {
            if ($selectedItem -eq $sortedItems[$lastIndex]) {
                Write-Output "Reached last item of directory"
                $zipFolder += $sortedItems[$lastIndex]
            }

            Compress-Archive -CompressionLevel NoCompression -Path $zipFolder -DestinationPath "$zipFilePath\$($path.BaseName) - $zipFileCounter.zip"

            $logContent = "Contents of $($path.BaseName) - $($zipFileCounter).zip is:`n"

            foreach ($item in $zipFolder) {
                if ($item.PSIsContainer) {
                    $logContent += "Folder: $($item.BaseName)`n"
        
                    $subfiles = Get-ChildItem -LiteralPath $item.FullName -Recurse -File
                    $subfiles | ForEach-Object {
                        $logContent += ("`t" + $_.Name + "`n")
                    }
                }
                else {
                    $logContent += ((Get-Item $item).BaseName + "`n")
                }
            }
        
            $logContent | Out-File -Append "$zipFilePath\ZippedFoldersLog.txt"

            $currentSize = 0
            $zipFolder = @()
            $zipFileCounter++
        }

        $currentSize += $itemSize
        $zipFolder += $selectedItem
        $currentIndex++

        $allItems = $allItems | Where-Object { $_.FullName -notlike $selectedItem.FullName }
    }

    if ($allItems) {
        Write-Output "The following items did not get zipped up:"
        $allItems | ForEach-Object {
            Write-Output $_.BaseName
        }
    }

}