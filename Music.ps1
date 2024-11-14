function Propolis {
    C:\Users\Lance\AppData\Local\Personal\Propolis\propolis_windows.exe --no-specs .
}

function SoxDownsample([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location)), $process_all = "True") {
    py C:\Users\Lance\Documents\Powershell\python_scripts\sox_downsample.py $directory.FullName $process_all
}

function RenameFileRed([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\music.py rfr $directory.FullName
}

function GetEmbeddedImageSize([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\music.py calculate_image_size $directory.FullName
}

function ConvertToMP3([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location)), $process_all = "True") {
    py C:\Users\Lance\Documents\Powershell\python_scripts\convert_to_mp3.py $directory.FullName $process_all
}

function ZipFiles([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location)), $process_all = "True") {
    py C:\Users\Lance\Documents\Powershell\python_scripts\zip_folders.py $directory.FullName $process_all
}

function SACDExtract([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\sacd.py $directory.FullName
}

Set-Alias -Name rfr -Value renameFileRed