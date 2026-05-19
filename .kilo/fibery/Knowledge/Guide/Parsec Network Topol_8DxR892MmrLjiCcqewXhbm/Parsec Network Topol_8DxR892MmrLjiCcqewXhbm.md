# Description

-----------------------------

# Network Topology Analysis

## Architecture

```
DESKTOP-MJ3FF9U (Windows 10, AMD Ryzen 5 5500U)
    │
    ├── Wi-Fi: 192.168.1.101 (802.11n, 72.2 Mbps, 2.4 GHz Ch 5)
    │   Gateway: 192.168.1.1 (Router A - inner)
    │
    ├── Tailscale: 100.106.89.100 (WireGuard, MTU 1280)
    │   Tailnet: tail2e6179.ts.net
    │   Peers: media-server-vnic (linux), moto-g84-5g (android)
    │
    └── Hyper-V vEthernet (WSL, Docker):
        172.20.128.1, 172.24.96.1, 172.29.16.1

Router A (192.168.1.1):
    - WAN IP: 10.100.120.x (from ISP CGNAT pool)
    - LAN IP: 192.168.1.1
    - UPnP: Enabled (confirmed by Tailscale netcheck: "PortMapping: UPnP, NAT-PMP")

Router B (192.168.0.1):
    - ISP-provided modem/router
    - LAN IP: 192.168.0.1

ISP CGNAT:
    - Private range: 10.100.120.0/24
    - Public IP translation: → 103.207.57.31

## Interface Metrics
| Interface       | Metric | MTU        | Status    |
| --------------- | ------ | ---------- | --------- |
| Tailscale       | 5      | 1280       | Connected |
| vEthernet (WSL) | 15     | 1500       | Connected |
| Wi-Fi           | 55     | 1500       | Connected |
| Loopback        | 75     | 4294967295 | Connected |

## Default Route
Only ONE default route exists: 0.0.0.0/0 → 192.168.1.1 (Wi-Fi, metric 0)
Tailscale has no default route → interface metric does NOT affect outbound traffic routing.

## Parsec Traffic Path
Parsec uses Wi-Fi IP (192.168.1.101) as confirmed by TCP connection to parsec.app.
STUN replies received via Wi-Fi path → public IP correctly discovered as 103.207.57.31.

## Root Issue
Double NAT (2 routers) + ISP CGNAT. When both peers are behind CGNAT, STUN hole punching fails.
When only one peer has CGNAT, hole punching succeeds (as evidenced by April connections).
```