# Draft To-Do - COMPLETED

## Status Summary

| #   | Item                                       | Status                                          |
| --- | ------------------------------------------ | ----------------------------------------------- |
| 1   | Show verbose flag for all command help     | ✅ Already done                                  |
| 2   | Why is completion part of the CLI?         | ✅ Answered - .NET completion is for dotnet only |
| 3   | How to permanently enable auto completion? | ✅ `scripts completion install`                  |
| 4   | Best way to run without dotnet run         | ✅ Created `ScriptSetup.md`                      |
| 5   | Assess CSV async/sync                      | ✅ Keep both - sync used by orchestrator         |
| 6   | Export after lookup                        | ✅ Interactive export added                      |
| 7   | What values exported to Sheet?             | ✅ Documented in implementation plan             |
| 8   | How are missing fields handled?            | ✅ Empty string                                  |
| 9   | Which values are mandatory?                | ✅ Documented                                    |

---

## Detailed Answers

### Q1: Verbose Flag
All commands have `-v|--verbose` flag.

### Q2: CLI Completion
.NET's `dotnet complete` is **only for the dotnet CLI**, not custom apps. Custom completion implementation is required.

### Q3: Permanent Auto-Completion
```powershell
scripts completion install
```

### Q4: Run Without `dotnet run --`
Add to `$PROFILE`:
```powershell
function scripts { dotnet run --project "C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj" -- $args }
```

See `ScriptSetup.md` for details.

### Q5: CSV Async/Sync
**Keep both**: Sync version is used by `YouTubePlaylistOrchestrator`. Full orchestrator async migration would be needed to remove sync.

### Q6: Export After Lookup
Interactive prompt added:
```
Export to CSV?
  > All fields
    Default fields (Artist, Title, Year, Label)
    Skip
```
Filename auto-generated from album title.

### Q7-9: Sheet Export
- **Values**: Artist, Track, Album, PlayedAt (Last.fm) / Title, Channel, Duration, PublishedAt (YouTube)
- **Missing fields**: Empty string `""`
- **Mandatory**: Artist+Track+PlayedAt / VideoId+Title+PublishedAt