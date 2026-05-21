# Description

-----------------------------

# Plan

-----------------------------

# Fibery Strict Pipeline & Hook Architecture

## 1. Pipeline States (Workflow Schema)

Define a State field on the `Repos/Issue` database with the following strict progression:

1. **Research**: Gathering initial context.
2. **Plan**: Formulating the execution plan.
3. **Prompt**: Assembling the final system prompt.
4. **Execution**: AI agent executing tools and tasks.
5. **Validation**: Evaluating execution output against success criteria.
6. **Ticked**: Final completed state.

## 2. Document Fields & Enforcement

The `Repos/Issue` database requires the following Rich Text (Document) fields:

* `Repos/Research`
* `Repos/Plan`
* `Repos/Prompt`

**Enforcement Logic (Strict Order):**\
The hook verifies field population progressively before advancing state or allowing general tool execution:

1. `Repos/Research` must be non-empty before progressing to Plan.
2. `Repos/Plan` must be non-empty before progressing to Prompt.
3. `Repos/Prompt` must be non-empty before progressing to Execution.\
   *Binary Success Criteria:* Execution phase is only unlocked when all three fields evaluate as non-empty.

## 3. PowerShell Hook (`fibery-intake-gate.ps1`)

The PreToolUse hook gates tool access to prevent deadlocks and enforce discipline.

* **ALLOW (Always)**: `tool_search`, `use_mcp_tool`, `access_mcp_resource`, and all `mcp_fibery_*` tools. This prevents
  the MCP deadlock trap.
* **BLOCK**: All other execution tools (e.g., execute_command, write_to_file) until Research, Plan, and Prompt are
  populated sequentially.
* **SessionStart**: Clear previous hook state and log the required workflow instructions for the orchestrator agent.

## 4. Subagent Delegation Schema

* The Orchestrator agent creates `Repos/Issue` entries and uses the `Repos/Parent Issue` self-relation to spawn
  sub-issues.
* The Orchestrator populates Research, Plan, and Prompt for the parent.
* The subagent is passed ONLY the `Repos/Prompt` field of its assigned sub-issue, restricting its scope and preventing
  context overflow.

## 5. Tailored Prompt Template

Every `Repos/Prompt` must adhere to this template:

* **Pass Criteria**: Binary, terminal-verifiable state.
* **Current State**: Snapshot of the relevant environment.
* **Numbered Steps**: Explicit execution commands.
* **Fail Criteria**: Specific failure definitions (e.g., timeout, permission denied).
* **Scope Boundary**: Strict limits on what files/tools the agent may touch.

# Prompt

-----------------------------

# Execution Prompt: Fibery Workflow & Hooks Architecture

## Pass Criteria

- [ ] Output terminal log confirms `fibery-intake-gate.ps1` allows `tool_search` and `mcp_fibery_*` commands.
- [ ] Terminal test simulating `execute_command` returns `deny` with reason specifying the missing Fibery field (
  Research/Plan/Prompt).

## Current State

* Agents bypass Fibery documentation, hallucinate completions, and deadlock due to blocked MCP tool lookups.

## Steps

1. Write the provided PowerShell script template into `.copilot/session/fibery-intake-gate.ps1`.
2. Ensure the hook logic processes the JSON payload for `eventName` (`PreToolUse`) and `toolName`.
3. Implement the bypass for `tool_search`, `use_mcp_tool`, `access_mcp_resource`, and regex `^mcp_fibery_`.
4. Read the `fibery-intake-state.json` file. If state variables (`pendingResearch`, `pendingPlan`, `pendingPrompt`)
   evaluate to `$true`, output a `deny` decision JSON payload.
5. Test the hook locally using a mock JSON payload piped into the script.

## Fail Criteria

- [ ] Hook throws a PowerShell parsing error.
- [ ] Hook blocks `tool_search`.
- [ ] Hook allows `execute_command` when `pendingPlan` is `$true`.

## Scope

* You may ONLY write to `.copilot/session/fibery-intake-gate.ps1` and `.copilot/session/fibery-intake-state.json`.
* You may NOT modify any actual Fibery database fields from within the hook.

# Research

-----------------------------

# Validation

-----------------------------

