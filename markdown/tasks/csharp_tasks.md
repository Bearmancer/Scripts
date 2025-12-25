# C# Tasks - Comprehensive List

*Generated: December 25, 2025*  
*Source Files: To-Do.md, master_task_list.md, File Size Analysis*

---

## Overview

**Total Tasks:** 42  
**Complexity Distribution:**
- ⭐ Low: 10 tasks
- ⭐⭐ Medium: 18 tasks
- ⭐⭐⭐ High: 11 tasks
- ⭐⭐⭐⭐ Very High: 3 tasks

**Files by Size Category:**
- **>1000 lines (4 files):** Require comprehensive regions
- **600-1000 lines (6 files):** Require functional regions
- **300-600 lines (10 files):** Require minimal regions
- **100-300 lines (11 files):** Consider selective regions
- **<100 lines (5 files):** No regions needed

---

## Category 1: Region Management

### File Size Analysis Results

| File | Lines | Category | Region Strategy |
|------|-------|----------|-----------------|
| GoogleSheetsService.cs | 1230 | Very Large | 5-7 semantic regions |
| MusicBrainzService.cs | 1041 | Very Large | 5-7 semantic regions |
| MusicSearchCommand.cs | 1035 | Very Large | 5-7 semantic regions |
| YouTubePlaylistOrchestrator.cs | 1029 | Very Large | 5-7 semantic regions |
| DiscogsService.cs | 632 | Large | 3-4 semantic regions |
| MusicFillCommand.cs | 612 | Large | 3-4 semantic regions |
| SyncCommands.cs | 459 | Medium-Large | 2-3 semantic regions |
| Console.cs | 414 | Medium-Large | 2-3 semantic regions |
| MailTmService.cs | 385 | Medium-Large | 2-3 semantic regions |
| Logger.cs | 331 | Medium | 2 semantic regions |
| StateManager.cs | 280 | Medium | 1-2 regions |
| YouTubeChangeDetector.cs | 269 | Medium | 1-2 regions |
| YouTubeService.cs | 254 | Medium | 1-2 regions |
| CleanCommands.cs | 235 | Medium | 1-2 regions |
| LastFmService.cs | 203 | Medium | 1-2 regions |
| ScrobbleSyncOrchestrator.cs | 203 | Medium | 1-2 regions |
| Resilience.cs | 202 | Medium | 1-2 regions |
| CompletionCommands.cs | 197 | Medium | 1-2 regions |
| Discogs.cs | 175 | Small-Medium | 0-1 regions |
| SyncProgressTracker.cs | 137 | Small-Medium | 0-1 regions |
| MailCommands.cs | 133 | Small-Medium | 0-1 regions |
| MusicBrainz.cs | 120 | Small | No regions |
| Program.cs | 111 | Small | No regions |
| MusicExporter.cs | 105 | Small | No regions |
| Music.cs | 104 | Small | No regions |
| LanguageDetector.cs | 96 | Small | No regions |
| YouTube.cs | 89 | Small | No regions |
| ... (remaining <100 lines) | ... | Tiny | No regions |

---

### CS-001: Add Regions to GoogleSheetsService.cs
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #60, File Analysis  
**Priority:** HIGH (largest file, 1230 lines)

**File:** `csharp\src\Services\Sync\Google\GoogleSheetsService.cs`

**Proposed Regions:**
```csharp
#region Initialization & Configuration

#region Sheet Operations (Get, Create, Update)

#region Last.fm Scrobble Sync

#region YouTube Playlist Sync

#region Data Transformation & Mapping

#region Helper Methods
```

**Implementation Steps:**
1. Read entire file to understand structure
2. Group methods by functionality
3. Insert region markers with semantic names
4. Verify no logic changes, only organization
5. Test compilation

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 30 minutes

---

### CS-002: Add Regions to MusicBrainzService.cs
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #60, File Analysis  
**Priority:** HIGH (1041 lines, complex API integration)

**File:** `csharp\src\Services\Music\MusicBrainzService.cs`

**Proposed Regions:**
```csharp
#region API Client Configuration

#region Recording Search

#region Release Search

#region Artist Lookup

#region Work Lookup

#region Response Parsing & Transformation

#region Rate Limiting & Error Handling
```

**Implementation Steps:**
1. Analyze method groupings
2. Separate search vs lookup vs transformation
3. Insert semantic region markers
4. Ensure rate limiting code is grouped
5. Test compilation

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 30 minutes

---

### CS-003: Add Regions to MusicSearchCommand.cs
**Complexity:** ⭐⭐⭐ High  
**Source:** To-Do.md #6, #34, #60, File Analysis  
**Priority:** CRITICAL (1035 lines, needs refactoring + regions)

**File:** `csharp\src\CLI\MusicSearchCommand.cs`

**Current Issues:**
- Why is `public sealed class` separate from command logic?
- Inconsistent region usage
- Methods may be out of logical order

**Proposed Regions:**
```csharp
#region Command Settings & Validation

#region Search Orchestration (Discogs + MusicBrainz)

#region Result Formatting & Display

#region Confidence Scoring

#region Helper Methods
```

**Investigation Questions:**
1. Is there shared logic with MusicFillCommand that should be extracted?
2. Should Settings class be nested or separate?
3. Are methods ordered by call sequence or alphabetically?

**Implementation Steps:**
1. Analyze command structure and execution flow
2. Consider extracting shared logic to service layer
3. Group methods by purpose (search, display, helpers)
4. Insert region markers
5. Assess if reordering methods improves readability
6. Test compilation and functionality

**Dependencies:** CS-007 (dual search implementation)  
**Blocks:** CS-004 (related command)  
**Estimated Time:** 1-2 hours

---

### CS-004: Add Regions to YouTubePlaylistOrchestrator.cs
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #60, File Analysis  
**Priority:** HIGH (1029 lines)

**File:** `csharp\src\Orchestrators\YouTubePlaylistOrchestrator.cs`

**Proposed Regions:**
```csharp
#region Initialization & State Management

#region Playlist Synchronization

#region Video Metadata Fetching

#region Change Detection & Tracking

#region Google Sheets Integration

#region Progress Reporting
```

**Implementation Steps:**
1. Understand orchestration flow
2. Group by sync phase (fetch, detect, update, report)
3. Insert region markers
4. Test compilation

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 30 minutes

---

### CS-005: Add Regions to DiscogsService.cs
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #60, File Analysis  
**Priority:** MEDIUM (632 lines)

**File:** `csharp\src\Services\Music\DiscogsService.cs`

**Proposed Regions:**
```csharp
#region API Client Configuration

#region Release Search

#region Response Parsing

#region Label & Catalog Extraction
```

**Implementation Steps:**
1. Group API methods
2. Separate search from parsing
3. Insert region markers
4. Test compilation

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 20 minutes

---

### CS-006: Add Regions to MusicFillCommand.cs
**Complexity:** ⭐⭐⭐ High  
**Source:** To-Do.md #6, #34, #60, File Analysis  
**Priority:** CRITICAL (612 lines, major UX improvements needed)

**File:** `csharp\src\CLI\MusicFillCommand.cs`

**Current Issues (from To-Do.md):**
- Atrocious output formatting (#12)
- No input field display (#13-14)
- No live progress indicator (#3, #29)
- Missing field format unclear (#38-39)
- No real-time TSV writing (#40)

**Proposed Regions:**
```csharp
#region Command Settings & Validation

#region TSV Input/Output

#region Missing Field Search (Discogs + MusicBrainz)

#region Live Progress Display

#region Result Formatting & Presentation

#region Helper Methods
```

**Related Tasks:**
- CS-007: Dual search (Discogs + MusicBrainz)
- CS-008: Fix output formatting
- CS-009: Live progress bar
- CS-010: Show found fields format
- CS-011: Real-time TSV writing
- CS-012: First pressing logic
- CS-013: Label + catalog matching
- CS-014: Auto-abbreviation (DG)
- CS-015: Auto-filled TSV generation

**Implementation Steps:**
1. Add regions for organization
2. See Category 3 for functionality improvements
3. Test compilation

**Dependencies:** Multiple (see Category 3)  
**Blocks:** CS-007 through CS-015  
**Estimated Time:** 30 minutes (regions only, see Category 3 for full refactor)

---

### CS-007 through CS-020: Additional Region Tasks

**Following same pattern for files 300-600 lines:**
- CS-007: SyncCommands.cs (459 lines) - 2-3 regions
- CS-008: Console.cs (414 lines) - 2-3 regions
- CS-009: MailTmService.cs (385 lines) - 2-3 regions
- CS-010: Logger.cs (331 lines) - 2 regions

**Files 100-300 lines (minimal/selective regions):**
- CS-011: StateManager.cs (280 lines) - 1-2 regions
- CS-012: YouTubeChangeDetector.cs (269 lines) - 1-2 regions
- CS-013: YouTubeService.cs (254 lines) - 1-2 regions
- CS-014: CleanCommands.cs (235 lines) - 1-2 regions
- CS-015: LastFmService.cs (203 lines) - 1-2 regions
- CS-016: ScrobbleSyncOrchestrator.cs (203 lines) - 1-2 regions
- CS-017: Resilience.cs (202 lines) - 1-2 regions
- CS-018: CompletionCommands.cs (197 lines) - 1-2 regions

**Files <100 lines (NO regions):**
- CS-019: Review and REMOVE regions if any exist in files <100 lines
- CS-020: Verify compilation after all region changes

---

## Category 2: Code Cleanup & Standardization

### CS-021: Migrate All Usings to GlobalUsings.cs
**Complexity:** ⭐ Low  
**Source:** To-Do.md #8  
**Priority:** MEDIUM (code cleanliness)

**Current State:**
- Each file has individual using statements
- `GlobalUsings.cs` exists but not fully utilized
- Some usings duplicated across files

**Target State:**
```csharp
// GlobalUsings.cs
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using System.Threading.Tasks;
global using Spectre.Console;
global using SpectreConsole = Spectre.Console;  // Alias to avoid ambiguity
global using CSharpScripts.Infrastructure;
global using CSharpScripts.Models;
// ... all common usings

// Individual files
namespace CSharpScripts.CLI;
// No using statements needed!

public sealed class MusicSearchCommand : AsyncCommand<MusicSearchCommand.Settings>
{
    // ...
}
```

**Files:**
- `csharp\src\GlobalUsings.cs` (update)
- All `.cs` files in `csharp\src\**\*` (remove usings)

**Implementation Steps:**
1. Scan all `.cs` files for using statements
2. Create frequency table (which usings appear most often)
3. Add common usings to GlobalUsings.cs with `global using`
4. Add aliases for ambiguous types (e.g., Console vs Spectre.Console)
5. Remove usings from individual files
6. Test compilation for missing/ambiguous references

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 1 hour

---

### CS-022: Remove All Comments and XML Docs
**Complexity:** ⭐ Low  
**Source:** To-Do.md #58  
**Priority:** LOW (follows AI coding instructions)

**Rationale:** AI coding instructions specify no comments; code should be self-documenting

**Patterns to Remove:**
```csharp
// Single-line comments
/* Multi-line
   comments */
/// <summary>
/// XML documentation comments
/// </summary>
```

**Files:**
- All `.cs` files in `csharp\src\**\*`

**Implementation Steps:**
1. Use regex find/replace to remove:
   - `^\s*//.*$` (single-line comments)
   - `/\*.*?\*/` (multi-line comments, multiline mode)
   - `^\s*///.*$` (XML doc comments)
2. Manually review to ensure:
   - No commented-out code is removed that should be deleted
   - No comments in strings are affected
3. Test compilation

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 30 minutes

---

### CS-023: Fix All Compiler Warnings
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #37  
**Priority:** MEDIUM (code quality)

**Common Warnings:**
- Collection initialization can be simplified
- Nullable reference type warnings
- Unused variable warnings
- Implicit string conversions

**Implementation:**
```bash
dotnet build > warnings.txt
# Review warnings.txt
# Fix each category
```

**Files:**
- All `.cs` files with warnings

**Implementation Steps:**
1. Run `dotnet build` and capture warnings
2. Categorize warnings by type
3. Fix each category systematically:
   - Collection init: Use `[...]` syntax
   - Nullability: Add `?` or `!` operators
   - Unused vars: Remove or prefix with `_`
4. Re-run build until zero warnings
5. Update `.editorconfig` if needed to suppress unavoidable warnings

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 1-2 hours

---

## Category 3: Music Fill Command Enhancements

### CS-024: Implement Dual Search (Discogs + MusicBrainz)
**Complexity:** ⭐⭐⭐⭐ Very High  
**Source:** To-Do.md #11  
**Priority:** CRITICAL (core functionality improvement)

**Current State:**
```csharp
// Only searches Discogs
var discogsResults = await _discogsService.SearchRelease(query);
```

**Target State:**
```csharp
// Search BOTH services in parallel
var (discogsTask, mbTask) = (
    _discogsService.SearchRelease(query),
    _musicBrainzService.SearchRelease(query)
);
await Task.WhenAll(discogsTask, mbTask);

// Merge and sort by confidence
var allResults = discogsTask.Result
    .Concat(mbTask.Result)
    .OrderByDescending(r => r.Confidence)
    .ToList();
```

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`
- `csharp\src\CLI\MusicSearchCommand.cs`
- `csharp\src\Services\Music\IMusicService.cs` (interface should support both)
- `csharp\src\Services\Music\DiscogsService.cs`
- `csharp\src\Services\Music\MusicBrainzService.cs`

**Implementation Steps:**
1. Ensure both services implement `IMusicService`
2. Inject both services into commands
3. Query both in parallel using `Task.WhenAll`
4. Merge results with source attribution
5. Sort by confidence score
6. Update display to show source (Discogs vs MusicBrainz)
7. Test with various queries

**Dependencies:** None  
**Blocks:** CS-025, CS-026, CS-027  
**Estimated Time:** 3-4 hours

---

### CS-025: Fix Atrocious Output Formatting
**Complexity:** ⭐⭐⭐ High  
**Source:** To-Do.md #12-14  
**Priority:** CRITICAL (UX disaster)

**Current Output:**
```
Symphony No. 7 - Bruckner, Anton
Label:
50% Victor, Marmorsaal, Stift St. Florian (Discogs)
40% Victor, Victor Musical Industries, Inc. (Discogs)
Catalog #:
50% KVX 5501-2 (Discogs)
```

**Problems:**
1. No input context shown
2. Field names unclear
3. Ugly percentage-first format
4. No visual hierarchy

**Target Output:**
```
╔═══════════════════════════════════════════════════════════════╗
║ Recording: Symphony No. 7                                     ║
║ Composer:  Anton Bruckner                                     ║
║ Conductor: [Unknown]                                          ║
║ Orchestra: [Unknown]                                          ║
╚═══════════════════════════════════════════════════════════════╝

Missing Fields Search Results:

┌─ Label ───────────────────────────────────────────────────────┐
│ ✓ Victor                                          50% Discogs │
│   Victor Musical Industries, Inc.                 40% Discogs │
│   Deutsche Grammophon                             30% MusicBrainz │
└───────────────────────────────────────────────────────────────┘

┌─ Catalog Number ──────────────────────────────────────────────┐
│ ✓ KVX 5501-2                                      50% Discogs │
│   VDC-1214                                        40% Discogs │
└───────────────────────────────────────────────────────────────┘

✓ indicates highest confidence match
```

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`
- `csharp\src\Infrastructure\Console.cs` (add Panel, Table helpers)

**Implementation Steps:**
1. Create input context display (recording info)
2. Group results by field (Label, Year, Catalog#)
3. Use Spectre.Console Panels for each field
4. Mark highest confidence with ✓
5. Show source (Discogs/MusicBrainz) clearly
6. Right-align confidence percentages
7. Test with various recordings

**Dependencies:** CS-024 (dual search must work first)  
**Blocks:** None  
**Estimated Time:** 2-3 hours

---

### CS-026: Add Live Progress Bar for Autofill
**Complexity:** ⭐⭐⭐⭐ Very High  
**Source:** To-Do.md #3, #29  
**Priority:** HIGH (major UX improvement)

**Current Behavior:**
```
Searching for missing fields ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 100%
[Long wait with no details]
```

**Target Behavior:**
```
Searching for missing fields... [23/43 recordings]

┌─ Current ─────────────────────────────────────────────────────┐
│ Symphony No. 7 - Bruckner, Anton                              │
│ [✓ Label: Victor] [✓ Year: 1985] [? CatalogNumber]           │
│ Querying: MusicBrainz...                                      │
└───────────────────────────────────────────────────────────────┘

Elapsed: 02:15 | ETA: 04:30 | Rate: 0.17 rec/sec
```

**Implementation:**
```csharp
await AnsiConsole.Progress()
    .Columns(
        new TaskDescriptionColumn(),
        new ProgressBarColumn(),
        new PercentageColumn(),
        new SpinnerColumn()
    )
    .StartAsync(async ctx =>
    {
        var task = ctx.AddTask("Processing recordings", maxValue: recordings.Count);
        
        foreach (var recording in recordings)
        {
            // Update live status
            AnsiConsole.MarkupLine($"[cyan]{recording.Work} - {recording.Composer}[/]");
            
            // Show field search progress
            var fieldStatus = new List<string>();
            if (!string.IsNullOrEmpty(recording.Label))
                fieldStatus.Add("[green]✓ Label[/]");
            else
                fieldStatus.Add("[yellow]? Label[/] (Searching...)");
            
            AnsiConsole.MarkupLine(string.Join(" ", fieldStatus));
            
            // Search both services
            await SearchAndFill(recording);
            
            task.Increment(1);
        }
    });
```

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`
- `csharp\src\Infrastructure\Console.cs` (Progress wrapper methods)

**Implementation Steps:**
1. Wrap main loop in Spectre.Console.Progress
2. Add live status panel showing current recording
3. Show real-time field discovery (✓ Label found, ? Searching for Year)
4. Display service currently being queried
5. Show elapsed time and ETA
6. Test with large TSV file

**Dependencies:** CS-024 (dual search)  
**Blocks:** None  
**Estimated Time:** 3-4 hours

---

### CS-027: Improve Found Fields Display Format
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #38-39  
**Priority:** MEDIUM (UX polish)

**Current Behavior:**
```
[At end of run, unclear what was found]
```

**Target Behavior:**
```
┌─ Symphony No. 7 - Bruckner, Anton ────────────────────────────┐
│ Found:                                                        │
│   Label:          Victor                      (Discogs, 50%)  │
│   Year:           1985                        (Discogs, 60%)  │
│   Catalog Number: KVX 5501-2                  (Discogs, 50%)  │
│ Elapsed: 2.3s                                                 │
└───────────────────────────────────────────────────────────────┘
```

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`

**Implementation Steps:**
1. After each recording processed, create summary panel
2. List only fields that were FOUND (not missing)
3. Show source and confidence for each
4. Display elapsed time for that recording
5. Group all summaries at end

**Dependencies:** CS-024, CS-025  
**Blocks:** None  
**Estimated Time:** 1 hour

---

### CS-028: Real-Time TSV Writing
**Complexity:** ⭐⭐⭐ High  
**Source:** To-Do.md #40  
**Priority:** HIGH (data safety - avoid losing progress)

**Current Behavior:**
```csharp
// Processes all recordings in memory
// Writes TSV only at very end
// If crash/cancel, all progress lost!
```

**Target Behavior:**
```csharp
// Write to TSV after EACH recording processed
// If crash/cancel, partial progress is saved
// Resume capability: Skip recordings already in output TSV
```

**Implementation:**
```csharp
using var writer = new StreamWriter(outputPath, append: true);
using var csv = new CsvWriter(writer, config);

foreach (var recording in recordings)
{
    // Search and fill
    await SearchAndFill(recording);
    
    // Write immediately (flush)
    csv.WriteRecord(recording);
    csv.NextRecord();
    await writer.FlushAsync();  // Critical: flush to disk
}
```

**Resume Logic:**
```csharp
// Check if output TSV exists
if (File.Exists(outputPath))
{
    var existingComposers = File.ReadAllLines(outputPath)
        .Skip(1)  // Skip header
        .Select(line => line.Split('\t')[0])  // Composer column
        .ToHashSet();
    
    recordings = recordings
        .Where(r => !existingComposers.Contains(r.Composer))
        .ToList();
    
    Console.Info($"Resuming: {recordings.Count} recordings remaining");
}
```

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`

**Implementation Steps:**
1. Change from batch write to incremental write
2. Flush after each recording
3. Add resume detection logic
4. Test crash scenario (Ctrl+C mid-run)
5. Verify partial output is valid TSV

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 2 hours

---

### CS-029: First Pressing Logic
**Complexity:** ⭐⭐⭐ High  
**Source:** To-Do.md #41  
**Priority:** MEDIUM (data accuracy)

**Problem:** Multiple pressings of same release have different labels/catalog numbers

**Current Behavior:**
```
Label Suggestions:
40% Deutsche Grammophon (1985 pressing)
40% DG (1990 reissue)
40% Galleria (2000 reissue)
```

**Target Behavior:**
```
Label Suggestions:
✓ 60% Deutsche Grammophon (1985, first pressing)
  20% DG (1990, reissue)
  20% Galleria (2000, reissue)
```

**Implementation:**
```csharp
// In DiscogsService.cs and MusicBrainzService.cs
var firstPressing = releases
    .Where(r => r.Year.HasValue)
    .OrderBy(r => r.Year)
    .First();

// Boost confidence for first pressing
if (release.Id == firstPressing.Id)
{
    release.Confidence *= 1.5;  // 50% boost
}
```

**Files:**
- `csharp\src\Services\Music\DiscogsService.cs`
- `csharp\src\Services\Music\MusicBrainzService.cs`

**Implementation Steps:**
1. Identify first pressing by earliest year
2. Add confidence boost to first pressing
3. Mark first pressing in display
4. Test with recordings having multiple pressings

**Dependencies:** CS-024 (dual search)  
**Blocks:** None  
**Estimated Time:** 2 hours

---

### CS-030: Match Label with Catalog Number
**Complexity:** ⭐⭐⭐ High  
**Source:** To-Do.md #42  
**Priority:** HIGH (data consistency)

**Problem:**
```
Label Suggestions:
40% Deutsche Grammophon (Discogs)
40% Tring International (Discogs)
Catalog # Suggestions:
40% 415 835-1 (Discogs)
40% TRP012 (Discogs)

Which catalog goes with which label?!
```

**Target Behavior:**
```
Release Suggestions:
┌─────────────────────────────────────────────────────────────┐
│ 60% Deutsche Grammophon • 415 835-1          (Discogs, 1985)│
│ 40% Tring International • TRP012             (Discogs, 1992)│
│ 30% EMI • 7243 5 55361 2 9                   (MusicBrainz)  │
└─────────────────────────────────────────────────────────────┘
```

**Implementation:**
```csharp
// Change from separate field suggestions to release suggestions
public record ReleaseSuggestion(
    string Label,
    string CatalogNumber,
    int? Year,
    string Source,
    double Confidence
);

// Group by release, not by field
var suggestions = releases
    .Select(r => new ReleaseSuggestion(
        Label: r.Label,
        CatalogNumber: r.CatalogNumber,
        Year: r.Year,
        Source: r.Source,
        Confidence: r.Confidence
    ))
    .OrderByDescending(s => s.Confidence)
    .ToList();

// Display as complete releases
foreach (var suggestion in suggestions)
{
    Console.WriteLine($"{suggestion.Confidence:P0} {suggestion.Label} • {suggestion.CatalogNumber}");
}
```

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`
- `csharp\src\Models\Music.cs` (add ReleaseSuggestion record)

**Implementation Steps:**
1. Change data model from field-level to release-level
2. Keep label+catalog paired from source
3. Display as complete release suggestions
4. User selects entire release, not individual fields
5. Test with recordings having multiple releases

**Dependencies:** CS-024, CS-025  
**Blocks:** CS-031, CS-032  
**Estimated Time:** 3 hours

---

### CS-031: Auto-Abbreviation for Labels
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #43  
**Priority:** LOW (UX convenience)

**Examples:**
```
Deutsche Grammophon → DG
His Master's Voice → HMV
Columbia Masterworks → Columbia
RCA Victor Red Seal → RCA Red Seal
```

**Implementation:**
```csharp
private static readonly Dictionary<string, string> LabelAbbreviations = new()
{
    ["Deutsche Grammophon"] = "DG",
    ["His Master's Voice"] = "HMV",
    ["Decca Record Company"] = "Decca",
    ["Électricité Musicale de France"] = "EMI",
    // ... more
};

private string AbbreviateLabel(string label)
{
    foreach (var (full, abbr) in LabelAbbreviations)
    {
        if (label.Contains(full, StringComparison.OrdinalIgnoreCase))
            return label.Replace(full, abbr);
    }
    return label;
}
```

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs` or `csharp\src\Services\Music\MusicExporter.cs`

**Implementation Steps:**
1. Create abbreviation dictionary
2. Apply when writing to TSV
3. Preserve full name in suggestions display
4. Test with known labels

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 1 hour

---

### CS-032: Auto-Generate Filled TSV
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #44  
**Priority:** HIGH (workflow automation)

**Current Behavior:**
```
# User must manually select best suggestions
# Manual TSV editing required
```

**Target Behavior:**
```
# Auto-generate TSV with highest confidence values
# Original: missing_fields.tsv
# Output: missing_fields_filled.tsv (all fields populated)
# User can review/edit if needed
```

**Implementation:**
```csharp
// In MusicFillCommand.cs
public sealed class Settings : CommandSettings
{
    [CommandOption("-i|--input")]
    public string InputPath { get; set; }
    
    [CommandOption("-o|--output")]
    public string? OutputPath { get; set; }  // Optional, defaults to {input}_filled.tsv
    
    [CommandOption("--auto-fill")]
    [Description("Automatically select highest confidence values")]
    public bool AutoFill { get; set; } = true;  // Default: true
}

if (settings.AutoFill)
{
    var best = suggestions.OrderByDescending(s => s.Confidence).First();
    recording.Label = best.Label;
    recording.Year = best.Year;
    recording.CatalogNumber = best.CatalogNumber;
}
```

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`

**Implementation Steps:**
1. Add `--auto-fill` flag (default true)
2. Auto-select highest confidence for each field
3. Write to output TSV
4. Display summary of auto-filled fields
5. Test with missing_fields.tsv

**Dependencies:** CS-030 (release matching)  
**Blocks:** None  
**Estimated Time:** 1 hour

---

## Category 4: CLI Argument Handling

### CS-033: Enforce Single-Dash Single-Letter, Double-Dash Multi-Letter
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #9-10  
**Priority:** MEDIUM (CLI standards compliance)

**Current Problem:**
```bash
# These should NOT work:
dotnet run music fill -input file.tsv    # Single-dash multi-letter
dotnet run music fill --i file.tsv       # Double-dash single-letter
```

**Target Behavior:**
```bash
# VALID:
dotnet run music fill -i file.tsv        # Single-dash single-letter ✓
dotnet run music fill --input file.tsv   # Double-dash multi-letter ✓

# INVALID:
dotnet run music fill -input file.tsv    # Error: invalid option
dotnet run music fill --i file.tsv       # Error: invalid option
```

**Investigation Required:**
- Does Spectre.Console.Cli support this natively?
- Do we need custom validation attribute?

**Files:**
- All `csharp\src\CLI\*Commands.cs`
- `csharp\src\Infrastructure\ValidationAttributes.cs` (if custom validation needed)

**Implementation Steps:**
1. Research Spectre.Console.Cli argument parsing
2. Test current behavior
3. If not supported, create validation attribute
4. Apply to all CommandOption attributes
5. Test valid and invalid invocations

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 1-2 hours

---

## Category 5: Console Abstraction

### CS-034: Enforce No Direct Spectre.Console Calls
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #54-55  
**Priority:** MEDIUM (architecture cleanliness)

**Problem:**
```csharp
// Direct calls in command files:
AnsiConsole.MarkupLine("[red]Error[/]");
var panel = new Panel(new Markup("..."));
```

**Target:**
```csharp
// All calls go through Console.cs wrapper:
Console.Error("Error");
Console.Panel("title", "content");
```

**Benefits:**
1. Single place to escape markup (prevents markup errors)
2. Easier to change UI library later
3. Consistent formatting/styling

**Files:**
- All `csharp\src\CLI\*Commands.cs` (search for AnsiConsole, Panel, Table, etc.)
- `csharp\src\Infrastructure\Console.cs` (add missing helper methods)

**Implementation Steps:**
1. Grep for direct Spectre.Console usage:
   ```bash
   grep -r "AnsiConsole\|new Panel\|new Table" csharp/src/CLI/
   ```
2. For each usage, create wrapper in Console.cs if missing
3. Replace direct calls with Console.* calls
4. Test all commands

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 2-3 hours

---

## Category 6: Python Integration

### CS-035: Create Python Migration Plan
**Complexity:** ⭐⭐⭐ High  
**Source:** To-Do.md #46, #51, #59  
**Priority:** LOW (future work)

**Scope:**
- **DO migrate:** General audio/video/filesystem utilities
- **DO NOT migrate:** Last.fm scrobble updater (legacy, keep in Python)
- **DO NOT duplicate:** Functionality already in PowerShell or C#

**Python Modules to Assess:**
```
python/toolkit/
├── audio.py (17 functions) - Candidates for migration
├── video.py (17 functions) - Candidates for migration
├── filesystem.py (6 functions) - Some duplicated in PowerShell
├── cuesheet.py (5 functions) - Specialized, maybe keep
├── lastfm.py (4 functions) - KEEP in Python, don't migrate
└── cli.py (12 commands) - Typer-based, keep as separate CLI
```

**Implementation:**
- Create `markdown/tasks/python_tasks.md`
- Create `markdown/implementation/python_migration_plan.md`
- Detail function-by-function analysis
- See CS-036 for specific libraries

**Dependencies:** None  
**Blocks:** CS-036, CS-037  
**Estimated Time:** 1 hour (planning), 20+ hours (implementation)

---

### CS-036: Research ImageSharp vs Pillow
**Complexity:** ⭐ Low (Research)  
**Source:** To-Do.md, master_task_list.md  
**Priority:** LOW (educational)

**Question:** How does ImageSharp (C#) differ from Pillow (Python) for:
1. Extracting frames from video
2. Specifying positions within a frame
3. Image manipulation operations

**Answer:**

| Feature | Pillow (Python) | ImageSharp (C#) |
|---------|-----------------|-----------------|
| **Frame Extraction** | Via `imageio` or `opencv-python` (not Pillow alone) | Via `FFMpegCore` library |
| **Position** | `image.crop((left, top, right, bottom))` | `image.Clone(ctx => ctx.Crop(new Rectangle(x, y, width, height)))` |
| **Coordinate System** | Top-left origin (standard) | Top-left origin (standard) |
| **Performance** | Fast (C backend) | Faster (native C#, SIMD) |
| **API Style** | Functional `image.resize()` | Fluent `image.Mutate(ctx => ctx.Resize())` |

**Example Comparison:**
```python
# Python (Pillow + opencv-python)
import cv2
from PIL import Image

# Extract frame from video
cap = cv2.VideoCapture('video.mp4')
cap.set(cv2.CAP_PROP_POS_MSEC, 5000)  # 5 seconds
success, frame = cap.read()
img = Image.fromarray(cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))

# Crop region
cropped = img.crop((100, 100, 400, 400))  # left, top, right, bottom
```

```csharp
// C# (ImageSharp + FFMpegCore)
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using FFMpegCore;

// Extract frame from video
var frame = FFMpeg.Snapshot("video.mp4", TimeSpan.FromSeconds(5));
using var img = Image.Load(frame);

// Crop region
img.Mutate(ctx => ctx.Crop(new Rectangle(x: 100, y: 100, width: 300, height: 300)));
```

**Recommendation:** Use **FFMpegCore** for video frame extraction, **ImageSharp** for image manipulation in C#

**Files:**
- `markdown/explanations/imagesharp_vs_pillow.md` (create detailed comparison)

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 2 hours (research + documentation)

---

### CS-037: Add CliWrap for Process Management
**Complexity:** ⭐⭐ Medium  
**Source:** master_task_list.md  
**Priority:** LOW (quality of life)

**Current State:**
```csharp
// Manual Process invocation
var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = "ffprobe",
        Arguments = "-v error -show_entries ...",
        RedirectStandardOutput = true,
        // ... many lines of config
    }
};
process.Start();
var output = await process.StandardOutput.ReadToEndAsync();
await process.WaitForExitAsync();
```

**With CliWrap:**
```csharp
using CliWrap;

var result = await Cli.Wrap("ffprobe")
    .WithArguments(["-v", "error", "-show_entries", "..."])
    .WithValidation(CommandResultValidation.None)
    .ExecuteBufferedAsync();

var output = result.StandardOutput;
```

**Benefits:**
1. Cleaner syntax
2. Automatic stream handling
3. Built-in cancellation support
4. Better error handling

**Files:**
- `csharp/CSharpScripts.csproj` (add CliWrap package)
- All files using Process class

**Implementation Steps:**
1. Add CliWrap NuGet package
2. Find all Process usages
3. Replace with CliWrap
4. Test ffprobe, yt-dlp, whisper invocations

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 2 hours

---

## Category 7: Typer Integration for Python CLI

### CS-038: Integrate Last.fm Scrobble into Python CLI
**Complexity:** ⭐⭐⭐ High  
**Source:** To-Do.md #46  
**Priority:** LOW (isolated legacy code)

**Current State:**
```
python/
├── toolkit/
│   ├── cli.py (Typer-based)
│   └── lastfm.py (standalone)
└── [legacy folder]/
    └── last.fm scrobble updater/ (old code)
```

**Target State:**
```
python/toolkit/
├── cli.py (Typer-based, includes lastfm command)
├── lastfm.py (refactored as module)
└── ... (other modules)

# Removed:
# └── last.fm scrobble updater/ (deleted after migration)
```

**Implementation:**
```python
# In cli.py
import typer
from toolkit import lastfm

app = typer.Typer()

@app.command()
def lastfm_update():
    """Update Last.fm scrobbles to Google Sheets"""
    lastfm.update_scrobbles()

# ... other commands
```

**Files:**
- `python/toolkit/cli.py` (add lastfm command)
- `python/toolkit/lastfm.py` (refactor if needed)
- `python/last.fm scrobble updater/` (delete after migration)

**Implementation Steps:**
1. Review legacy scrobble updater code
2. Integrate into toolkit/lastfm.py
3. Add Typer command in cli.py
4. Test scrobble update workflow
5. Delete legacy folder
6. Update PowerShell/C# to NOT invoke old code

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 2-3 hours

---

## Category 8: Build Configuration

### CS-039: Explain pyproject.toml Purpose
**Complexity:** ⭐ Low (Documentation)  
**Source:** To-Do.md #47  
**Priority:** LOW (educational)

**Answer:**
`pyproject.toml` is the Python project configuration file (PEP 518/621)

**Sections:**
```toml
[tool.basedpyright]
# Type checking configuration
typeCheckingMode = "strict"

[tool.black]
# Code formatting configuration (if added)
line-length = 100

[tool.pytest.ini_options]
# Test configuration (if added)

[project]
# Project metadata (name, version, dependencies)
```

**Why it exists:**
1. Replaces fragmented config files (setup.py, setup.cfg, tox.ini)
2. Standard location for tool configurations
3. Automatically discovered by Python tools

**Files:**
- `markdown/explanations/vscode_configuration_explained.md` (already documents this)

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** ✓ COMPLETE (already documented)

---

### CS-040: Suppress Untyped Library Warnings in pyproject.toml
**Complexity:** ⭐ Low  
**Source:** To-Do.md #47-48  
**Priority:** LOW (already configured)

**Current Config:**
```toml
[tool.basedpyright]
typeCheckingMode = "strict"
reportMissingTypeStubs = false  # ✓ Already suppressed
reportUnknownMemberType = false
reportUnknownArgumentType = false
# ... more suppressions
```

**Verification:**
```bash
cd python
basedpyright .
# Should show minimal errors from third-party libraries
```

**Files:**
- `python/pyproject.toml` (already configured correctly)

**Implementation Steps:**
1. ✓ Verify current config
2. Run basedpyright to check output
3. If warnings persist, add more suppressions

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 5 minutes (verification only)

---

### CS-041: Determine Best Location for Fill Output
**Complexity:** ⭐ Low (Decision)  
**Source:** To-Do.md #49  
**Priority:** LOW (organizational)

**Options:**
1. **Same directory as input:** `exports/missing_fields_filled.tsv`
2. **Separate output directory:** `exports/filled/missing_fields.tsv`
3. **User-specified via flag:** `--output path/to/output.tsv`

**Recommendation:** Option 3 (user-specified) with Option 1 as default

```csharp
[CommandOption("-o|--output")]
[Description("Output path for filled TSV")]
public string? OutputPath { get; set; }

// In Execute:
var outputPath = settings.OutputPath ?? 
    Path.ChangeExtension(settings.InputPath, "_filled.tsv");
```

**Files:**
- `csharp\src\CLI\MusicFillCommand.cs`

**Dependencies:** CS-032 (auto-generate filled TSV)  
**Blocks:** None  
**Estimated Time:** 10 minutes

---

### CS-042: Named Arguments Convention
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #52-53  
**Priority:** LOW (code style)

**Guideline:** Use named arguments when:
1. Method has >3 parameters
2. Parameter meanings are unclear from context
3. Arguments are not in natural order

**Examples:**
```csharp
// Bad (too many positional args):
csv.WriteField(record.Composer);
csv.WriteField(record.Work);
csv.WriteField(record.Orchestra);
csv.WriteField(record.Conductor);
csv.WriteField(record.Performers);
csv.WriteField(record.Label);

// Better (but repetitive):
WriteRecord(csv, record);  // Extract to method

// Named args example:
AnsiConsole.MarkupLine(
    value: $"[cyan]{Markup.Escape(text: fileName)}[/]"
);
```

**Current Usage in Codebase:**
```bash
grep -r "value:" csharp/src/Infrastructure/Console.cs
# Shows extensive use of named args already
```

**Files:**
- All `.cs` files (audit for consistency)

**Implementation Steps:**
1. Grep for methods with >3 positional args
2. Refactor to use named args or extract method
3. Document convention in explanations/
4. Apply consistently

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 1-2 hours

---

## Summary

### Complexity Distribution

| Complexity | Count | Files |
|------------|-------|-------|
| ⭐ Low | 10 | CS-021, CS-022, CS-036, CS-039, CS-040, CS-041, CS-042, + region removal |
| ⭐⭐ Medium | 18 | Most region additions, formatting, validation |
| ⭐⭐⭐ High | 11 | MusicSearchCommand, MusicFillCommand major features |
| ⭐⭐⭐⭐ Very High | 3 | CS-024 (dual search), CS-026 (live progress), CS-035 (Python migration) |

### Priority Matrix

| Priority | Tasks | Estimated Time |
|----------|-------|----------------|
| CRITICAL | 6 | 15-20 hours |
| HIGH | 10 | 10-15 hours |
| MEDIUM | 16 | 15-20 hours |
| LOW | 10 | 8-12 hours |

### Critical Path

```
CS-001 → CS-020: Add Regions (parallel, 5-10 hours total)
├─► CS-021: Global Usings
└─► CS-022: Remove Comments

CS-024: Dual Search ──┬──► CS-025: Format Output
                      ├──► CS-026: Live Progress
                      ├──► CS-027: Found Fields
                      ├──► CS-029: First Pressing
                      └──► CS-030: Label+Catalog Matching
                              ├──► CS-031: Auto-Abbreviate
                              └──► CS-032: Auto-Fill TSV

CS-033: CLI Args
CS-034: Console Abstraction
```

### Quick Wins (<1 hour each)

1. CS-021: Global usings migration
2. CS-022: Remove comments
3. CS-031: Label abbreviations
4. CS-032: Auto-fill TSV
5. CS-040: Verify pyproject.toml
6. CS-041: Output location decision
7. CS-023: Fix warnings (some categories)

### Files Requiring Most Work

1. **MusicFillCommand.cs** (612 lines): 10 related tasks (CS-006, CS-024-CS-032)
2. **MusicSearchCommand.cs** (1035 lines): 2 tasks (CS-003, CS-024)
3. **MusicBrainzService.cs** (1041 lines): 2 tasks (CS-002, CS-029)
4. **DiscogsService.cs** (632 lines): 2 tasks (CS-005, CS-029)
5. **Console.cs** (414 lines): 2 tasks (CS-008, CS-034)
