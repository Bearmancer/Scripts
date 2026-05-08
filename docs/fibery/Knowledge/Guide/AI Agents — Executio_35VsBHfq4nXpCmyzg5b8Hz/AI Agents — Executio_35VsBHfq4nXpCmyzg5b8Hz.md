# Description

-----------------------------

# AI Agents — Execution Log System

## Purpose

Comprehensive audit trail and doom-loop prevention system integrated with Cline hooks and Fibery Execution Logs.

---

## Architecture

```
Cline Hook Pipeline
├── PreToolUse: DoomLoop.ps1 (query prior failures → allow/block)
├── Tool Executes
└── PostToolUse: ExecutionLog.ps1 (classify → filter → log to Fibery)
```

### Components

| File                   | Phase        | Purpose                                                   |
|------------------------|--------------|-----------------------------------------------------------|
| `Invoke-FiberyLog.ps1` | Shared       | REST API wrapper, command classifier, reasoning engine    |
| `ExecutionLog.ps1`     | PostToolUse  | Logs meaningful commands to Fibery `Repos/Execution Logs` |
| `DoomLoop.ps1`         | PreToolUse   | Blocks retry storms by querying prior failures            |
| `HookRuntime.ps1`      | Orchestrator | Dispatches concerns per phase                             |

---

## A+B Hybrid Issue Linking

* When `.state/current-issue.json` exists → command logs link to specified issue
* When file absent → logs are orphaned (link later via Fibery UI)
* Staleness check: if file > 30 min old → auto-orphan (prevent wrong issue links)

### Template

```json
{
  "issueId": "<fibery-entity-id>",
  "issueName": "<descriptive>",
  "setAt": "<ISO-8601 timestamp>"
}
```

---

## Command Classification

| Class        | Pattern                                                                                 | Action                               |
|--------------|-----------------------------------------------------------------------------------------|--------------------------------------|
| **Skip**     | `gci`, `ls`, `dir`, `cat`, `pwd`, `whoami`, `git status`, `git log`, `git diff`         | No log entry created                 |
| **Pipeline** | Contains pipe `\|`                                                                      | Logs summarized terminal action only |
| **Action**   | `npm install`, `docker build`, `git push`, `Set-Content`, `Remove-Item`, `curl -X POST` | Full log entry created               |

---

## Doom-Loop Prevention

### Fail-Count Threshold

* **3+ failures** in 15 min → **unconditional block** regardless of reason

### Reasoning-Based Gate

| Class              | Keywords                                              | Decision                                             |
|--------------------|-------------------------------------------------------|------------------------------------------------------|
| **Z** (Transient)  | timeout, retry-after, rate-limit, connection-reset    | Allow with backoff suggestion                        |
| **L** (Logic/Perm) | syntax, invalid, permission, auth, credential, denied | Block — retry won't help                             |
| **U** (Unknown)    | (empty or unmatched)                                  | 1 failure: allow / 2+ failures: block conservatively |

---

## Execution Log Schema (Repos/Execution Logs)

| Field                | Content                         |
|----------------------|---------------------------------|
| `Repos/Name`         | "Cmd: <first 80 chars>"         |
| `Repos/Command`      | Full command text               |
| `Repos/Error Return` | stderr output (first 500 chars) |
| `Repos/Status`       | Success / Failed                |
| `Repos/Reasoning`    | "\[Z/L/U\] <context>"           |
| `Repos/Timestamp`    | UTC ISO 8601                    |
| `Repos/Issue`        | Linked issue (A+B Hybrid)       |

---

## Skills Junction Map

```
C:\Users\Lance\.agent\skills\     ← canonical source
    ↑ junction
C:\Users\Lance\.cline\skills\     ← Cline's native skills dir
    ↑ junction
C:\Users\Lance\.config\kilo\skills\ ← Kilo's skills dir
```

---

## CPM Enforcement

Each issue links to its CPM chain:

* `Next Issue` / `Prev Issue` = doubly-linked list per parent
* `Sequence` = position in chain (0, 1, 2...)
* `Ticked` = completion gate (only set when `Validation` = PASS)

---

## Retry Intelligence

* Context-aware retry budgets (npm=5, git=3, curl=5, default=2)
* Exponential backoff detection (distinguishes thrashing from deliberate retries)
* Error pattern suggestions (ENOENT → npm install, EACCES → chmod, EADDRINUSE → kill)

*Last Verified: 2026-05-06*
