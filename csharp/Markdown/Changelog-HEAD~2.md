# Changelog: HEAD~2 → HEAD

Generated: 2025-12-16 14:15 IST  
Commits covered: `34801b4` → `5ed42bf`

---

## Commit Summary

| Hash      | Message                                                                                                | Date                      |
| --------- | ------------------------------------------------------------------------------------------------------ | ------------------------- |
| `5ed42bf` | feat: Add Google Sheets service for data synchronization, along with supporting CLI commands and tests | 2025-12-16 13:59:05 +0530 |
| `cd5c743` | Saving state                                                                                           | 2025-12-16 13:28:28 +0530 |

---

## Statistics

- **45 files changed**
- **+5,582 insertions**
- **-674 deletions**

---

## Files Created (25)

### Documentation (13)
| File                                                 | Lines |
| ---------------------------------------------------- | ----- |
| `csharp/Markdown/AsyncMigrationPlan.md`              | 286   |
| `csharp/Markdown/AsyncSyncDesign.md`                 | 210   |
| `csharp/Markdown/CLIDesign.md`                       | 295   |
| `csharp/Markdown/ClassicalModeImplementation.md`     | 264   |
| `csharp/Markdown/DiscogsSearchSorting.md`            | 269   |
| `csharp/Markdown/DotNetTabCompletion.md`             | 208   |
| `csharp/Markdown/JSONScriptingIntegration.md`        | 181   |
| `csharp/Markdown/LookupVsSearch.md`                  | 148   |
| `csharp/Markdown/MusicBrainzClassicalDifferences.md` | 227   |
| `csharp/Markdown/MusicBrainzScoring.md`              | 184   |
| `csharp/Markdown/MusicMetadataFields.md`             | 243   |
| `csharp/Markdown/TerminalCompatibility.md`           | 115   |
| `csharp/Markdown/To-Do.md`                           | 190   |

### Source Code (2)
| File                                       | Lines |
| ------------------------------------------ | ----- |
| `csharp/src/CLI/CompletionCommands.cs`     | 192   |
| `csharp/src/Models/MusicMetadataSchema.cs` | 700   |

### Tests (9)
| File                                         | Lines |
| -------------------------------------------- | ----- |
| `csharp/tests/CSharpScripts.Tests.csproj`    | 24    |
| `csharp/tests/GlobalUsings.cs`               | 4     |
| `csharp/tests/LastFmServiceTests.cs`         | 82    |
| `csharp/tests/MSTestSettings.cs`             | 1     |
| `csharp/tests/MusicSearchCommandTests.cs`    | 112   |
| `csharp/tests/MusicSearchTests.cs`           | 72    |
| `csharp/tests/ResilienceAsyncTests.cs`       | 99    |
| `csharp/tests/ResilienceTests.cs`            | 84    |
| `csharp/tests/YouTubeChangeDetectorTests.cs` | 88    |

### State (1)
| File                                            | Description       |
| ----------------------------------------------- | ----------------- |
| `state/youtube/playlists/Roger Norrington.json` | New playlist data |

---

## Files Modified (21)

### CLI Layer
| File                              | Changes |
| --------------------------------- | ------- |
| `csharp/src/CLI/MailCommands.cs`  | +1/-1   |
| `csharp/src/CLI/MusicCommands.cs` | +582    |
| `csharp/src/CLI/SyncCommands.cs`  | +99     |

### Infrastructure
| File                                      | Changes                               |
| ----------------------------------------- | ------------------------------------- |
| `csharp/src/Infrastructure/Console.cs`    | -36 (removed content)                 |
| `csharp/src/Infrastructure/Logger.cs`     | -16 (removed content)                 |
| `csharp/src/Infrastructure/Paths.cs`      | +1                                    |
| `csharp/src/Infrastructure/Resilience.cs` | Major refactor (-336 lines rewritten) |

### Models
| File                                 | Changes |
| ------------------------------------ | ------- |
| `csharp/src/Models/TrackMetadata.cs` | +8/-0   |

### Orchestrators
| File                                                      | Changes |
| --------------------------------------------------------- | ------- |
| `csharp/src/Orchestrators/ScrobbleSyncOrchestrator.cs`    | +42     |
| `csharp/src/Orchestrators/YouTubePlaylistOrchestrator.cs` | +44     |

### Services
| File                                                         | Changes           |
| ------------------------------------------------------------ | ----------------- |
| `csharp/src/Services/Mail/MailTmService.cs`                  | +28               |
| `csharp/src/Services/Music/DiscogsService.cs`                | +14               |
| `csharp/src/Services/Music/MusicBrainzService.cs`            | +12               |
| `csharp/src/Services/Sync/Google/GoogleCredentialService.cs` | +46               |
| `csharp/src/Services/Sync/Google/GoogleSheetsService.cs`     | +336 (major)      |
| `csharp/src/Services/Sync/LastFm/LastFmService.cs`           | +17               |
| `csharp/src/Services/Sync/YouTube/YouTubeChangeDetector.cs`  | +164              |
| `csharp/src/Services/Sync/YouTube/YouTubeService.cs`         | +168 (refactored) |

### Project Files
| File                          | Changes |
| ----------------------------- | ------- |
| `csharp/CSharpScripts.csproj` | +1      |
| `csharp/src/GlobalUsings.cs`  | +1      |
| `csharp/src/Program.cs`       | +25     |

---

## Files Renamed (1)

| From                                       | To                                       |
| ------------------------------------------ | ---------------------------------------- |
| `state/youtube/playlists/Geopolitics.json` | `state/youtube/deleted/Geopolitics.json` |

---

## Files Deleted (0)

*No files were deleted in these commits.*

---

## Key Changes by Area

### 1. Resilience Infrastructure Overhaul
`csharp/src/Infrastructure/Resilience.cs` underwent a major refactor:
- Renamed `operationName` parameter to `operation` for brevity
- Removed `postAction` callback pattern - delays now handled internally
- Added `ExecuteAsync` overloads for async operations
- Added `IsFatalQuotaError()` helper for detecting quota-exceeded errors
- Simplified the retry/backoff logic

### 2. Google Sheets Service Enhancement
`csharp/src/Services/Sync/Google/GoogleSheetsService.cs`:
- Added async methods alongside sync methods
- Enhanced ETag-based conditional request support
- Improved batch operations for reading/writing

### 3. YouTube Service Refactoring
`csharp/src/Services/Sync/YouTube/YouTubeService.cs`:
- Extracted `FetchAllPlaylistItems()` as shared pagination helper
- Reduced code duplication between `GetPlaylistSummaries`, `GetPlaylistMetadata`, `GetAllPlaylists`
- Updated to use simplified `Resilience.Execute` API (no `postAction`)
- Added `request.Fields` for partial responses (bandwidth optimization)

### 4. New Test Suite
Created `csharp/tests/` project with MSTest + Shouldly:
- `ResilienceTests.cs` - Sync resilience tests
- `ResilienceAsyncTests.cs` - Async resilience tests
- `YouTubeChangeDetectorTests.cs` - Change detection logic tests
- `LastFmServiceTests.cs` - Last.fm service tests
- `MusicSearchTests.cs` - Music search tests
- `MusicSearchCommandTests.cs` - CLI command tests

### 5. Music Metadata Schema
New `csharp/src/Models/MusicMetadataSchema.cs` (700 lines):
- Comprehensive schema definitions for music metadata
- Supports MusicBrainz and Discogs field mappings

### 6. CLI Tab Completion
New `csharp/src/CLI/CompletionCommands.cs` (192 lines):
- PowerShell/Bash/Zsh completion script generation
- Dynamic completion for commands and arguments

---

## State File Changes

| File                                        | Change Type |
| ------------------------------------------- | ----------- |
| `logs/lastfm.jsonl`                         | Updated     |
| `logs/youtube.jsonl`                        | Updated     |
| `state/lastfm/scrobbles.json`               | Updated     |
| `state/lastfm/sync.json`                    | Updated     |
| `state/youtube/playlists/Simon Rattle.json` | Updated     |
| `state/youtube/sync.json`                   | Updated     |
