param (
    [Parameter(Mandatory=$true)]
    [string]$FolderPath
)

$mkvFiles = Get-ChildItem $FolderPath -Recurse -Filter  *.mkv

foreach ($file in $mkvFiles) {
    $inputFilePath = $file.FullName
    $outputFilePath = [System.IO.Path]::ChangeExtension($inputFilePath, ".mp4")
    # HandbrakeCLI --preset-import-file "C:\Users\Lance\AppData\Local\Personal\HandBrakeCLI 1.8.0\Default.json" --preset "New Default" -i $inputFilePath -o $outputFilePath
    
    handbrakecli --preset-import-gui -i $inputFilePath -o $outputFilePath

    if (Test-Path $outputFilePath) {
        # Remove-Item $inputFilePath
    } else {
        Write-Host "Failed to compress $inputFilePath"
    }
}