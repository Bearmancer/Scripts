# Implementation Recommendations
*Created: January 2025*

## Summary of Research Findings

### 1. Completion System Status ✅

**Current State: WORKING CORRECTLY**
- PSCompletions (Menu UI): 2 completions (`psc`, `winget`)
- Carapace (Data Provider): 670 completions
- argc (Supplemental): 2 completions (`dotnet`, `whisper-ctranslate2`)
- Conflicts: RESOLVED (removed overlapping PSCompletions)
- Tab works without errors

**Remaining Minor Issues:**
| Issue | Priority | Fix |
|-------|----------|-----|
| COMP-001: Double space on Tab | Medium | Verify `completion_suffix=""` |
| COMP-002: Tab selects instead of fills | Medium | `psc config enable_enter_when_single 1` |
| COMP-003: winget dynamic search | Low | PSCompletions limitation - use `winget search` |

### 2. PSFzf Decision ✅

**Recommendation: ADD PSFzf with LAZY LOADING**

**Why?**
- PSCompletions/Carapace don't provide fuzzy history search
- PSReadLine native Ctrl+R is substring search (not fuzzy)
- ~421ms impact can be deferred to first use

**Implementation (add to profile):**
```powershell
#region PSFzf - Lazy loaded on first Ctrl+R/Ctrl+T
Set-PSReadLineKeyHandler -Key 'Ctrl+r' -ScriptBlock {
    if (-not (Get-Module PSFzf)) {
        Import-Module PSFzf
        Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t' -PSReadlineChordReverseHistory 'Ctrl+r'
    }
    Invoke-FzfPsReadlineHandlerHistory
}
Set-PSReadLineKeyHandler -Key 'Ctrl+t' -ScriptBlock {
    if (-not (Get-Module PSFzf)) {
        Import-Module PSFzf
        Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t' -PSReadlineChordReverseHistory 'Ctrl+r'
    }
    Invoke-FzfPsReadlineHandlerProvider
}
#endregion
```

**Benchmark Impact:**
- Startup: 0ms (lazy loaded)
- First Ctrl+R: ~421ms (one-time load)
- Subsequent: ~0ms

**Alternatives Evaluated:**
| Option | Pros | Cons |
|--------|------|------|
| PSReadLine Ctrl+R | Zero overhead | Substring only, not fuzzy |
| Out-GridView F7 | Zero overhead, visual | Not fuzzy, blocks terminal |
| F7History | Console GUI popup | ~471ms, not fuzzy |
| PSFzf (lazy) | True fuzzy search | 421ms on first use |

---

## Priority Task Recommendations

### Immediate (Can implement now)

#### 1. Add PSFzf Lazy Loading to Profile
**Task:** COMP-016 (modified for lazy loading)
**Time:** 5 minutes
**Impact:** Adds fuzzy history without startup penalty

#### 2. Fix Tab Behavior Issues
**Tasks:** COMP-001, COMP-002
**Time:** 2 minutes
```powershell
psc config enable_enter_when_single 1  # COMP-002
# Verify completion_suffix is empty
$PSCompletions.config.completion_suffix
```

---

### Short Term (This Week)

#### 3. CS-003: Add Regions to Large C# Files ⭐⭐⭐ High
**Files needing regions (>300 lines):**
1. `GoogleSheetsService.cs` (1230 lines) - Auth, Read, Write, Sync, Export
2. `MusicBrainzService.cs` (1041 lines) - Search, Lookup, Parse, Format
3. `MusicSearchCommand.cs` (1035 lines) - Settings, Search Modes, Rendering
4. `YouTubePlaylistOrchestrator.cs` (1029 lines) - Fetch, Compare, Sync, Progress
5. `DiscogsService.cs` (633 lines) - Auth, Search, Parse, Format
6. `MusicFillCommand.cs` (612 lines) - Settings, TSV Processing, Output
7. `SyncCommands.cs` (459 lines) - YouTube, LastFm, Status, Help
8. `Console.cs` (414 lines) - Logging, Progress, Tables, Formatting
9. `MailTmService.cs` (385 lines) - Auth, Messages, Parse
10. `Logger.cs` (331 lines) - Config, File, Console, Format

**Time:** 1-2 hours total
**Approach:** Add regions without changing logic, semantic groupings

#### 4. CS-019: Fix Console.cs Wrapper Violation ⭐⭐ Medium
**Current Violation:** [CompletionCommands.cs#L55-L66](csharp/src/CLI/CompletionCommands.cs#L55-L66)
```csharp
// Direct Spectre call (BAD):
var panel = new Panel(new Markup(...))

// Should use wrapper (GOOD):
Console.WritePanel(title: "System Configuration", content: "...", borderColor: Color.Blue)
```

**Required Changes:**
1. Add `WritePanel(string title, string content, Color? borderColor = null)` to Console.cs
2. Refactor CompletionCommands.cs to use wrapper

**Time:** 15 minutes

---

### Medium Term (This Month)

#### 5. CS-007 to CS-016: Music Command Improvements ⭐⭐⭐-⭐⭐⭐⭐
These tasks are interconnected improvements to `music fill`:

| Task | Description | Dependency |
|------|-------------|------------|
| CS-007 | Search both Discogs AND MusicBrainz | None |
| CS-008 | Fix search result formatting | CS-007 |
| CS-009 | Integrate live progress bar | CS-007 |
| CS-010 | Show found fields with elapsed time | CS-009 |
| CS-011 | Write found fields in real-time | CS-010 |
| CS-012 | Prefer first pressing for labels | CS-007 |
| CS-013 | Match label with catalog number | CS-008 |
| CS-014 | Auto-shorten label names | CS-011 |
| CS-015 | Create auto-filled TSV output | CS-011 |
| CS-016 | Finish missing fields implementation | CS-007 |

**Recommended Order:**
1. CS-007 (parallel search) - Foundation
2. CS-008 (formatting) - UI improvement
3. CS-016 (missing fields) - Core feature
4. CS-009 (live progress) - UX improvement
5. CS-013 (label/catalog matching) - Data quality

**Time:** 4-6 hours total

#### 6. CS-029: Merge Music Commands with Regions ⭐⭐⭐ High
After CS-003, consider merging:
- `MusicFillCommand.cs` (612 lines)
- `MusicSearchCommand.cs` (1035 lines)
- → `MusicCommands.cs` (~1600 lines with regions)

**Regions:**
```csharp
#region Fill Command Settings
#region Fill Command Execution
#region Search Command Settings
#region Search Command Execution
#region Shared Search Logic
#region Output Rendering
```

---

### Long Term (Next Month)

#### 7. Python to C# Migration (CS-024, CS-025, PY-004-008) ⭐⭐⭐⭐
**Python Toolkit Functions to Migrate:**

| Module | Functions | C# Equivalent |
|--------|-----------|---------------|
| audio.py | 17 | NAudio / FFmpeg.NET |
| video.py | 17 | FFmpeg.NET / ImageSharp |
| lastfm.py | 4 | LastFmService (exists) |
| cuesheet.py | 5 | New CuesheetService |
| filesystem.py | 6 | System.IO wrappers |
| cli.py | 12 | Already in Spectre.Console.Cli |

**Recommended Approach:**
1. **Keep Python for now** - Working, stable
2. **Don't duplicate in PowerShell** - Adds maintenance burden
3. **Migrate to C# incrementally** - When Python version needs changes
4. **Priority for migration:** lastfm.py (scrobble sync to Google Sheets)

**lastfm.py Migration Plan:**
- `authenticate_google_sheets()` → Use Google.Apis.Sheets.v4
- `get_last_scrobble_timestamp()` → GoogleSheetsService.cs (extend)
- `prepare_track_data()` → New LastFmScrobbleService.cs
- `sync_scrobbles()` → Integrate with existing SyncCommands.cs

---

## Files Created This Session

1. [markdown/explanations/completion_architecture.md](markdown/explanations/completion_architecture.md)
   - Complete documentation of completion system interplay
   - PSCompletions + Carapace + argc data flow
   - PSFzf lazy loading implementation

2. [markdown/implementation/implementation_recommendations.md](markdown/implementation/implementation_recommendations.md) (this file)
   - Priority task recommendations
   - Implementation approach for each task

---

## Quick Reference: Profile Changes

### Add PSFzf Lazy Loading
Add after the argc region in profile:

```powershell
#region PSFzf - Lazy loaded fuzzy finder
Set-PSReadLineKeyHandler -Key 'Ctrl+r' -ScriptBlock {
    if (-not (Get-Module PSFzf)) {
        Import-Module PSFzf
        Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t' -PSReadlineChordReverseHistory 'Ctrl+r'
    }
    Invoke-FzfPsReadlineHandlerHistory
}
Set-PSReadLineKeyHandler -Key 'Ctrl+t' -ScriptBlock {
    if (-not (Get-Module PSFzf)) {
        Import-Module PSFzf
        Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t' -PSReadlineChordReverseHistory 'Ctrl+r'
    }
    Invoke-FzfPsReadlineHandlerProvider
}
#endregion
```

### Fix Tab Behavior
Run once:
```powershell
psc config enable_enter_when_single 1
```

---

## Next Actions

1. **Now:** Apply PSFzf lazy loading to profile
2. **Now:** Fix Tab behavior with `psc config`
3. **This week:** Add regions to 5 largest C# files
4. **This week:** Fix Console.cs wrapper violation
5. **This month:** Implement CS-007 (parallel search) as foundation for music improvements
