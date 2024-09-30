function Propolis {
    C:\Users\Lance\AppData\Local\Personal\Propolis\propolis_windows.exe --no-specs .
}

function SoxDownsample([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\sox_downsample.py $directory.FullName
}

function RenameFileRed([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\music.py rfr $directory.FullName
}

function GetEmbeddedImageSize([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\music.py calculate_image_size $directory.FullName
}

function ConvertToMP3([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\convert_to_mp3.py $directory.FullName
}

function SACDExtract([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\sacd.py $directory.FullName
}

function MakeTorrents([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    rfr $directory
    py -m py3createtorrent -P -t "https://home.opsfet.ch/7a0917ca5bbdc282de7f2eed00a69e2b/announce" -s "OPS" $directory -o "$env:USERPROFILE\Desktop\$($directory.BaseName) - OPS.torrent"
    py -m py3createtorrent -P -t "https://flacsfor.me/250f870ba861cefb73003d29826af739/announce" -s "RED" $directory -o "$env:USERPROFILE\Desktop\$($directory.BaseName) - RED.torrent"
}

Set-Alias -Name rfr -Value renameFileRed