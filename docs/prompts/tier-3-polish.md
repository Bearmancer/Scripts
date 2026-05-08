# Tier 3 — Polish Prompt

> **Plan:** `.kilo/plans/cpm.md` | **Active Task:** `.kilo/prompt/active-task.md`
> **Tasks:** T10 (Docker MCP Gateway), T11 (Knowledge Base Fixes), T12 (Terminal Fixes)
> **Parallel Groups:** T10 after T01; T11+T12 after T09

---

## T10 — Docker MCP Gateway

### Objective

Replace local MCP server management with Docker MCP Toolkit.

### Actions

1. **Verify Docker Desktop 4.62+** with MCP Toolkit enabled in Beta features
2. **Create profile:**
    ```
    docker mcp profile create scripts-dev
    ```
3. **Add servers:**
    - `fetch` (mcp-server-fetch)
    - `playwright` (@playwright/mcp)
    - `context7` (@upstash/context7-mcp)
4. **Configure gateway in `mcp_settings.json`** to use Docker profile
5. **Remove old VS Code `mcp.json` entries** for migrated servers
6. **Remove local npm packages** for servers now in Docker

### Win Gate

```
docker mcp profile show scripts-dev
```

Lists 3+ servers.

---

## T11 — Knowledge Base Fixes

### Objective

Fix fabricated claims, update for current architecture.

### Actions

1. **Fix `architecture.md`:**
    - Remove "Zero-Ceremony" EF Core claims
    - Replace with real EF10/EF11 PostgreSQL features
    - Update dependency graph to show PostgresService, not GoogleSheetsService

2. **Fix MCP definition:**
    - Update to reference JSON-RPC protocol
    - Note Docker MCP Gateway as canonical manager

3. **Update `standards.md`:**
    - Already has cmd.exe awareness and psql invocation patterns

4. **Create verification index:**
    - Add `.kilo/knowledge/verification/README.md` listing verification records

### Win Gate

```
Select-String "Zero-Ceremony" .kilo/knowledge/architecture.md
```

Returns 0 matches.

---

## T12 — Terminal & Environment Fixes

### Objective

Fix Rider terminal freeze, clean orphaned artifacts, wire up logging.

### Actions

1. **Fix PowerShell profile logging:**
    - `powershell/Microsoft.PowerShell_profile.ps1` — remove unused `Invoke-LoggedCommand`, add actual `prompt` function
      override for command logging
    - Ensure `.kilo/logs/execution-log.jsonl` receives entries

2. **Fix Rider terminal config:**
    - Edit `terminal-local.xml`:
        ```xml
        <option name="shellArguments" value="-NoLogo -NoProfile" />
        ```

3. **Delete orphaned artifacts:**
    - `.kilo/research/` if exists
    - `merged.md` if exists
    - Any remaining `.github/` orchestrator artifacts

### Win Gate

```
Test-Path .kilo/research
```

Returns `False`.

```
Test-Path merged.md
```

Returns `False`.

