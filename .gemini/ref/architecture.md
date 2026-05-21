# Architecture Reference

> **Purpose:** Technical spec for agents implementing schema, EF Core, or Fibery migration tasks.
> Read this file only when a task prompt directs you here. This is not a README or a workflow.
> For system overview context see: `prompt/execution-prompt.md`. For task order: `plans/cpm.md`.

Modernization of the scrobble/music/planner ecosystem by replacing Fibery (online-only, high latency,
semantic-dependent) with a hybrid **Local PostgreSQL 18 + Neon (Cloud)** architecture.

```
Old Pipeline: API → .NET Object → JSON → CSV → Google Sheets
New Pipeline: API → .NET 10 Service → Local PostgreSQL 18 ($PGCONNSTR) → Neon (logical replication)
```

---

## 2. Data Model (3NF)

### Music Domain

**`artists`**
| Column | Type | Constraints |
| ---------- | ----- | ----------------------- |
| `id`       | UUID | PK |
| `name`     | TEXT | Indexed |
| `mbid`     | TEXT | Unique — MusicBrainz ID |
| `metadata` | JSONB | Extensible attributes |

**`albums`**
| Column | Type | Constraints |
| -------------- | ---- | ------------ |
| `id`           | UUID | PK |
| `artist_id`    | UUID | FK → artists |
| `title`        | TEXT | Indexed |
| `release_date` | DATE | |
| `mbid`         | TEXT | Unique |

**`tracks`**
| Column | Type | Constraints |
| ----------- | ---- | ------------ |
| `id`        | UUID | PK |
| `album_id`  | UUID | FK → albums |
| `artist_id` | UUID | FK → artists |
| `title`     | TEXT | Indexed |
| `duration`  | INT | Seconds |
| `mbid`      | TEXT | Unique |

**`scrobbles`**
| Column | Type | Constraints |
| ----------- | ----------- | ---------------------------- |
| `id`        | BIGINT | PK |
| `track_id`  | UUID | FK → tracks |
| `timestamp` | TIMESTAMPTZ | Indexed |
| `platform`  | ENUM | `lastfm`, `youtube`, `other` |

### Management Domain

**`execution_logs`**
| Column | Type | Constraints |
| ------------ | ----------- | ---------------------- |
| `id`         | SERIAL | PK |
| `timestamp`  | TIMESTAMPTZ | |
| `session_id` | TEXT | |
| `payload`    | JSONB | Full execution context |
| `exit_code`  | INT | |

---

## 3. Synchronization Architecture

### Local vs. Cloud

- **Local PostgreSQL** — primary sink for all .NET orchestration. Optimized for low-latency, high-throughput MCP calls.
- **Neon (Cloud)** — read-only remote mirror via **Logical Replication**. Provides mobile access and off-site
  redundancy.
- **Rewind** — managed via Neon Branching (snapshots) and PITR (Point-In-Time Recovery).

### MCP Context

MCP (Model Context Protocol) is an open protocol by Anthropic that enables AI agents to interact with external tools
via JSON-RPC servers. Kilo uses stdio-based MCP servers configured in `kilo.jsonc`. The MCP servers provide tools
that the agent can invoke for browser automation, structured data extraction, library documentation queries, remote
SSH execution, and PostgreSQL interaction.

**Current architecture:** Docker MCP Gateway manages all MCP server containers via a single `docker` entry in
`kilo.jsonc`. The gateway runs with `--profile scripts-dev` which defines which MCP servers (fetch, playwright,
context7, pgEdge PostgreSQL) are available as Docker containers.

---

## 4. Fibery Schema Reference

> Planned: Docker MCP Gateway architecture managing all MCP server containers (fetch, playwright, context7, pgEdge
> PostgreSQL).
> See Section 3 of `.kilo/plans/1778576490736-jolly-cactus.md` for the full migration plan.

---

## 5. EF Core Patterns (8 → 10) — PostgreSQL-Compatible Features

All features listed below are compatible with PostgreSQL 18 via Npgsql 10. SQL Server-only features are documented
with their PostgreSQL equivalent. The project uses `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x with EF Core 10 LTS.
Connection string via `$PGCONNSTR` (see `.env`)

### EF Core 8: Foundation (LTS until Nov 2026)

- **Complex Types as Value Objects**: Simplifies domain models without the overhead of owned entities. Used for value
  types that don't need independent tracking.
- **Primitive Collections**: Direct mapping of `List<int>` or `string[]` to PostgreSQL array columns.
- **Unmapped Queries (`SqlQuery<T>`)**: Run raw SQL and project into any type `T` without registering in `DbContext`.
- **Bulk Operations**: `ExecuteUpdate` / `ExecuteDelete` for database mutations — bypasses change tracker,
  used for `scrobbles` and `source_records` updates.

### EF Core 9: Stability Release (STS, ends Nov 2026)

- **GREATEST/LEAST functions**: Npgsql translates these natively for PostgreSQL.
- **Query parameterization control**: `EF.Constant()` for small collections, `Parameter()` for large.
- **Inlined subqueries + Count→EXISTS optimization**: Automatic query improvements.
- **Concurrent migration protection**: Npgsql locking support prevents CI/CD race conditions.
- **Complex types (continued)**: ExecuteUpdate support, GroupBy with complex types.
- **Precompiled Models (Native AOT)**: Experimental in EF9 — skip until GA.
- **OpenTelemetry Integration**: Minimal-overhead structured tracing via Npgsql 9+.

### EF Core 10: LTS (Nov 2025 — Nov 2028) — Primary Target

- **LeftJoin/RightJoin operators**: Native Npgsql translation — replaces `SelectMany+GroupBy+DefaultIfEmpty`.
- **Complex types improvements**: Table splitting (JSONB column mapping), JSON mapping (strongly-typed Fibery data),
  struct support (DTO value semantics, no identity tracking).
- **Named query filters**: Filter by `platform` enum without per-query WHERE — multi-platform filtering.
- **Parameterized collections (new default)**: Auto-padding for plan cache — `WHERE id IN (...)` optimization.
- **DateOnly translations**: Npgsql-native, used for `albums.release_date`.
- **Split query ordering consistency**: Fixes ordering mismatch in `Include().ThenInclude()` chains.
- **JSONB via PostgreSQL native types**: PostgreSQL JSONB supports GIN indexing, `@>`, `?`, `?|`, `?&` operators —
  superior to SQL Server JSON support.

### EF Core 11: Preview (GA expected Nov 2026)

- **To-one join optimization (29%)**: Prunes unnecessary JOINs + ORDER BY keys when loading navigation properties
  (`Track.Album`, `Track.Artist`).
- **MaxBy/MinBy**: Native Npgsql translation — `scrobbles.MaxBy(s => s.Timestamp)` for last scrobble per track.
- **Exclude FK constraints from migrations**: Critical for Fibery sync — out-of-order data arrival without FK
  enforcement.
- **Latest migration ID in snapshot**: Team merge detection for concurrent schema changes.
- **`--add` flag for migration**: One-step `dotnet ef database update --add` for CI/CD containers.
- **`--connection`/`--offline` flags**: Connection string passed directly for remove operations.

### PostgreSQL Extension Features (Beyond EF Core)

- **Vector search via pgvector**: `CREATE EXTENSION vector;` + `pgvector-dotnet` NuGet. Use raw SQL for
  `ORDER BY embedding <=> '[1,2,3]' LIMIT 5` with IVFFlat/HNSW indexes.
- **Full-text search via tsvector/tsquery**: GIN-indexed FTS — `WHERE to_tsvector('english', "Name") @@
  plainto_tsquery('english', 'search term')` via `FromSqlRaw`.
- **JSONB operators `@>`, `?`, `?|`, `?&`**: Query `source_records.RawData` without full document load.
- **ltree hierarchy**: `CREATE EXTENSION ltree` — tree queries if hierarchy needs arise.

### Design Mandates (C# layer)

- Primary Constructors, File-Scoped Namespaces, Global Usings, Minimal Hosting.
- Strictly use `ExecuteUpdate` / `ExecuteDelete` for mutations — never `SaveChanges()` loops.
- No Dapper, no legacy Repository pattern.
