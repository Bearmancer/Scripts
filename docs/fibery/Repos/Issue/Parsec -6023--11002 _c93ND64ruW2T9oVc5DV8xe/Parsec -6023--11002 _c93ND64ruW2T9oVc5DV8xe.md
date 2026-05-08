# Description

-----------------------------

# Parsec Connectivity Failure

## Current Status

**Error**: -6023 / -11002 (since May 4, 2026)\
**Previous errors**: Error 6 (access denied, intermittent)

## Affected Users

* **Host**: Lance (DESKTOP-MJ3FF9U) — user ID 14251798
* **Client**: ndharmateja#18840947

## Timeline

* **First UPnP rejection**: Jan 14, 2026
* **Last successful connection**: May 4, 2026 10:04 AM (via BUD to 49.47.249.7:30843)
* **Recent failures**: May 4, 2026 (2 attempts, both -6023/-11002)

## Key Files

* Parsec config: `%APPDATA%/Parsec/config.json`
* Host logs: `%APPDATA%/Parsec/log.txt`

# Plan

-----------------------------

# Resolution Approaches

## A: Eliminate Double NAT (Router Bridge Mode)

1. Access router at 192.168.1.1
2. Enable Access Point (bridge) mode
3. Eliminates one NAT layer, potentially making UPnP functional
4. Risk: May break DHCP for other devices

## B: ISP-Level Fix (Request Public IP)

1. Contact ISP
2. Request static or dynamic public IPv4
3. Eliminates CGNAT entirely from this side
4. With only one side behind CGNAT, hole punching should work
5. Cost: Typically Rs.50-200/month

## C: Software Workaround (Disable UPnP)

1. Add network_upnp_enabled: false to config.json
2. Skips failing UPnP step (saves \~5-10 seconds per attempt)
3. Relies on STUN-only hole punching
4. Does NOT fix underlying NAT issue
5. Marginal benefit, zero risk

## D: Alternative P2P VPN (ZeroTier)

1. Install ZeroTier (Parsec-recommended fallback)
2. Both sides join same ZeroTier network
3. Connect via ZeroTier IP instead of attempting direct P2P
4. ndharmateja cannot join Tailscale, but ZeroTier may be acceptable

## E: Connect via LAN (Local Use)

* For Bangalore local use: connect via 192.168.0.2 (outer router subnet)
* Bypasses WAN entirely using local BUD
* Already confirmed working (logs show BUD|192.168.0.2 connections)

## Recommendation

Short term: C (disable UPnP) + E (LAN connection)\
Long term: A (bridge mode) or B (public IP)\
Fallback: D (ZeroTier P2P VPN)

# Prompt

-----------------------------

# Research

-----------------------------

# Network Diagnostic Battery (May 4, 2026)

## 1. Network Topology

```
PC (192.168.1.101, Wi-Fi)
    ↓
[Router A] 192.168.1.1 (inner, HTTP reachable)
    ↓
[Router B] 192.168.0.1 (outer/ISP, HTTP reachable)  ← DOUBLE NAT
    ↓
[ISP CGNAT] 10.100.120.34                           ← CARRIER-GRADE NAT
    ↓
Public IP: 103.207.57.31
```

**Tracert to 1.1.1.1**:

* 1: 192.168.1.1 (3ms) — Inner router
* 2: 192.168.0.1 (2ms) — Outer router
* 3: 10.100.120.34 (2ms) — ISP CGNAT (in 10.0.0.0/8 reserved range)
* 4: 1.1.1.1 (3ms) — Destination

## 2. Parsec UPnP Status

**Message**: "A valid connected IGD has been found but its IP address is reserved (non routable)"

* Router found via SSDP at 192.168.1.1
* WAN IP reported as 10.100.120.x (CGNAT range)
* Parsec rejects because IP is in reserved range
* **Occurs on 100% of Parsec restarts** (confirmed since Jan 14, 2026)

## 3. Tailscale Status

* **Active**: Yes, on tail2e6179.ts.net
* **IP**: 100.106.89.100
* **Interface metric**: 5 (lowest, but no default route)
* **Peers**: media-server-vnic (online), moto-g84-5g (offline)
* **MagicDNS**: Enabled
* **Exit node**: Not active
* **UPnP/NAT-PMP**: Working (Tailscale reports success)
* **WAN IP (seen by Tailscale)**: 103.207.57.31

## 4. Firewall Analysis

* **Inbound Parsec rule**: Enabled, Any/Any, for parsecd.exe
* **Outbound Parsec rule**: None specifically (default allow all)
* **Third-party firewall**: None running

## 5. Parsec Process Ports

* TCP 192.168.1.101:58262 → 104.18.0.181:443 (parsec.app auth)
* UDP 127.0.0.1:5309 (localhost only)
* **No external UDP listener observed**

## 6. Server Reachability

* parsec.app (TCP 443): Reachable
* stun.parsec.app (UDP 3478): Replies received
* kessel.parsecgaming.com: Unreachable (deprecated)

## 7. Wi-Fi Quality

* SSID: Bearmancer | 802.11n | Channel 5 (2.4 GHz)
* Link speed: 72.2 Mbps Tx/Rx | Signal: 100%

## 8. Error Code Summary

| Code          | Count  | Description                            |
|---------------|--------|----------------------------------------|
| \-6023/-11002 | 2      | CGNAT/double NAT (May 4)               |
| Error 6       | 3      | Access denied (Apr 22, 24, 29)         |
| \-6105        | \~30+  | Signaling thread hang                  |
| \-15101       | \~500+ | AMD encoder init failure               |
| \-710049      | \~20+  | BUD write packet failure (disconnect)  |
| \-710022      | \~25   | BUD write packet failure (May 4 flood) |

## 9. Machine Specs

* CPU: AMD Ryzen 5 5500U with Radeon Graphics
* GPU: AMD Radeon (PID 164c, driver 27.20.21030.1005)
* OS: Windows 10 (build 19045)
* Encoder: AMD h264, encode_init error -15101 on every connection
* Streaming resolution: 1024x768
* Parsec version: release18 (150-103a, service 12)

## 10. Performance Data (from logs)

* Latency: 9ms to 151ms (high variance)
* FPS: 8.5 to 60 (wide swings)
* Bandwidth: 0.3 to 2.9 Mbps
* Network drops: Thousands per session (N counter in FPS lines)

# Validation

-----------------------------

# What Was Validated

## Confirmed

* Double NAT: 192.168.1.1 → 192.168.0.1 (both HTTP accessible)
* CGNAT: ISP hop at 10.100.120.34 (in 10.0.0.0/8 reserved range)
* UPnP found but rejected: router WAN IP is non-routable
* Tailscale active but NOT overriding default route (no exit node)
* Parsec auth servers reachable (TCP 443)
* STUN servers reachable (UDP 3478)
* Windows Firewall: Parsec allowed inbound
* Public IP: 103.207.57.31
* Wi-Fi: 802.11n, 72.2 Mbps, signal 100%

## Root Cause

**Double NAT + CGNAT on BOTH peers** prevents UDP hole punching. Parsec docs: "With only one person behind CGNAT, Parsec
should work. If both sides have the issue, it cannot make the connection."

April 22-29 connections worked because ndharmateja was on a different network (IPv6 or non-CGNAT ISP). May 4 failures
indicate both peers behind restrictive NAT.

## Fixes Rejected

* Raise Tailscale metric: No effect (Tailscale has no default route)
* Disable UPnP alone: Speeds fallback but doesn't fix NAT traversal

## Secondary Issues (Unresolved)

* AMD encoder error (-15101) on every connection
* Wi-Fi 802.11n on 2.4 GHz (72.2 Mbps max, congested band)
* bud_write_packet errors (-710049/-710022): packet loss during sessions
* High latency variance: 9ms to 151ms

## May 4 Fix Attempt: Disable UPnP in Config

**Attempted**: Added `network_upnp_enabled: false` to `config.json`\
**Result**: Parsec overwrote config on restart, stripped unknown key. Release 18 (v150-103a) does not recognize this
config key.\
**Conclusion**: This approach does NOT work with current Parsec version. Config mechanism only supports documented keys.
