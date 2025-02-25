function OpenCommandHistory {
    code C:\Users\Lance\AppData\Roaming\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt
}

function PrintVideoResolutions([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\video_editing.py PrintVideoResolution $directory.FullName
}

function RemuxDisc ([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    Write-Host "Remuxing disc in $directory"

    py C:\Users\Lance\Documents\Powershell\python_scripts\video_editing.py RemuxDisc $directory.FullName

    Write-Host "Remuxing has finished"
}

function BatchCompression ([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\video_editing.py BatchCompression $directory.FullName
}

function ExtractChapters ([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\video_editing.py ExtractChapters $directory.FullName
}

function ListDirectories([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location)), [string]$sort_order = "0") {
    py C:\Users\Lance\Documents\Powershell\python_scripts\misc.py list_dir $directory.FullName $sort_order
}

function ListFilesAndDirectories([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location)), [string]$sort_order = "0") {
    py C:\Users\Lance\Documents\Powershell\python_scripts\misc.py list_files_and_dirs $directory.FullName $sort_order
}

function MakeTorrents {[CmdletBinding()] param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location)),

        [Parameter(Position = 1)]
        [bool]$recursive = $false
    )

    if ($recursive) {
        py C:\Users\Lance\Documents\Powershell\python_scripts\misc.py make_torrents $directory.FullName --recursive
    } else {
        py C:\Users\Lance\Documents\Powershell\python_scripts\misc.py make_torrents $directory.FullName
    }
}