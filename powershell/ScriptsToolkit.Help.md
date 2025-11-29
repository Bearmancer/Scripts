# ScriptsToolkit PowerShell Module

A unified toolkit for audio/video processing, filesystem operations, YouTube downloads, and scheduled task management.

## Installation

The module is loaded automatically via your PowerShell profile. To manually import:

```powershell
Import-Module "$env:USERPROFILE\Dev\Scripts\powershell\ScriptsToolkit.psd1"
```

## Quick Reference

| Function | Purpose |
|----------|---------|
| `Get-Directories` | List directories with sizes |
| `Get-FilesAndDirectories` | List all items with sizes |
| `New-Torrents` | Create .torrent files |
| `Start-DiscRemux` | Remux video discs |
| `Start-BatchCompression` | Compress videos in batch |
| `Get-VideoChapters` | Extract chapter timestamps |
| `Get-VideoResolution` | Report video resolutions |
| `Convert-Audio` | Convert audio files |
| `Convert-ToMP3` | Convert to MP3 |
| `Convert-ToFLAC` | Convert to FLAC |
| `Convert-SACD` | Extract SACD ISO files |
| `Rename-MusicFiles` | Rename files using RED naming |
| `Get-EmbeddedImageSize` | Report embedded art sizes |
| `Invoke-Propolis` | Run Propolis analyzer |
| `Invoke-Whisper` | Transcribe single file |
| `Invoke-WhisperFolder` | Transcribe folder |
| `Invoke-WhisperJapanese` | Transcribe Japanese audio |
| `Invoke-WhisperJapaneseFolder` | Transcribe Japanese folder |
| `Save-YouTubeVideo` | Download YouTube videos |
| `Register-ScheduledSyncTask` | Create scheduled task |
| `Register-AllSyncTasks` | Register all sync tasks |
| `Open-CommandHistory` | Open PS history file |
| `Invoke-ToolkitAnalyzer` | Run PSScriptAnalyzer |

---

## Filesystem Functions

### Get-Directories

Lists directories with human-readable sizes, sorted by size or name.

```powershell
Get-Directories
Get-Directories -Directory "D:\Music" -SortBy name
```

**Parameters:**
- `-Directory` — Target directory (default: current)
- `-SortBy` — Sort order: `size` (default) or `name`

### Get-FilesAndDirectories

Lists all files and directories with sizes.

```powershell
Get-FilesAndDirectories
Get-FilesAndDirectories -Directory "C:\Projects" -SortBy size
```

**Parameters:**
- `-Directory` — Target directory (default: current)
- `-SortBy` — Sort order: `size` (default) or `name`

### New-Torrents

Creates .torrent files for directories.

```powershell
New-Torrents
New-Torrents -Directory "D:\Uploads" -IncludeSubdirectories
```

**Parameters:**
- `-Directory` — Target directory (default: current)
- `-IncludeSubdirectories` — Process subdirectories recursively

---

## Video Functions

### Start-DiscRemux

Remuxes video disc folders (BDMV/DVD structure) to MKV.

```powershell
Start-DiscRemux
Start-DiscRemux -Directory "D:\Rips\Movie" -SkipMediaInfo
```

**Parameters:**
- `-Directory` — Directory containing disc structure (default: current)
- `-SkipMediaInfo` — Skip MediaInfo verification

### Start-BatchCompression

Compresses all videos in a directory using optimized settings.

```powershell
Start-BatchCompression
Start-BatchCompression -Directory "D:\Videos"
```

**Parameters:**
- `-Directory` — Directory containing videos (default: current)

### Get-VideoChapters

Extracts and displays chapter timestamps from videos.

```powershell
Get-VideoChapters
Get-VideoChapters -Directory "D:\Movies"
```

**Parameters:**
- `-Directory` — Directory to scan (default: current)

### Get-VideoResolution

Reports video resolutions for all video files.

```powershell
Get-VideoResolution
Get-VideoResolution -Directory "D:\Downloads"
```

**Parameters:**
- `-Directory` — Directory to scan (default: current)

---

## Audio Functions

### Convert-Audio

Converts audio files to various formats.

```powershell
Convert-Audio
Convert-Audio -Directory "D:\Music\Album" -Format mp3
Convert-Audio -Format 24-bit
```

**Parameters:**
- `-Directory` — Directory containing audio files (default: current)
- `-Format` — Target format: `24-bit`, `flac`, `mp3`, or `all` (default)

### Convert-ToMP3

Shortcut for MP3 conversion.

```powershell
Convert-ToMP3
Convert-ToMP3 -Directory "D:\FLAC"
```

### Convert-ToFLAC

Shortcut for FLAC conversion.

```powershell
Convert-ToFLAC
Convert-ToFLAC -Directory "D:\WAV"
```

### Convert-SACD

Extracts audio from SACD ISO files.

```powershell
Convert-SACD
Convert-SACD -Directory "D:\SACDs" -Format flac
```

**Parameters:**
- `-Directory` — Directory containing ISO files (default: current)
- `-Format` — Output format: `24-bit`, `flac`, `mp3`, or `all` (default)

### Rename-MusicFiles

Renames music files using RED naming conventions.

```powershell
Rename-MusicFiles
Rename-MusicFiles -Directory "D:\Music\Unsorted"
```

**Parameters:**
- `-Directory` — Directory to process (default: current)

### Get-EmbeddedImageSize

Reports embedded album art sizes in audio files.

```powershell
Get-EmbeddedImageSize
Get-EmbeddedImageSize -Directory "D:\Music"
```

**Parameters:**
- `-Directory` — Directory to scan (default: current)

### Invoke-Propolis

Runs the Propolis audio analyzer.

```powershell
Invoke-Propolis
Invoke-Propolis -Directory "D:\Music\Album"
```

**Parameters:**
- `-Directory` — Directory to analyze (default: current)

---

## Transcription Functions

### Invoke-Whisper

Transcribes a single audio/video file using Whisper.

```powershell
Invoke-Whisper -FilePath "video.mp4"
Invoke-Whisper -FilePath "audio.mp3" -Language ja -Translate
Invoke-Whisper -FilePath "lecture.mkv" -Model large-v3 -OutputDir "D:\Subtitles"
```

**Parameters:**
- `-FilePath` — Path to media file (required)
- `-Language` — Source language code (default: `en`)
- `-Model` — Whisper model (default: `distil-large-v3.5` for English, `medium` otherwise)
- `-Translate` — Translate to English
- `-OutputDir` — Output directory (default: current)

### Invoke-WhisperFolder

Transcribes all media files in a folder.

```powershell
Invoke-WhisperFolder
Invoke-WhisperFolder -Directory "D:\Videos" -Language en
Invoke-WhisperFolder -Directory "D:\Anime" -Language ja -Translate -Force
```

**Parameters:**
- `-Directory` — Directory to process (default: current)
- `-Language` — Source language code (default: `en`)
- `-Model` — Whisper model
- `-Translate` — Translate to English
- `-Force` — Overwrite existing SRT files
- `-OutputDir` — Output directory (default: current)

### Invoke-WhisperJapanese

Shortcut for Japanese transcription.

```powershell
Invoke-WhisperJapanese -FilePath "anime.mkv"
Invoke-WhisperJapanese -FilePath "drama.mp4" -Translate
```

### Invoke-WhisperJapaneseFolder

Shortcut for Japanese folder transcription.

```powershell
Invoke-WhisperJapaneseFolder
Invoke-WhisperJapaneseFolder -Directory "D:\Anime" -Translate -Force
```

---

## YouTube Functions

### Save-YouTubeVideo

Downloads YouTube videos with optional transcription.

```powershell
Save-YouTubeVideo "https://youtube.com/watch?v=abc123"
Save-YouTubeVideo "https://youtube.com/watch?v=abc123" "https://youtube.com/watch?v=def456"
Save-YouTubeVideo "https://youtube.com/watch?v=abc123" -Transcribe -Language ja -Translate
Save-YouTubeVideo "https://youtube.com/watch?v=abc123" -OutputDir "D:\Downloads"
```

**Parameters:**
- `-Urls` — One or more YouTube URLs (required)
- `-Transcribe` — Generate subtitles after download
- `-Language` — Source language for transcription (default: `en`)
- `-Model` — Whisper model for transcription
- `-Translate` — Translate transcription to English
- `-OutputDir` — Download directory (default: current)

---

## Scheduled Task Functions

### Register-ScheduledSyncTask

Creates a scheduled task for running sync commands. **Requires administrator privileges.**

```powershell
Register-ScheduledSyncTask -TaskName "DailyBackup" -Command "backup run" -DailyTime "06:00:00"
Register-ScheduledSyncTask -TaskName "LastFmSync" -Command "sync lastfm" -Description "Sync Last.fm data"
```

**Parameters:**
- `-TaskName` — Task name (required)
- `-Command` — dotnet run command to execute (required)
- `-DailyTime` — Time to run daily as TimeSpan (default: `09:00:00`)
- `-Description` — Task description

**Settings Applied:**
- `StartWhenAvailable` — Runs if missed (e.g., computer was off)
- `RunOnlyIfNetworkAvailable` — Waits for network connection
- `AllowStartIfOnBatteries` — Runs on battery power
- `DontStopIfGoingOnBatteries` — Continues on battery
- `WakeToRun` — Wakes computer from sleep

### Register-AllSyncTasks

Registers all predefined sync tasks. **Requires administrator privileges.**

```powershell
Register-AllSyncTasks
```

Registers:
- `LastFmSync` at 09:00 — Syncs Last.fm scrobbles to Google Sheets
- `YouTubeSync` at 10:00 — Syncs YouTube playlists to Google Sheets

---

## Utility Functions

### Open-CommandHistory

Opens the PowerShell command history file in VS Code.

```powershell
Open-CommandHistory
```

### Invoke-ToolkitAnalyzer

Runs PSScriptAnalyzer on the toolkit code.

```powershell
Invoke-ToolkitAnalyzer
Invoke-ToolkitAnalyzer -Path "C:\Scripts\MyModule.psm1"
```

**Parameters:**
- `-Path` — Path to analyze (default: module directory)

---

## Viewing This Help

To view this documentation:

```powershell
Get-Help ScriptsToolkit
code "$env:USERPROFILE\Dev\Scripts\powershell\ScriptsToolkit.Help.md"
```

To view help for a specific function:

```powershell
Get-Help Convert-Audio -Full
Get-Help Invoke-Whisper -Examples
```
