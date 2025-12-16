# MusicBrainz Classical Music: Site vs API

Analysis of differences between MusicBrainz website display and API responses for classical music.

---

## Website Display (Rich)

The MusicBrainz website shows extensive classical music information:

### Example Release Page

```
Release: Respighi: Pini di Roma / Fontane di Roma / Feste romane

Track 1: Pini di Roma: I. I pini di Villa Borghese
├── Recording artist: The Philadelphia Orchestra, Eugene Ormandy
├── Producer: Howard H. Scott
├── Orchestra: The Philadelphia Orchestra (on 1958-03-03)
├── Conductor: Eugene Ormandy (on 1958-03-03)
├── Recorded at: Broadwood Hotel in Philadelphia
├── Recording of: Pini di Roma, P 141: I. I pini di Villa Borghese
├── Composer: Ottorino Respighi (in 1924)
└── Part of: Pini di Roma, P 141 (Pines of Rome)
```

### Information Sources

| Data               | Origin                 | Display        |
| ------------------ | ---------------------- | -------------- |
| Composer           | Work → Relations       | Shown on track |
| Conductor          | Recording → Relations  | Shown on track |
| Orchestra          | Recording → Relations  | Shown on track |
| Recording date     | Recording → Attributes | Shown on track |
| Recording location | Recording → Relations  | Shown on track |
| Work title         | Work entity            | Shown on track |
| Catalog number     | Work → Attributes      | Visible        |

---

## API Response (Flat)

### Default Recording Query

```bash
GET /ws/2/recording/abc123
```

Returns:
```json
{
  "id": "abc123",
  "title": "Pini di Roma: I. I pini di Villa Borghese",
  "length": 157000,
  "artist-credit": [
    {"name": "The Philadelphia Orchestra"},
    {"name": "Eugene Ormandy", "joinphrase": ", "}
  ]
}
```

**Missing**: Composer, conductor role, orchestra role, work, recording date, location.

### With Includes

```bash
GET /ws/2/recording/abc123?inc=artist-credits+work-rels+artist-rels
```

Returns more, but requires **multiple requests**:
1. Recording with `work-rels` → gets Work ID
2. Work with `artist-rels` → gets Composer
3. Recording with `artist-rels` → gets Conductor/Orchestra roles

---

## Key Differences

| Aspect              | Website                        | API                        |
| ------------------- | ------------------------------ | -------------------------- |
| **Work info**       | Inline on track                | Requires separate lookup   |
| **Composer**        | Directly visible               | Nested in Work relations   |
| **Roles**           | Labeled (conductor, orchestra) | Generic relationship types |
| **Recording date**  | Formatted                      | ISO format in attributes   |
| **Location**        | Full venue name                | Place ID (needs lookup)    |
| **Catalog numbers** | Display formatted              | Raw P-number               |

---

## Retrieving Full Classical Data

### Required API Calls

1. **Release with recordings**
   ```
   /ws/2/release/{id}?inc=recordings+artist-credits
   ```

2. **Each recording with work relations**
   ```
   /ws/2/recording/{id}?inc=work-rels+work-level-rels
   ```

3. **Work with composer**
   ```
   /ws/2/work/{id}?inc=artist-rels
   ```

### Relationship Types for Classical

| Type           | Code  | Meaning                 |
| -------------- | ----- | ----------------------- |
| `composer`     | `168` | Wrote the work          |
| `conductor`    | `234` | Conducted recording     |
| `orchestra`    | `45`  | Orchestra performing    |
| `performer`    | `156` | General performer       |
| `recording of` | `278` | Recording → Work link   |
| `part of`      | `316` | Work → Parent Work link |

---

## MetaBrainz.MusicBrainz Library

### Fetching Classical Data

```csharp
// Get release with all needed includes
var release = await query.LookupReleaseAsync(
    mbid,
    Include.Recordings | Include.ArtistCredits | 
    Include.RecordingRelationships | Include.WorkRelationships
);

// Traverse relationships
foreach (var recording in release.Media.SelectMany(m => m.Tracks))
{
    var workRels = recording.Recording.Relationships
        .Where(r => r.Type == "recording of");
    
    foreach (var workRel in workRels)
    {
        var work = workRel.Work;
        var composerRels = work.Relationships
            .Where(r => r.Type == "composer");
        
        foreach (var composerRel in composerRels)
        {
            var composer = composerRel.Artist;
            // composer.Name = "Ottorino Respighi"
        }
    }
}
```

---

## Classical Mode Implementation

### Goal

Display classical releases like the website does:

```
Composer: Respighi
Work: Pini di Roma, P 141
Movement: I. I pini di Villa Borghese
Performers: Philadelphia Orchestra, Ormandy
Year: 1958
Label: Columbia
```

### Strategy

1. **Search for Release Group** (canonical version)
2. **Fetch best Release** (main release ID)
3. **Get Recordings with Work relations**
4. **Traverse Work → Composer relation**
5. **Extract Conductor/Orchestra from Recording relations**

---

## Limitations

> **Optimization Rule**: Fetch all relationship data in minimal API calls, but never skip fields to save bandwidth.
> Missing data should be logged, not silently ignored.

### API Constraints

- Rate limiting (1 request/second without auth)
- Complex nested lookups for full data
- Large response sizes with all includes

### Data Quality

- Not all recordings have Work links
- Some relationships may be missing
- Classical-specific attributes (catalog numbers) vary

---

## Recommendations

1. **Do NOT cache works/composers blindly** — A work can have multiple recordings with different:
   - Soloists per movement
   - Recording dates
   - Orchestras/conductors
   
   **Implementation**: Log statistics when a work has multiple recordings with varying performers:
   ```csharp
   Logger.Info("Work '{0}' has {1} recordings with {2} unique performer sets", 
       work.Title, recordingCount, uniquePerformerSets);
   ```

2. **Use release groups** — Canonical version reduces duplicates  

3. **Batch relationship lookups** — Minimize API calls while fetching all data

4. **Never use fallback values** — If a field is missing, display empty or log the absence:
   ```csharp
   // WRONG:
   var composer = work.Composer ?? "Unknown";
   
   // CORRECT:
   var composer = work.Composer ?? "";
   if (string.IsNullOrEmpty(composer))
       Logger.Debug("No composer found for work: {0}", work.Title);
   ```
