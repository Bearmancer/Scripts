param (
    [Parameter(Mandatory = $true)]
    [String]$rootDir
)

Set-StrictMode -Version 1

$dvdPath = $rootDir
$nonRemuxable = @()

function Convert-DVDToMKV([System.IO.FileSystemInfo]$file, [System.IO.FileSystemInfo]$dvdFolder) { 
    $outputPath = Join-Path -Path $dvdPath.FullName -ChildPath "Converted"

    New-Item -ItemType Directory $outputPath -Force

    & "C:\Program Files (x86)\MakeMKV\makemkvcon64.exe" mkv file:"$($file.FullName)" all $dvdPath --minlength=180
}

Get-ChildItem $rootDir -Directory | ForEach-Object {
    $dvdPath = $_

    Write-Host "---------------------------------------------`nNow converting $($dvdPath)"

    $remuxable = Get-ChildItem $dvdPath -File -Recurse -Include VIDEO_TS.IFO, index.bdmv

    if ($_.FullName -match "BACKUP") { continue }
    
    foreach ($file in $remuxable) {
        if (Test-Path -Path (Join-Path $file.Directory.FullName 'Converted')) {
            Write-Host "$($file) has already been remuxed."
            Continue
        }
    
        Convert-DVDToMKV $file $dvdPath
    }
    
    if ($remuxable.Length -eq 0) {
        Write-Host "No remuxable files found in $($dvdPath)."
        $nonRemuxable += $dvdPath
    }
}

if ($nonRemuxable.Length -gt 0) {
    Write-Host "`nDVD to MKV remuxing process completed."
    Write-Host "Folders that couldn't be remuxed:"; foreach ($folder in $nonRemuxable) { Write-Host $folder }
}

& "C:\Users\Lance\Documents\PowerShell\Custom\Split Video By Chapters.ps1" .

# -sel:all,+sel:audio*stereo,+sel:audio*mono,+sel:video*(!mvcvideo),=1:video*(!mvcvideo),+sel:subtitle
