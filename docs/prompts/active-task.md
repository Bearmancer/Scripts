# Active Task Tracker

> **Plan:** `.kilo/plans/cpm.md` | **Standards:** `.kilo/rules/standards.md`
> **Last updated:** 2026-05-12T11:13 UTC

---

## Current Task: T02 — Directory Normalization `[IN PROGRESS]`

**Next on critical path:** T00 → T02 → T03 → T04 → T05 → T06 → T07+T08 → T09 → T10/T11/T12

---

## Win Gate Status

| Gate | Condition                                       | Status | Blocked By |
| ---- | ----------------------------------------------- | :----: | ---------- |
| G1   | MCP servers operational                         |   ✅   | —          |
| G2   | PG schema deployed (7 tables)                   |   ✅   | —          |
| G3   | CSV data ingested                               |   ✅   | —          |
| G4   | Scrobble sync works                             |   ❌   | T07        |
| G5   | Execution logs captured                         |   ✅   | —          |
| G6   | EF Core builds (dotnet build exit 0)            |   ❌   | T05        |
| G7   | Neon sync verified                              |   ❌   | G4, G6     |
| G8   | Orchestrator clean (0 GoogleSheetsService refs) |   ❌   | T07, T08   |

---

## Task Status Dashboard

| Task                                      | Tier                  |   Status    | Depends On |
| ----------------------------------------- | --------------------- | :---------: | ---------- |
| T00 — Log Reorganization                  | 0 — Foundation        |    DONE     | —          |
| T01 — MCP Migration                       | 0 — Foundation        |    DONE     | —          |
| T02 — Directory Normalization             | 1 — Infrastructure    | IN PROGRESS | T00        |
| T03 — AGENTS.md Rewrite                   | 1 — Infrastructure    |   BACKLOG   | T02        |
| T04 — Slash Google Files                  | 2 — Build & Migration |   BACKLOG   | T03        |
| T05 — Fix Build Errors                    | 2 — Build & Migration |   BACKLOG   | T04        |
| T06 — Expand PostgresService              | 2 — Build & Migration |   BACKLOG   | T05        |
| T07 — Rewrite ScrobbleSyncOrchestrator    | 2 — Build & Migration |   BACKLOG   | T06        |
| T08 — Rewrite YouTubePlaylistOrchestrator | 2 — Build & Migration |   BACKLOG   | T06        |
| T09 — CleanResetCommand Fix               | 2 — Build & Migration |   BACKLOG   | T07, T08   |
| T10 — Docker MCP Gateway                  | 3 — Polish            |   BACKLOG   | T01        |
| T11 — Knowledge Base Fixes                | 3 — Polish            |   BACKLOG   | T09        |
| T12 — Terminal & Environment Fixes        | 3 — Polish            |   BACKLOG   | T09        |

---

## Parallel Groups

| Group | Tasks       |    Ready?     |
| ----- | ----------- | :-----------: |
| A     | T00, T01    |    ✅ Now     |
| B     | T02, T03    |   After T00   |
| C     | T04→T05→T06 |  Sequential   |
| D     | T07, T08    |   After T06   |
| E     | T09         | After T07+T08 |
| F     | T10         |   After T01   |
| F     | T11, T12    |   After T09   |

