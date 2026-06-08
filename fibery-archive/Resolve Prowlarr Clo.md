# Description

-----------------------------

## Diagnosis: Prowlarr Cloudflare Connectivity

### Root Cause

Prowlarr's `nzbfinder.ws` indexer is returning **Cloudflare anti-bot challenge HTML** instead of XML API responses. The
XML parser crashes on the HTML ("Name cannot begin with '=' character"). This is NOT a Prowlarr configuration issue.

### Evidence

* 9 identical `TestConnection()` calls in 21 minutes — all returned same 5,813-byte Cloudflare HTML
* 25-26 repeated Sonarr `RemotePathMapping` log polls showing no changes
* FlareSolverr was detected ("Cloudflare Detected, Applying FlareSolverr Proxy") but was not restarted or diagnosed
* The `PUT /api/v1/indexer/28` reconfiguration was attempted but underlying Cloudflare block persisted

### Required Actions

1. **Check FlareSolverr**: Verify container is running, check logs for errors
2. **Test FlareSolverr directly**: `curl http://flaresolverr:8191/v1` to confirm it responds
3. **If FlareSolverr is dead**: Restart the container or switch to a different proxy
4. **If FlareSolverr is alive but Cloudflare still blocks**: The OCI IP may be flagged — consider VPN/proxy routing for
   indexer traffic only
5. **Remove nzbfinder.ws indexer**: As a workaround, disable the problematic indexer until proxy is fixed

### Immediate Workaround

```bash
# On OCI server via SSH:
docker logs flaresolverr --tail 50
docker restart flaresolverr
curl http://localhost:8191/v1 -H "Content-Type: application/json" -d '{"cmd":"request.get","url":"https://nzbfinder.ws"}'
```

# Plan

-----------------------------

# Prompt

-----------------------------

# Research

-----------------------------

# Validation

-----------------------------

Validated: Root cause (Cloudflare HTML blocking Prowlarr XML parser) confirmed by 9 identical failures in logs. Required
actions listed are concrete and verifiable. Immediate workaround provided. PASS.
