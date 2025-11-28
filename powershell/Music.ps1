function Invoke-Propolis {
    & "$env:LOCALAPPDATA\Personal\Propolis\propolis_windows.exe" --no-specs .
}

function Convert-Audio {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .),
        [string]$Format = 'all'
    )

    py "$global:ScriptRoot\python\music_conversion.py" -m convert -d $Directory.FullName -f $Format
}

function Convert-ToMP3 {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .)
    )

    Convert-Audio -Directory $Directory -Format mp3
}

function Convert-ToFLAC {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .)
    )

    Convert-Audio -Directory $Directory -Format flac
}

function Convert-SACD {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .),

        [string]$Format = 'all'
    )

    py "$global:ScriptRoot\python\music_conversion.py" -m extract -d $Directory.FullName -f $Format
}

function Rename-MusicFiles {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .)
    )

    py "$global:ScriptRoot\python\music_tools.py" -c rfr -d $Directory.FullName
}

function Get-EmbeddedImageSize {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .)
    )

    py "$global:ScriptRoot\python\music_tools.py" -c calculate_image_size -d $Directory.FullName
}

Set-Alias -Name propolis -Value Invoke-Propolis
Set-Alias -Name music -Value Convert-Audio
Set-Alias -Name tomp3 -Value Convert-ToMP3
Set-Alias -Name toflac -Value Convert-ToFLAC
Set-Alias -Name sacd -Value Convert-SACD
Set-Alias -Name renmusic -Value Rename-MusicFiles
Set-Alias -Name imgsize -Value Get-EmbeddedImageSize
