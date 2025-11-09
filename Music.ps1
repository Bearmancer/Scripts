function Propolis {
    C:\Users\Lance\AppData\Local\Personal\Propolis\propolis_windows.exe --no-specs .
}

function ConvertMusic([System.IO.DirectoryInfo]$directory = $( Get-Item . ), [string]$format = 'all') {
    python C:\Users\Lance\Documents\Powershell\python_scripts\music_conversion.py convert $directory.FullName -f $format
}

function ConvertToMP3([System.IO.DirectoryInfo]$directory = $( Get-Item . )) {
    python C:\Users\Lance\Documents\Powershell\python_scripts\music_conversion.py -f mp3 $directory.FullName
}

function ConvertToFLAC([System.IO.DirectoryInfo]$directory = $( Get-Item . )) {
    python C:\Users\Lance\Documents\Powershell\python_scripts\music_conversion.py -f flac $directory.FullName
}

function RenameFileRed([System.IO.DirectoryInfo]$directory = $( Get-Item . )) {
    python C:\Users\Lance\Documents\Powershell\python_scripts\music_tools.py -c rfr -d $directory.FullName
}

function GetEmbeddedImageSize([System.IO.DirectoryInfo]$directory = $( Get-Item . )) {
    python C:\Users\Lance\Documents\Powershell\python_scripts\music_tools.py -c calculate_image_size -d $directory.FullName
}

function SACDExtract([System.IO.DirectoryInfo]$directory = $( Get-Item . ), [string]$format = 'all') {
    python C:\Users\Lance\Documents\Powershell\python_scripts\music_conversion.py -f $format $directory
}

Set-Alias -Name rfr -Value RenameFileRed