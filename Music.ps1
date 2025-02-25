function Propolis {
    C:\Users\Lance\AppData\Local\Personal\Propolis\propolis_windows.exe --no-specs .
}

function ConvertMusic([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\music_utility.py $directory
}

function RenameFileRed([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\music.py rfr $directory.FullName
}

function GetEmbeddedImageSize([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\music.py calculate_image_size $directory.FullName
}

function ZipFiles([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location)), $process_all = "True") {
    py C:\Users\Lance\Documents\Powershell\python_scripts\zip_folders.py $directory.FullName $process_all
}

function SACDExtract([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\sacd.py $directory.FullName
}

Set-Alias -Name rfr -Value renameFileRed