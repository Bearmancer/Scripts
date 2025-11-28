function Open-CommandHistory {
    code "$env:APPDATA\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt"
}

function Get-VideoResolution {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .)
    )

    py "$global:ScriptRoot\python\video_editing.py" PrintVideoResolution $Directory.FullName
}

function Start-DiscRemux {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .),

        [switch]$IncludeMediaInfo
    )

    $pyArgs = @("$global:ScriptRoot\python\video_editing.py", 'RemuxDisc', $Directory.FullName)
    if ($IncludeMediaInfo) { $pyArgs += '--get-mediainfo' }

    py @pyArgs
}

function Start-BatchCompression {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .)
    )

    py "$global:ScriptRoot\python\video_editing.py" BatchCompression $Directory.FullName
}

function Get-VideoChapters {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .)
    )

    py "$global:ScriptRoot\python\video_editing.py" ExtractChapters $Directory.FullName
}

function Get-Directories {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .),

        [string]$SortBy = 'name'
    )

    $sortMap = @{ name = '0'; size = '1'; date = '2' }
    py "$global:ScriptRoot\python\misc.py" list_dir $Directory.FullName $sortMap[$SortBy]
}

function Get-FilesAndDirectories {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .),

        [string]$SortBy = 'name'
    )

    $sortMap = @{ name = '0'; size = '1'; date = '2' }
    py "$global:ScriptRoot\python\misc.py" list_files_and_dirs $Directory.FullName $sortMap[$SortBy]
}

function New-Torrents {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [System.IO.DirectoryInfo]$Directory = (Get-Item .),

        [switch]$Recursive
    )

    $pyArgs = @('make_torrents', $Directory.FullName)
    if ($Recursive) { $pyArgs += '--recursive' }

    py "$global:ScriptRoot\python\misc.py" @pyArgs
}

Set-Alias -Name history -Value Open-CommandHistory
Set-Alias -Name vidres -Value Get-VideoResolution
Set-Alias -Name remux -Value Start-DiscRemux
Set-Alias -Name compress -Value Start-BatchCompression
Set-Alias -Name chapters -Value Get-VideoChapters
Set-Alias -Name lsd -Value Get-Directories
Set-Alias -Name lsf -Value Get-FilesAndDirectories
Set-Alias -Name torrent -Value New-Torrents
