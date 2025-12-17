# Last.fm Python to Toolkit Migration Plan

## Current State

### Python Implementation (`python/last.fm Scrobble Updater/last.fm scrobble updater.py`)
- **Purpose**: Fetches recent scrobbles and inserts them into Google Sheets
- **Libraries**: `pylast`, `gspread`, `google-auth`
- **Authentication**: Pickle-based OAuth token storage
- **Features**:
  * Get last scrobble timestamp from sheet
  * Fetch new scrobbles since that timestamp
  * Insert rows into Google Sheet

## Migration Plan: Integrate into Toolkit CLI

### Target Structure
```
python/toolkit/
├── cli.py                   # Add 'sync' subcommand
├── sync/
│   ├── __init__.py
│   └── lastfm.py            # Migrated from 'last.fm Scrobble Updater'
```

### CLI Integration
Add to `cli.py`:
```python
# New subcommand
sync = subparsers.add_parser("sync", help="Sync data from various services")
sync_sub = sync.add_subparsers(dest="sync_command", required=True)

sync_lastfm = sync_sub.add_parser("lastfm", help="Sync Last.fm scrobbles to Google Sheets")
sync_lastfm.set_defaults(func=cmd_sync_lastfm)
```

### PowerShell Wrapper
Add to `ScriptsToolkit.psm1`:
```powershell
function Invoke-LastFmSync {
    [Alias('synclf')]
    param()
    Invoke-ToolkitPython -ArgumentList @('sync', 'lastfm')
}
```

### Migration Steps
1. Create `python/toolkit/sync/` directory
2. Move `last.fm scrobble updater.py` → `python/toolkit/sync/lastfm.py`
3. Refactor to module pattern (extract main logic into functions)
4. Add CLI handler in `cli.py`
5. Add PowerShell wrapper function
6. Update credential paths to use `$env:USERPROFILE/Services`
7. Delete old `python/last.fm Scrobble Updater/` directory
8. Test via both `toolkit sync lastfm` and `synclf` alias

### Credential Management
Move from hardcoded paths to environment-based:
```python
CREDS_DIR = Path(os.getenv("TOOLKIT_CREDS_DIR", Path.home() / "Services"))
CREDS_FILE = CREDS_DIR / "Google Sheets Credentials.json"
TOKEN_FILE = CREDS_DIR / "lastfm_token.pickle"
```

## Verification
```powershell
# Direct Python
python -m toolkit sync lastfm

# PowerShell alias
synclf
```
