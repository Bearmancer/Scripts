function OpenCommandHistory {
    code C:\Users\Lance\AppData\Roaming\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt
}

function PrintVideoResolutions([System.IO.DirectoryInfo]$directory = $( Get-Item . )) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\video_editing.py PrintVideoResolution $directory.FullName
}

function RemuxDisc([System.IO.DirectoryInfo]$directory = $( Get-Item . ), [switch]$get_media_info) {
    $arg = @("C:\Users\Lance\Documents\Powershell\python_scripts\video_editing.py", "RemuxDisc", $directory.FullName)

    if ($get_media_info) {
        $arg += "--get-mediainfo"
    }

    py $arg
}

function BatchCompression([System.IO.DirectoryInfo]$directory = $( Get-Item . )) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\video_editing.py BatchCompression $directory.FullName
}

function ExtractChapters([System.IO.DirectoryInfo]$directory = $( Get-Item . )) {
    py C:\Users\Lance\Documents\Powershell\python_scripts\video_editing.py ExtractChapters $directory.FullName
}

function ListDirectories([System.IO.DirectoryInfo]$directory = $( Get-Item . ), [string]$sort_order = "0") {
    py C:\Users\Lance\Documents\Powershell\python_scripts\misc.py list_dir $directory.FullName $sort_order
}

function ListFilesAndDirectories([System.IO.DirectoryInfo]$directory = $( Get-Item . ), [string]$sort_order = "0") {
    py C:\Users\Lance\Documents\Powershell\python_scripts\misc.py list_files_and_dirs $directory.FullName $sort_order
}

function MakeTorrents([System.IO.DirectoryInfo]$directory = $( Get-Item . ), [switch]$recursive) {
    $baseArgs = @("make_torrents", $directory.FullName)

    if ($recursive) {
        $baseArgs += "--recursive"
    }

    py C:\Users\Lance\Documents\Powershell\python_scripts\misc.py $baseArgs
}