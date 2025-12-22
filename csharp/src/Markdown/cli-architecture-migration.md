# CLI Architecture Migration Plan

## Current State Analysis

### Problems Identified

1. **MusicCommands.cs contains business logic that should be in services:**
   - `TrackCsv` (file class) - CSV read/write for track metadata
   - `JsonOptions` (file class) - JSON serialization options
   - `MusicBrainzEnrichmentState` (record) - State management
   - `WorkSummary` (record) - Data model for grouped works
   - `GroupTracksByWork()` - Business logic for grouping tracks
   - `DetectMissingWorkHierarchy()` - Business logic for validation
   - `EnrichTracksWithProgressAsync()` - Orchestration with UI logic mixed in
   - `LoadFromDumps()` - Dump reconstruction logic

2. **Methods that should move out of CLI:**
   - All `Extract*`, `Calculate*`, `Normalize*` helpers - these are business logic
   - `SaveSearchDumps()` - file I/O operation
   - `GroupTracksByWork()` - data transformation
   - `DetectMissingWorkHierarchy()` - data validation

3. **What should stay in CLI:**
   - Command classes (`MusicSearchCommand`, `MusicSchemaCommand`)
   - Settings classes
   - `ExecuteAsync()` methods (thin orchestration only)
   - Display/UI formatting helpers

---

## CLI Files Summary

| File                    | Lines | Commands                                                             | Issues                    |
| ----------------------- | ----- | -------------------------------------------------------------------- | ------------------------- |
| MusicCommands.cs        | 1524  | MusicSearchCommand, MusicSchemaCommand                               | Heavy - needs refactoring |
| SyncCommands.cs         | 456   | SyncAllCommand, SyncYouTubeCommand, SyncLastFmCommand, StatusCommand | OK - orchestration only   |
| CleanCommands.cs        | 232   | CleanLocalCommand, CleanPurgeCommand                                 | OK - light logic          |
| MailCommands.cs         | 128   | MailCreateCommand, MailCheckCommand, MailDeleteCommand               | OK - delegates to service |
| CompletionCommands.cs   | 190   | CompletionInstallCommand, CompletionSuggestCommand                   | OK - self-contained       |
| ValidationAttributes.cs | 45    | AllowedValuesAttribute, NotEmptyAttribute                            | OK - validation only      |

---

## Proposed Architecture

### New Files to Create

#### 1. `Services/Music/TrackStateManager.cs`
Handles all track state persistence (CSV, JSON, dumps):
```csharp
public class TrackStateManager
{
    public void AppendTrack(string releaseId, TrackMetadata track)
    public List<TrackMetadata> Load(string releaseId)
    public void Delete(string releaseId)
    public List<TrackMetadata> LoadFromDumps(string releaseId, List<TrackMetadata> baseTracks)
    public int GetEnrichedCount(string releaseId)
}
```

#### 2. `Services/Music/TrackEnricher.cs`
Handles track enrichment with progress reporting:
```csharp
public class TrackEnricher(IMusicService service, TrackStateManager stateManager)
{
    public event Action<TrackMetadata>? TrackEnriched
    public event Action<int, int>? ProgressChanged
    public Task<List<TrackMetadata>> EnrichAsync(string releaseId, List<TrackMetadata> tracks, bool fresh, CancellationToken ct)
}
```

#### 3. `Services/Music/WorkGrouper.cs`
Handles work grouping and hierarchy detection:
```csharp
public static class WorkGrouper
{
    public static List<WorkSummary> GroupTracksByWork(List<TrackMetadata> tracks)
    public static void DetectMissingWorkHierarchy(List<WorkSummary> works)
}
```

#### 4. `Models/WorkSummary.cs`
Move WorkSummary record to Models:
```csharp
public record WorkSummary(...)
```

#### 5. `Models/MusicBrainzEnrichmentState.cs`
Move state record to Models:
```csharp
public record MusicBrainzEnrichmentState(...)
```

---

## Migration Steps

### Phase 1: Extract Data Models
1. Move `WorkSummary` to `Models/WorkSummary.cs`
2. Move `MusicBrainzEnrichmentState` to `Models/MusicBrainzEnrichmentState.cs`
3. Update imports in MusicCommands.cs

### Phase 2: Extract Track State Management
1. Create `Services/Music/TrackStateManager.cs`
2. Move `TrackCsv` methods to TrackStateManager
3. Move `LoadFromDumps` logic
4. Update MusicCommands to use TrackStateManager

### Phase 3: Extract Work Grouping
1. Create `Services/Music/WorkGrouper.cs`
2. Move `GroupTracksByWork` method
3. Move `DetectMissingWorkHierarchy` method
4. Update MusicCommands to use WorkGrouper

### Phase 4: Extract Enrichment Logic
1. Create `Services/Music/TrackEnricher.cs`
2. Extract enrichment loop (without UI)
3. Keep only UI/progress display in CLI
4. Wire up events for progress updates

### Phase 5: Clean Up CLI
1. Remove helper methods that moved to services
2. MusicSearchCommand.ExecuteAsync becomes thin orchestrator
3. Consider extracting remaining display logic to a renderer class

---

## Expected Result

**MusicCommands.cs after migration:**
- ~300-400 lines (down from 1524)
- Contains only: Command classes, Settings, ExecuteAsync, UI rendering

**New service files:**
- TrackStateManager.cs: ~150 lines
- TrackEnricher.cs: ~200 lines  
- WorkGrouper.cs: ~100 lines
- Models: ~30 lines each

---

## Implementation Order

// turbo-all
1. Create Models/WorkSummary.cs
2. Create Models/MusicBrainzEnrichmentState.cs
3. Create Services/Music/TrackStateManager.cs
4. Create Services/Music/WorkGrouper.cs
5. Create Services/Music/TrackEnricher.cs
6. Update MusicCommands.cs to use new services
7. Delete file-scoped classes from MusicCommands.cs
8. Run build and tests
