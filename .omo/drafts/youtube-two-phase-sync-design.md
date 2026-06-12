# YouTube Two-Phase Sync Architecture Design

## 1. Problem Statement
The previous architecture mandated that YouTube API responses go directly into PostgreSQL (`youtube.videos.Metadata` JSONB column) without any intermediate disk buffer. However, the project's requirement to perform regular "fresh installs" of the database means the schema (and the stored `Metadata`) is frequently wiped. Rebuilding the database required making redundant network calls to the YouTube API, risking rate limiting and slowing down development.

## 2. Proposed Solution: Two-Phase Sync
We are introducing a decoupled, two-phase synchronization process for YouTube (and eventually other external APIs).

### Phase 1: Fetch & Cache (Network Bound)
- **Action**: Fetch data from the YouTube API.
- **Storage**: Save the raw, unmodified JSON responses directly to a local disk buffer (`.omo/yt-cache/`).
- **Resiliency**: If a network failure occurs, the fetch phase can resume seamlessly without hitting the DB.
- **Quota Protection**: The API is only hit when actual new data is needed, shielding our YouTube API quota from local DB wipe/rebuild cycles.

### Phase 2: DB Ingestion (Disk Bound)
- **Action**: Read the raw JSON payload from the `.omo/yt-cache/` buffer.
- **Processing**: Map structured fields (Title, Url, ChannelName) to strongly-typed EF Core entities.
- **Storage**: Insert the mapped entity along with the complete JSON payload into the `youtube.videos` table in PostgreSQL.
- **Speed**: DB wipes only require a fast local disk read to reconstruct the database state.

## 3. Implementation Details
- **Cache Location**: `C:\Users\Lance\Dev\Scripts\.omo\yt-cache\` (should be `.gitignore`'d).
- **Service Changes**: `YouTubeService` will be refactored into two logical components or methods: `FetchToDiskAsync` and `IngestToDatabaseAsync`.
- **Database Schema**: The `youtube.videos` EF Core schema remains completely unchanged. It will continue to store the `Metadata` as `jsonb` for queryability, but the source of truth for raw data moves to the disk cache.

## 4. Success Metrics
- Rebuilding the database from scratch requires **0 YouTube API network calls**.
- Database ingestion time is drastically reduced.
- Reduced risk of hitting API rate limits during heavy schema evolution cycles.

## 5. Current Assessment Request
- The user wants an architecture assessment of the current YT pipeline end-to-end, from source acquisition through local buffering, GitHub interactions, and PostgreSQL ingestion.
- The highest-priority decision areas are long-term API performance, minimizing YouTube/Google API calls, data retention policy, and data safety across Google, local state, GitHub repo storage, and PostgreSQL integration.
- We need to identify which parts of the pipeline are authoritative source of truth, which are cache/materialization layers, and which should be durable versus disposable.

## 6. Open Questions to Resolve
- What is the desired source of truth for fetched YT data: Google API, local disk cache, GitHub repo artifacts, or PostgreSQL?
- How long should raw API payloads and derived entities be retained locally?
- Which data, if any, is acceptable to commit or mirror into GitHub?
- What recovery guarantees are expected after DB wipes, local machine loss, or GitHub repo rebuilds?
- What Google API quota / latency / freshness trade-offs are acceptable?
- Should the pipeline prefer freshness, quota conservation, or reproducibility when those goals conflict?

## 7. Research Findings
- YouTube Data API quota is expensive enough that the pipeline should minimize calls via `part`, `fields`, ETags, gzip, and incremental fetches rather than brute-force refreshes.
- Local caching should be treated as a bounded staging layer with TTL / refresh / purge behavior, not a permanent archive.
- PostgreSQL should likely store typed hot fields plus `jsonb` for the full payload, with targeted indexes on queried paths.
- The current repo already has a `youtube.videos.Metadata` JSONB pattern and a `SyncedAt` field in the authoritative schema doc, which supports incremental sync semantics rather than full re-syncs.
- GitHub should remain code/config only unless a deliberately sanitized fixture or schema artifact is needed.

## 8. Confirmed Architecture Decisions
- **Authority hierarchy**: Google is the external source; PostgreSQL is the durable operational source; JSON cache is the first gate before PGSQL ingestion.
- **Sync strategy**: Use ETag / conditional fetch and incremental playlist sync rather than full re-syncs.
- **Cache policy**: Playlist caches persist so the pipeline can avoid re-calling the whole API; JSON is the first gate and PGSQL is the second gate.
- **GitHub policy**: Code/config only.

## 9. Current Codebase Findings
- The active cache/state root is `state/` under the project root, not `.omo/`.
- YouTube sync state lives at `state/youtube/sync.json`.
- Playlist caches live under `state/youtube/playlists/*.json`.
- Deleted playlists are archived under `state/youtube/deleted/*.json`.
- Cache/state writes are file-based JSON with atomic temp-file replacement, and the code can migrate older cache layouts into the current directory structure.
- The UI/status code treats `state/youtube/sync.json` as the signal for whether YouTube sync is cached and whether it completed.

## 10. Current Interpretation of the Cache Pipeline
- Raw playlist JSON should remain immutable once written; it is the upstream snapshot that all downstream work compares against.
- Playlist filename can stay human-readable for Explorer, but playlist ID must remain the stable identity used for change detection.
- Rename is a presentation-layer concern, not a content identity change.
- Translation should be treated as a derived layer, not as a mutation of the raw cache, because mixing translation output into raw payloads makes it harder to tell whether the upstream data changed or only the derived view changed.
- PostgreSQL should be treated as a materialized projection of the raw cache; if it diverges, the default repair path should be rebuild from the raw JSON cache, unless the cache itself is corrupt.

## 11. Derived Layer Clarification
- A derived layer is data computed *from* the raw API payload rather than being the raw payload itself.
- Examples in this pipeline could include:
  - translated titles/descriptions
  - normalized search strings
  - change summaries
  - display-friendly metadata
  - Postgres rows populated from raw JSON
- The derived layer depends on the raw payload because if the raw payload changes, the derived output may need to be recomputed.
- Playlist ID is relevant because it gives the derived output a stable anchor even when the playlist title changes. If derived data is keyed only by title, a rename looks like a new object; if it is keyed by playlist ID, the system can say "same playlist, different title" and preserve continuity.

## 12. Observed Implementation Note
- The current YouTube sync code appears to write translated `YouTubeVideo` objects back through `SavePlaylistCache`, so the on-disk playlist cache is currently a hybrid of raw upstream data plus derived translation fields.
- That means the present implementation is closer to "raw base + derived overlay stored together" than to a perfectly raw-only cache.
- If the intended policy is strict raw-only cache, translation persistence needs to move out of the playlist JSON file and into a separate derived store or be recomputed on demand.

## 13. Confirmed User Preferences
- **Local file name**: use playlist name for Explorer readability.
- **Identity / change tracking**: keep playlist ID as the stable key so rename does not force a full re-fetch.
- **Sorting direction**: automatic playlist sorting is a later-stage concern; the long-term approach may use LIS, but it should not dominate the current architecture plan.
- **Scope filter**: only user playlists should be included.
- **Cache authority**: local cache is the truth point.
- **Translation impact**: translation should not cause a full playlist recomputation if only the derived layer changes.
- **QA**: use Momus aggressively to nitpick the plan from every angle.

## 14. Best-Practice Brainstorm
- Keep raw playlist snapshots immutable and separate from derived translation output.
- Use playlist ID as the internal machine identity even if the filename remains human-readable by playlist name.
- Prefer a lightweight manifest or index only if title-based filenames need stable lookup acceleration.
- Treat `state/` as durable local truth with explicit cleanup rules, not as throwaway temp data.
- Keep PostgreSQL as a rebuildable projection of local truth, not the only durable copy.
- Restrict scope to user-owned playlists only to avoid noise and quota waste.
- Use change detection based on ID + ETag + sequence rather than full playlist replay.

## 15. Directory Structure Best Practices
- Organize by concern first, then by service, then by data kind.
- Keep raw snapshots, derived outputs, deleted archives, and orchestration state in separate sibling directories.
- Keep filenames human-readable if Explorer use matters, but add stable identity inside the file payload and/or a manifest.
- Avoid mixing raw and derived data in the same directory unless the filename suffix makes the distinction explicit.
- If a file is primarily used for recovery/rebuild, prefer clear placement over deep nesting.
- If a file is only used for computed output, isolate it so it can be regenerated or purged independently.

## 16. Explicit Manifest Best Practice
- Use an explicit manifest when human-readable filenames and stable machine identity both matter.
- The manifest should be the authoritative index that maps playlist name ↔ playlist ID ↔ raw cache file ↔ derived cache file ↔ deleted archive file.
- The manifest reduces reliance on directory scanning and makes rename handling explicit instead of inferred.
- The manifest should not replace the raw snapshot; it should only point to it.
- If the manifest and a cache file disagree, treat the manifest as the routing/index layer and the raw payload as the content truth, then repair the inconsistency.

## 17. Example Manifest
```json
{
  "version": 1,
  "service": "youtube",
  "lastUpdated": "2026-06-12T00:00:00Z",
  "playlists": [
    {
      "playlistId": "PL1234567890ABCDE",
      "name": "My Favorites",
      "status": "active",
      "etag": "\"abc123etag\"",
      "rawPath": "state/youtube/playlists/My Favorites.json",
      "derivedPath": "state/youtube/derived/translations/My Favorites.json",
      "deletedPath": null,
      "contentHash": "sha256:7c3d0a1f...",
      "lastSyncedAt": "2026-06-12T00:00:00Z",
      "lastDerivedAt": "2026-06-12T00:05:00Z"
    },
    {
      "playlistId": "PL9999999999ZZZZZ",
      "name": "Old Playlist",
      "status": "deleted",
      "etag": "\"oldetag\"",
      "rawPath": null,
      "derivedPath": null,
      "deletedPath": "state/youtube/deleted/Old Playlist.json",
      "contentHash": "sha256:1a2b3c4d...",
      "lastSyncedAt": "2026-05-01T00:00:00Z",
      "lastDerivedAt": null
    }
  ]
}
```

## 18. Manifest Use Cases for Sync and PostgreSQL
- During sync, the manifest is the lookup table that tells the orchestrator which playlists exist, which are deleted, and which raw/derived files correspond to each stable playlist ID.
- During PostgreSQL ingestion, the manifest is the replay plan that tells the ingester which raw snapshots to materialize, which derived files are available, and which playlists should be skipped because they are deleted or stale.
- The manifest is especially useful when PGSQL is wiped or rebuilt, because it prevents folder scanning from becoming the source of orchestration truth.

## 19. Manifest Optionality
- If the user prefers to avoid an explicit manifest, the system can instead rely on the raw playlist files plus `sync.json` and stable `playlistId` fields embedded in each file.
- The tradeoff is that rename handling, replay planning, and PGSQL rebuilds become more dependent on scanning the directory structure and reading each file.
- In that design, the raw file remains the truth point and the sync state file acts as the control record; the manifest is simply omitted as an explicit index layer.

## 20. Manifest Lookup During Sync
- The manifest helps sync by letting the orchestrator answer "what file belongs to this playlist ID?" without scanning every playlist file.
- It also answers "has this playlist been renamed?" by keeping a stable ID entry even when the display name changes.
- It answers "is this playlist active or deleted?" without inferring status from folder names alone.
- It answers "what should be replayed to PostgreSQL?" by listing the raw snapshot path and any derived cache path for each playlist.
- In short, the manifest is a direct lookup table that turns sync from directory discovery into indexed routing.

## 21. Manifest Update Flow
- The manifest is updated by the sync pipeline itself, not by hand.
- Typical update sequence:
  1. Load existing manifest and `sync.json`.
  2. Fetch YouTube summaries / IDs / ETags.
  3. Compare against manifest entries by `playlistId`.
  4. If a playlist is new, add a manifest entry and write the new raw file.
  5. If a playlist title changed, update the manifest name and rename the human-readable file path.
  6. If a playlist was deleted, mark it deleted and move its file into the deleted archive path.
  7. If derived translation changed, update only the derived path / derived metadata.
  8. Save the manifest atomically after the file operation completes.
- In short, the manifest is a sidecar index that is kept in sync whenever the raw/derived/deleted files change.

## 22. Rename Example Walkthrough
### Before rename
- Playlist ID: `PL123`
- Name: `My Favorites`
- Raw file: `state/youtube/playlists/My Favorites.json`
- Derived file: `state/youtube/derived/translations/My Favorites.json`
- Manifest entry points to both files and marks the playlist as `active`.

### Rename event from Google
- Google returns the same `playlistId` but a new title: `My Top Picks`.

### Sync behavior
1. Sync loads the manifest and sees `PL123` already exists.
2. Sync detects the same playlist ID, so it knows this is not a new playlist.
3. Sync updates the manifest name from `My Favorites` to `My Top Picks`.
4. Sync renames the raw file to `state/youtube/playlists/My Top Picks.json`.
5. Sync renames the derived file to `state/youtube/derived/translations/My Top Picks.json`.
6. Sync keeps the same playlist ID and preserves all identity/history fields.
7. PostgreSQL ingest treats this as a rename/update, not a new playlist insert.

### Why this is important
- Filename remains human-readable.
- Stable identity remains machine-safe.
- Rename does not force a full re-fetch.
- Derived data stays associated with the same playlist ID.

## 23. Delegation Request
- The user wants the final architecture creation pipeline delegated to a subagent.
- The requested style is exhaustive TDD, with every step backed by a failing command and a passing command as gating logic.
- The architecture work should be treated as a stage-gated pipeline rather than a monolithic write-up.

## 24. Concern Buckets for Refinement
- **Google / YouTube API**: quota minimization, incremental sync, ETag use, user-playlist scope, failure/retry posture.
- **JSON caching**: raw snapshot persistence, directory layout, manifest/indexing, delete/archive policy, rename behavior.
- **Translation layer / storage**: whether translations are separate derived files, how they key off playlist ID, how they avoid raw-cache mutation.
- **JSON schema / storage**: exact shape of raw cache files, manifest fields, per-playlist metadata, stable identity handling.
- **JSON ↔ translation output**: how translated JSON is produced from raw JSON without causing false positives or whole-playlist recomputation.
- **PGSQL ↔ JSON translation layer**: how PGSQL materialization consumes raw and derived data, how rename/replay/rebuild works.
- **Online storage**: what lives in PostgreSQL vs local state, and whether any other online storage exists.
- **PGSQL rolling backup**: what backup cadence/versioning is needed to preserve rebuildability and recovery without re-calling Google.

## 25. Reset Scope Confirmation
- The user clarified that the current cache should be destroyed and the pipeline should restart from the beginning using all playlist data.
- This means the architecture should be planned as a full rebuild/reset path, not as an incremental migration from existing cache files.
- The question to resolve is whether any pre-reset backup of current state should be retained before destruction, or whether the existing cache can be discarded entirely.
- The user further clarified that the current state should be purged, so the plan should assume a clean start with no dependency on prior cache contents.

## 26. Clarification Needed for Final Defaults
- Several subagent refinement questions need plain-language translation before a default can be chosen.
- The next response should explain each unclear concern in simple terms and then ask whether to proceed with recommended defaults for the final architecture plan.

## 27. Plain-Language Clarifications from User
- The user wants to understand how PostgreSQL would store deleted playlists.
- The user does not understand the schema authority question and needs it translated into plain terms.
- The user wants to know whether ETag can centralize change detection for translation as well as PGSQL.
- The user wants collision handling for playlists with identical names.
- The user wants to know where sync history / daily runs are stored.
- The user confirmed raw cache should store raw API JSON only, not mutated data.
- The user is asking what "rebuild" means in this context, specifically whether PGSQL is being rebuilt from JSON and translations.

## 28. User Answers / Constraints to Lock In
- PostgreSQL should contain all data, including deleted/history information, though the exact representation still needs to be explained clearly.
- Translation is provided by an external service; the user wants a simple model for that.
- The user wants a clear way to track translation/field change state.
- The user wants the ID formatting explained more cleanly and wants an alternative naming schema proposed.
- The user wants explicit explanation of whether PostgreSQL has native history/run tracking for operations.
- The user wants the meaning of an optional derived translation cache explained.
- The user says if translation runs, everything gets translated.
- The user wants the remaining options asked more clearly, with a final schema showing all steps, models, services, layers, and flow.

## 29. New Constraints from Latest Reply
- Translation cache should be a separate file for all files, not a mixed inline payload.
- The user wants PostgreSQL vs JSON separation of concern to be explicit and not muddy.
- PostgreSQL history store / incremental backup behavior needs research.
- Filename should use playlist ID plus name in a cleaner format.
- Deleted videos and deleted playlists should have separate history tables.
- Translation API cost should not be wasted by rerunning everything unnecessarily.
- The final architecture must be unambiguous and explicitly separate PGSQL from JSON responsibilities.

## 30. Final Architecture Synthesis
- **Raw capture layer**: immutable raw YouTube JSON per playlist; human-readable filename plus stable playlist ID.
- **Translation layer**: separate per-playlist translation file for the full playlist; only regenerate when raw source changes or translator version changes.
- **Manifest/index layer**: explicit mapping from playlist ID to raw file, translation file, and deletion/archive path.
- **Sync/run layer**: local `sync.json` only for current cursor/progress; run history lives in PostgreSQL.
- **PostgreSQL current layer**: current normalized playlists/videos plus translated fields as the queryable materialized projection.
- **PostgreSQL history layer**: append-only history tables for deleted playlists/videos and change history.
- **Backup layer**: PostgreSQL base backup + WAL/PITR; incremental backup optional if size/ops justify it.

## 31. Doubling-of-Concern Clarification
- Some information will appear in more than one place on purpose, but each copy must have a different job.
- **Allowed doubling**:
  - `playlistId` appears in raw files, manifest, translation files, and PostgreSQL because it is the stable identity anchor.
  - translated fields may appear in translation files and PostgreSQL current tables because one is a derived artifact and the other is the queryable projection.
  - deleted items may exist in local archives and PostgreSQL history tables because one is replay material and the other is domain history.
- **Not allowed**:
  - raw JSON mutated into translated JSON.
  - manifest becoming a second business database.
  - sync.json storing durable history instead of only current run cursor.
  - translation cache becoming the source of truth for current state.
- The rule is: duplicate the data only when the copies have different responsibility boundaries.

## 32. History Ownership Clarification
- **Sync history** is owned by the sync/run layer and PostgreSQL run-history tables.
- **Playlist history** is owned by PostgreSQL playlist history tables, with local deleted archives as replay material.
- **Video history** is owned by PostgreSQL video history tables, with local raw/derived files as replay material.
- Sync history answers: when did a run happen, what step failed, what cursor/progress existed, what changed in that run?
- Playlist history answers: how did a playlist change over time, when was it deleted, what was its prior state?
- Video history answers: how did a video change over time, what raw snapshot did it come from, when was it added/removed/translated?
- The manifest and local files help replay and lookup, but they are not the long-term owner of domain history.
