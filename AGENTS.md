# Repository Execution Guide

> Execution pipeline and guide for all tasks in this repository.
> Plan: docs/plans/cpm.md | Standards: .kilo/rules/standards.md

---

## Directory Map

| Path                       | Purpose                                          |
| -------------------------- | ------------------------------------------------ |
| `docs/plans/cpm.md`        | CPM plan — single source of task state           |
| `docs/research/`           | Research & analysis documents                    |
| `docs/prompts/`            | Tiered execution prompts + active-task tracker   |
| `docs/knowledge/`          | Technical reference, verification records        |
| `docs/Fibery Export/`      | Fibery CSV export data for migration             |
| `.kilo/skills/`            | Agent skills                                     |
| `.kilo/rules/standards.md` | Coding standards — read before writing any code  |
| `.kilo/logs/`              | Runtime logs — not committed                     |
| `AGENTS.md`                | This file — Top level repository execution guide |

---

## MCP Servers

| Server     | Profile    | Provider                  | Purpose                 |
| ---------- | ---------- | ------------------------- | ----------------------- |
| SSH        | -          | `.kilo/mcp_settings.json` | Remote execution on OCI |
| fetch      | `default`  | Docker MCP Gateway        | Web content retrieval   |
| context7   | `default`  | Docker MCP Gateway        | Library documentation   |
| playwright | `default`  | Docker MCP Gateway        | Browser automation      |
| neon       | `database` | Docker MCP Gateway        | PostgreSQL migration    |

MCP servers are managed via Docker MCP Toolkit (`docker mcp`).
We use two primary profiles: `default` (for general tools) and `database` (for Neon PostgreSQL).

---

## Agent Strategy & Subagents

- **Single Responsibility Principle:** Fiercely adhere to single-responsibility workflows.
- **Maximize Subagent Deployment:** Deploy multiple subagents concurrently to analyze, explore, or execute non-dependent
  tasks. Never perform manual open-ended codebase exploration when subagents can run it in parallel.
- **Task Prompts:** Strict 1-to-1 mapping. Do not combine multiple tasks from cpm.md into one prompt execution.

---

## Task Lifecycle

```
BACKLOG → IN PROGRESS → VERIFY → DONE
```

- **One task at a time.** Make the smallest safe change set.
- **Check parallel groups** in cpm.md — Groups A and B can run concurrently via subagents.
- Tasks with HIGH float can be deferred without blocking the critical path.

---

## Execution Pipeline

### 1 — Orient

Before starting any task:

1. Open docs/plans/cpm.md — identify the next TODO task on the critical path.
2. Open docs/prompts/active-task.md — confirm win/fail gates for the task.
3. Read relevant docs/knowledge/\*.md only if the task requires it.
4. Read .kilo/rules/standards.md.

### 2 — Research (only if needed)

- Use subagents to concurrently read files, glob directories, and grep content.
- Resolve library ID → fetch docs for any library/framework/SDK question.
- Save findings to .kilo/logs/ — not to a new research/ directory.

### 3 — Execute

- Apply the smallest safe change set.
- PowerShell 7 only — never bash.
- Never repeat commands already in .kilo/logs/execution-log.jsonl.
- If a task involves multiple distinct steps, delegate to subagents.

### 4 — Verify

- Run the verifiable command from cpm.md or active-task.md for the task.
- Log result to .kilo/logs/.
- If verification fails → **stop, report, do not advance.**

### 5 — Advance

1. Mark task DONE in cpm.md.
2. If a win gate now passes, update active-task.md.
3. Delete any scratch notes — never leave parallel active docs.
4. Proceed to next CPM task (check Parallel Groups).

---

## Hygiene Rules

- Never create files outside .kilo/ for orchestration artifacts.
- Never create a research/ directory — use docs/research/ for findings, .kilo/logs/ for transient logs.
- Never archive — files are either active or deleted.

---

## Log Files

| File                           | Purpose                                            |
| ------------------------------ | -------------------------------------------------- |
| .kilo/logs/execution-log.jsonl | All terminal executions (stdout, stderr, exitCode) |
| .kilo/logs/session-files.json  | Per-task file tracking for lang checks             |

