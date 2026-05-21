# Description

-----------------------------

# Docker Modular Architecture

## Modular Split Strategy

Deconstruct the monolithic 18-service `docker-compose.yml` into four distinct stacks to isolate failures and
dependencies:

* **Stack A (VPN)**: `gluetun`, `qbittorrent` (Network namespace strictly restricted to Gluetun).
* **Stack B (Downloading)**: `prowlarr`, `radarr`, `sonarr`, `sabnzbd`, `bazarr`, `whisparr`.
* **Stack C (Serving)**: `emby`, `calibre`, `calibre-web`.
* **Stack D (Monitoring/Utility)**: `uptime-kuma`, `dozzle`, `homepage`, `diun`, `docker-socket-proxy`, `ntfy`,
  `flaresolverr`.

## .env Cleanup

* Audit `.env` files for stale placeholders and removed service variables.
* Identify any WireGuard secrets or VPN credentials in `.env` and migrate them to proper configuration files.
