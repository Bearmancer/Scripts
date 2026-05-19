-- Initial Schema for Personal DB (pg_db)
-- Adheres to PostgreSQL Table Design Skill
-- Credentials in .env / $PGCONNSTR

CREATE TABLE users
(
	user_id    BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
	email      TEXT        NOT NULL UNIQUE,
	name       TEXT        NOT NULL,
	created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
	updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Case insensitive email index
CREATE UNIQUE INDEX ON users (LOWER (email));
CREATE INDEX ON users (created_at);

-- Add any other tables here later
