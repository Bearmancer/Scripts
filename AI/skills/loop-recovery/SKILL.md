---
name: loop-recovery
description: Systematic recovery procedures for stuck agents, repeated-fix loops, compounding errors, and doom loops. Invoke when you have emitted [LOOP DETECTED] or when two or more consecutive attempts at the same fix have failed.
---

# Loop Recovery Procedures

Follow these steps in order. Do not skip ahead.

## Step 1 — Declare State

Emit the following exactly:

## Step 1b — Persist State to Disk
```powershell
$sessionId = "recovery-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$logDir = ".kilo/logs/loop-debug/$sessionId"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
@{attempts=N; action="..."; expected="..."; actual="..."} | ConvertTo-Json -Compress | Out-File "$logDir/state.jsonl"
```
All recovery state must be written to `.kilo/logs/`. Compaction will drop session memory. Disks survive.
```
[LOOP RECOVERY INITIATED]
Attempts made: <N>
Last action taken: <one sentence>
What was expected: <one sentence>
What actually happened: <one sentence>
```

## Step 2 — Discard the Current Hypothesis

The root-cause model that produced the last two attempts is wrong. Explicitly discard it:
- Write: "Discarding hypothesis: [prior hypothesis]"
- Do not issue any further commands based on the discarded hypothesis.

## Step 3 — Re-read All Available Evidence From Scratch

Read the following in order, not from memory:
1. The original task description or user request.
2. The full current state of any file you modified (not a cached version).
3. The actual error output — captured with `2>&1` or equivalent, not inferred from a prior read.
4. Any logs, exit codes, or tool output produced by the last failed attempt.
5. The session change log (what was changed, from what, to what).

## Step 4 — Surface-Level Errors Are Symptoms

"Cannot connect," "file not found," "access denied," and "permission denied" are almost never the root cause. The root cause is named explicitly in the process's own stderr. If you have not yet read stderr directly, do that now before forming any new hypothesis.

## Step 5 — Form a New Hypothesis

Write the new hypothesis explicitly:
```
New hypothesis: The failure is caused by [specific root cause],
evidenced by [specific piece of evidence from Step 3].
```
If you cannot point to specific evidence, return to Step 3.

## Step 6 — State the Smallest Possible Test

Before making any change, state the minimum action that would confirm or refute the new hypothesis without side effects. Prefer read-only verification over mutations.

## Step 7 — Execute the Minimum Test, Then Report

Run the test. Report the result before taking any corrective action. Do not combine the test and the fix in the same operation.

## Step 8 — If Still Stuck After Two Full Recovery Cycles

Stop all forward progress and surface the situation to the user:
```
[LOOP RECOVERY EXHAUSTED]
I have attempted recovery twice without resolving the issue.
Evidence collected: <summary>
Current best hypothesis: <hypothesis>
I need your guidance before proceeding.
```
Do not attempt a third recovery cycle without user input.

## Appendix: Common Loop Triggers

**Timeout loops:** A timed-out operation will time out again if retried identically. Change strategy — manual extraction, pre-built binary, staged download, or different acquisition path. Check partial completion before restarting.

**Structured data corruption:** If a YAML/JSON/TOML file is misbehaving after edits, your string-based edit likely broke structure. Stop, restore from backup, and use a parser.

**Elevated execution silence:** `sudo`/RunAs/UAC failures produce no stderr to the calling process. "No output" is not success. Verify the effect directly on the system.

**Stale line numbers:** Numbers obtained from a previous command are invalid after any edit. Re-query before use.

**Config drift:** A service restart may have reloaded a config from a different location than the file you edited. Verify which config file the running service is actually reading.