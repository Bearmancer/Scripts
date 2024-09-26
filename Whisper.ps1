function WhisperLogic([System.IO.FileInfo]$file, [string]$model, [string]$language) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Whisper.py" "WhisperLogic" $file.FullName $model $language 
}

function Whisp([System.IO.FileInfo]$file) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Whisper.py" "Whisp" $file.FullName
}

function WhisperPath([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Whisper.py" "WhisperPath" $folder.FullName
}

function WhisperPathRecursive([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Whisper.py" "WhisperPathRecursive" $folder.FullName
}

function WhisperJapanese ([System.IO.FileInfo] $file) {
        py "C:\Users\Lance\Documents\Powershell\Python Scripts\Whisper.py" "WhisperJapanese" $file.FullName
}

function WhisperJapanese ([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Whisper.py" "whisperPathJapanese" $folder.FullName
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

Set-Alias -Name wp -Value whisperPath
Set-Alias -Name wj -Value whisperJapanese
Set-Alias -Name wpj -Value whisperPathJapanese
Set-Alias -Name wpf -Value whisperJapaneseFile
Set-Alias -Name wpr -Value whisperPathRecursive
Set-Alias -Name rsd -Value RemoveSubtitleDuplication