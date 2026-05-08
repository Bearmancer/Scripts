# Description

-----------------------------

# Plan

-----------------------------

# Plan: Bind All Docker Ports to Tailscale IP

## Approach

**Option A: Bind to Tailscale IP in docker-compose.yml**

All container ports will be bound to `100.68.154.15` (Tailscale IP) instead of `0.0.0.0`. This is the simplest, most
declarative approach.

### Why this approach

1. Declarative — lives in compose file, survives reboots and `docker compose up`
2. No iptables race conditions with Docker
3. Easy to audit and revert
4. No special cases needed — VPN traffic (qbittorrent, gluetun) flows through container-internal Wireguard tunnels, not
   host port publishing
5. Container-to-container communication uses Docker bridge networks (not port publishing), so it's unaffected

### Changes Required

#### File: `/data/config/docker-compose.yml`

Change all port bindings from `HOST:CONTAINER` to `100.68.154.15:HOST:CONTAINER`:

| Service     | Current           | New                             |
|-------------|-------------------|---------------------------------|
| gluetun     | `8888:8888`       | `100.68.154.15:8888:8888`       |
| gluetun     | `5010:5010`       | `100.68.154.15:5010:5010`       |
| qbittorrent | `8080:8080`       | `100.68.154.15:8080:8080`       |
| qbittorrent | `56789:56789`     | `100.68.154.15:56789:56789`     |
| qbittorrent | `56789:56789/udp` | `100.68.154.15:56789:56789/udp` |
| sabnzbd     | `8085:8080`       | `100.68.154.15:8085:8080`       |
| prowlarr    | `9696:9696`       | `100.68.154.15:9696:9696`       |
| sonarr      | `8989:8989`       | `100.68.154.15:8989:8989`       |
| radarr      | `7878:7878`       | `100.68.154.15:7878:7878`       |
| whisparr    | `6969:6969`       | `100.68.154.15:6969:6969`       |
| bazarr      | `6767:6767`       | `100.68.154.15:6767:6767`       |
| calibre     | `8087:8080`       | `100.68.154.15:8087:8080`       |
| calibre     | `8081:8081`       | `100.68.154.15:8081:8081`       |
| calibre     | `9090:9090`       | `100.68.154.15:9090:9090`       |
| calibre-web | `8082:8083`       | `100.68.154.15:8082:8083`       |
| emby        | `8096:8096`       | `100.68.154.15:8096:8096`       |
| dozzle      | `8088:8080`       | `100.68.154.15:8088:8080`       |
| uptime-kuma | `3001:3001`       | `100.68.154.15:3001:3001`       |
| homepage    | `3000:3000`       | `100.68.154.15:3000:3000`       |

### Defense-in-depth: DOCKER-USER rules (optional follow-up)

After the compose change, optionally add iptables rules to DOCKER-USER as defense-in-depth:

```bash
# Allow traffic from Tailscale interface
iptables -I DOCKER-USER -i tailscale0 -j ACCEPT
# Allow inter-container traffic
iptables -I DOCKER-USER -i br-7241364be57e -j ACCEPT
iptables -I DOCKER-USER -i br-b9759d9ba52a -j ACCEPT
# Allow established connections
iptables -I DOCKER-USER -m state --state RELATED,ESTABLISHED -j ACCEPT
# Drop everything else trying to reach Docker ports
iptables -A DOCKER-USER -j DROP
```

These would need a systemd service or startup script to apply after Docker starts.

### Execution Steps

1. **Backup** the current compose file
2. **Edit** `/data/config/docker-compose.yml` — change all port bindings to use `100.68.154.15:` prefix
3. **Validate** with `docker compose config` to ensure syntax is correct
4. **Apply** with `docker compose up -d` — this will recreate containers with new bindings
5. **Verify** port bindings with `docker ps --format '{{.Ports}}'` — should show `100.68.154.15:PORT:PORT` not `0.0.0.0`
6. **Test access** from Tailscale client — all services should be accessible
7. **Test block** from public IP — ports should be unreachable

### Rollback

If anything breaks, revert the compose file and run `docker compose up -d` again.

### What This Does NOT Affect

* SSH (port 22) — managed by host, not Docker
* Container-to-container communication — uses Docker bridge networks directly
* Docker DNS resolution — unaffected
* Health checks — use container-internal networking, not published ports

# Prompt

-----------------------------

# Execute: Bind All Docker Ports to Tailscale IP

## Context

The media server at `/data/config/docker-compose.yml` has all container ports published on `0.0.0.0`, making them
potentially accessible from the public internet. We need to bind them all to the Tailscale IP `100.68.154.15` instead.

## SSH Safety

* SSH is on port 22, managed by the host. Docker changes will NOT affect SSH access.
* If anything goes wrong, SSH will always be available for rollback.

## Steps

### Step 1: Backup

```bash
sudo cp /data/config/docker-compose.yml /data/config/docker-compose.yml.bak.$(date +%Y%m%d)
```

### Step 2: Edit compose file

Edit `/data/config/docker-compose.yml`. For every port mapping in every service, prefix with `100.68.154.15:`.

Specific changes (use sed or edit directly):

```yaml
# gluetun
ports:
  - 100.68.154.15:8888:8888
  - 100.68.154.15:5010:5010

# qbittorrent
ports:
  - 100.68.154.15:8080:8080
  - 100.68.154.15:56789:56789
  - 100.68.154.15:56789:56789/udp

# sabnzbd
ports:
  - 100.68.154.15:8085:8080

# prowlarr
ports:
  - 100.68.154.15:9696:9696

# sonarr
ports:
  - 100.68.154.15:8989:8989

# radarr
ports:
  - 100.68.154.15:7878:7878

# whisparr
ports:
  - 100.68.154.15:6969:6969

# bazarr
ports:
  - 100.68.154.15:6767:6767

# calibre
ports:
  - 100.68.154.15:8087:8080
  - 100.68.154.15:8081:8081
  - 100.68.154.15:9090:9090

# calibre-web
ports:
  - 100.68.154.15:8082:8083

# emby
ports:
  - 100.68.154.15:8096:8096

# dozzle
ports:
  - 100.68.154.15:8088:8080

# uptime-kuma
ports:
  - 100.68.154.15:3001:3001

# homepage
ports:
  - 100.68.154.15:3000:3000
```

### Step 3: Validate

```bash
cd /data/config && docker compose config
```

Ensure no YAML errors.

### Step 4: Apply

```bash
cd /data/config && docker compose up -d
```

This will recreate containers with new port bindings. Some services may briefly restart.

### Step 5: Verify bindings

```bash
docker ps --format 'table {{.Names}}\t{{.Ports}}' | grep -v '^$'
```

All port bindings should show `100.68.154.15:PORT->PORT/tcp` instead of `0.0.0.0:PORT->PORT/tcp`.

Also verify with:

```bash
sudo ss -tlnp | grep -E '(3000|3001|8080|8096|8888)'
```

Should show `100.68.154.15:PORT` not `0.0.0.0:PORT`.

### Step 6: Test access from Tailscale

From the local machine (connected to Tailscale), test a few services:

```bash
curl -s -o /dev/null -w '%{http_code}' http://100.68.154.15:3000/  # homepage
curl -s -o /dev/null -w '%{http_code}' http://100.68.154.15:8096/  # emby
curl -s -o /dev/null -w '%{http_code}' http://100.68.154.15:8080/  # qbittorrent
curl -s -o /dev/null -w '%{http_code}' http://100.68.154.15:8088/  # dozzle
```

All should return HTTP 200 or 301/302.

### Step 7: Test public IP is blocked

From the server itself, try accessing via public interface:

```bash
curl -s -o /dev/null -w '%{http_code}' --connect-timeout 3 http://10.0.0.129:3000/  # OCI private IP
curl -s -o /dev/null -w '%{http_code}' --connect-timeout 3 http://129.159.233.131:3000/  # public IP (won't work from inside)
```

The OCI private IP should fail (connection refused) since port is bound to Tailscale IP only.

### Rollback (if needed)

```bash
cp /data/config/docker-compose.yml.bak.YYYYMMDD /data/config/docker-compose.yml
cd /data/config && docker compose up -d
```

## Important Notes

* Container-to-container communication (e.g., prowlarr → sonarr) uses Docker bridge DNS, NOT published ports. These are
  unaffected.
* Health checks use container-internal networking (`localhost:PORT`), not host port bindings. These are unaffected.
* The `flaresolverr` service uses `network_mode: service:gluetun` — no published ports, unaffected.
* `diun` and `docker-socket-proxy` have no published ports — unaffected.
* If Tailscale IP changes in the future, update the compose file and re-run `docker compose up -d`.

## Optional Follow-up: DOCKER-USER Defense-in-Depth

After confirming the port bindings work, optionally add iptables rules:

```bash
sudo iptables -I DOCKER-USER 1 -m state --state RELATED,ESTABLISHED -j ACCEPT
sudo iptables -I DOCKER-USER 2 -i tailscale0 -j ACCEPT
sudo iptables -I DOCKER-USER 3 -i br-7241364be57e -j ACCEPT
sudo iptables -I DOCKER-USER 4 -i br-b9759d9ba52a -j ACCEPT
sudo iptables -A DOCKER-USER -j DROP
```

Then persist with a systemd service or startup script (since Docker creates DOCKER-USER chain on startup).

# Research

-----------------------------

# Research: Tighten Docker Port Exposure to Tailscale Interface Only

## Current State

### Compose File Location

* `/data/config/docker-compose.yml`

### All Published Ports (all on `0.0.0.0`)

| Port  | Service                   | Container Port | Network              |
|-------|---------------------------|----------------|----------------------|
| 3000  | homepage                  | 3000           | apps-net + infra-net |
| 3001  | uptime-kuma               | 3001           | apps-net             |
| 5010  | gluetun (Mullvad forward) | 5010           | apps-net             |
| 56789 | qbittorrent torrent       | 56789 TCP+UDP  | apps-net             |
| 6767  | bazarr                    | 6767           | apps-net             |
| 6969  | whisparr                  | 6969           | apps-net             |
| 7878  | radarr                    | 7878           | apps-net             |
| 8080  | qbittorrent WebUI         | 8080           | apps-net             |
| 8081  | calibre server            | 8081           | apps-net             |
| 8082  | calibre-web               | 8083           | apps-net             |
| 8085  | sabnzbd                   | 8080           | apps-net             |
| 8087  | calibre web UI            | 8080           | apps-net             |
| 8088  | dozzle                    | 8080           | infra-net            |
| 8096  | emby                      | 8096           | apps-net             |
| 8888  | gluetun HTTP proxy        | 8888           | apps-net             |
| 8989  | sonarr                    | 8989           | apps-net             |
| 9090  | calibre                   | 9090           | apps-net             |
| 9696  | prowlarr                  | 9696           | apps-net             |

### Network Interfaces

* `enp0s6`: 10.0.0.129/24 (OCI private) → public 129.159.233.131
* `tailscale0`: 100.68.154.15/32 (Tailscale)
* `docker0`: 172.17.0.1/16 (unused)
* `br-7241364be57e`: 172.18.0.1/16 (media-server-apps)
* `br-b9759d9ba52a`: 172.19.0.1/16 (media-server-infra)

### Host iptables (from `/etc/iptables/rules.v4`)

INPUT chain:

* RELATED,ESTABLISHED → ACCEPT
* ICMP → ACCEPT
* lo → ACCEPT
* TCP port 22 (SSH) → ACCEPT
* Everything else → REJECT

FORWARD chain (persisted): single rule `REJECT --reject-with icmp-host-prohibited`

**BUT** Docker dynamically inserts rules at runtime. Live FORWARD chain:

1. `ts-forward` — Tailscale forwarding (marks and accepts tailscale0 traffic, accepts responses, drops spoofed
   100.64.0.0/10)
2. `DOCKER-USER` — **EMPTY** (no custom rules)
3. `DOCKER-FORWARD` — Docker managed per-container rules
4. `REJECT` — catch-all

### Docker NAT DNAT Rules

All published ports have DNAT rules redirecting to container IPs via PREROUTING. These bypass the INPUT chain entirely,
going through FORWARD instead.

### Docker Raw Table

Anti-spoofing rules: DROP any traffic destined to container IPs that doesn't come from the correct bridge interface.
This prevents direct access to container IPs but does NOT prevent access via DNAT (published ports).

### ts-forward Chain Analysis

```
MARK    tailscale0 → 0x40000/0xff0000
ACCEPT  mark 0x40000/0xff0000 (allows forwarded traffic from tailscale0)
DROP    * → tailscale0 src 100.64.0.0/10 (anti-spoofing)
ACCEPT  * → tailscale0 (allows responses back to tailscale)
```

This means Tailscale traffic IS allowed through FORWARD, and then Docker's DNAT rules handle the rest.

### Persistence Mechanism

* `iptables-persistent` package installed
* `netfilter-persistent.service` enabled
* Rules saved in `/etc/iptables/rules.v4` and `/etc/iptables/rules.v6`
* **BUT**: Docker dynamically adds/removes rules at startup; the persisted rules only cover the base host rules
* Custom DOCKER-USER rules would need to be re-applied after Docker starts but the chain is created by Docker

### SSH Safety

* SSH on port 22, managed by host systemd, NOT Docker
* Changing Docker port bindings or adding iptables rules to DOCKER-USER does NOT affect SSH
* SSH is protected by the INPUT chain

## Security Analysis

### Current Exposure

All 18 published ports are bound to `0.0.0.0` AND `[::]`. The DNAT rules in PREROUTING bypass the INPUT chain. The only
protection is:

1. OCI Security List/NSG (cloud-level firewall)
2. Docker's raw table anti-spoofing (only protects direct container IP access)

### Option A: Bind to Tailscale IP in compose

Change `ports: - 3000:3000` to `ports: - 100.68.154.15:3000:3000`

* **Pros**: Simple, declarative in compose file, survives reboots, survives `docker compose up`
* **Cons**:
	* Breaks access from OCI private network (10.0.0.0/24) — not needed currently
	* If Tailscale IP changes, all bindings break
	* Must also bind to `[::]` or disable IPv6 binding
	* qbittorrent torrent port 56789 and gluetun port 5010 may need public access for torrenting
* **Risk**: LOW — compose file change, easy to revert

### Option B: DOCKER-USER iptables rules

Add rules to DOCKER-USER chain:

```
iptables -I DOCKER-USER -i tailscale0 -j ACCEPT
iptables -I DOCKER-USER -i br-7241364be57e -j ACCEPT
iptables -I DOCKER-USER -i br-b9759d9ba52a -j ACCEPT
iptables -I DOCKER-USER -i lo -j ACCEPT
iptables -A DOCKER-USER -j RETURN
```

Wait — DOCKER-USER defaults to RETURN, so without explicit DROP, all traffic passes through. We'd need:

```
iptables -A DOCKER-USER -i tailscale0 -j ACCEPT
iptables -A DOCKER-USER -i br-7241364be57e -j ACCEPT
iptables -A DOCKER-USER -i br-b9759d9ba52a -j ACCEPT
iptables -A DOCKER-USER -i lo -j ACCEPT
iptables -A DOCKER-USER -j DROP
```

Wait — this would block enp0s6 traffic to all Docker ports. But it would also block qbittorrent torrent port 56789 from
the internet (needed for seeding) and gluetun's port forward 5010.

* **Pros**: Single point of control, doesn't change compose file, interface-based (not IP-based)
* **Cons**:
	* Need to persist rules (iptables-persistent exists but DOCKER-USER chain is created by Docker)
	* Must be applied AFTER Docker starts
	* Would break torrent port 56789 and gluetun 5010 if they need internet access
	* Race condition: between Docker creating the chain and rules being applied
* **Risk**: MEDIUM — iptables rules affect all Docker networking

### Option C: OCI Security List (status quo)

* If OCI NSG already blocks these ports, no action needed
* Defense-in-depth would still recommend host-level protection
* Cannot verify from inside the instance

## Recommendation

**Hybrid approach (Option A with exceptions):**

1. Bind most ports to Tailscale IP `100.68.154.15` in docker-compose.yml
2. Keep `56789` (qbittorrent torrent) on `0.0.0.0` — needed for torrent peer connections
3. Keep `5010` (gluetun port forward) on `0.0.0.0` — needed for Mullvad port forwarding
4. Also add DOCKER-USER rules as defense-in-depth (Block all non-tailscale/non-bridge traffic EXCEPT for ports 5010 and
   56789)

Actually, re-evaluating: **qbittorrent runs through gluetun VPN**, so port 56789 traffic arrives via the VPN tunnel, NOT
from the public internet directly. Similarly port 5010 is for VPN port forwarding. These could also be Tailscale-only.

**Final recommendation: Option A — bind all ports to Tailscale IP.**

Wait — actually, gluetun's port 5010 and qbittorrent's 56789 need to accept incoming connections from the VPN tunnel.
The VPN tunnel is inside the container (network stack), so port publishing for these is actually for EXTERNAL access. If
qbittorrent uses gluetun as network (`network_mode: service:gluetun`), then... checking compose:

* qbittorrent does NOT use `network_mode: service:gluetun` — it has its own WireGuard setup in the entrypoint
* gluetun publishes 5010 for Mullvad port forwarding (incoming VPN connections)
* qbittorrent publishes 56789 for torrent peers

Since qbittorrent has its own Wireguard tunnel, port 56789 is exposed on the host. If it's bound only to Tailscale,
torrent peers can't connect via the VPN tunnel's public IP. BUT the VPN tunnel is inside the container, so incoming
connections from the VPN would arrive on the container's eth0 (via the bridge network), not through the host's port
publishing.

Actually — port publishing maps HOST port → CONTAINER port. For qbittorrent with its own Wireguard: the VPN gives it a
public IP on an interface inside the container. Torrent peers connect to that VPN public IP, which goes through the
container's Wireguard interface directly — no host port mapping needed.

Similarly, gluetun's port 5010: Mullvad port forward goes through gluetun's Wireguard tunnel inside the container. The
host port publishing is for accessing these from the host/Tailscale.

**CONFIRMED: All ports can be safely bound to Tailscale IP only.**

### Special consideration: gluetun HTTP proxy (8888)

The HTTP proxy on 8888 is published so other devices on the Tailscale network can use it. Binding to Tailscale IP is
exactly what we want.

### Special consideration: Dozzle (8088) on infra-net

Dozzle is on infra-net, bound to 0.0.0.0:8088→8080. Should be Tailscale-only.

### IPv6 consideration

Current bindings show `[::]:PORT→PORT` for most ports. We need to either:

* Explicitly bind only to the Tailscale IPv4 IP (which implicitly prevents IPv6 binding)
* Or also handle IPv6

Docker Compose format: `100.68.154.15:3000:3000` binds only to that IPv4 address.

# Validation

-----------------------------

