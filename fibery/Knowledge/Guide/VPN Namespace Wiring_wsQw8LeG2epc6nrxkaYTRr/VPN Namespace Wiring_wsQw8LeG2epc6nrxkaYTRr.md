# Description

-----------------------------

# VPN Namespace Wiring

## Configuration

1. Update `/data/config/.env` with WireGuard values
2. Ensure `gluetun`, `qbittorrent`, and `flaresolverr` are wired so VPN/DNS failure does not stop unrelated services
3. WireGuard config goes in `/data/config/gluetun/wireguard/wg0.conf`
4. Verify VPN-dependent containers report healthy

## DNS Settings

For Gluetun plain DNS upstreams, use `DNS_UPSTREAM_RESOLVER_TYPE=plain` with `DNS_UPSTREAM_PLAIN_ADDRESSES=<ip:53,...>`.