# Description

-----------------------------

# Fibery Migration Mega-Issue: Scripts → Fibery

## Objective

Migrate all data sinks currently targeting Google Sheets, CSV, and JSON files to write directly to Fibery via REST API.
Eliminate the Google Sheets dependency entirely. Minimise API calls and handle timeouts gracefully.

## Git Snapshot

* **Stash**: `stash@{0}: On main: snapshot: pre-Fibery-migration 20260504`
* **HEAD**: `6f50f91 chore: strip translation/lang keys from playlist JSONs`
* **Repo**: `https://github.com/Bearmancer/Scripts`
* All unstaged modifications and untracked files preserved in stash. Pop with `git stash pop` to restore.

## Scripts Inventory

### Python (`python/toolkit/`)

| File          | Current Sink                                                             | Data Written                          | Migration Target                                         |
| ------------- | ------------------------------------------------------------------------ | ------------------------------------- | -------------------------------------------------------- |
| `lastfm.py`   | Google Sheets (Sheet ID: `1scv0dBa7iGx0hQTqmMwvzceoZlyiRSjswz80FCO1cco`) | timestamp, title, album, artist_name  | `Music/Scrobbles` (schema-compatible, no changes needed) |
| `video.py`    | Local filesystem (MKV/MP4)                                               | Chapter extractions, HandBrake output | No Fibery target — local only                            |
| `pristine.py` | Local filesystem (album downloads)                                       | Album files via Playwright/Edge       | No Fibery target — local only                            |
| `audio.py`    | Local filesystem                                                         | Audio processing                      | No Fibery target — local only                            |
| `cuesheet.py` | Local filesystem                                                         | Cue sheets                            | No Fibery target — local only                            |

### C# (`csharp/src/` via `tools.exe`)

| Command        | Current Sink   | Data Written                    | Migration Target                                             |
| -------------- | -------------- | ------------------------------- | ------------------------------------------------------------ |
| `sync yt`      | Google Sheets  | YouTube playlist video metadata | New `Music/YouTubePlaylist` + `Music/YouTubeVideo` databases |
| `music search` | Console/stdout | MusicBrainz metadata queries    | No persistent sink                                           |

### PowerShell (`powershell/ScriptsToolkit/ScriptsToolkit.psm1`)

| Function       | Alias    | Current Sink                     | Migration Target         |
| -------------- | -------- | -------------------------------- | ------------------------ |
| `Sync-YouTube` | `syncyt` | Google Sheets (via C# tools.exe) | Fibery REST API          |
| `Invoke-Tools` | `tools`  | Delegates to tools.exe           | Unchanged (orchestrator) |

### State / Log Files

| File                             | Format | Purpose              | Migration Target               |
| -------------------------------- | ------ | -------------------- | ------------------------------ |
| `logs/lastfm.jsonl`              | JSONL  | Last.fm sync run log | Append-only; keep as local log |
| `logs/youtube.jsonl`             | JSONL  | YT sync run log      | Append-only; keep as local log |
| `logs/music.jsonl`               | JSONL  | Music operations log | Keep local                     |
| `logs/sheets.jsonl`              | JSONL  | Sheets API call log  | Deprecate post-migration       |
| `state/lastfm/scrobbles.json`    | JSON   | Scrobble sync cursor | Keep as local cursor           |
| `state/lastfm/sync.json`         | JSON   | Last sync metadata   | Keep local                     |
| `state/youtube/sync.json`        | JSON   | YT sync state        | Keep local                     |
| `state/youtube/playlists/*.json` | JSON   | Playlist snapshots   | Migrate to Fibery as canonical |

## Is JSON Still Needed?

**Local state cursors** (last sync timestamp, playlist snapshots): **Yes** — keep as lightweight local state to avoid
fetching from Fibery on every run.

**Log files** (`.jsonl`): **Yes** — keep as append-only structured logs for debugging; do not migrate to Fibery.

**Google Sheets as sync target**: **No** — replace entirely with Fibery REST API calls.

## Fibery API — Timeout & Reliability Strategy

### API Call Pattern

* Use `POST /api/commands` (batch endpoint) — send up to **50 entities per request** to minimise call count
* Each batch: `fibery.entity/batch` with array of `fibery.entity/create` or `fibery.entity/update` commands
* Auth: `Authorization: Token <FIBERY_API_KEY>` header

### Timeout Handling

```python
import requests
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry

session = requests.Session()
retry = Retry(total=3, backoff_factor=2, status_forcelist=[429, 500, 502, 503, 504])
session.mount('https://', HTTPAdapter(max_retries=retry))
response = session.post(url, json=payload, timeout=30)
```

### Deduplication

* `Music/Scrobbles`: Use existing `Music/Unique Key` formula (Artist+Track+Album+Time) — query before insert
* `Music/YouTubeVideo`: Use video ID as unique key field

### Minimise API Calls

1. Batch all creates into groups of 50 per POST
2. Cache last-seen cursor in local JSON (avoid re-querying Fibery for sync state)
3. Use `q/where` with timestamp filter to check only the latest record before deciding sync range
4. One read call per sync run (get latest timestamp), N/50 write calls (batched creates)

## Full C# Source Inventory (Google Sheets Surface)

### Sync Commands (direct Google Sheets dependency)

| File                                                 | Role                           | Migration Action                                  |
| ---------------------------------------------------- | ------------------------------ | ------------------------------------------------- |
| `CLI/Sync/SyncLastFmCommand.cs`                      | CLI entry for lastfm sync      | Replace Google Sheets call with Fibery REST       |
| `CLI/Sync/SyncYouTubeCommand.cs`                     | CLI entry for YT sync          | Replace Google Sheets call with Fibery REST       |
| `CLI/Sync/SyncAllCommand.cs`                         | Runs both syncs                | No change needed — orchestrator only              |
| `CLI/Sync/HistoryCommand.cs`                         | View sync history              | Keep — reads local JSONL                          |
| `Orchestrators/ScrobbleSyncOrchestrator.cs`          | Last.fm sync orchestration     | Replace GoogleSheetsService with FiberyService    |
| `Orchestrators/YouTubePlaylistOrchestrator.cs`       | YT playlist sync orchestration | Replace GoogleSheetsService with FiberyService    |
| `Services/Sync/GoogleSheetsContext.cs`               | Sheets connection context      | **Delete** post-migration                         |
| `Services/Sync/GoogleSheetsService.cs`               | Core Sheets write/read         | **Replace** with FiberyService                    |
| `Services/Sync/SheetFormattingService.cs`            | Column formatting              | **Delete** — not needed for Fibery                |
| `Services/Sync/SheetMetadataService.cs`              | Sheet header/metadata          | **Delete** — not needed for Fibery                |
| `Services/Sync/SheetRowService.cs`                   | Row-level CRUD                 | **Delete** — not needed for Fibery                |
| `Services/Sync/SpreadsheetBootstrapper.cs`           | Sheet bootstrapping            | **Delete** — not needed for Fibery                |
| `Core/Auth/GoogleAuth.cs`                            | OAuth2 for Google APIs         | **Delete** — replace with `FIBERY_API_KEY`        |
| `Core/Auth/Secrets.cs`                               | Secrets management             | Add `FIBERY_API_KEY` retrieval                    |
| `Core/SheetNameHelper.cs`                            | Sheet name utilities           | **Delete** — not needed for Fibery                |
| `Services/Sync/YouTube/YouTubeService.cs`            | YT API + Sheets writer         | Split: keep YT API fetch, replace Sheets write    |
| `Services/Sync/YouTube/YouTubeChangeDetector.cs`     | Delta detection                | Keep — logic is Sheets-agnostic, reuse for Fibery |
| `Services/Sync/YouTube/YouTubeTranslationService.cs` | Translation of video titles    | Keep                                              |
| `Services/Sync/LastFmService.cs`                     | Last.fm API fetch              | Keep fetch logic; remove Sheets write             |

### Non-Sheets C# Files (no migration needed)

* All `Services/Music/` — MusicBrainz/Discogs metadata, no Sheets dependency
* All `Services/Read/` — EPUB/PDF extraction, no Sheets dependency
* All `Services/Mail/` — temp mail, no Sheets dependency
* All `Services/Language/` — translation, no Sheets dependency
* `Services/Cloud/CloudUsageService.cs` — cloud usage, no Sheets dependency
* All `Models/` — POCOs, no change
* All `Tests/` — update mocks for FiberyService post-migration

## CSV Assessment

No CSV source or sink found anywhere in the Scripts codebase. All structured data flows through Google Sheets (Python
`gspread` + C# Google Sheets API) or local JSON/JSONL state files. **CSV migration is not applicable.**

# Plan

-----------------------------

# CPM Plan: Scripts → Fibery Migration

## Critical Path

```
A → B → C → D → E → F → G → H
```

| ID  | Task                                                                                       | Depends On | Owner | Effort |
| --- | ------------------------------------------------------------------------------------------ | ---------- | ----- | ------ |
| A   | Pop stash, verify working tree clean                                                       | —          | Human | 5 min  |
| B   | Add `FIBERY_API_KEY` env var to system/profile                                             | A          | Human | 5 min  |
| C   | Create Music/YouTubePlaylist + Music/YouTubeVideo databases in Fibery                      | B          | Agent | 15 min |
| D   | Write `python/toolkit/fibery_client.py` — batch REST client with retry/timeout             | B          | Agent | 30 min |
| E   | Refactor `lastfm.py`: replace gspread sink with `fibery_client.batch_create_scrobbles()`   | D          | Agent | 30 min |
| F   | Refactor C# `sync yt` command: replace Google Sheets writes with Fibery REST               | D          | Agent | 60 min |
| G   | Update `pyproject.toml`: remove `gspread`, `google-auth`; add `requests` if not present    | E          | Agent | 10 min |
| H   | Integration test: run `toolkit lastfm` dry-run against Fibery sandbox, verify entity count | E,F,G      | Agent | 20 min |
| I   | Remove `logs/sheets.jsonl` from tracking; update `.gitignore`                              | H          | Agent | 5 min  |
| J   | Commit: `feat: migrate lastfm + yt sync from Google Sheets to Fibery`                      | I          | Agent | 5 min  |

## Phase Detail

### Phase 1 — Foundation (A, B, C, D)

* Restore working tree from stash
* Configure auth
* Create new Fibery databases for YouTube data
* Write shared Fibery REST client module

### Phase 2 — lastfm Migration (E, G)

* Replace `authenticate_google_sheets()` + `gspread` calls in `lastfm.py`
* New flow: `get_latest_fibery_scrobble_time()` (1 API read) → `batch_create_scrobbles()` (N/50 API writes)
* Preserve local `state/lastfm/scrobbles.json` as cursor fallback
* Remove `gspread`, `google-auth` from dependencies

### Phase 3 — YouTube Sync Migration (F)

* C# `sync yt` currently writes playlist+video metadata to Google Sheets
* Replace with Fibery batch REST calls to `Music/YouTubePlaylist` + `Music/YouTubeVideo`
* Use video ID as idempotency key (update if exists, create if not)
* Preserve `state/youtube/playlists/*.json` as local cache

### Phase 4 — Cleanup & Verification (H, I, J)

* Run integration tests
* Remove Google Sheets dependency artifacts
* Final commit

## Minimise API Calls — Rules

1. **One read per sync run** — query Fibery for latest timestamp only
2. **Batch 50 entities per POST** — never create one entity at a time
3. **Local cursor JSON** — store last-synced timestamp locally; skip Fibery read if cursor is fresh (<1 hour)
4. **No polling** — event-driven only (cron/scheduled task triggers sync)

## Timeout / Resilience Rules

1. `requests.Session` with `Retry(total=3, backoff_factor=2, status_forcelist=[429,500,502,503,504])`
2. `timeout=30` on all write calls, `timeout=15` on read calls
3. On timeout: log error to JSONL, skip batch, continue next batch (partial success is acceptable)
4. On 401: raise immediately — invalid API key, do not retry

## Extended CPM — C# Migration Detail

| ID  | Task                                                                                                                                                                   | Depends On | Files Touched  |
| --- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------- | -------------- |
| F1  | Write `Services/Sync/FiberyService.cs` — batch REST client (C#)                                                                                                        | D          | new file       |
| F2  | Refactor `ScrobbleSyncOrchestrator.cs` — replace GoogleSheetsService                                                                                                   | F1         | Orchestrators/ |
| F3  | Refactor `YouTubePlaylistOrchestrator.cs` — replace GoogleSheetsService                                                                                                | F1,C       | Orchestrators/ |
| F4  | Delete `GoogleSheetsContext`, `GoogleSheetsService`, `SheetFormattingService`, `SheetMetadataService`, `SheetRowService`, `SpreadsheetBootstrapper`, `SheetNameHelper` | F2,F3      | Services/Sync/ |
| F5  | Delete `Core/Auth/GoogleAuth.cs`; add `FIBERY_API_KEY` to `Core/Auth/Secrets.cs`                                                                                       | F4         | Core/Auth/     |
| F6  | Remove Google Sheets NuGet packages from `CSharpScripts.csproj`                                                                                                        | F5         | csharp/        |
| F7  | Update `Tests/` mocks — replace GoogleSheetsService mock with FiberyService mock                                                                                       | F1         | Tests/         |
| F8  | `dotnet test` — all tests pass                                                                                                                                         | F7         | —              |

## Full CPM Critical Path (combined)

```
A → B → D → E → G → H → I → J
              ↓
              F1 → F2 → F3 → F4 → F5 → F6 → F7 → F8
         C ──────────────────────────↗
```

## CSV Note

No CSV sources or sinks exist anywhere in the Scripts codebase. CSV migration is not applicable.

# Prompt

-----------------------------

# Execution Prompt: Scripts → Fibery Migration

## Pass Criteria

- [ ] `python/toolkit/fibery_client.py` exists with `batch_create_scrobbles()` and `get_latest_fibery_scrobble_time()`
- [ ] `lastfm.py` contains zero references to `gspread` or `google.auth`
- [ ] `pyproject.toml` has `requests` in dependencies, `gspread` removed
- [ ] `Music/YouTubePlaylist` and `Music/YouTubeVideo` databases exist in Fibery
- [ ] `toolkit lastfm` executes without error against Fibery (check `logs/lastfm.jsonl` for success entry)
- [ ] No Google Sheets API calls in any Python toolkit file
- [ ] Commit `feat: migrate lastfm + yt sync from Google Sheets to Fibery` on main

## Current State

* Git HEAD: `6f50f91 chore: strip translation/lang keys from playlist JSONs`
* Snapshot stash: `stash@{0}: On main: snapshot: pre-Fibery-migration 20260504`
* `lastfm.py` writes to Google Sheets via `gspread` (Sheet ID: `1scv0dBa7iGx0hQTqmMwvzceoZlyiRSjswz80FCO1cco`)
* C# `sync yt` writes YouTube playlist metadata to Google Sheets
* `Music/Scrobbles` Fibery database exists and is schema-compatible (no field changes needed)
* `Music/YouTubePlaylist` and `Music/YouTubeVideo` do NOT exist yet
* `FIBERY_API_KEY` env var: NOT yet set

## Steps

1. `git stash pop` — restore working tree
2. Set `FIBERY_API_KEY` in environment (system or `~/.profile`)
3. **Agent**: Create `Music/YouTubePlaylist` database in Fibery with fields: Name, Playlist ID, URL, Last Synced
4. **Agent**: Create `Music/YouTubeVideo` database in Fibery with fields: Title, Video ID, Channel, Published At,
   Duration; relation to Playlist
5. **Agent**: Write `python/toolkit/fibery_client.py`:
	* `_fibery_session()` — requests.Session with retry adapter
	* `get_latest_fibery_scrobble_time() -> datetime | None` — one GET query
	* `batch_create_scrobbles(rows)` — batch POST in groups of 50
	* `batch_upsert_yt_videos(videos)` — batch POST with video ID dedup
6. **Agent**: Refactor `lastfm.py`:
	* Remove `gspread`, `google.auth` imports
	* Replace `authenticate_google_sheets()` with `fibery_client.get_latest_fibery_scrobble_time()`
	* Replace `sheet.insert_rows()` with `fibery_client.batch_create_scrobbles()`
	* Preserve `state/lastfm/scrobbles.json` cursor fallback
7. **Agent**: Refactor C# YouTube sync to POST to Fibery instead of Google Sheets
8. **Agent**: Update `pyproject.toml` — remove `gspread`, confirm `requests` present
9. Run `uv sync` to update lockfile
10. Run `uv run toolkit lastfm` — verify success in `logs/lastfm.jsonl`
11. Query Fibery `Music/Scrobbles` — verify new entities exist
12. `git add -A && git commit -m 'feat: migrate lastfm + yt sync from Google Sheets to Fibery'`

## Fail Criteria

* Any `gspread` import remaining in Python toolkit files
* `toolkit lastfm` raises an exception or logs an ERROR
* Fibery REST returns non-2xx and retry exhausted
* `Music/Scrobbles` entity count unchanged after a run with known new scrobbles
* `pyproject.toml` still lists `gspread` as dependency

## Extended Prompt — C# Migration Detail

### Pass Criteria (C#)

- [ ] `Services/Sync/FiberyService.cs` exists, implements batch REST posts
- [ ] `GoogleSheetsService.cs` and all `Sheet*.cs` helpers are deleted
- [ ] `GoogleAuth.cs` is deleted; `Secrets.cs` reads `FIBERY_API_KEY`
- [ ] Google Sheets NuGet packages removed from `CSharpScripts.csproj`
- [ ] `dotnet test` passes with updated FiberyService mocks
- [ ] `Invoke-Tools sync yt` succeeds, new entities visible in Fibery `Music/YouTubeVideo`

### Steps (C# Execution)

7a. **Agent**: Create `csharp/src/Services/Sync/FiberyService.cs`. Implement `BatchCreateAsync` and
`GetLatestScrobbleTimeAsync`.\
7b. **Agent**: Refactor `ScrobbleSyncOrchestrator.cs` and `YouTubePlaylistOrchestrator.cs` to call `FiberyService`.\
7c. **Agent**: Delete all Google Sheets services (`GoogleSheetsService`, `GoogleSheetsContext`, `SheetRowService`,
etc.)\
7d. **Agent**: Delete `GoogleAuth.cs`. Update `Secrets.cs` to fetch `FIBERY_API_KEY` environment variable.\
7e. **Agent**: Remove `Google.Apis.Sheets.v4` and `Google.Apis.Auth` from `CSharpScripts.csproj`.\
7f. **Agent**: Refactor `Tests/` directory, replacing Google Sheets mocks with FiberyService mocks.\
7g. **Agent**: Run `dotnet test` in `csharp/` directory.\
7h. **Agent**: Run `pwsh -Command "Import-Module ./powershell/ScriptsToolkit/ScriptsToolkit.psd1; tools sync yt"` to
verify end-to-end sync.

# Research

-----------------------------

# Research: Scripts → Fibery Migration

## Fibery REST API

* **Batch endpoint**: `POST https://<account>.fibery.io/api/commands`
* **Auth**: `Authorization: Token <FIBERY_API_KEY>`, `Content-Type: application/json`
* **Create entity**:
  `{"command": "fibery.entity/create", "args": {"type": "Music/Scrobbles", "entity": {"Music/Track Title": "...", ...}}}`
* **Batch**: wrap array of commands in single POST — up to 50 per request recommended
* **Rate limit**: no documented hard limit; use retry with exponential backoff on 429/5xx

## Music/Scrobbles Schema (already exists — no changes needed)

| Fibery Field      | Type                | Maps from lastfm.py           |
| ----------------- | ------------------- | ----------------------------- |
| Music/Track Title | fibery/text         | `title`                       |
| Music/Album       | fibery/text         | `album`                       |
| Music/Artist      | fibery/text         | `artist_name`                 |
| Music/Time        | fibery/date-time    | `timestamp` (unix → ISO 8601) |
| Music/Unique Key  | formula (read-only) | Artist+Track+Album+Time       |

## New Databases Needed

### Music/YouTubePlaylist

* Name (fibery/text)
* Playlist ID (fibery/text) — YouTube playlist ID, unique key
* URL (fibery/text)
* Last Synced (fibery/date-time)

### Music/YouTubeVideo

* Title (fibery/text)
* Video ID (fibery/text) — unique key
* Channel (fibery/text)
* Published At (fibery/date-time)
* Duration (fibery/text)
* Playlist (relation → Music/YouTubePlaylist, many-to-one)

## gspread Replacement Pattern

```python
# OLD (gspread)
sheet.insert_rows(values=sorted_new_data, row=2)

# NEW (Fibery batch REST)
import os, requests
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry

FIBERY_API_KEY = os.environ['FIBERY_API_KEY']
FIBERY_URL = 'https://lancetest.fibery.io/api/commands'

def _fibery_session() -> requests.Session:
    session = requests.Session()
    retry = Retry(total=3, backoff_factor=2, status_forcelist=[429,500,502,503,504])
    session.mount('https://', HTTPAdapter(max_retries=retry))
    session.headers.update({'Authorization': f'Token {FIBERY_API_KEY}', 'Content-Type': 'application/json'})
    return session

def batch_create_scrobbles(rows: list[list[str]]) -> None:
    session = _fibery_session()
    BATCH_SIZE = 50
    for i in range(0, len(rows), BATCH_SIZE):
        chunk = rows[i:i+BATCH_SIZE]
        commands = [{
            'command': 'fibery.entity/create',
            'args': {
                'type': 'Music/Scrobbles',
                'entity': {
                    'Music/Time': row[0],  # ISO 8601
                    'Music/Track Title': row[1],
                    'Music/Album': row[2],
                    'Music/Artist': row[3],
                }
            }
        } for row in chunk]
        resp = session.post(FIBERY_URL, json=commands, timeout=30)
        resp.raise_for_status()
```

## Sync Cursor Strategy (minimise API calls)

```python
# One read per run — get latest scrobble time from Fibery
def get_latest_fibery_scrobble_time() -> datetime | None:
    query = [{'command': 'fibery.entity/query',
               'args': {'query': {
                   'q/from': 'Music/Scrobbles',
                   'q/select': ['Music/Time'],
                   'q/order-by': [[["Music/Time"], "q/desc"]],
                   'q/limit': 1
               }}}]
    resp = _fibery_session().post(FIBERY_URL, json=query, timeout=15)
    results = resp.json()[0].get('result', [])
    if results:
        return datetime.fromisoformat(results[0]['Music/Time'])
    return None
```

## Existing Issue #185 (Fibery Workspace Audit May 2026)

* Already documents Music/Scrobbles schema compatibility
* Confirms no schema changes needed for lastfm sync
* Recommends `FIBERY_API_KEY` env var auth pattern
* Recommends adding Music/YouTubePlaylist and Music/YouTubeWatchHistory databases

## Pre-existing Stash

* `stash@{1}: On main: pre-pull staged changes 20260316-002833` — older snapshot, unrelated
* `stash@{0}: On main: snapshot: pre-Fibery-migration 20260504` — current full state snapshot

# Validation

-----------------------------

# Validation

## Git Snapshot

* ✅ `stash@{0}: On main: snapshot: pre-Fibery-migration 20260504` — confirmed via `git stash list`
* All unstaged modifications (logs, state, playlists) and untracked files (Remove-PlaylistLangKeys.ps1, new playlists,
  backup dir) preserved

## Issue Status

* 🔄 Not yet executed — awaiting Phase 1 (env var + Fibery DB creation)
* Ticked: false

## Linked Issues

* Related: Issue #185 (Fibery Workspace Audit May 2026) — contains prior schema analysis confirming Music/Scrobbles
  compatibility

## C# Validation Checklist

* ✅ All C# Google Sheets service and orchestrator files identified from `stash@{0}` via `csharp/todo_plan.md`
* ✅ C# NuGet dependencies mapped for removal
* ✅ CSV usage confirmed as non-existent across repo
