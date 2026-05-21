# Description

-----------------------------

# Parsec Network Diagnostics Reference

Comprehensive guide for diagnosing and fixing Parsec connectivity issues on Bearmancer LAN.

## Quick Reference: Common Error Codes

* **-6023/-11002**: CGNAT or double NAT (both peers behind restrictive NAT)
* **-6023/-11000/-11004/-11010**: UPnP disabled/unavailable on router
* **-6101**: Cannot reach Parsec auth servers (TCP 443 blocked)
* **-6013**: UDP blocked by network
* **Error 6**: Access denied (auth/permission issue)

## Network Topology (This Machine)

```
PC → 192.168.1.1 (inner router) → 192.168.0.1 (outer/ISP router) → 10.100.120.x (ISP CGNAT) → Internet
```

## Key Diagnostic Commands

```powershell
# Check for CGNAT/double NAT
tracert -h 10 1.1.1.1

# Check Parsec logs
Get-Content $env:APPDATA\Parsec\log.txt | Select-String 'UPNP|6023|failed'

# Check Tailscale interference
tailscale netcheck
tailscale status
Get-NetIPInterface -AddressFamily IPv4 | Format-Table

# Check firewall
Get-NetFirewallRule -DisplayName '*Parsec*' | Format-List

# Check server reachability
Test-NetConnection parsec.app -Port 443
```

## Known Working Fixes

1. **Disable UPnP** in Parsec config: `network_upnp_enabled: false`
2. **LAN connection**: Use 192.168.0.x IP for local devices
3. **Bridge mode**: Eliminate double NAT by putting inner router in AP mode
4. **Public IP**: Request from ISP to exit CGNAT

## Tailscale Note

Tailscale is installed and active but does NOT interfere with default routing (no exit node configured). Its interface
metric (5) does not override the single Wi-Fi default route.
