function TranslateFile($file) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\deepl_translation.py $file
}

function OpenCommandHistory {
    code C:\Users\Lance\AppData\Roaming\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt
}

function PrintVideoResolutions([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\video_editing.py PrintVideoResolution $directory.FullName
}

function RemuxDisc ([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\video_editing.py RemuxDisc $directory.FullName
}

function BatchCompression ([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\video_editing.py BatchCompression $directory.FullName
}

function ExtractChapters ([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\video_editing.py ExtractChapters $directory.FullName
}

function ListDirectories([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\misc.py list_dir $directory.FullName
}

function ListFilesAndDirectories([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\misc.py list_files_and_dirs $directory.FullName
}

function MakeTorrents([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location)), $process_all = "True") {
    py C:\Users\Lance\Documents\Powershell\python_scripts\misc.py make_torrents $directory.FullName $process_all
}