function WhisperLogic([System.IO.FileInfo]$file, [string]$model, [string]$language) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Whisper.py" "WhisperLogic" $file.FullName $model $language 
}

function Whisp([System.IO.FileInfo]$file) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Whisper.py" "Whisp" $file.FullName
}

function WhisperPath([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Whisper.py" "WhisperPath" $directory.FullName
}

function WhisperPathRecursive([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Whisper.py" "WhisperPathRecursive" $directory.FullName
}

function WhisperJapanese ([System.IO.FileInfo] $file) {
        py "C:\Users\Lance\Documents\Powershell\Python Scripts\Whisper.py" "WhisperJapanese" $file.FullName
}

function WhisperPathJapanese ([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Whisper.py" "WhisperPathJapanese" $directory.FullName
}

function WhisperJapaneseFile([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Whisper.py" "WhisperJapaneseFile"
}

function SRTtoWord([System.IO.FileInfo] $file) {
    py 'C:\Users\Lance\Documents\Powershell\Python Scripts\Word and SRT Conversions.py' $file.FullName srt
}

function WordToSRT([System.IO.FileInfo] $file) {
    py 'C:\Users\Lance\Documents\Powershell\Python Scripts\Word and SRT Conversions.py' $file.FullName docx
}

Set-Alias -Name wp -Value WhisperPath
Set-Alias -Name wj -Value WhisperJapanese
Set-Alias -Name wpj -Value WhisperPathJapanese
Set-Alias -Name wpf -Value WhisperJapaneseFile
Set-Alias -Name wpr -Value WhisperPathRecursive
Set-Alias -Name rsd -Value RemoveSubtitleDuplication