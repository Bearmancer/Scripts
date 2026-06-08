# Agent Rules — Plan Governance Workflow

> These rules are enforced for ALL agents operating on this repository. Violations = plan sprawl.

---

## General

- `./PLAN.md` is the ONLY place for implementation phases, tasks, and status tracking.
- No other file may contain task lists, phase definitions, or status checkboxes.
- Violation = sprawl.

## Automated Sprawl Detection

- Enforce all markdown files in one dir (except `AGENTS.md` and `PLAN.md`)
- Never have more than 1 file for research, plan, diagram each.
- ALWAYS prefer update existing files.
- Be squeamish with generating new markdown files. ONLY if needed.

## Key Decisions

These were decided in earlier sessions. Do not question them again:

- EF entities are TARGET STATE (not dead/aspirational)
- Google Sheets → PostgreSQL (all data migrates, Sheets is legacy)
- Fresh install of PostgreSQL (no backward compat)
- Monolithic program (not library, single csproj, no exclusions)
- API data goes straight to PGSQL (no JSON buffer)
- Migrating work state stored on Fibery natively onto PGSQL 
- MCP integration AFTER work PGSQL has finished

## 