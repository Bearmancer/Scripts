-- init_schema.sql

-- Artists table
CREATE TABLE IF NOT EXISTS artists
(
	id
	UUID
	DEFAULT
	gen_random_uuid
(
)
	PRIMARY
	KEY,
	name
	TEXT
	NOT
	NULL,
	mbid
	TEXT
	UNIQUE,
	metadata
	JSONB
	);
CREATE INDEX IF NOT EXISTS idx_artists_name_ci ON artists (Lower (name));
DROP INDEX IF EXISTS idx_artists_name;

-- Albums table
CREATE TABLE IF NOT EXISTS albums
(
	id
	UUID
	DEFAULT
	gen_random_uuid
(
)
	PRIMARY
	KEY,
	artist_id
	UUID
	NOT
	NULL
	REFERENCES
	artists
(
	id
),
	title TEXT NOT NULL,
	release_date DATE,
	mbid TEXT UNIQUE
	);
CREATE INDEX IF NOT EXISTS idx_albums_artist_title ON albums (artist_id, title);
DROP INDEX IF EXISTS idx_albums_title;

-- Tracks table
CREATE TABLE IF NOT EXISTS tracks
(
	id
	UUID
	DEFAULT
	gen_random_uuid
(
)
	PRIMARY
	KEY,
	album_id
	UUID
	NOT
	NULL
	REFERENCES
	albums
(
	id
),
	artist_id UUID NOT NULL REFERENCES artists
(
	id
),
	title TEXT NOT NULL,
	duration INT, -- Seconds
	mbid TEXT UNIQUE
	);
CREATE INDEX IF NOT EXISTS idx_tracks_album_title ON tracks (album_id, title);
DROP INDEX IF EXISTS idx_tracks_title;

-- Platform Enum
DO
$$
BEGIN
CREATE TYPE platform AS ENUM ('lastfm', 'youtube', 'other');
EXCEPTION
    WHEN duplicate_object THEN null;
END $$;

-- Scrobbles table
CREATE TABLE IF NOT EXISTS scrobbles
(
	track_id
	UUID
	NOT
	NULL
	REFERENCES
	tracks
(
	id
),
	timestamp TIMESTAMPTZ NOT NULL,
	platform platform NOT NULL,
	PRIMARY KEY
(
	track_id,
	timestamp
)
	);
CREATE INDEX IF NOT EXISTS idx_scrobbles_timestamp_desc ON scrobbles (timestamp DESC) INCLUDE (track_id, platform);
DROP INDEX IF EXISTS idx_scrobbles_timestamp;

-- Execution logs table
CREATE TABLE IF NOT EXISTS execution_logs
(
	id
	SERIAL
	PRIMARY
	KEY,
	timestamp
	TIMESTAMPTZ
	NOT
	NULL
	DEFAULT
	CURRENT_TIMESTAMP,
	session_id
	TEXT,
	payload
	JSONB,
	exit_code
	INT
);

-- Source Records (formerly fibery_entities)
CREATE TABLE IF NOT EXISTS source_records
(
	id
	UUID
	PRIMARY
	KEY,
	source_id
	TEXT
	NOT
	NULL,
	source_type
	TEXT
	NOT
	NULL,
	raw_data
	JSONB
);
CREATE UNIQUE INDEX IF NOT EXISTS idx_source_records_unique ON source_records(source_id, source_type);

-- Failed Tasks
CREATE TABLE IF NOT EXISTS failed_tasks
(
	id
	SERIAL
	PRIMARY
	KEY,
	task_name
	TEXT
	NOT
	NULL,
	error_message
	TEXT,
	timestamp
	TIMESTAMPTZ
	NOT
	NULL
	DEFAULT
	CURRENT_TIMESTAMP
);
