function WhisperLogic([System.IO.FileInfo]$file, [string]$model, [string]$language)
{
    py "C:\Users\Lance\Documents\Powershell\python_scripts\whisper_ai.py" whisper_logic $file.FullName -m $model -l $language
}

function Whisp([System.IO.FileInfo]$file)
{
    py "C:\Users\Lance\Documents\Powershell\python_scripts\whisper_ai.py" whisp $file.FullName
}

function WhisperPath([System.IO.DirectoryInfo]$directory = $( Get-Item . ))
{
    py "C:\Users\Lance\Documents\Powershell\python_scripts\whisper_ai.py" whisper_path $directory.FullName
}

function WhisperPathRecursive([System.IO.DirectoryInfo]$directory = $( Get-Item . ))
{
    py "C:\Users\Lance\Documents\Powershell\python_scripts\whisper_ai.py" whisper_path_recursive $directory.FullName
}

function WhisperJapanese([System.IO.FileInfo]$file)
{
    py "C:\Users\Lance\Documents\Powershell\python_scripts\whisper_ai.py" whisper_japanese $file.FullName
}

function WhisperPathJapanese([System.IO.DirectoryInfo]$directory = $( Get-Item . ))
{
    py "C:\Users\Lance\Documents\Powershell\python_scripts\whisper_ai.py" whisper_path_japanese $directory.FullName
}

function SRTtoWord([System.IO.FileInfo] $file)
{
    py "C:\Users\Lance\Documents\Powershell\python_scripts\whisper_ai.py" srt_to_word $file.FullName
}

function WordToSRT([System.IO.FileInfo] $file)
{
    py "C:\Users\Lance\Documents\Powershell\python_scripts\whisper_ai.py" word_to_srt $file.FullName
}

Set-Alias -Name wp -Value WhisperPath
Set-Alias -Name wj -Value WhisperJapanese
Set-Alias -Name wpj -Value WhisperPathJapanese
Set-Alias -Name wpr -Value WhisperPathRecursive
Set-Alias -Name rsd -Value RemoveSubtitleDuplication