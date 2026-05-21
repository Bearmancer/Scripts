# Description

-----------------------------

Root issue for network architecture audit and simplification.

## Network Inventory Complete

All network values have been inventoried. See sub-issues for execution plan.

# Plan

-----------------------------

# Prompt

-----------------------------

# Research

-----------------------------

# Complete Network Inventory — OCI Media Server

## Host Identity

| Property             | Value                                           |
| -------------------- | ----------------------------------------------- |
| Hostname             | media-server-vnic                               |
| Public IP            | 129.159.233.131                                 |
| Private IP (OCI VCN) | 10.0.0.129/24                                   |
| Gateway              | 10.0.0.1                                        |
| Tailscale IP         | 100.68.154.15                                   |
| DNS                  | systemd-resolved (127.0.0.53)                   |
| Search Domains       | tail2e6179.ts.net, mediaservervcn.oraclevcn.com |
| SSH                  | Port 22 (default)                               |
| Platform             | Oracle Cloud Infrastructure (ARM)               |

## Tailscale Network

| Peer                     | IP             | OS      | Status                             |
| ------------------------ | -------------- | ------- | ---------------------------------- |
| media-server-vnic (self) | 100.68.154.15  | Linux   | \-                                 |
| lance                    | 100.106.89.100 | Windows | active; direct 103.207.57.31:41641 |
| moto-g84-5g              | 100.85.84.56   | Android | \-                                 |

**Health warning**: "Tailscale can't reach the configured DNS servers"\
**MagicDNS**: enabled, suffix tail2e6179.ts.net\
**Split DNS**: ts.net → 199.247.155.53, 2620:111:8007::53

## Docker Networks

| Network            | Subnet        | Purpose                            |
| ------------------ | ------------- | ---------------------------------- |
| media-server-apps  | 172.18.0.0/16 | Application services               |
| media-server-infra | 172.19.0.0/16 | Infrastructure (monitoring, proxy) |
| bridge             | 172.17.0.0/16 | Docker default (unused)            |

## Container IP Map (media-server-apps / 172.18.0.0/16)

| Container   | IP          | Ports                           |
| ----------- | ----------- | ------------------------------- |
| sabnzbd     | 172.18.0.2  | 8085→8080                       |
| prowlarr    | 172.18.0.3  | 9696→9696                       |
| calibre     | 172.18.0.4  | 8087→8080, 8081→8081, 9090→9090 |
| uptime-kuma | 172.18.0.5  | 3001→3001                       |
| emby        | 172.18.0.6  | 8096→8096                       |
| qbittorrent | 172.18.0.7  | 8080→8080, 56789→56789 TCP+UDP  |
| homepage    | 172.18.0.9  | 3000→3000                       |
| calibre-web | 172.18.0.10 | 8082→8083                       |
| radarr      | 172.18.0.11 | 7878→7878                       |
| sonarr      | 172.18.0.12 | 8989→8989                       |
| whisparr    | 172.18.0.13 | 6969→6969                       |
| bazarr      | 172.18.0.14 | 6767→6767                       |
| gluetun     | 172.18.0.15 | 8888→8888, 5010→5010            |

## Container IP Map (media-server-infra / 172.19.0.0/16)

| Container             | IP         |
| --------------------- | ---------- |
| homepage (dual-homed) | 172.19.0.2 |
| docker-socket-proxy   | 172.19.0.3 |
| dozzle                | 172.19.0.4 |
| diun                  | 172.19.0.5 |

## Special Networking

| Container    | Mode                          | Details                                                                       |
| ------------ | ----------------------------- | ----------------------------------------------------------------------------- |
| flaresolverr | network_mode: service:gluetun | Shares gluetun's network namespace. No own IP. Accessible at 172.18.0.15:8191 |
| gluetun      | Own WireGuard tunnel (tun0)   | IP: 100.64.74.5/32, Exit: 91.148.228.78 (Netherlands)                         |
| qbittorrent  | Own WireGuard tunnel (wg0)    | **SAME CREDENTIALS** as gluetun. IP: 100.64.74.5/32. Custom entrypoint hack.  |

## VPN Configuration

| Property          | Value                                            |
| ----------------- | ------------------------------------------------ |
| Type              | WireGuard (custom provider)                      |
| Endpoint          | 91.148.228.71:51820                              |
| Tunnel Address    | 100.64.74.5/32                                   |
| Public Key (peer) | KgTUh3KLijVluDvNpzDCJJfrJ7EyLzYLmdHCksG4sRg=     |
| Exit IP           | 91.148.228.78 (Netherlands, Flevoland, Lelystad) |
| HTTP Proxy        | gluetun:8888                                     |
| Control Server    | gluetun:9999                                     |

## DNS Configuration (4 different configs!)

| Scope           | DNS Server                    | Upstream                                    |
| --------------- | ----------------------------- | ------------------------------------------- |
| Host            | 127.0.0.53 (systemd-resolved) | OCI DHCP                                    |
| Most containers | 127.0.0.11 (Docker embedded)  | 100.100.100.100 (Tailscale) + 1.1.1.1       |
| Gluetun         | 127.0.0.1 (own resolver)      | 198.18.0.1, 198.18.0.2 (Tailscale internal) |
| qbittorrent     | Hardcoded in entrypoint       | 1.1.1.1                                     |

## Host Firewall (iptables)

* INPUT: Only SSH (22) + RELATED/ESTABLISHED + ICMP + lo + Tailscale (ts-input)
* FORWARD: Tailscale (ts-forward) → Docker (DOCKER-USER → DOCKER-FORWARD) → REJECT
* Docker DNAT rules in PREROUTING bypass INPUT chain for published ports
* Raw table: anti-spoofing rules block direct container IP access from wrong interfaces

## Compose File Location

`/data/config/docker-compose.yml`

# Validation

-----------------------------

