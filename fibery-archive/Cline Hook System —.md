# Description

-----------------------------

# Cline Hook System — Architecture, Protocol & Validation

> See also: \[Knowledge/Guide #30 — PowerShell Cline Hook Best Practices\] for runtime hardening rules (§as-hashtable,
> §no-type-constraint, §escape-order, §single-hardening).

---

## 1. Architecture Overview

Language-first, minimal schema, auto-install hook system for Cline. All hooks live in `~/Documents/Cline/Hooks/`.

**Design principle:** Guidance over termination. Hooks guide the agent to self-correct; they only block (`cancel=true`)
for genuinely destructive operations.

### File Layout (Target State)

```
~/Documents/Cline/Hooks/
├── common.ps1              # Shared lib: stdin reader, output emitter, dispatcher
├── hooks.proto             # Wire-format schema (camelCase)
├── Test-Hooks.ps1          # Offline validator / CI gate
├── PreToolUse.ps1          # 2-line delegator → Invoke-HookPipeline
├── PostToolUse.ps1         # 2-line delegator → Invoke-HookPipeline
├── TaskStart.ps1           # 2-line delegator → Invoke-HookPipeline
├── TaskComplete.ps1        # 2-line delegator → Invoke-HookPipeline
├── TaskResume.ps1          # 2-line delegator (passthrough)
├── TaskCancel.ps1          # 2-line delegator (passthrough)
├── UserPromptSubmit.ps1    # 2-line delegator (passthrough)
├── PreCompact.ps1          # 2-line delegator (passthrough)
├── concerns/
│   ├── guard.ps1           # Pre-tool safety (banned commands + overwrite advisory)
│   ├── track.ps1           # Post-tool file tracker → .state/session-files.json
│   └── fibery-inject.ps1   # TaskStart: injects Fibery Delivery Pipeline context
└── lang/
    ├── py.ps1              # Python (ruff format + ruff check)
    ├── ps1.ps1             # PowerShell (AST + PSScriptAnalyzer)
    ├── json.ps1            # JSON / JSONC / JSONL (prettier + parse)
    ├── md.ps1              # Markdown / MDX / .agent.md / .prompt.md (prettier + markdownlint)
    ├── yaml.ps1            # YAML / YML (prettier + yamllint)
    ├── sh.ps1              # Shell / Bash (shfmt + shellcheck)
    ├── toml.ps1            # TOML (taplo fmt + taplo lint)
    └── java.ps1            # Java (google-java-format + checkstyle)
```

> **Live bug (as of 2026-05-04):** `$HookConcerns` in `common.ps1` references `'guard.ps1'`, `'track.ps1'`,
`'fibery-inject.ps1'` but the actual files are named `GuardRails.ps1`, `Tracking.ps1`, `FiberyInjection.ps1` at the root
> level. These concerns are silently skipped at runtime. Fix = rename files and move to `concerns/` subdir, then prefix
> paths in `$HookConcerns` with `'concerns/'`.

### Dispatch Hashtable

All routing in `common.ps1` — no switch, no manifest, no JSON config:

```powershell
$HookConcerns = @{
    PreToolUse   = @('concerns/guard.ps1')
    PostToolUse  = @('concerns/track.ps1')
    TaskStart    = @('concerns/fibery-inject.ps1')
    TaskComplete = @('lang/py.ps1','lang/ps1.ps1','lang/json.ps1','lang/md.ps1',
                     'lang/yaml.ps1','lang/sh.ps1','lang/toml.ps1','lang/java.ps1')
    TaskResume   = @()
    TaskCancel   = @()
    PreCompact   = @()
}
```

Adding a language = append one string to `TaskComplete` + create the `.ps1` file under `lang/`.

---

## 2. Concern Files

### concerns/guard.ps1 — PreToolUse Safety

Two phases:

**Phase 1 — Shell command blocking (`cancel=true`):** Matches command strings against banned patterns:

* Shell redirect (`>`, `>>`)
* Destructive git (`push --force`, `reset --hard`, `clean -f/d/x`)
* Recursive force-delete (`rm -rf`, `Remove-Item -Recurse -Force`)
* Fork bomb, `chmod 777`, `curl | bash`
* `/dev/null` on Windows (creates literal file)
* Env var secret leak (`export SECRET=...`)

**Phase 2 — Destructive edit recovery (advisory `contextModification`):** When `write_to_file` overwrites an existing
file, injects undo guidance — `git checkout path` for git-tracked files, backup suggestion for untracked. Also blocks
writes to home root (`$USERPROFILE` directly).

### concerns/track.ps1 — PostToolUse File Tracking

Records file paths from `write_to_file` / `replace_in_file` into `.state/session-files.json`, keyed by `taskId`. Fields:
`path`, `extension`, `tool`. Staleness purge: entries older than 24 hours removed on each write. Never returns
`cancel=true`.

### concerns/fibery-inject.ps1 — TaskStart Context Injection

Injects the Fibery Delivery Pipeline workflow reminder into `contextModification` on every new task start:

```powershell
$ContextMod = "You must strictly follow the Fibery Delivery Pipeline workflow. To do this, open and execute the steps in Workflows/fibery-delivery-pipeline.md before starting any other work."
return @{ cancel = $false; errorMessage = ''; contextModification = $ContextMod }
```

### lang/\*.ps1 — TaskComplete Format+Lint

| File       | Extensions                               | Formatter            | Linter           |
| ---------- | ---------------------------------------- | -------------------- | ---------------- |
| `py.ps1`   | `.py`                                    | `ruff format`        | `ruff check`     |
| `ps1.ps1`  | `.ps1`, `.psm1`                          | PowerShell AST       | PSScriptAnalyzer |
| `json.ps1` | `.json`, `.jsonc`, `.jsonl`              | `prettier`           | JSON parse       |
| `md.ps1`   | `.md`, `.mdx`, `.agent.md`, `.prompt.md` | `prettier`           | `markdownlint`   |
| `yaml.ps1` | `.yaml`, `.yml`                          | `prettier`           | `yamllint`       |
| `sh.ps1`   | `.sh`, `.bash`                           | `shfmt`              | `shellcheck`     |
| `toml.ps1` | `.toml`                                  | `taplo fmt`          | `taplo lint`     |
| `java.ps1` | `.java`                                  | `google-java-format` | `checkstyle`     |

Each lang file: reads `.state/session-files.json`, filters to its extensions, auto-installs tool if missing (`winget`
first, then language-native fallback), formats in-place, lints, returns `$null` (clean) or warning string. Never
`cancel=true`.

---

## 3. Wire Protocol

Cline invokes hooks as child processes, scanning `~/Documents/Cline/Hooks/` for `.ps1` files matching event names. Each
hook receives one JSON line via stdin and must emit exactly one JSON line via stdout. Stderr is free-form for logging.

### stdin: HookInput (flattened)

```json
{
  "taskId": "abc123",
  "hookName": "PreToolUse",
  "clineVersion": "3.82.0",
  "timestamp": "1736654400000",
  "workspaceRoots": ["/path/to/project"],
  "userId": "user_123",
  "model": { "provider": "openrouter", "slug": "anthropic/claude-sonnet-4.5" },
  "preToolUse": {
    "tool": "write_to_file",
    "parameters": { "path": "src/foo.java", "content": "..." }
  }
}
```

Hook-specific data is at the **root level** (`preToolUse`, `postToolUse`, etc.), NOT in a `data` wrapper. The proto file
is internal gRPC only; the child-process bridge uses flattened JSON.

### stdout: HookOutput

```json
{"cancel": false, "errorMessage": "", "contextModification": ""}
```

Fields: `cancel` (bool), `errorMessage` (string), `contextModification` (string — adds text to Cline's system prompt for
subsequent turns).

Non-empty `errorMessage` with `cancel=false` triggers "Proceed anyway?" dialog — use sparingly.

### PostToolUse Extra Fields

```json
{
  "postToolUse": {
    "tool": "write_to_file",
    "parameters": { "path": "..." },
    "success": true,
    "result": "...",
    "durationMs": 234
  }
}
```

Note: `durationMs` on the wire (not `execution_time_ms` as in the proto).

### Hook Event Types

| File                   | Event              | Fires When                                                                |
| ---------------------- | ------------------ | ------------------------------------------------------------------------- |
| `PreToolUse.ps1`       | pre_tool_use       | Before any tool executes. Can cancel.                                     |
| `PostToolUse.ps1`      | post_tool_use      | After tool completes. Receives `success`, `result`, `durationMs`.         |
| `UserPromptSubmit.ps1` | user_prompt_submit | User submits a new prompt.                                                |
| `TaskStart.ps1`        | task_start         | New task/session starts.                                                  |
| `TaskResume.ps1`       | task_resume        | Existing task resumed.                                                    |
| `TaskCancel.ps1`       | task_cancel        | Task cancelled.                                                           |
| `TaskComplete.ps1`     | task_complete      | Task finishes.                                                            |
| `PreCompact.ps1`       | pre_compact        | Before context compaction. Receives `contextJsonPath` + `contextRawPath`. |

### Discovery

* **Windows:** Cline scans for `<HookName>.ps1` files. Any `.ps1` matching an event name is auto-discovered. No
  registration needed.
* **Hooks directory:** `~/Documents/Cline/Hooks/` (global) or `<workspace>/.clinerules/hooks/` (per-project).

---

## 4. State File

`.state/session-files.json` structure:

```json
{
  "task-abc123": {
    "lastModified": "2026-05-04T01:desktop_computer:00Z",
    "files": [
      { "path": "/project/src/main.py", "extension": ".py", "tool": "write_to_file" }
    ]
  }
}
```

* Written by `track.ps1` on every PostToolUse
* Read by lang files at TaskComplete
* Purged after TaskComplete (task entry removed; file deleted if empty)
* Stale entries (>24h) cleaned on next PostToolUse

---

## 5. common.ps1 Functions

| Function              | Purpose                                                                    |
| --------------------- | -------------------------------------------------------------------------- |
| `Read-HookStdin`      | §single-hardening: sole input boundary. Returns `[hashtable]`.             |
| `Write-HookOutput`    | Emits `{"cancel":…,"errorMessage":…,"contextModification":…}` to stdout.   |
| `Write-HookDeny`      | Sugar: `Write-HookOutput -Cancel $true -ErrorMessage $Reason`.             |
| `Write-HookAllow`     | Sugar: `Write-HookOutput -Cancel $false`.                                  |
| `Write-HookCrash`     | Crash handler: logs to stderr, emits `{"cancel":false,"errorMessage":""}`. |
| `Write-HookLog`       | Logs timestamped message to stderr only.                                   |
| `Invoke-HookPipeline` | Reads stdin, dispatches to concerns, aggregates results.                   |
| `Read-SessionState`   | Reads `.state/session-files.json` for a given `taskId`.                    |

---

## 6. Customization Architecture (Module Roles)

| Module        | Role                                                                      | Location                        |
| ------------- | ------------------------------------------------------------------------- | ------------------------------- |
| **Workflows** | Executable step-by-step procedures with tool-execution tags               | `Workflows/*.md`                |
| **Hooks**     | Event-driven PowerShell triggered by Cline lifecycle events               | `Hooks/*.ps1`                   |
| **Rules**     | Always-on guardrails, non-negotiable constraints                          | `Rules/*.md` / `~/.clinerules/` |
| **Skills**    | On-demand domain expertise, invoked by LLM via `description:` frontmatter | `~/.copilot/skills/*/SKILL.md`  |
| **Subagents** | Parallelized exploratory processes, out-of-band research                  | Via `<use_subagents>`           |
| **Ignore**    | Visibility scoping and exclusion masking                                  | `.clineignore`                  |

**Discrepancies (local vs official docs.cline.bot):**

1. Hook payload uses flattened JSON on the wire, not the gRPC `camelCase` proto wrapper.
2. Auto-trigger of workflows requires `contextModification` in `TaskStart` (no native auto-run).
3. Subagents cannot write directly to Fibery; findings must be aggregated by the primary agent.

---

## 7. Validator (Test-Hooks.ps1)

```powershell
pwsh -NoProfile -File Test-Hooks.ps1
# Flags: -SkipSyntax -SkipStructure -SkipDryRun -SkipJson -ShowStderr
```

Exits 0 on pass, non-zero on any failure. CI-friendly.

### Validation Sections

| Section                         | What It Checks                                                                      |
| ------------------------------- | ----------------------------------------------------------------------------------- |
| **File Discovery**              | `common.ps1`, all entry points, concern files, `hooks.proto` exist                  |
| **Syntax (AST)**                | All `.ps1` parsed via `[Parser]::ParseFile()` — catches syntax errors pre-execution |
| **§no-type-constraint**         | Scans all scripts for `[hashtable]` in `param()` — crashes before `try/catch`       |
| **Reserved variable shadowing** | Checks `param()` blocks for `$Event`, `$Input`, `$Error`, etc.                      |
| **§as-hashtable**               | All `ConvertFrom-Json` calls include `-AsHashtable`                                 |
| **Write-HookCrash order**       | Verifies function is defined before it's called in `common.ps1`                     |
| **Manual JSON building**        | Verifies `Write-HookOutput` uses manual JSON, not `ConvertTo-Json`                  |
| **Entry point structure**       | Each entry point dot-sources `common.ps1` and calls `Invoke-HookPipeline`           |
| **Concern params**              | Each concern accepts `$Payload` and `$SessionFile` params                           |
| **Dry-run execution**           | Pipes sample JSON into each entry point, validates output structure                 |
| **JSON protocol conformance**   | Emitted fields matched against `hooks.proto`                                        |

### Required common.ps1 Functions (Validator Asserts)

`Read-HookStdin`, `Write-HookOutput`, `Write-HookDeny`, `Write-HookAllow`, `Write-HookCrash`, `Write-HookLog`,
`Invoke-HookPipeline`, `Read-SessionState`

---

## 8. Adding a New Language

1. Create `lang/<ext>.ps1` following the template: `Read-SessionState` → filter extensions → auto-install tool →
   format → lint → return `$null` or warning string.
2. Append `'lang/<ext>.ps1'` to the `TaskComplete` array in `$HookConcerns` in `common.ps1`.
3. Run `Test-Hooks.ps1` to validate.

## 9. Best Practices (Runtime)

1. `stdout` = always exactly 1 compact JSON line. Use `Write-HookOutput`, never `ConvertTo-Json`.
2. Log only to `stderr` via `Write-HookLog` or `[Console]::Error.WriteLine(...)`.
3. State persistence via filesystem — hooks are child processes with no shared memory.
4. Fail-open on missing tools (formatter not on PATH → log + allow, never block workflow).
5. Keep hook runtime <100ms. Use `Parser::ParseFile` for syntax checks (no process spawn).
6. De-bounce lang checks: only fire on `write_to_file`/`replace_in_file`, match by extension.
