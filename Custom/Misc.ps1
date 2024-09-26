function CallCmdletAllSubFolders($command) {
    foreach ($folder in Get-ChildItem -Directory -Recurse) {
        Start-Process -FilePath pwsh.exe -ArgumentList "-NoExit", "-Command", "$command" -WorkingDirectory $folder
    }
}

function CallCmdletAllFiles($command) {
    $files = Get-ChildItem $directoryPath -File

    foreach ($file in $files) {
        Start-Process -FilePath pwsh.exe -ArgumentList "-NoExit", "-Command &$command '$($file)'"
    }
}

function RunCommandAllSubFolders($command) {
    Get-ChildItem -Directory -Recurse | ForEach-Object { Push-Location $_.FullName; $command; Pop-Location }
}