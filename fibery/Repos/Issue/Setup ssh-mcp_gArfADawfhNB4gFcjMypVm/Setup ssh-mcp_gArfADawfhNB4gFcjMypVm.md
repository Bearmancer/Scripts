# Description

-----------------------------

# Overview

Configured `ssh-mcp` for the OCI instance.

# Changes

* Checked `C:\Users\Lance\.ssh\config` for the OCI host configuration.
* Configured `ssh-mcp` in
  `C:\Users\Lance\AppData\Roaming\Code - Insiders\User\globalStorage\saoudrizwan.claude-dev\settings\cline_mcp_settings.json`
  pointing to `129.159.233.131` with the user `ubuntu` and the identity file `C:\Users\Lance\.ssh\oci`.
* Verified that the `c-q6-L0mcp0exec` tool executes correctly by running `ls -la` on the remote server.

# Plan

-----------------------------

# Prompt

-----------------------------

# Research

-----------------------------

# Validation

-----------------------------

# SSH MCP Verification - 2026-05-04

## Status: ✅ PASSING

## Tests Executed

### 1. Basic Connectivity (exec tool)

```
Command: echo 'SSH-MCP-CONNECTION-OK' && hostname && whoami && uptime
Result: SSH-MCP-CONNECTION-OK, media-server-vnic, ubuntu, up 3 days 9:30
```

### 2. Auth & Sudo Check

```
SSH Key Auth: ✅ (single key in authorized_keys)
Sudo: ✅ Passwordless sudo confirmed (sudo -n whoami → root)
```

### 3. sudo-exec Tool

```
Command: tailscale status
Result: ✅ Shows 3 peers (media-server-vnic, lance/Windows, moto-g84-5g)
Health: ⚠️ Tailscale DNS health warning (tracked in Issue #198)
```

### 4. Docker Access

```
Command: docker ps
Result: ✅ All 16 containers listed, all healthy
```

## Configuration

* Host: 129.159.233.131 (media-server-vnic)
* User: ubuntu
* Key: C:\\Users\\Lance.ssh\\oci
* Build: build/index.js exists

## Known Issues

* Tailscale DNS health warning (Issue #198, not blocking)
