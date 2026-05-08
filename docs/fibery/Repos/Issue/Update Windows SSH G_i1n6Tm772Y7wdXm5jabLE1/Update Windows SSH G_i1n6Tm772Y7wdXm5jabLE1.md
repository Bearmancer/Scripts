# Description

-----------------------------

# Plan

-----------------------------

# Windows SSH and Tailscale SSH Update Plan

## Overview

Update existing SSH documentation and create new Tailscale-specific guides based on current Windows SSH configuration (
already Tailscale-only listener).

## Execution Steps

### Phase 1: Update Existing Guide

**Target**: "SSH: Inbound Windows SSH for Remote Connection" guide

**Updates**:

1. Add new section: "Tailscale-Only Configuration"
2. Document current `sshd_config` state
3. Add subsection: "Preventing Hard Lock Scenarios"
4. Add recovery procedures table
5. Add verification step for Tailscale-only listener

### Phase 2: Create New Tailscale SSH Guide

**Title**: "SSH: Tailscale-Only Windows SSH Configuration"\
**Location**: Knowledge/Guide database

**Sections to include**:

1. **Overview**: What Tailscale-only SSH means and why it's secure
2. **Prerequisites**: Tailscale installed, Windows OpenSSH Server
3. **Configuration Steps**:
	* Set `ListenAddress` to Tailscale IP only
	* Disable password auth
	* Enable pubkey auth
4. **Tailscale MagicDNS Setup**: Use hostname instead of IP
5. **Testing**: Verify SSH works only via Tailscale
6. **Emergency Recovery**: Procedures to re-enable public interface access
7. **Verification Checklist**

### Phase 3: Update SSH Setup Script Guide

**Updates**:

1. Add note about Tailscale-only listener being default recommendation
2. Document that script assumes Tailscale is available for Windows SSH
3. Add warning: "Do not use this script if Tailscale is unavailable"

### Phase 4: Update OCI Artifacts

**Action**:

1. Add deprecation notice at top: "DEPRECATED: This artifact is historical. See Fibery Knowledge/Guide for current
   procedures."
2. Reference updated guides instead of this artifact

### Phase 5: Create Emergency Recovery Procedure

**Location**: Append to new Tailscale SSH guide (Phase 2)

**Content**:

```powershell
# Emergency: Re-enable SSH on all interfaces
# Run from local console or RDP if Tailscale fails

$sshdConfig = "C:\ProgramData\ssh\sshd_config"

# Backup current config
Copy-Item $sshdConfig "$sshdConfig.backup-$(Get-Date -Format 'yyyyMMddTHHmmss')"

# Remove ListenAddress restriction (listen everywhere)
(Get-Content $sshdConfig) -notmatch '^ListenAddress' | Set-Content $sshdConfig

# Restart sshd
Restart-Service sshd

# Verify
netstat -ano | Select-String ':22.*LISTENING'
```

## Dependencies

| Step    | Depends On | Notes                            |
|---------|------------|----------------------------------|
| Phase 1 | None       | Existing guide exists            |
| Phase 2 | Phase 1    | Builds on updated existing guide |
| Phase 3 | None       | Independent, can run parallel    |
| Phase 4 | None       | Simple deprecation notice        |
| Phase 5 | Phase 2    | Part of new guide                |

## Success Criteria

- [ ] Guide updated with Tailscale section and hard lock prevention
- [ ] New Tailscale SSH guide created in Knowledge/Guide database
- [ ] Setup script guide updated with Tailscale recommendations
- [ ] OCI artifacts marked as deprecated
- [ ] Emergency recovery procedure documented
- [ ] All guides reference each other correctly
- [ ] No conflicting information across guides

## Estimated Time

* Phase 1: 10 minutes
* Phase 2: 15 minutes
* Phase 3: 5 minutes
* Phase 4: 5 minutes
* Phase 5: 5 minutes (included in Phase 2)
* **Total**: 35-40 minutes

# Prompt

-----------------------------

# Execution Prompt

## Task

Update Windows SSH documentation with Tailscale SSH research and ensure no hard lock scenarios are documented.

## Current State

* Windows SSH server running, listening ONLY on Tailscale IP (`100.106.89.100:22`)
* Fibery guides exist: "Windows SSH and OCI SSH Setup Script" (ID: 4), "SSH: Inbound Windows SSH for Remote
  Connection" (ID: 7)
* Research completed and documented in Research field
* Plan created with 4 phases of execution
* OCI artifacts exist in `.copilot/artifacts/` but are outdated

## Target State

* Guide 7 updated with Tailscale section and hard lock prevention procedures
* New "SSH: Tailscale-Only Windows SSH Configuration" guide created
* Guide 4 updated with Tailscale recommendations
* OCI artifacts marked as deprecated
* Emergency recovery procedures documented

## Scope Boundary

* Update only the specified guides (IDs 4, 7)
* Create one new guide for Tailscale SSH
* Add deprecation notices to OCI artifacts (do not delete)
* Do NOT modify actual SSH configuration (documentation only)
* Do NOT create new scripts or binaries

## Verification Steps

After each phase, verify:

1. Phase 1: Guide 7 contains new Tailscale section with hard lock table
2. Phase 2: New guide exists in Knowledge/Guide with correct sections
3. Phase 3: Guide 4 contains Tailscale notes
4. Phase 4: OCI artifacts have deprecation notice
5. Phase 5: Emergency procedure is in new guide
6. Final: All guides link correctly, no conflicts

## Completion Evidence

* Updated guides can be queried and show new content
* New guide appears in Knowledge/Guide list
* OCI artifacts have deprecation headers
* Validation field in this issue completed with summary
* Issue marked as Ticked

# Research

-----------------------------

## SSH FS Setup (Phase 2)

### SSH Agent Configuration

* Windows OpenSSH agent running with `ssh-ed25519` key loaded from `C:\Users\Lance\.ssh\oci`
* SSH config: Host `oci` = 129.159.233.131, user ubuntu, IdentityFile `~/.ssh/oci`
* Agent-based auth verified: `ssh -T oci` succeeds without passphrase prompt

### Config Files Updated

#### Cline MCP Settings (`cline_mcp_settings.json`)

* Removed `--key` flag from `ssh-mcp` server config
* SSH MCP now relies on SSH agent for authentication
* No key path in config

#### Kilo MCP Config (`kilo.jsonc`)

* Added `ssh-oci` MCP server pointing to local `ssh-mcp` build
* Uses agent-based auth (no `--key` flag)
* Timeout: 120s

### Key Safety

* Verified: no `-----BEGIN.*PRIVATE KEY-----` patterns in modified config files
* All auth via SSH agent forwarding only

### OCI Host Details

* Host: 129.159.233.131
* User: ubuntu
* OS: Ubuntu 24.04.4 LTS (GNU/Linux 6.17.0-1010-oracle aarch64)
* Connection confirmed working via agent

# Validation

-----------------------------

# Validation (2026-04-30)

## All completion criteria met:

1. All 7 fragmented SSH guides consolidated into single Master Guide (#18)
2. Tailscale SSH research incorporated into Master Guide Section 5
3. Deprecated guides documented with cross-references
4. CPM recovery schedule created (Issue #144)
5. Duplicate tool_call_id workaround documented (Guide #19)
6. Kilo terminal startup fix documented (Guide #20)
7. DeepSeek batch_tool: false fix applied in kilo.jsonc

**TICKED: true**
