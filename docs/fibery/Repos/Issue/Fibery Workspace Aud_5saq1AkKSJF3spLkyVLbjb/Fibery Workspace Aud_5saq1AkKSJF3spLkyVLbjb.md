# Description

-----------------------------

# Fibery Workspace Audit — May 2026

## Duplicates Cleaned

* **Sonarr qBittorrent 403**: 3 copies → deleted 2 (TEST + null-ticked)
* **Parsec -6023/-11002**: 3 copies → deleted 2

## Issues with null Ticked (schema gap)

These issues have `Ticked=null` (not `false`) — likely created before the field existed:

* Windows Update Failure - Error 80246007
* Task 3: Subagent Delegation (Audit, Discovery, Integration)
* Task 1: Forensics & Foundation
* Task 2: Hook Refactoring & Cleanup
* Phase 3: Subagent CPM Delegation & Sync
* OCI Media Server - Container Configuration Fix

## Fibery Concurrent To-Do Assessment

Ticking an issue does **not** hide it from views by default. Fibery is designed for project/issue tracking, not a
disappearing task list. To simulate that behavior: create a filtered **Active Issues** grid view with `Ticked = false`.

## YT + Last.fm Sync Assessment

* `Music/Scrobbles` space exists with: Artist, Track Title, Album, Time, Unique Key (formula).
* **Last.fm sync**: feasible via periodic script writing to `Music/Scrobbles` using the Last.fm API.
* **YouTube sync**: no YT space exists. Recommend adding `Music/YouTubePlaylist` and `Music/YouTubeWatchHistory`
  databases.
* Native Fibery automations can trigger syncs via webhook if a middleware endpoint is set up.

## VS Code Webview Crash Fix

* **Root cause**: Race condition — webview HTML is set before the iframe is fully mounted, causing Service Worker
  `InvalidStateError`.
* **Fix**: Check `webview.visible` before calling `webview.html = ...`; debounce rapid updates.
* **Freeze on resume**: Cline should implement a watchdog/heartbeat to detect frozen states and prompt user to reload
  panel.
* **Kilo crash**: Same root cause — `kilo` uses VS Code webview API and hits the same race condition on panel restore.

## Local File vs Fibery Comparison

* `.cline/data/tasks/*/focus_chain_*.md` — stray agent artifacts, cleaned.
* `.copilot/session-state/*/checkpoints/*.md` — Copilot planner artifacts, cleaned.
* `.copilot/ide/*.lock` — zero-byte dead lock files, cleaned.

## Stray Artifact Deletion Command

```powershell
pwsh -Command "Remove-Item -Path 'C:\Users\Lance\.cline\data\tasks\*\focus_chain_*.md', 'C:\Users\Lance\.copilot\session-state\*\checkpoints\*.md', 'C:\Users\Lance\.copilot\ide\*.lock' -Force -ErrorAction SilentlyContinue"
```

## Last.fm Sync Schema Assessment (from \~/Dev/Scripts/python/toolkit/lastfm.py)

**Current implementation**: Python script syncing to Google Sheets (Sheet ID:
`1scv0dBa7iGx0hQTqmMwvzceoZlyiRSjswz80FCO1cco`)

**Data schema** (per scrobble):

| Script Field | Fibery Field      | Type                             |
|--------------|-------------------|----------------------------------|
| timestamp    | Music/Time        | fibery/date-time                 |
| title        | Music/Track Title | fibery/text                      |
| album        | Music/Album       | fibery/text                      |
| artist_name  | Music/Artist      | fibery/text                      |
| (derived)    | Music/Unique Key  | formula: Artist+Track+Album+Time |

**Fibery Native Sync Assessment**: The `Music/Scrobbles` space is **already schema-compatible**. To migrate from Google
Sheets → Fibery natively:

1. Replace `gspread` writes with Fibery REST API calls (`POST /api/commands` with `fibery.entity/create`)
2. Use `Music/Unique Key` formula field for deduplication (already exists as formula)
3. Auth: Use `FIBERY_API_KEY` env var instead of OAuth2 flow
4. **No schema changes needed** — existing fields cover all data.

**YouTube**: No YT scripts found in \~/Dev/Scripts. Would need new `Music/YouTubePlaylist` and
`Music/YouTubeWatchHistory` databases if desired.

## Knowledge Space Audit

18 guides found, **no duplicates**. Parsec guides (3) are distinct: Diagnostics, Topology Analysis, Performance Data.

# Plan

-----------------------------

# Prompt

-----------------------------

# Research

-----------------------------

# Validation

-----------------------------

