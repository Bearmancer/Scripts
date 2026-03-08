# ScriptsToolkit Data File
#region Constants, completers, and shared helpers

$Script:TIME_FORMAT = 'HH:mm:ss'
$Script:DATETIME_FORMAT = 'yyyy/MM/dd HH:mm:ss'


$Script:MediaExtensions = @('.mp4', '.mkv', '.avi', '.mp3', '.flac', '.wav', '.webm', '.m4a', '.opus', '.ogg')

$Script:WhisperLanguages = @(
    'af', 'am', 'ar', 'as', 'az', 'ba', 'be', 'bg', 'bn', 'bo', 'br', 'bs', 'ca', 'cs', 'cy',
    'da', 'de', 'el', 'en', 'es', 'et', 'eu', 'fa', 'fi', 'fo', 'fr', 'gl', 'gu', 'ha', 'haw',
    'he', 'hi', 'hr', 'ht', 'hu', 'hy', 'id', 'is', 'it', 'ja', 'jw', 'ka', 'kk', 'km', 'kn',
    'ko', 'la', 'lb', 'ln', 'lo', 'lt', 'lv', 'mg', 'mi', 'mk', 'ml', 'mn', 'mr', 'ms', 'mt',
    'my', 'ne', 'nl', 'nn', 'no', 'oc', 'pa', 'pl', 'ps', 'pt', 'ro', 'ru', 'sa', 'sd', 'si',
    'sk', 'sl', 'sn', 'so', 'sq', 'sr', 'su', 'sv', 'sw', 'ta', 'te', 'tg', 'th', 'tk', 'tl',
    'tr', 'tt', 'uk', 'ur', 'uz', 'vi', 'yi', 'yo', 'yue', 'zh'
)

$Script:WhisperModels = @(
    'tiny', 'tiny.en', 'base', 'base.en', 'small', 'small.en', 'medium', 'medium.en',
    'large-v1', 'large-v2', 'large-v3', 'large-v3-turbo', 'turbo',
    'distil-large-v2', 'distil-large-v3', 'distil-large-v3.5', 'distil-medium.en', 'distil-small.en'
)
#endregion

function Get-Timestamp { "[$(Get-Date -Format $Script:TIME_FORMAT)]" }

function Assert-NetworkAvailable {
    if (-not [System.Net.NetworkInformation.NetworkInterface]::GetIsNetworkAvailable()) {
        throw 'No network connection available.'
    }
}

function Assert-CommandExists {
    param([string]$Command)
    $null = Get-Command $Command -ErrorAction Stop
}

function Test-SrtExists {
    param([string]$FilePath)
    Test-Path ([System.IO.Path]::ChangeExtension($FilePath, '.srt'))
}

function Get-MediaFiles {
    param([System.IO.DirectoryInfo]$Directory)
    Get-ChildItem $Directory -Recurse -File | Where-Object { $_.Extension.ToLower() -in $Script:MediaExtensions }
}