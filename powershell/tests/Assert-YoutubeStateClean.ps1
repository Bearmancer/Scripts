$root = Resolve-Path "$PSScriptRoot/../.."
$youtubeState = "$root/state/youtube"
$playlists = "$youtubeState/playlists"
$deleted   = "$youtubeState/deleted"
$syncJson  = "$youtubeState/sync.json"

$failed = $false

if (Test-Path $playlists) {
    $remaining = Get-ChildItem "$playlists/*.json" -ErrorAction SilentlyContinue
    if ($remaining) {
        Write-Error "FAIL: $($remaining.Count) playlist JSON files remain in state/youtube/playlists/"
        $remaining | ForEach-Object { Write-Error "  - $($_.Name)" }
        $failed = $true
    }
}
else {
    Write-Warning "playlists dir missing — expected after purge but not required"
}

if (Test-Path $deleted) {
    $remaining = Get-ChildItem "$deleted/*.json" -ErrorAction SilentlyContinue
    if ($remaining) {
        Write-Error "FAIL: $($remaining.Count) deleted JSON files remain in state/youtube/deleted/"
        $remaining | ForEach-Object { Write-Error "  - $($_.Name)" }
        $failed = $true
    }
}

if (Test-Path $syncJson) {
    $sync = Get-Content $syncJson -Raw | ConvertFrom-Json
    if ($sync.PSObject.Properties.Name -contains "PlaylistSnapshots") {
        $count = @($sync.PlaylistSnapshots.PSObject.Properties).Count
        Write-Error "FAIL: sync.json still contains $count PlaylistSnapshots entries — not cursor-only"
        $failed = $true
    }
}

if (Test-Path $youtubeState) {
    $anyJson = Get-ChildItem "$youtubeState/*.json" -ErrorAction SilentlyContinue | Where-Object { $_.Name -ne "sync.json" }
    $wildJson = Get-ChildItem "$youtubeState/**/*.json" -ErrorAction SilentlyContinue
    $total = @($anyJson) + @($wildJson)
    if ($total) {
        Write-Error "FAIL: $($total.Count) stale JSON files found under state/youtube/ (excluding top-level sync.json)"
        $total | ForEach-Object { Write-Error "  - $($_.FullName)" }
        $failed = $true
    }
}

if (-not $failed) {
    Write-Host "PASS: YouTube state is clean — no stale cache files remain" -ForegroundColor Green
    exit 0
}

exit 1
