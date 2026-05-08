# Description

-----------------------------

# Plan

-----------------------------

# Plan: Unify DNS Configuration

## Prerequisites

* Issue #1 (qbittorrent VPN fix) should be completed first, so qbittorrent routes through gluetun and no longer needs
  its own DNS config.

## Steps

### Step 1: Update the DNS anchor in docker-compose.yml

Change the shared DNS anchor from Tailscale+Cloudflare to reliable public DNS:

```yaml
# Before:
x-common:
  dns: &id004
  - 100.100.100.100
  - 1.1.1.1

# After:
x-common:
  dns: &id004
  - 1.1.1.1
  - 8.8.8.8
```

Rationale: Remove `100.100.100.100` (Tailscale MagicDNS) which is unreliable from bridge networks. Use `1.1.1.1` (
Cloudflare) as primary and `8.8.8.8` (Google) as fallback — both fast, reliable public resolvers.

### Step 2: Keep gluetun DNS config as-is

Gluetun has its own internal DNS resolver (`127.0.0.1`) with Tailscale upstream (`198.18.0.1:53, 198.18.0.2:53`). This
is intentional for VPN DNS leak protection and should NOT be changed.

### Step 3: Remove qbittorrent hardcoded DNS (if Issue #1 is done)

After qbittorrent is routing through gluetun (network: `service:gluetun`), remove any hardcoded DNS in its entrypoint
script. It will inherit gluetun's DNS automatically.

### Step 4: Apply changes

```bash
cd /home/lance/docker-compose
docker compose up -d
```

Only containers whose DNS config actually changed will be recreated.

## Verification

1. `docker exec prowlarr nslookup google.com` — should resolve via public DNS
2. `docker exec prowlarr nslookup sonarr` — Docker internal DNS should still work
3. `docker exec gluetun nslookup google.com` — gluetun DNS still works independently
4. Check container logs for DNS errors: `docker compose logs --tail=20 prowlarr sonarr radarr`
5. Verify Tailscale DNS health check clears

## Rollback

If DNS breaks, revert the anchor to include `100.100.100.100` and re-run `docker compose up -d`.

# Prompt

-----------------------------

# Execution Prompt: Unify DNS Configuration

## Context

You are fixing DNS configuration across Docker containers. The shared DNS anchor in docker-compose.yml currently points
to `100.100.100.100` (Tailscale MagicDNS) which is unreliable from Docker bridge networks. You are replacing it with
reliable public DNS resolvers.

## Prerequisites Check

Before starting, verify Issue #1 (qbittorrent VPN/gluetun routing) is resolved:

```bash
ssh lance@mediaserver 'docker inspect qbittorrent --format "{{.HostConfig.NetworkMode}}"'
```

If it shows `container:<gluetun-container-id>` or similar, proceed. If not, note that qbittorrent DNS cleanup is
skipped.

## Execution Steps

### 1. Read the current docker-compose.yml

```bash
ssh lance@mediaserver 'cat /home/lance/docker-compose/docker-compose.yml'
```

Identify the `x-common` section and the DNS anchor (`&id004` or similar name).

### 2. Edit the DNS anchor

Change the DNS anchor under `x-common` from:

```yaml
dns: &id004
- 100.100.100.100
- 1.1.1.1
```

To:

```yaml
dns: &id004
- 1.1.1.1
- 8.8.8.8
```

Use sed or a targeted replacement. Be careful to preserve YAML indentation exactly.

Example sed command:

```bash
ssh lance@mediaserver "sed -i '/^  dns: &id004$/,/^  - 1.1.1.1$/c\  dns: \&id004\n  - 1.1.1.1\n  - 8.8.8.8' /home/lance/docker-compose/docker-compose.yml"
```

**IMPORTANT**: After editing, verify the YAML is valid:

```bash
ssh lance@mediaserver 'docker compose -f /home/lance/docker-compose/docker-compose.yml config > /dev/null && echo YAML_VALID || echo YAML_INVALID'
```

### 3. Handle qbittorrent DNS (if Issue #1 is done)

If qbittorrent routes through gluetun, check if there's a hardcoded DNS in its entrypoint:

```bash
ssh lance@mediaserver 'docker inspect qbittorrent --format="{{.Config.Entrypoint}}"'
```

If there's a custom script with `nameserver 1.1.1.1`, remove just the DNS-related lines (or the entire
`/etc/resolv.conf` overwrite), since gluetun provides DNS.

### 4. Do NOT modify gluetun's DNS config

Gluetun's `DNS_UPSTREAM_RESOLVER_TYPE` and `DNS_UPSTREAM_PLAIN_ADDRESSES` environment variables should remain unchanged.

### 5. Apply changes

```bash
ssh lance@mediaserver 'cd /home/lance/docker-compose && docker compose up -d'
```

### 6. Verify DNS works in containers

```bash
ssh lance@mediaserver 'docker exec prowlarr nslookup google.com'
ssh lance@mediaserver 'docker exec prowlarr nslookup sonarr'
ssh lance@mediaserver 'docker exec gluetun nslookup google.com'
```

Expected: All should resolve successfully. `nslookup google.com` should show `1.1.1.1` or `8.8.8.8` as the server for
prowlarr. Gluetun should show `127.0.0.1` as server.

### 7. Check for errors in logs

```bash
ssh lance@mediaserver 'cd /home/lance/docker-compose && docker compose logs --tail=30 prowlarr sonarr radarr bazarr sabnzbd calibre emby'
```

Look for any DNS resolution errors or connection failures.

## Rollback Plan

If anything breaks:

```bash
ssh lance@mediaserver "sed -i 's/  - 1.1.1.1/  - 100.100.100.100\n  - 1.1.1.1/' /home/lance/docker-compose/docker-compose.yml && sed -i '/  - 8.8.8.8/d' /home/lance/docker-compose/docker-compose.yml"
ssh lance@mediaserver 'cd /home/lance/docker-compose && docker compose up -d'
```

# Research

-----------------------------

# DNS Configuration Research

## Current DNS Landscape (4 configurations)

### 1. Host

* systemd-resolved at `127.0.0.53`
* Search domains: `tail2e6179.ts.net`, `mediaservervcn.oraclevcn.com`

### 2. Most Containers (prowlarr, sonarr, radarr, whisparr, bazarr, sabnzbd, calibre, calibre-web, emby, uptime-kuma, homepage)

* Docker embedded DNS (`127.0.0.11`) → upstream: `100.100.100.100` (Tailscale MagicDNS) + `1.1.1.1`
* Set via compose `dns:` override anchor:

  ```yaml
  x-common:
    dns: &id004
    - 100.100.100.100
    - 1.1.1.1
  ```
* Most services use `dns: *id004`

### 3. Gluetun (VPN gateway)

* Own DNS server at `127.0.0.1` → upstream: `198.18.0.1`, `198.18.0.2` (Tailscale internal DNS IPs)
* Set via env: `DNS_UPSTREAM_RESOLVER_TYPE: plain`, `DNS_UPSTREAM_PLAIN_ADDRESSES: 198.18.0.1:53,198.18.0.2:53`
* Has its own internal DNS resolver for VPN routing purposes

### 4. qbittorrent

* Hardcoded `nameserver 1.1.1.1` in its custom entrypoint script
* After Issue #1 (qbittorrent VPN fix) is resolved, qbittorrent will route through gluetun and use gluetun's DNS

## Issues Found

1. **100.100.100.100 unreachable from bridge networks**: The Tailscale MagicDNS IP (`100.100.100.100`) is served by the
   Tailscale daemon on the host. Docker containers on bridge networks may or may not be able to reach it reliably. Even
   when reachable via the gateway → Tailscale interface route, it adds unnecessary latency.
2. **Tailscale DNS health check warning**: "Tailscale can't reach the configured DNS servers. Internet connectivity may
   be affected."
3. **Gluetun DNS intermittent failures**: `dial tcp: lookup ifconfig.me on 127.0.0.1:53: server misbehaving` — gluetun's
   internal DNS sometimes fails to resolve.
4. **No unified strategy**: Each container category has different DNS config, making debugging harder.

## Safety Assessment

* **SSH safety**: HIGH — DNS changes on containers won't affect SSH (SSH uses host DNS/PAM)
* **Risk**: LOW — worst case some containers lose DNS temporarily, but SSH remains intact
* **Recovery**: Revert the YAML anchor change and `docker compose up -d`

## Key Files

* `docker-compose.yml` — contains the `x-common` DNS anchor and all service definitions
* `qbittorrent` entrypoint script — hardcoded DNS

# Validation

-----------------------------

