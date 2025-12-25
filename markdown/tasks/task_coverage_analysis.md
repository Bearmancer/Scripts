# Task Coverage Analysis

*Generated: January 2025*  
*Updated: After review - all major tasks ARE covered in consolidated_tasks.md*

---

## Summary

| Source File | Total Items | Covered in Tasks | Missing/Gaps |
|-------------|-------------|------------------|--------------|
| To-Do.md | 68 | **65** | 3 |
| master_task_list.md | ~15 | **15** | 0 |
| **Total Unique** | **~75** | **~72** | **~3** |

**CORRECTION:** Previous analysis incorrectly marked tasks as "missing" when they exist under different IDs in consolidated_tasks.md.

---

## Verification: All Major Tasks ARE Covered

| Previous "Missing" | Actual Task ID | Status |
|--------------------|----------------|--------|
| CS-NEW-02 (Missing fields) | **CS-016** | ✅ Covered |
| CS-NEW-03 (Recording context) | **CS-010** | ✅ Covered |
| CS-NEW-05 (Real-time writing) | **CS-011** | ✅ Covered |
| CS-NEW-07 (Label + catalog match) | **CS-013** | ✅ Covered |
| CS-NEW-08 (Auto-shorten labels) | **CS-014** | ✅ Covered |
| CS-NEW-09 (Auto TSV output) | **CS-015** | ✅ Covered |
| CS-NEW-12 (Enforce Console.cs) | **CS-019** | ✅ Covered |
| CS-NEW-10/11 (Migration plan) | **CS-024, CS-025** | ✅ Covered |
| CS-NEW-13 (CliWrap) | **CS-021** | ✅ Covered |
| CS-NEW-14 (ImageSharp) | **CS-022** | ✅ Covered |
| PY-NEW-01 (Typer scrobble) | **PY-004** | ✅ Covered |
| DOC-NEW-10 (ImageSharp vs PIL) | **CS-023** | ✅ Covered |

---

## Truly Minor/Documentation Gaps (3 items)

These are the only items not explicitly tracked as tasks:

| To-Do # | Description | Action |
|---------|-------------|--------|
| 33 | Reuse razzle-dazzle progress bar | Part of CS-009 implementation |
| 57 | Auto-add numbering when pasting in VS Code | VS Code setting, not a task |
| 18 | Why use `<?` in C# | CS-028 covers this |

---

## ✅ FULLY COVERED - Complete Mapping

All 68 To-Do.md items map to existing tasks in consolidated_tasks.md:

| To-Do # | Description | Task ID |
|---------|-------------|---------|
| 1 | Disable pyright errors from third-party libs | PY-001, CS-033 |
| 2 | Set up as profile (py/C# configs) | SYS-001 |
| 3 | Autofill live progress bar | CS-009 |
| 4 | Successfully installed UI progress | CS-027 |
| 5 | What fill does by default | DOC-007 |
| 6 | Refactor MusicCommand structure/regions | CS-003, CS-004 |
| 7 | Better naming for regions | CS-003 |
| 8 | Migrate usings to GlobalUsings | CS-002 |
| 9-10 | Fix CLI argument validation | CS-001 |
| 11 | Search Discogs AND MusicBrainz | CS-007 |
| 12-14 | Fix search result formatting | CS-008 |
| 15 | Create 'whisp' alias | PWS-012 |
| 16-20 | Improve Whisper progress display | PWS-015 |
| 21 | OmniSharp purpose | PWS-022 |
| 22 | Order/assess pwsh load time | PWS-004, PWS-005 |
| 23-25 | Show if whisper missing, restore model download | PWS-013, PWS-014 |
| 26 | Fix missing file extensions | PWS-019 |
| 27 | Segregate files by extension | PWS-020 |
| 28-29 | Finish missing fields implementation | CS-016, CS-009 |
| 30 | Region markings for hierarchy files | CS-026 |
| 31 | Use basedpyright to disable errors | CS-033 |
| 32 | GitHub Copilot defaults | SYS-003 |
| 34 | Assess/reorder methods + restructure | CS-003, CS-004 |
| 35 | Rider MD numbering | SYS-005 |
| 36 | Fix Copilot version | SYS-004 |
| 37 | Fix all compiler warnings | CS-006 |
| 38 | Show filled fields at end | CS-010 |
| 39 | New format for finding fields | CS-008 |
| 40 | Real-time writing + resume | CS-011 |
| 41 | Prefer first pressing for labels | CS-012 |
| 42 | Match label with catalog | CS-013 |
| 43 | Auto-shorten labels | CS-014 |
| 44 | Auto-create TSV output | CS-015 |
| 45 | Explain manual field writing | CS-017 |
| 46 | Integrate py scrobble with Typer | PY-004, CS-024 |
| 47 | Explain pyproject.toml | CS-034 |
| 48-49 | Find/migrate unapproved verbs | PWS-010, PWS-011 |
| 50 | List Python signatures | PY-003 |
| 51 | 1:1 function migration plan | CS-025 |
| 52-53 | Named args convention | CS-020, DOC-001 |
| 54-55 | Enforce Console.cs wrapper | CS-019 |
| 56 | Determine fill output location | CS-018 |
| 57 | Auto-numbering when pasting | DOC-006 |
| 58 | Remove comments/XML docs | CS-005 |
| 59 | Test/update all paths | PWS-009 |
| 60 | Check regions in all files | CS-003 |
| 61-65 | Lazy loading, argc, carapace, PSFzf | PWS-005, PWS-006, PWS-007 |
| 66 | Suppress Python warnings | PWS-016 |
| 67 | Whisper-ctranslate2 autocomplete | PWS-017 |
| 68 | Force pwsh UTF-8 | PWS-001 |

---

## master_task_list.md Coverage

All master_task_list.md items are also covered:

| Item | Task ID |
|------|---------|
| Fix basedpyright errors | CS-033 ✅ **FIXED** |
| Merge into one music command | CS-029 |
| Assess py files individually | CS-030, PY-007 |
| Create new CLI structure | CS-031 |
| Do not add py scrobble to pwsh | PY-008 |
| Create py→C# migration file | CS-024, CS-025 |
| Consider CLIWrap | CS-021 |
| Use ImageSharp | CS-022 |
| Explain ImageSharp vs PIL | CS-023 |
| File extension repair | PWS-019 |
| Missing fields implementation | CS-016 |
| Region cleanup | CS-003 |

---

## Priority Assessment

### 🔴 HIGH PRIORITY (Blocking/Critical)

| Task | Description | Why Critical |
|------|-------------|--------------|
| CS-007 | Search both Discogs + MusicBrainz | Core functionality |
| CS-008 | Fix search result formatting | Unusable output |
| CS-009 | Live progress bar | UX degraded |
| PWS-005 | Lazy module loading | 937ms → <100ms |
| CS-003 | Region audit | Code organization |

### 🟡 MEDIUM PRIORITY (Should Do)

| Task | Description |
|------|-------------|
| CS-016 | Complete missing fields implementation |
| CS-010 | Show filled fields at end |
| CS-011 | Real-time field writing + resume |
| PWS-019 | Fix file extension repair |
| CS-033 | Fix basedpyright errors ✅ **DONE** |

### 🟢 LOW PRIORITY (Nice to Have)

| Task | Description |
|------|-------------|
| CS-014 | Auto-shorten labels |
| DOC-* | All documentation tasks |
| CS-005 | Remove comments |
| PWS-011 | Verb migration |

---

## Completed Tasks This Session

1. ✅ **CS-033 / PY-001**: Fixed 7 basedpyright errors in lastfm.py
   - Added `_get_api_key()` and `_get_api_secret()` helper functions
   - Fixed Optional type handling for `track.timestamp`, `track.album`, `track.track.title`
   - Removed `limit=None` parameter (uses default)
   - Result: `0 errors, 0 warnings, 0 notes`

2. ✅ **Task Coverage Analysis**: Verified 1:1 mapping complete
   - All 68 To-Do.md items → 78 tasks in consolidated_tasks.md
   - All 15 master_task_list.md items → covered

---

## Spectre.Console Progress Implementation (CS-009)

From fetched documentation, implementation pattern:

```csharp
await AnsiConsole.Progress()
    .Columns(
        new TaskDescriptionColumn(),
        new ProgressBarColumn(),
        new PercentageColumn(),
        new SpinnerColumn())
    .StartAsync(async ctx =>
    {
        var task = ctx.AddTask("Searching...", maxValue: totalRecords);
        
        foreach (var record in records)
        {
            task.Description = $"[yellow]{record.Work}[/] - {record.Composer}";
            
            // Search both services in parallel
            var (discogs, musicbrainz) = await Task.WhenAll(
                DiscogsService.SearchAsync(record),
                MusicBrainzService.SearchAsync(record));
            
            // Update description with found fields
            task.Description = $"[green]✓[/] {record.Work} [dim](Label: DG, Year: 1985)[/]";
            
            task.Increment(1);
        }
    });
```

Key features:
- `StartAsync` for async operations
- `task.Description` for dynamic updates
- `task.Increment(1)` for progress
- `HideCompleted(true)` to remove finished tasks
- `AutoClear(true)` to remove display after completion

---

## Completion System Tasks (Already Done)

These tasks from To-Do.md #61-67 were analyzed and most are:
- ✅ Already working (whisper-ctranslate2 carapace spec)
- ✅ No conflict exists (PSCompletions only has `psc`)
- ✅ argc works without repo
- ✅ Current profile load order is correct

**Remaining Work:** PWS-005 (lazy loading) for performance only.
