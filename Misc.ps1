function TranslateFile($file) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\DeepL Translation.py" $file
}

function CallCommandHistory {
    code C:\Users\Lance\AppData\Roaming\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt
}

function PrintVideoResolutions {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Print Video Resolutions.py"
}

function RemuxDVD([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Remux DVDs.py" $directory.FullName
}

function CompressVideoFiles {
    & "C:\Users\Lance\Documents\PowerShell\Custom\Handbrake Batch Compress.ps1" .
}

function SplitVideosByChapter() {
    & "C:\Users\Lance\Documents\PowerShell\Custom\Split Video By Chapters.ps1" .
}

function CallCmdletAllSubFolders([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location)), $command) {
    py C:\Users\Lance\Documents\Powershell\Python Scripts\Misc.py ccas $command
}

function CallCmdletAllFiles([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location)), $command) {
    py C:\Users\Lance\Documents\Powershell\Python Scripts\Misc.py ccaf $command
}



function ListDirectories([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\List Files and Directories.py" list_dir $directory.FullName
}

function ListFilesAndDirectories([System.IO.DirectoryInfo]$directory = $(Get-Item (Get-Location))) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\List Files and Directories.py" list_files_and_dirs $directory.FullName
}

Set-Alias -Name ccas -Value CallCmdletAllSubFolders
Set-Alias -Name ccaf -Value CallCmdletAllFiles
Set-Alias -Name rcas -Value RunCommandAllSubFolders