# Description

-----------------------------

## Root Cause

HookRuntime.ps1:51 had unquoted string: \[Console\]::Error.WriteLine( \[$Timestamp][$Level\] $Message)\
PowerShell interpreted \[ as type cast on $Timestamp (invalid type). Parse error blocked all hooks.

## Failure Chain

1. HookRuntime.ps1 parse error
2. PreToolUse.ps1 / PostToolUse.ps1 dot-source fails silently
3. Invoke-HookPipeline never defined - all hooks blocked
4. DoomLoop, ExecutionLog, Tracking, GuardRails, lang/ all offline

## Secondary Issues

* HookRuntime.ps1:57 unquoted string
* 6 empty catch {} blocks across HookRuntime, Tracking, GuardRails, lang/ps1.ps1
* Tracking.ps1 called Get-UtcTimestamp without self-contained definition

## Fixes Applied

1. Quoted strings on lines 51 and 57
2. Replaced empty catch {} with Write-Error logging
3. Inlined Get-UtcTimestamp in Tracking.ps1
4. PSScriptAnalyzer: 0 ERRORs (down from 96 total issues)

## AI Agent Chat Log Locations

* Cline: .cline/data/tasks/\*/api_conversation_history.json (864KB)
* Cline CLI: .cline/data/logs/cline-cli.1.log (14MB)
* Copilot: .copilot/session-state/\*/events.jsonl (11.3MB)
* Kilo: .local/state/kilo/prompt-history.jsonl (31KB)
* Gemini Antigravity: .gemini/antigravity/conversations/\*.pb (33.8MB)
* Gemini CLI: .gemini/tmp/lance/chats/\*.jsonl (1.2MB)
* Cursor: .cursor/chats/\*/store.db + WAL (115KB)

## AI Agent Chat Log Locations (Complete Map)

| \# | Agent              | Path                                                  | Format          | Size      |
|----|--------------------|-------------------------------------------------------|-----------------|-----------|
| 1  | Cline tasks        | `~/.cline/data/tasks/*/api_conversation_history.json` | JSON            | \~864 KB  |
| 2  | Cline CLI          | `~/.cline/data/logs/cline-cli.1.log`                  | JSON-structured | \~14 MB   |
| 3  | Copilot            | `~/.copilot/session-state/*/events.jsonl`             | JSONL + SQLite  | \~11.3 MB |
| 4  | Copilot logs       | `~/.copilot/logs/process-*.log`                       | Text            | \~62 KB   |
| 5  | Kilo               | `~/.local/state/kilo/prompt-history.jsonl`            | JSONL           | \~31 KB   |
| 6  | Gemini Antigravity | `~/.gemini/antigravity/conversations/*.pb`            | Protobuf        | \~33.8 MB |
| 7  | Gemini CLI         | `~/.gemini/tmp/lance/chats/*.jsonl`                   | JSONL           | \~1.2 MB  |
| 8  | Cursor             | `~/.cursor/chats/*/store.db` + WAL                    | SQLite          | \~115 KB  |

### Top 3 by Volume

1. Gemini Antigravity: 33.8 MB (9 protobuf conversations)
2. Cline CLI log: 14 MB (API request/response trace)
3. Copilot session: 10 MB single session (JSONL)
