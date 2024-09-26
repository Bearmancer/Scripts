function Propolis {
    C:\Users\Lance\AppData\Local\Personal\Propolis\propolis_windows.exe --no-specs .
}

function SoxDownsample([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Sox Downsample.py" $directory.FullName
}

function RenameFileRed([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Rename Files.py" $directory.FullName
}

function ConvertToMP3([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\ConvertToMP3.py" $directory
}

function GetEmbeddedImageSize([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Get Embedded Image Size.py" $directory.FullName
}

function SACDExtract([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\SACD.py" $directory.FullName
}

function MakeTorrents([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    rfr $directory
    py -m py3createtorrent $directory
    Get-ChildItem *.torrent | ForEach-Object { Move-Item $_ $env:USERPROFILE\Desktop }
}

Set-Alias -Name rfr -Value renameFileRed