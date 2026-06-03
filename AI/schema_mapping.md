# PostgreSQL Database Schema Mapping

## Overview

Three separate databases for different concerns:

```mermaid
graph TB
    subgraph "PostgreSQL Server"
        subgraph "Database 1: YouTube"
            YB[youtube_db]
        end
        subgraph "Database 2: Fibery"
            FB[fibery_db]
        end
        subgraph "Database 3: Last.fm"
            LB[lastfm_db]
        end
    end

    YT[YouTube API] --> YB
    FY[Fibery API] --> FB
    LF[Last.fm API] --> LB
```

---

## Database 1: YouTube Playlists

```mermaid
erDiagram
    playlists {
        uuid id PK "gen_random_uuid()"
        varchar playlist_id UK "YouTube playlist ID like PLxxxxx"
        text title
        integer video_count
        varchar etag "For change detection"
        timestamptz last_updated
        timestamptz created_at
    }

    playlist_videos {
        uuid id PK "gen_random_uuid()"
        uuid playlist_id FK
        integer video_id FK
        integer position "Order in playlist"
        timestamptz added_at
    }

    videos {
        integer id PK "IDENTITY ALWAYS"
        text url UK "youtube.com/watch?v=xxx"
        text title "Original language"
        text translated_title "English translation"
        text description "Original"
        text translated_description "English"
        timestamptz translated_at "When translated"
        varchar detected_language "ISO 639-1 code"
        text channel_name
        date upload_date
        timestamptz synced_at
        jsonb metadata "Channel ID, thumbnails, etc."
    }

    playlists ||--o{ playlist_videos : "has"
    videos ||--o{ playlist_videos : "appears in"
```

### Key Indexes
- `playlists.playlist_id` — UNIQUE
- `videos.url` — UNIQUE
- `playlist_videos(playlist_id, video_id)` — UNIQUE composite
- `videos.title` — GIN trigram for fuzzy search

---

## Database 2: Fibery (Notes & Logs)

```mermaid
erDiagram
    fibery_entities {
        uuid id PK "gen_random_uuid()"
        varchar fibery_id UK "Fibery system ID"
        varchar entity_type "Document, Task, Project"
        jsonb raw_data "Full Fibery API response"
        timestamptz synced_at "Last sync timestamp"
    }

    execution_logs {
        integer id PK "IDENTITY BY DEFAULT"
        text session_id "Agent session identifier"
        integer exit_code "0 = success"
        jsonb payload "Command output, details"
        timestamptz timestamp "DEFAULT CURRENT_TIMESTAMP"
    }

    failed_tasks {
        uuid id PK "gen_random_uuid()"
        text task_name "SyncYouTube, UpdateSheets, etc."
        text error_message "Exception details"
        timestamptz timestamp "DEFAULT CURRENT_TIMESTAMP"
    }

    source_records {
        uuid id PK "gen_random_uuid()"
        text source_id "Fibery entity UUID"
        text entity_type "What kind of record"
        jsonb raw_data "Original API payload"
    }

    execution_logs ||--o{ failed_tasks : "may produce"
    fibery_entities ||--o{ source_records : "maps to"
```

### Key Indexes
- `fibery_entities(fibery_id, entity_type)` — UNIQUE composite
- `failed_tasks.task_name` — for filtering by task
- `execution_logs.session_id` — for grouping

---

## Database 3: Last.fm Scrobbles (UTC 24h)

```mermaid
erDiagram
    artists {
        integer id PK "IDENTITY ALWAYS"
        text name UK "Trigram index enabled"
        jsonb metadata "Last.fm artist data"
    }

    albums {
        integer id PK "IDENTITY ALWAYS"
        integer artist_id FK
        text title
        date release_date
    }

    tracks {
        integer id PK "IDENTITY ALWAYS"
        integer album_id FK
        integer artist_id FK
        text title
        integer duration_seconds
    }

    scrobbles {
        bigint id PK "IDENTITY ALWAYS"
        integer track_id FK
        timestamptz scrobbled_at "UTC stored as timestamptz"
        varchar platform "LASTFM, WEB, SPOTIFY"
    }

    release_progress {
        bigint id PK "IDENTITY BY DEFAULT"
        text release_id "MusicBrainz release ID"
        integer disc_number
        integer track_number
        text title
        text artist
        text composer
        text conductor
        text orchestra
        jsonb soloists
        text recording_venue
        integer recording_year
        text duration
        text work_name
        timestamptz created_at "DEFAULT CURRENT_TIMESTAMP"
    }

    artists ||--o{ albums : "produces"
    artists ||--o{ tracks : "records"
    albums ||--o{ tracks : "contains"
    tracks ||--o{ scrobbles : "scrobbled as"
```

### Key Indexes
- `scrobbles(track_id, scrobbled_at)` — UNIQUE composite
- `tracks(artist_id, title)` — UNIQUE composite
- `albums(artist_id, title)` — UNIQUE composite
- All name/title columns use GIN trigram indexes

---

## UTC Time Handling

```mermaid
flowchart LR
    subgraph "Last.fm API"
        TS[Unix Timestamp]
    end

    subgraph "C# Processing"
        DTO[DateTimeOffset.UtcNow]
    end

    subgraph "PostgreSQL"
        TT[timestamptz]
    end

    subgraph "Display"
        IST[IST Conversion]
        UTC[UTC Display]
    end

    TS -->|fromtimestamp| DTO
    DTO -->|INSERT| TT
    TT -->|ToLocalTime| IST
    TT -->|As-is| UTC
```

**Safety chain:**
1. Last.fm API returns Unix timestamp (always UTC)
2. C# uses `DateTimeOffset` (not `DateTime`) — preserves offset
3. PostgreSQL `timestamptz` stores as UTC internally
4. Display layer calls `.ToLocalTime()` or `.ToIst()` only for presentation
5. Never muddles — `DateTimeKind.Utc` enforced at source

---

## New Fields for YouTube Translation

```sql
-- Migration: Add translation fields to videos table
ALTER TABLE videos
    ADD COLUMN translated_title text,
    ADD COLUMN translated_description text,
    ADD COLUMN translated_at timestamptz,
    ADD COLUMN detected_language varchar(10);

-- Index for finding untranslated videos
CREATE INDEX idx_videos_needs_translation
    ON videos (detected_language)
    WHERE translated_title IS NULL AND detected_language IS NOT NULL;
```

---

## Entity Relationships Summary

| Database | Tables | FK Relationships |
|----------|--------|------------------|
| YouTube | `playlists`, `playlist_videos`, `videos` | playlist_videos → playlists, playlist_videos → videos |
| Fibery | `fibery_entities`, `execution_logs`, `failed_tasks`, `source_records` | failed_tasks ← execution_logs, source_records → fibery_entities |
| Last.fm | `artists`, `albums`, `tracks`, `scrobbles`, `release_progress` | albums → artists, tracks → albums + artists, scrobbles → tracks |
