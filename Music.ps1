function Propolis {
    C:\Users\Lance\AppData\Local\Personal\Propolis\propolis_windows.exe --no-specs .
}

function ConvertMusic([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location)), [string]$format = "all") {
    py C:\Users\Lance\Documents\Powershell\python_scripts\music_conversion.py $directory -f $format
}

function ConvertToMP3([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\music_conversion.py -f mp3 $directory.FullName
}

function ConvertToFLAC([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\music_conversion.py -f flac $directory.FullName
}

function RenameFileRed([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\music_tools.py -c rfr -d $directory.FullName
}

function GetEmbeddedImageSize([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\music_tools.py -c calculate_image_size -d $directory.FullName
}

function ZipFiles([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location)), $process_all = "True") {
    py C:\Users\Lance\Documents\Powershell\python_scripts\zip_folders.py $directory.FullName $process_all
}

function SACDExtract([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\music_conversion.py $directory.FullName
}

Set-Alias -Name rfr -Value renameFileRed