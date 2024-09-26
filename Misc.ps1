function TranslateFile($file) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\DeepL Translation.py" $file
}

function CallCommandHistory {
    code C:\Users\Lance\AppData\Roaming\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt
}

function PrintVideoResolutions([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Video Editing.py" $directory.FullName PrintVideoResolution
}

function RemuxDVD ([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Video Editing.py" $directory.FullName RemuxDVD
}

function BatchCompression ([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Video Editing.py" $directory.FullName BatchCompression
}

function ExtractChapters ([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Video Editing.py" $directory.FullName ExtractChapters
}

function ListDirectories([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Misc.py" $directory.FullName list_dir
}

function ListFilesAndDirectories([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\List Files and Directories.py" $directory.FullName list_files_and_dirs
}