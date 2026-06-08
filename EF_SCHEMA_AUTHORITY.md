# EF Core PostgreSQL Authoritative Schema

## 🏗️ Architecture Overview
This project utilizes a single PostgreSQL database with four distinct schemas to isolate domain concerns and prevent naming collisions.

**Global Constraints:**
- **Deletion Policy**: No hard deletes. All Foreign Keys must use `RESTRICT` or `NO ACTION` instead of `CASCADE`.
- **Soft Delete**: No `IsDeleted` or `DeletedAt` flags; cleanup is performed via date-based bulk deletes during resync.
- **Indexing**: All indexes follow the `idx_{table}_{columns}` naming convention.
- **Timezones**: All timestamps use `timestamptz` (UTC).

---

## 📺 Schema: `youtube`
*Focused on scraped YouTube playlist metadata.*

### Table: `videos`
| Column                  | Type          | Constraint       | Purpose                                              |
| :---------------------- | :------------ | :--------------- | :--------------------------------------------------- |
| `Id`                    | `int`         | PK, Identity     | Unique internal identifier for each video            |
| `Title`                 | `text`        | NOT NULL         | The original video title as provided by YouTube      |
| `TranslatedTitle`       | `text`        | NULLABLE         | English translation of the video title               |
| `Description`           | `text`        | NOT NULL         | The original video description text                  |
| `TranslatedDescription` | `text`        | NULLABLE         | English translation of the video description         |
| `ChannelName`           | `text`        | NOT NULL         | The name of the uploading YouTube channel            |
| `UploadDate`            | `date`        | NULLABLE         | The date the video was published to YouTube          |
| `Url`                   | `text`        | NOT NULL, UNIQUE | The source YouTube URL; prevents duplicate imports   |
| `SyncedAt`              | `timestamptz` | NULLABLE         | Timestamp of the last successful API synchronization |
| `Metadata`              | `jsonb`       | NULLABLE         | Raw JSON response from YouTube for future-proofing   |

**Indexes:**
- `idx_videos_url` (UNIQUE): Enforces URL uniqueness.
- `idx_videos_title`: Optimizes standard title searches.
- `idx_videos_title_trgm`: Enables fuzzy/similarity matching on titles.
- `idx_videos_channel`: Optimizes queries by channel name.
- `idx_videos_upload_date`: Optimizes date-range filtering.
- `idx_videos_translated_title`: Optimizes English title lookups.

---

## 🎵 Schema: `music`
*Focused on Last.fm scrobbles and classical music ranking.*

### Table: `artists`
| Column         | Type          | Constraint       | Purpose                                    |
| :------------- | :------------ | :--------------- | :----------------------------------------- |
| `Id`           | `int`         | PK, Identity     | Unique internal identifier for each artist |
| `Name`         | `text`        | NOT NULL, UNIQUE | Canonical name of the artist               |
| `ExternalId`   | `text`        | NULLABLE         | External ID from source system             |
| `SourceSystem` | `varchar(50)` | NULLABLE         | Source system (e.g., MusicBrainz)          |
| `Metadata`     | `jsonb`       | NULLABLE         | Raw API metadata for the artist            |

**Indexes:**
- `idx_artists_external_id_source` (UNIQUE): Enforces unique mapping per external system.

### Table: `albums`
| Column         | Type          | Constraint                    | Purpose                                          |
| :------------- | :------------ | :---------------------------- | :----------------------------------------------- |
| `Id`           | `int`         | PK, Identity                  | Unique internal identifier for each album        |
| `ArtistId`     | `int`         | FK $\rightarrow$ `artists.Id` | Links the album to its primary artist (RESTRICT) |
| `Title`        | `text`        | NOT NULL                      | The title of the album                           |
| `ReleaseDate`  | `date`        | NULLABLE                      | The date the album was released                  |
| `ExternalId`   | `text`        | NULLABLE                      | External ID from source system                   |
| `SourceSystem` | `varchar(50)` | NULLABLE                      | Source system (e.g., MusicBrainz)                |

**Indexes:**
- `idx_albums_external_id_source` (UNIQUE): Enforces unique mapping per external system.

### Table: `tracks`
| Column            | Type          | Constraint                    | Purpose                                                    |
| :---------------- | :------------ | :---------------------------- | :--------------------------------------------------------- |
| `Id`              | `int`         | PK, Identity                  | Unique internal identifier for each track                  |
| `AlbumId`         | `int`         | FK $\rightarrow$ `albums.Id`  | Links the track to its parent album (RESTRICT)             |
| `ArtistId`        | `int`         | FK $\rightarrow$ `artists.Id` | Direct link to artist for compilations/features (RESTRICT) |
| `Title`           | `text`        | NOT NULL                      | The title of the track                                     |
| `DurationSeconds` | `int`         | NULLABLE                      | Length of the track in seconds                             |
| `ExternalId`      | `text`        | NULLABLE                      | External ID from source system                   |
| `SourceSystem`    | `varchar(50)` | NULLABLE                      | Source system (e.g., MusicBrainz)                |

**Indexes:**
- `idx_tracks_external_id_source` (UNIQUE): Enforces unique mapping per external system.

### Table: `scrobbles`
| Column        | Type          | Constraint                   | Purpose                                                    |
| :------------ | :------------ | :--------------------------- | :--------------------------------------------------------- |
| `Id`          | `long`        | PK, Identity                 | Unique internal identifier for each play event             |
| `TrackId`     | `int`         | FK $\rightarrow$ `tracks.Id` | Links the scrobble to the specific track played (RESTRICT) |
| `ScrobbledAt` | `timestamptz` | NOT NULL                     | The exact time the track was played                        |
| `Platform`    | `varchar(50)` | NULLABLE                     | The source platform (e.g., Spotify, Apple Music)           |

### Table: `release_progress`
| Column           | Type          | Constraint   | Purpose                                    |
| :--------------- | :------------ | :----------- | :----------------------------------------- |
| `Id`             | `long`        | PK, ValueGen | Primary key for classical music tracking   |
| `ExternalId`     | `text`        | NULLABLE     | External ID from source system             |
| `SourceSystem`   | `varchar(50)` | NULLABLE     | Source system (e.g., MusicBrainz)          |
| `DiscNumber`     | `int`         | NULLABLE     | Disc position in multi-disc sets           |
| `TrackNumber`    | `int`         | NULLABLE     | Track position on the disc                 |
| `Title`          | `text`        | NULLABLE     | Specific track title for this recording    |
| `Artist`         | `text`        | NULLABLE     | Performer name for this specific recording |
| `Composer`       | `text`        | NULLABLE     | The composer of the musical work           |
| `Conductor`      | `text`        | NULLABLE     | The conductor of the recording             |
| `Orchestra`      | `text`        | NULLABLE     | The performing orchestra                   |
| `Soloists`       | `jsonb`       | NULLABLE     | List of soloists in JSON format            |
| `WorkName`       | `text`        | NULLABLE     | The overarching musical work name          |
| `RecordingVenue` | `text`        | NULLABLE     | Location where the recording took place    |
| `CreatedAt`      | `timestamptz` | DEFAULT NOW  | Record creation timestamp                  |

**Indexes:**
- `idx_release_progress_external_id_source` (UNIQUE): Enforces unique mapping per external system.

---

## 🛠️ Schema: `work`
*Native work tracker replacing legacy Fibery; follows Linear-esque patterns.*

### Table: `projects`
| Column      | Type           | Constraint       | Purpose                                                       |
| :---------- | :------------- | :--------------- | :------------------------------------------------------------ |
| `Id`        | `uuid`         | PK, Random       | Unique identifier for the project                             |
| `Name`      | `text`         | NOT NULL         | Human-readable name of the project                            |
| `Slug`      | `varchar(100)` | NOT NULL, UNIQUE | URL-safe identifier for the project                           |
| `Status`    | `varchar(20)`  | NOT NULL         | Current state (planned, active, paused, completed, cancelled) |
| `CreatedAt` | `timestamptz`  | DEFAULT NOW      | Project creation timestamp                                    |
| `UpdatedAt` | `timestamptz`  | DEFAULT NOW      | Last modification timestamp                                   |

### Table: `issues`
| Column         | Type          | Constraint                     | Purpose                                               |
| :------------- | :------------ | :----------------------------- | :---------------------------------------------------- |
| `Id`           | `uuid`        | PK, Random                     | Unique identifier for the task/issue                  |
| `Identifier`   | `varchar(20)` | NOT NULL, UNIQUE               | Human-friendly ID (e.g., "SCRIPTS-123")               |
| `Title`        | `text`        | NOT NULL                       | Short summary of the task                             |
| `Description`  | `text`        | NULLABLE                       | Detailed explanation of the task                      |
| `Status`       | `varchar(30)` | NOT NULL                       | State: backlog, todo, in_progress, in_review, done    |
| `Priority`     | `varchar(20)` | NOT NULL                       | Importance: urgent, high, medium, low, no_priority    |
| `PrioritySort` | `int`         | NOT NULL                       | Numeric order for sorting issues within same priority |
| `Estimate`     | `int`         | NULLABLE                       | Effort estimation (e.g., story points)                |
| `ProjectId`    | `uuid`        | FK $\rightarrow$ `projects.Id` | Links issue to its project (RESTRICT)                 |
| `ParentId`     | `uuid`        | FK $\rightarrow$ `issues.Id`   | Self-link for sub-task hierarchy (RESTRICT)           |
| `CreatedAt`    | `timestamptz` | DEFAULT NOW                    | Issue creation timestamp                              |
| `UpdatedAt`    | `timestamptz` | DEFAULT NOW                    | Last modification timestamp                           |

---

## ⚙️ Schema: `public`
*Infrastructure and cross-cutting application logic.*

### Table: `execution_logs`
| Column      | Type          | Constraint   | Purpose                                         |
| :---------- | :------------ | :----------- | :---------------------------------------------- |
| `Id`        | `int`         | PK, Identity | Unique identifier for each execution run        |
| `Timestamp` | `timestamptz` | DEFAULT NOW  | Time the command was executed                   |
| `SessionId` | `text`        | NULLABLE     | Groups related executions into a single session |
| `Payload`   | `jsonb`       | NULLABLE     | Input/output data for technical debugging       |
| `ExitCode`  | `int`         | NULLABLE     | Command return code (0 for success)             |

### Table: `failed_tasks`
| Column           | Type          | Constraint                           | Purpose                                                |
| :--------------- | :------------ | :----------------------------------- | :----------------------------------------------------- |
| `Id`             | `int`         | PK, Identity                         | Unique identifier for each failure event               |
| `TaskName`       | `text`        | NULLABLE                             | Name of the operation that failed                      |
| `ErrorMessage`   | `text`        | NULLABLE                             | Technical error details for debugging                  |
| `Timestamp`      | `timestamptz` | DEFAULT NOW                          | Time the failure occurred                              |
| `ExecutionLogId` | `int`         | FK $\rightarrow$ `execution_logs.Id` | Links failure to the specific execution run (SET NULL) |

---

## 🗺️ ER Relationship Summary

*ER diagram removed - will be regenerated via mermaid skill in diagrams directory*
