# Description

-----------------------------

qBittorrent (host=gluetun, port=8080, user=admin) returns 403 from Sonarr container. AuthSubnetWhitelist changed to
172.18.0.0/16 but container NOT restarted. Temp password from logs: Mvd9TNnQ8 likely expired. Sonarr DB stores
password=admin123 but qBittorrent has PBKDF2 hash that may not match. Log snippet: System.Net.Http.HttpRequestException:
Name does not resolve (qbittorrent:8080) - old hostname in logs, current config uses gluetun.

# Plan

-----------------------------

# Prompt

-----------------------------

# Research

-----------------------------

# Validation

-----------------------------

