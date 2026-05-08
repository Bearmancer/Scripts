# Description

-----------------------------

# Plan

-----------------------------

# Implementation Plan: Local PostgreSQL Mirror for Fibery Data

## Decision Summary

| Question                  | Decision                                                            |
|---------------------------|---------------------------------------------------------------------|
| DB engine                 | **PostgreSQL** (open-source, jsonb, OCI-native, no size cap)        |
| MCP server for agent      | **bytebase/dbhub** (active, read+write, multi-DB)                   |
| MCP for humans in VS Code | **DBCode** v1.31.1 (50+ DBs, ER diagrams, MCP integration)          |
| Remote access method      | SSH tunnel (dev) + SSL/scram-sha-256 direct (scheduled jobs)        |
| Sync strategy             | Polling by `fibery/modification-date` every 5 min via systemd timer |
| Hook log storage          | Hybrid: local PG primary + async Fibery replication                 |
| Snapshot backup           | Weekly `pg_dump` + daily WAL archive to OCI Object Storage          |

## ⚡ Efficiency Optimization Strategy

The top time waste per new chat is schema discovery. Each new conversation requires MCP `describe_database` calls (
200-800ms per Fibery API round-trip). Here's the efficiency plan:

### A. Schema Materialization Table

Create `fibery_schema_cache` table in PostgreSQL that mirrors the schema metadata:

```sql
CREATE TABLE fibery_schema_cache (
  database_name TEXT PRIMARY KEY,
  fields JSONB NOT NULL,  /* Full field list with types */
  related_dbs JSONB,      /* Related database schemas */
  fetched_at TIMESTAMPTZ DEFAULT NOW()
);
```

Populated by systemd timer. Agent reads via single dbhub query (\~5ms local), not N Fibery API calls.

### B. Persisted Schema Context File

Write `.state/fibery-schema.md` — a pre-compiled markdown file containing all schemas. Agent reads this file at session
start instead of making MCP describe calls. Updated by the 5-min sync timer.

### C. Schema File in `.clinerules/` or `.github/copilot-instructions.md`

Place the schema reference text into `.clinerules/` so every new Cline session inherits schema knowledge without any
tool calls. This is ZERO-latency schema context.

### D. Pre-Joined View Materialization

Create materialized views for common join patterns (execution logs + issues + projects) so agent queries a single view
rather than multiple Fibery API calls.

### E. Connection Pooling with PgBouncer

Transaction-mode PgBouncer to eliminate TCP connection overhead on every hook call.

## CPM Sequence

1. ✅ This assessment issue (#219)
2. Install PostgreSQL on OCI Ubuntu, secure pg_hba.conf, enable SSL
3. Create `fibery_mirror` DB + schema tables + materialized views
4. Write incremental sync script (Python + psycopg2, polls Fibery API by modification-date)
5. Configure systemd timer for 5-min sync (triggers schema cache population)
6. Install and configure bytebase/dbhub as MCP server for Cline
7. Write `.state/fibery-schema.md` generator (post-sync hook)
8. Migrate hook execution logs to write to local PG (with Fibery fallback)
9. Set up weekly pg_dump backup cron + WAL archive to OCI Object Storage
10. Validate: measure round-trips per chat, verify zero schema queries on new session

# Prompt

-----------------------------

# Research

-----------------------------

# Research: Local SQL DB Strategy for Fibery Data

## 1. PostgreSQL vs MSSQL

| Factor           | PostgreSQL                                                 | MSSQL                                                        |
|------------------|------------------------------------------------------------|--------------------------------------------------------------|
| Licensing        | Open-source, free forever                                  | Proprietary; Express has 10GB DB cap, no SQL Agent           |
| Linux/OCI        | First-class Ubuntu support                                 | Available but Windows-first; some features missing on Linux  |
| JSON             | `jsonb` binary JSON + GIN indexes + jsonpath — excellent   | `OPENJSON`/`FOR JSON`/`JSON_VALUE` — good but no binary JSON |
| Full-text search | `tsvector`/`tsquery`, `pg_trgm`                            | More polished FTS UI tooling                                 |
| OCI Free Tier    | Runs perfectly on 1 OCPU / 1GB ARM shape                   | SQL Server Express feasible but tight on 1GB RAM             |
| Ecosystem        | pgAdmin, DBeaver, PostgREST, Hasura, pgvector, TimescaleDB | SSMS, Azure Data Studio — Windows-centric                    |

**Recommendation: PostgreSQL.** MSSQL Express 10GB cap is a hard blocker for a full Fibery mirror. PostgreSQL's
`jsonb` + GIN indexing is purpose-built for Fibery's entity/document model. OCI ARM shapes run PG natively.

---

## 2. MCP vs Direct DB Invocation

**MCP servers for PostgreSQL:**

* `@modelcontextprotocol/server-postgres` v0.6.2 — **ARCHIVED** (moved to servers-archived repo, last commit
  2025-05-28). Read-only `query` tool only. No write, no list_tables tool.
* `bytebase/dbhub` — **Active** (2,691 stars, last push 2026-04-21). Supports PG, MySQL, MSSQL, SQLite. Tools:
  `execute_sql` (read+write), `search_objects`, schema exploration. Recommended replacement.
* **DBCode** v1.31.1 (updated 2026-05-05) — VS Code extension with built-in MCP integration. 162K installs, 4.7★.
  Supports 50+ databases. Has first-class Copilot/MCP integration.

**MCP vs direct (psycopg2/asyncpg):**

| Aspect       | MCP Server                               | Direct psycopg2                  |
|--------------|------------------------------------------|----------------------------------|
| Hook scripts | Agent calls MCP tool → tool calls DB     | Script imports psycopg2 directly |
| Latency      | Extra IPC hop (\~5–20ms)                 | Minimal (\~1ms local)            |
| Security     | DB credentials in MCP server config only | Credentials in each script       |
| Flexibility  | Limited to tools the MCP server exposes  | Full SQL power                   |
| Agent UX     | Agent can introspect DB naturally        | Agent blind unless told          |

**Recommendation:** Use **direct psycopg2** in hook scripts (latency-critical path). Use **dbhub MCP** for agent-driven
ad-hoc queries and schema exploration.

---

## 3. Remote SQL Access (OCI Ubuntu → Windows)

* PostgreSQL default port 5432; add OCI Security List ingress rule for port 5432 (restrict to home IP CIDR)
* `pg_hba.conf`: `hostssl all all <your-ip>/32 scram-sha-256`
* Generate self-signed SSL cert or use Let's Encrypt for `ssl = on` in postgresql.conf
* Connection string: `postgresql://user:pass@129.159.233.131:5432/fibery_mirror?sslmode=require`
* Connection pooling: **PgBouncer** (transaction mode) to handle multiple concurrent agent connections
* Alternative: **SSH tunnel** (`ssh -L 5432:localhost:5432 ubuntu@129.159.233.131`) — no firewall rule needed, encrypted
  by default

**Recommendation:** SSH tunnel for dev/agent use (zero firewall exposure). Direct SSL+scram for production scheduled
jobs.

---

## 4. Fibery MCP (Cloud API) vs Local SQL Efficiency

| Aspect             | Fibery MCP/API                                                  | Local PostgreSQL                                 |
|--------------------|-----------------------------------------------------------------|--------------------------------------------------|
| Latency per query  | 200–800ms (HTTPS round-trip to EU/US)                           | 1–5ms local / 20–50ms over SSH tunnel            |
| Rate limits        | Fibery imposes API rate limits (undocumented but \~10–50 req/s) | None                                             |
| Data freshness     | Real-time (live data)                                           | Stale by sync interval (minutes to hours)        |
| Offline capability | Requires internet + Fibery uptime                               | Works offline                                    |
| Query power        | Limited to Fibery query DSL                                     | Full SQL — JOINs, CTEs, window functions         |
| Write capability   | Yes (create/update entities)                                    | Read-only mirror unless bidirectional sync built |

**Recommendation:** Keep Fibery MCP for **writes and real-time lookups**. Use local PG for **analytical queries, hook
audit logs, and bulk reads** where latency matters.

---

## 5. Incremental Snapshots

**Pattern A — Poll by modification-date (simplest):**

```sql
SELECT * FROM fibery_entities
WHERE last_synced_at > NOW() - INTERVAL '5 minutes'
```

Poll Fibery API with `q_where: [">" , ["fibery/modification-date"], "$lastSync"]`. Upsert into PG with
`ON CONFLICT (fibery_id) DO UPDATE`.

**Pattern B — PostgreSQL logical replication / CDC (overkill for this use case):**\
wal2json / pgoutput only applies to changes *within* PG, not from external API. Not applicable here.

**Snapshot table design:**

```sql
CREATE TABLE fibery_snapshot (
  fibery_id UUID PRIMARY KEY,
  database_name TEXT NOT NULL,
  public_id TEXT,
  name TEXT,
  payload JSONB NOT NULL,
  fibery_created_at TIMESTAMPTZ,
  fibery_modified_at TIMESTAMPTZ,
  synced_at TIMESTAMPTZ DEFAULT NOW()
);
CREATE INDEX ON fibery_snapshot USING GIN (payload);
CREATE INDEX ON fibery_snapshot (database_name, fibery_modified_at DESC);
```

**Recommendation:** Pattern A with 5-minute polling via a systemd timer. Full snapshot weekly via `pg_dump`.

---

## 6. Robustness

* **ACID**: PostgreSQL is fully ACID — safe for concurrent hook writers
* **Backup**: `pg_dump fibery_mirror | gzip > /backup/fibery_$(date +%Y%m%d).sql.gz` via cron
* **WAL archiving**: Enable `archive_mode = on` + `archive_command` to OCI Object Storage for PITR
* **HA on OCI Free Tier**: Single-node only (free tier has 1 ARM instance). No failover unless paying.
* **Crash recovery**: PG WAL ensures crash-safe recovery automatically
* **OCI free tier risk**: If OCI VM goes down, local PG unavailable — hooks must degrade gracefully

**Recommendation:** Wrap all DB calls in try/catch with fallback to Fibery API. Never make local PG a hard dependency
for hook execution.

---

## 7. Hook Migration: Fibery API → Local PostgreSQL

Current hooks POST execution logs to Fibery REST API. Proposed: write to local PostgreSQL instead.

| Aspect             | Fibery API                   | Local PostgreSQL                          |
|--------------------|------------------------------|-------------------------------------------|
| Latency            | 200–800ms per log write      | 1–5ms local / 20–50ms tunnel              |
| Availability       | Fibery cloud uptime (>99.9%) | OCI VM uptime (single node, \~99%)        |
| Complexity         | Simple REST POST             | Requires DB connection management         |
| Offline resilience | Fails if no internet         | Fails if OCI VM down                      |
| Query power        | Fibery query DSL only        | Full SQL — much better for audit analysis |
| Searchability      | Fibery UI                    | Any SQL client / Grafana / pgAdmin        |

**Verdict: Mixed.** Local PG is faster and more queryable, but adds a new single point of failure. **Recommended hybrid
**: write to local PG primarily, async-replicate to Fibery for UI visibility. Or: write to both with local PG as primary
and Fibery as eventual-consistent audit trail.

---

## 8. PostgreSQL MCP Options

| Server                                  | Stars         | Status                 | Tools                                   | Use Case                 |
|-----------------------------------------|---------------|------------------------|-----------------------------------------|--------------------------|
| `@modelcontextprotocol/server-postgres` | 254           | ⚠️ ARCHIVED 2025-05-28 | `query` (read-only)                     | Obsolete                 |
| `bytebase/dbhub`                        | 2,691         | ✅ Active (Apr 2026)    | `execute_sql`, `search_objects`, schema | Best general-purpose     |
| `DBCode` MCP                            | 162K installs | ✅ Active (May 2026)    | Via VS Code extension MCP integration   | Best for in-editor agent |

**Recommendation:** Use **dbhub** as the MCP server for agent-driven PostgreSQL operations. Its `execute_sql` supports
both reads and writes with transaction safety.

---

## 9. dbcode vs postgresql MCP

**DBCode** (v1.31.1, updated 2026-05-05):

* VS Code extension, not a standalone MCP server
* 50+ databases, first-class MCP integration built-in
* Has Copilot integration, ER diagrams, SQL Notebooks, backup/restore
* MCP exposed through VS Code's language model tools API
* **Best for**: Human-in-the-loop database browsing + AI-assisted queries inside VS Code
* **Not suitable for**: Headless agent hook scripts running outside VS Code

**@modelcontextprotocol/server-postgres** (v0.6.2):

* ARCHIVED. Last meaningful update 2025. Read-only. Do not use for new work.

**bytebase/dbhub** (active 2026):

* Standalone MCP server, runs headlessly
* Supports read+write SQL
* **Best for**: Cline agent MCP tool calls to query/write local PostgreSQL

**Recommendation:**

* Install **DBCode** for human-driven DB inspection in VS Code
* Configure **dbhub** as the MCP server for Cline agent DB operations
* Do NOT use the archived `server-postgres`

# Validation

-----------------------------

