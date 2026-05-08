# Description

-----------------------------

## Overview

Auto-converts Cline hooks to native VS Code Copilot hook format. Generates a bridge adapter (`cline-adapter.ps1`) and
hook configuration (`cline-hooks.json`) for Copilot to discover and invoke Cline hooks through a translation layer.

**Does NOT modify any Cline source files.** The output is entirely in the Copilot hooks directory (`~/.copilot/hooks`).

## Phase Mapping

| Cline Phase      | Copilot Event    | Status                  |
|------------------|------------------|-------------------------|
| TaskStart        | SessionStart     | ✓ Mapped                |
| PreToolUse       | PreToolUse       | ✓ Mapped                |
| PostToolUse      | PostToolUse      | ✓ Mapped                |
| TaskComplete     | Stop             | ✓ Mapped                |
| UserPromptSubmit | UserPromptSubmit | ✓ Mapped                |
| PreCompact       | PreCompact       | ✓ Mapped                |
| TaskResume       | —                | ✗ No Copilot equivalent |
| TaskCancel       | —                | ✗ No Copilot equivalent |

## Output Files

### `cline-hooks.json` (Copilot hook registry)

Copilot auto-discovers this file at `~/.copilot/hooks/cline-hooks.json`. Maps each Copilot event to a command that
invokes the adapter with the phase name.

### `cline-adapter.ps1` (protocol bridge)

* Receives Copilot wire format (JSON) on stdin
* Translates to Cline wire format
* Calls the corresponding Cline hook entrypoint (e.g., `TaskStart.ps1`)
* Translates Cline output back to Copilot wire format
* Outputs to stdout

**Tool name translation** (Copilot snake_case → Cline names):

* `run_in_terminal` → `execute_command`
* `create_file` → `write_to_file`
* `replace_string_in_file` → `replace_in_file`

**Field translation**:

* Copilot's `filePath` → Cline's `path`

## Parameters

```powershell
.\Convert-ClineHooksToCopilot.ps1 [-ClineHooksRoot <path>] [-OutputDir <path>] [-WhatIf]
```

* **ClineHooksRoot**: Path to Cline hooks (default: `$USERPROFILE\Documents\Cline\Hooks`)
* **OutputDir**: Output directory (default: `$USERPROFILE\.copilot\hooks`)
* **-WhatIf**: Dry-run

## Usage

```powershell
# Initial setup
.\Convert-ClineHooksToCopilot.ps1

# After modifying Cline hooks, regenerate
.\Convert-ClineHooksToCopilot.ps1
```

## Key Design

* **No in-place modification**: Output goes to Copilot hooks directory only
* **Baked-in root path**: Cline hooks root embedded in adapter at generation time
* **Wire format translation**: Bridges Copilot vs Cline hook contracts
* **Silent pass-through**: Unknown phases reply `{"continue":true}` without error
* **Non-blocking errors**: Exit code 1 treated as warning by Copilot

## See Also

* [Cline Hook System — Architecture, Protocol & Validation](fibery://guide/44)
