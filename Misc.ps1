function TranslateFile($file) {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\DeepL Translation.py" $file
}

function CallCommandHistory {
    code C:\Users\Lance\AppData\Roaming\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt
}

function PrintVideoResolutions {
    py "C:\Users\Lance\Documents\Powershell\Python Scripts\Print Video Resolutions.py"
}

function RemuxDVD {
    & "C:\Users\Lance\Documents\PowerShell\Custom\MakeMKV Batch Remux.ps1" .
}

function CompressVideoFiles {
    & "C:\Users\Lance\Documents\PowerShell\Custom\Handbrake Batch Compress.ps1" .
}

function SplitVideosByChapter() {
    & "C:\Users\Lance\Documents\PowerShell\Custom\Split Video By Chapters.ps1" .
}

function DiscordTime([string]$time24h, [string]$dateDM) {
    $dateTime = [datetime]::ParseExact("$dateDM $time24h", "dd/MM HH:mm", $null)
    $unixTime = [math]::Round(($dateTime.ToUniversalTime()).Subtract((Get-Date "1970-01-01")).TotalSeconds)
    $discordTimeFormat = "<t:$unixTime>"

    Set-Clipboard $discordTimeFormat
}

function CallCmdletAllSubFolders($command) {
    foreach ($folder in Get-ChildItem -Directory -Recurse) {
        Start-Process -FilePath pwsh.exe -ArgumentList "-NoExit", "-Command", "$command" -WorkingDirectory $folder
    }
}

function CallCmdletAllFiles($command) {
    $files = Get-ChildItem -Path $directoryPath -File

    foreach ($file in $files) {
        Start-Process -FilePath pwsh.exe -ArgumentList "-NoExit", "-Command &$command '$($file)'"
    }
}

function RunCommandAllSubFolders($command) {
    Get-ChildItem -Directory -Recurse | ForEach-Object { Push-Location $_.FullName; $command; Pop-Location }
}

function ExtractCommentaryAudio() {
    $videoFiles = Get-ChildItem | Where-Object { $_.Extension -in @(".mkv", ".mp4") }

    foreach ($file in $videoFiles) {
        $audioTrackCount = (ffmpeg -i $file.FullName -hide_banner 2>&1 | Select-String "Stream #" -Context 0, 1 | Select-String "Audio" | Measure-Object).Count
        if ($audioTrackCount -gt 1) {
            $outputFile = Join-Path $PWD.Path ("$($file.BaseName) Audio Commentary.flac")
            ffmpeg -i $file.FullName -map 0:a:1 -sample_fmt s16 -acodec flac $outputFile
        }
    }

    Get-ChildItem -Filter *.flac | ForEach-Object { Write-Host $_.Name }
}
 
function ListDirectories {
    param (
        [int]$indent = 0
    )

    $indentation = " " * $indent

    Get-ChildItem -Directory | ForEach-Object {
        Write-Output ("{0} {1}" -f $indentation, $_.Name)
        Push-Location -LiteralPath $_.FullName
        ListDirectories -indent ($indent + 2)
        Pop-Location
    }
}

function ListFilesAndDirectories {
    param (
        [int]$indent = 0
    )

    $indentation = " " * $indent

    Get-ChildItem -Directory | ForEach-Object {
        Write-Output ("{0} {1}" -f $indentation, $_.Name)
        Push-Location -LiteralPath $_.FullName
        ListFilesAndDirectories -indent ($indent + 2)
        Pop-Location
    }

    Get-ChildItem -File | ForEach-Object {
        Write-Output ("{0} {1}" -f $indentation, $_.Name)
    }
}
  
function MeasureScriptTime([scriptblock] $command) {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    & $command
    $stopwatch.Stop()

    Write-Output "Elapsed time: $($stopwatch.Elapsed.TotalMinutes) minutes"
}

Set-Alias -Name ccas -Value CallCmdletAllSubFolders
Set-Alias -Name ccaf -Value CallCmdletAllFiles
Set-Alias -Name rcas -Value RunCommandAllSubFolders