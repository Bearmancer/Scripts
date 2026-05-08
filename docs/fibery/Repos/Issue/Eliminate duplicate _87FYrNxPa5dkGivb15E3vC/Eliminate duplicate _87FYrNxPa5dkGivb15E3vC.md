# Description

-----------------------------

# Plan

-----------------------------

# Plan: Eliminate Duplicate WireGuard Tunnel in qBittorrent

## Overview

Remove qBittorrent's standalone WireGuard tunnel and route its traffic through gluetun using Docker's
`network_mode: "service:gluetun"` — the same pattern already used by FlareSolverr.

## Steps

### 1. Edit `/data/config/docker-compose.yml`

#### qBittorrent service — REMOVE these:

* `entrypoint` block (entire custom WireGuard setup script)
* `networks` section (incompatible with network_mode)
* `dns` section (incompatible with network_mode)
* `ports` section (ports move to gluetun)
* `cap_add` (no longer needs NET_ADMIN)
* `devices` (no longer needs /dev/net/tun)
* Volume mount `./qbittorrent/wireguard:/etc/wireguard:ro`

#### qBittorrent service — ADD:

* `network_mode: "service:gluetun"`
* Keep all other environment variables, volume mounts (config, data), and the base image

#### gluetun service — ADD ports:

* `8080:8080/tcp` (qBittorrent WebUI)
* `56789:56789/tcp` (qBittorrent torrent port)
* `56789:56789/udp` (qBittorrent torrent port)

### 2. Remove WireGuard config directory

```bash
rm -rf /data/config/qbittorrent/wireguard/
```

### 3. Recreate containers

```bash
cd /data/config
docker compose up -d qbittorrent
docker compose up -d gluetun
```

This forces recreation of both containers with the new config.

### 4. Verify

* `docker exec qbittorrent curl -s ifconfig.me` → returns VPN IP `91.148.228.78`
* `docker exec gluetun wget -qO- http://localhost:9999/v1/publicip/ip` → returns VPN IP
* qBittorrent WebUI accessible at `http://<host>:8080`
* qBittorrent torrent port 56789 reachable
* `docker exec qbittorrent ip addr` → no wg0 interface present

## Rollback

If anything breaks, revert the compose changes and `docker compose up -d` again. The WireGuard config dir should be
backed up before deletion.

## Risk Assessment

* **Risk: LOW** — only affects Docker container networking
* Host SSH (port 22) and Tailscale are completely unaffected
* Pattern already proven in this compose file by FlareSolverr

# Prompt

-----------------------------

# Execution Prompt: Eliminate Duplicate WireGuard Tunnel in qBittorrent

You are modifying the Docker Compose file at `/data/config/docker-compose.yml` on a remote server via SSH.

## Context

qBittorrent currently runs its own WireGuard tunnel using the same credentials as gluetun — a protocol conflict. The fix
is to remove qBittorrent's WireGuard stack and share gluetun's network namespace, exactly like FlareSolverr already
does.

## Step 1 — Read current compose file

```bash
cat /data/config/docker-compose.yml
```

Study the full file. Identify the `qbittorrent` and `gluetun` service blocks.

## Step 2 — Back up the compose file and WireGuard config

```bash
cp /data/config/docker-compose.yml /data/config/docker-compose.yml.bak.$(date +%Y%m%d%H%M%S)
cp -r /data/config/qbittorrent/wireguard /data/config/qbittorrent/wireguard.bak.$(date +%Y%m%d%H%M%S) 2>/dev/null || true
```

## Step 3 — Edit the compose file

### In the `qbittorrent` service, REMOVE:

* The entire `entrypoint` block
* `networks` section
* `dns` section
* `ports` section
* `cap_add` section
* `devices` section
* The volume mount for `./qbittorrent/wireguard:/etc/wireguard:ro`

### In the `qbittorrent` service, ADD:

```yaml
      network_mode: "service:gluetun"
```

Place it at the same indentation level as `image:`, `environment:`, etc.

### In the `gluetun` service `ports` section, ADD:

```yaml
      - "8080:8080/tcp"
      - "56789:56789/tcp"
      - "56789:56789/udp"
```

These are qBittorrent's WebUI and torrent ports, now published through gluetun.

### Keep everything else unchanged:

* qBittorrent's `image`, `container_name`, `environment` (except any wireguard-related vars), `volumes` (config + data),
  `restart` policy
* All other services in the file

## Step 4 — Remove old WireGuard config directory

```bash
rm -rf /data/config/qbittorrent/wireguard/
```

## Step 5 — Recreate affected containers

```bash
cd /data/config && docker compose up -d --force-recreate qbittorrent gluetun
```

## Step 6 — Verify

Run these checks and report results:

```bash
# qBittorrent should have NO wg0 interface
docker exec qbittorrent ip addr 2>/dev/null || docker exec qbittorrent ifconfig 2>/dev/null

# qBittorrent should exit via VPN IP
docker exec qbittorrent curl -s --max-time 10 ifconfig.me

# gluetun public IP should match
docker exec gluetun wget -qO- http://localhost:9999/v1/publicip/ip

# qBittorrent WebUI should be reachable
curl -s -o /dev/null -w "%{http_code}" http://localhost:8080

# Both containers should be running
docker ps --filter name=qbittorrent --filter name=gluetun --format "table {{.Names}}\t{{.Status}}"
```

## Expected Results

* qBittorrent has NO wg0 interface (traffic goes through gluetun's tun0)
* Both containers report exit IP `91.148.228.78`
* WebUI returns HTTP 200 at port 8080
* Both containers show "Up" status

## Rollback

If verification fails:

```bash
cp /data/config/docker-compose.yml.bak.* /data/config/docker-compose.yml
cd /data/config && docker compose up -d --force-recreate qbittorrent gluetun
```

Use the most recent backup file.

# Research

-----------------------------

# Research: Eliminate Duplicate WireGuard Tunnel in qBittorrent

## Problem

qBittorrent container has a massive custom entrypoint that installs `wireguard-tools` at runtime and creates its own
WireGuard tunnel (wg0). It uses **identical credentials** as gluetun (same private key, same endpoint
`91.148.228.71:51820`, same address `100.64.74.5/32`). WireGuard only permits one active session per key pair — this is
a protocol conflict.

## Current State

### qBittorrent

* wg0 interface: UP, IP `100.64.74.5/32`, exit IP `91.148.228.78`
* eth0: `172.18.0.7/16` (media-server-apps network)
* Default route: through wg0
* Cannot reach internet via eth0 (expected)

### gluetun

* tun0 interface: UP, IP `100.64.74.5/32`, exit IP `91.148.228.78`
* eth0: `172.18.0.15/16` (media-server-apps network)
* HTTP proxy on `:8888`
* Control server on `:9999`
* FlareSolverr already uses `network_mode: "service:gluetun"`

### gluetun Firewall (iptables)

* INPUT: allows lo, RELATED/ESTABLISHED, `172.18.0.0/16` from eth0, port 56789 on tun0
* OUTPUT: allows lo, RELATED/ESTABLISHED, `172.18.0.0/16` from `172.18.0.15`, UDP to `91.148.228.71:51820`, all on tun0
* VPN input port 56789 is forwarded through tun0 for qBittorrent

## Compose File Location

`/data/config/docker-compose.yml`

## SSH Safety

* SSH on port 22, managed by host (not Docker)
* Host iptables allows SSH in INPUT chain
* Docker networking changes do NOT affect host SSH
* Tailscale runs on host, not in Docker — **must not be disturbed**
* **Risk level: LOW** (only affects Docker containers, not host networking)

## Recommended Pattern (from Gluetun docs)

```yaml
qbittorrent:
  network_mode: "service:gluetun"
  environment:
    - WEBUI_PORT=8080
```

Then publish qBittorrent ports on the gluetun service.

# Validation

-----------------------------

# Validation Results

## Issue 195: Eliminate Duplicate WireGuard Tunnel in qBittorrent

### Changes Applied

* Removed `entrypoint` (40-line WireGuard setup script)
* Removed `networks`, `dns`, `ports`, `cap_add`, `devices` from qbittorrent
* Removed volume mount `./qbittorrent/wireguard:/etc/wireguard:ro`
* Added `network_mode: "service:gluetun"` to qbittorrent
* Added ports 8080, 56789/tcp, 56789/udp to gluetun service
* Deleted `/data/config/qbittorrent/wireguard/` directory
* Backups: `docker-compose.yml.bak.20260504125416`, `wireguard.bak.20260504125416`

### Verification Results

| Check                        | Result                                             |
|------------------------------|----------------------------------------------------|
| wg0 interface in qbittorrent | **ABSENT** (traffic goes through gluetun's tun0) ✅ |
| qbittorrent exit IP          | `91.148.228.78` (VPN Netherlands) ✅                |
| WebUI accessible at :8080    | HTTP 200 ✅                                         |
| Containers running           | gluetun: Up (healthy), qbittorrent: Up (healthy) ✅ |

### Status: ✅ PASSED
