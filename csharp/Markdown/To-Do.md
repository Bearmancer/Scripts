# Unified Project To-Do List

**Last Updated: 2025-12-16**

---

## ⚠️ DOCUMENTATION FIRST (Before Any Refactoring)

All documentation tasks must be completed before proceeding with code refactoring.

---

## Phase 0: Answer Pending Questions in Markdown Files

### 0.1 CLIDesign.md Questions

- [x] **Q: Is barcode the same as catalogue number?**
  - Answer: No. Barcode = UPC/EAN (universal product code), CatNo = label-specific catalog number
  - ✅ Updated CLIDesign.md line 55 with clarification

- [x] **Q: Discogs does not have a `work` field - how to implement?**
  - Answer: Extract from title using regex patterns (Symphony No., Concerto, Op., etc.)
  - ✅ Documented in CLIDesign.md and DiscogsSearchSorting.md

- [x] **Q: What is the purpose of the 4-Mode Matrix?**
  - Answer: Shows how search behavior differs across all mode/source combinations
  - ✅ Added explanatory paragraph in CLIDesign.md

- [x] **Q: JSON output for scripting - can't C# handle it natively?**
  - ✅ Created `JSONScriptingIntegration.md`
  - Explains PowerShell/shell piping vs native C# processing

### 0.2 New Documentation Files Created

- [x] **`LookupVsSearch.md`** - Explains purpose of lookup and schema when search exists
- [x] **`JSONScriptingIntegration.md`** - JSON output use cases and examples
- [x] **`DotNetTabCompletion.md`** - .NET 10 tab completion research

### 0.3 MusicBrainz Scoring Questions

- [x] **Q: How to show match score from API?**
  - ✅ Updated MusicBrainzScoring.md with implementation code
  - Score available via `result.Score` (0-100)

### 0.4 Discogs Search Sorting Questions

- [x] **Q: Does Discogs have `master` releases like MusicBrainz?**
  - ✅ Clarified in DiscogsSearchSorting.md - YES, Discogs has Master Releases

- [x] **Write Do/Don't examples for Discogs search**
  - ✅ Added to DiscogsSearchSorting.md with code examples

- [x] **Write classical vs pop search examples**
  - ✅ Added to DiscogsSearchSorting.md with expected output tables

### 0.5 Updated Existing Documentation

- [x] **MusicBrainzClassicalDifferences.md**
  - ✅ Added logging strategy for re-recorded works
  - ✅ Removed fallback/default value patterns
  - ✅ Added API optimization notes

- [x] **ClassicalModeImplementation.md**
  - ✅ Added XML doc comments for column ordering
  - ✅ Added date formatting standard (MMM dd, yyyy)
  - ✅ Added code comments showing how to change formatting

---

## Phase 1: Logging Improvements

### 1.1 YouTube Sync Logging ✅
- [x] Enhanced `LogDetailedChanges` with compact format showing:
  - Modified playlists with video delta (+/- count)
  - New playlists with video count
  - Renamed playlists showing old → new title
  - Deleted playlists by name

### 1.2 Music Search Logging ✅
- [x] Created `dumps/music-search/{timestamp}-{query}/` folder structure
- [x] Each search result saved as individual JSON: `001-discogs-12345.json`
- [x] Combined results saved as `_all-results.json`
- [x] Activated in debug mode (`--debug` flag)

---

## Phase 2: CLI Rules (To Be Enforced)

### 2.1 Verbose Mode Behavior ✅
- [x] Debug mode now shows ALL fields: Score, Country, Genres, CatNo, Barcode
- [x] 13 columns in debug mode vs 5 in standard mode

### 2.2 No Default/Fallback Values ✅
- [x] Replaced `?? "Unknown"` with empty/"—" in display code
- [x] Updated: MusicCommands.cs, MailCommands.cs, YouTubeService.cs
- [x] Note: Error messages still use "Unknown" appropriately (e.g., exception text)

### 2.3 Terminal Testing ✅
- [x] Created `TerminalCompatibility.md` documentation
- [x] Documented hyperlink support matrix (Windows Terminal, iTerm2, VS Code)
- [x] Documented color theme and table rendering recommendations

---

## Phase 3: Code Refactoring (AFTER Documentation Complete)

### 3.1 Google Sheets Optimizations
- [x] Add field masks to `GetSpreadsheetMetadata`
- [x] Combine `InsertRows` into single batchUpdate
- [x] Group consecutive row deletions
- [x] Optimize `WriteRecords` (3 calls → 2)

### 3.2 YouTube Service Optimizations
- [x] Add field filters to all API requests
- [x] Extract common pagination logic into `FetchAllPlaylistItems`
- [x] Deduplicate playlist fetching methods

### 3.3 Async Migration ✅
- [x] `GoogleCredentialService.GetCredentialAsync` / `GetAccessTokenAsync`
- [x] `GoogleSheetsService.ExportEachSheetAsCSVAsync` (new async version)
- [x] `SyncAllCommand` → `AsyncCommand` with proper `await`
- [x] Centralize verbose handling (done via `--debug` flag)
- [x] Batch `SaveState()` - already batched in `onBatchComplete` callback

---

## Phase 4: Testing

### 4.1 Tests (29 passing) ✅
- [x] `LastFmServiceTests` (4 tests)
- [x] `MusicSearchTests` (5 tests)
- [x] `ResilienceTests` (4 tests)
- [x] `ResilienceAsyncTests` (5 tests - NEW)
- [x] `YouTubeChangeDetectorTests` (7 tests)
- [x] `MusicSearchCommandTests` (4 tests)

### 4.2 Pending Tests
- [x] Unit tests for SearchResult model with new fields
- [x] Async method tests (`ExecuteAsync`, throttle, cancellation)
- [ ] Integration tests for sync commands (requires API credentials - USER)
- [ ] Manual verification of search commands (USER)
- [ ] Verify Last.fm scrobble count (USER)

---

## Phase 5: CLI Implementation (Music Search)

### 5.1 Options ✅
- [x] `--mode pop|classical` (default: pop) - changes display columns
- [x] `--source discogs|musicbrainz|both` (default: discogs)
- [x] `--limit <N>` (default: 10)
- [x] `--fields <list>` (comma-separated) - dynamic field selection
- [x] `--output table|json` (default: table)

### 5.2 Search Behavior ✅
- [x] Pop mode: Artist, Title, Year, Type, ID columns
- [x] Classical mode: Composer, Work, Performers, Year, ID columns  
- [x] Basic composer/work extraction from title patterns
- [x] Display MusicBrainz score in debug mode
- [x] Client-side relevance scoring for Discogs (fuzzy matching)
- [x] Results sorted by score descending
- [x] Track filtering - excludes Recording/track types, focuses on collections

---

## Notes

### Coding Standards
- Named arguments for all Resilience calls
- No `[Obsolete]` flags -- remove methods directly
- Fail-fast on non-transient errors
- Sequential external API calls only (enforced by `SemaphoreSlim`)
- Color-coded, verbose logging
- **No default values ever** (empty string if missing, not placeholder)
- **Async suffix** only when both sync/async versions exist
- **Verbose flag**: `-v|--verbose` boolean (not a level value)

### Deferred (Low Priority)
- ETag conditional requests for YouTube (ETag is cached but not sent)
- Parallel API operations (currently prevented by design)

---

## Documentation Created

- `AsyncSyncDesign.md` - Async/sync coexistence patterns
- `TerminalCompatibility.md` - Terminal support matrix
- `LookupVsSearch.md` - API command distinctions
- `JSONScriptingIntegration.md` - JSON output for scripting
- `DotNetTabCompletion.md` - Tab completion research