# Implementation Plan: Classical vs Pop Mode

## Overview

Implementation of dual-mode music search supporting both classical and popular music conventions.

### Column Ordering via XML Comments

```csharp
/// <summary>
/// Display order can be changed by reordering this array.
/// Each string corresponds to a column in the output table.
/// </summary>
/// <remarks>
/// Pop mode: Artist → Album → Year → Label → Format → ID
/// Classical: Composer → Work → Performers → Conductor → Year → Label → ID
/// </remarks>
static readonly string[] PopColumns = ["Artist", "Album", "Year", "Label", "Format", "ID"];
static readonly string[] ClassicalColumns = ["Composer", "Work", "Performers", "Conductor", "Year", "Label", "ID"];
```

### Date Formatting Standard

All dates displayed as: **MMM dd, yyyy** (e.g., `Jan 13, 1999`)

```csharp
/// <summary>
/// Formats dates consistently across the application.
/// To change format, modify the format string below.
/// </summary>
/// <remarks>
/// Current format: "MMM dd, yyyy" → Jan 13, 1999
/// Alternatives:
///   "yyyy-MM-dd" → 1999-01-13
///   "dd MMM yyyy" → 13 Jan 1999
///   "MMMM d, yyyy" → January 13, 1999
/// </remarks>
static string FormatDate(DateTime? date) => 
    date?.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture) ?? "";
```

---

## Mode Behavior

Ensuring only albums

### Pop Mode (Default)

For rock, pop, electronic, hip-hop, jazz, and most commercial music.

**Field mapping:**
```csharp
// Standard pop interpretation
public static class PopFieldMapper
{
    public static string GetArtist(SearchResult r) => r.ArtistCredit?.FirstOrDefault()?.Name ?? "";
    public static string GetTitle(SearchResult r) => r.Title ?? "";
    public static string GetYear(SearchResult r) => r.Date?.Year?.ToString() ?? r.Year?.ToString() ?? "";
    public static string GetLabel(SearchResult r) => r.Labels?.FirstOrDefault()?.Name ?? "";
}
```

**Display order:**
1. Artist
2. Album
3. Recording Year
5. Format
4. Label
6. ID

### Classical Mode

For orchestral, chamber, opera, choral, and art music.

**Field mapping:**
```csharp
// Classical interpretation differs significantly
public static class ClassicalFieldMapper
{
    public static string GetComposer(MusicBrainzRelease r) => 
        r.Works?.FirstOrDefault()?.Relationships
            ?.FirstOrDefault(rel => rel.Type == "composer")?.Artist?.Name ?? "";
    
    public static string GetWorkTitle(MusicBrainzRelease r) =>
        r.Works?.FirstOrDefault()?.Title ?? ExtractWorkFromTitle(r.Title);
    
    public static string GetPerformers(MusicBrainzRelease r) =>
        string.Join(", ", r.ArtistCredit?.Select(a => a.Name) ?? []);
    
    public static string GetConductor(MusicBrainzRelease r) =>
        r.Relationships?.FirstOrDefault(rel => rel.Type == "conductor")?.Artist?.Name ?? "";
}
```

**Display order:**
1. Composer
2. Work
4. Soloist
5. Conductor
3. Orchestra
6. Recording Year
7. Label
8. ID

---

## Implementation Steps

### Phase 1: Field Extraction (Discogs)

1. **Map artist credits to roles**
   ```csharp
   // ExtraArtists often contains conductor, orchestra
   var conductor = release.ExtraArtists?
       .FirstOrDefault(a => a.Role?.Contains("Conductor") == true);
   ```

2. **Extract work from title**
   ```csharp
   // Common patterns:
   // "Symphony No. 9 in D minor, Op. 125"
   // "Piano Concerto No. 1 - Brahms"
   // "Mahler: Das Lied von der Erde"
   ```

3. **Separate composer from performers**
   - Discogs often lists "Beethoven; Berlin Philharmonic"
   - Parse artist string to extract composer

### Phase 2: Field Extraction (MusicBrainz)

1. **Work lookups required**
   ```csharp
   // Recording → Work → Composer chain
   var recording = release.Media.SelectMany(m => m.Tracks).First();
   var workRel = recording.Relationships.First(r => r.Type == "recording of");
   var composerRel = workRel.Work.Relationships.First(r => r.Type == "composer");
   ```

2. **Cache composer and work data**
   - Works/composers rarely change
   - Store in local state file

3. **Conductor/orchestra from recording relations**

### Phase 3: CLI Integration

1. **Add `--mode` parameter**
   ```csharp
   [CommandOption("--mode")]
   [DefaultValue("pop")]
   public string Mode { get; init; } = "pop";
   ```

2. **Conditional field mapping**
   ```csharp
   var mapper = settings.Mode switch
   {
       "classical" => new ClassicalFieldMapper(),
       _ => new PopFieldMapper()
   };
   ```

3. **Adjust default columns per mode**

---

## Special Cases

### Classical Detection Heuristics

When mode is "auto" (future enhancement):

```csharp
bool IsLikelyClassical(SearchResult r)
{
    // Genre/style indicators
    if (r.Genres?.Any(g => g.Contains("Classical")) == true) return true;
    if (r.Styles?.Any(s => ClassicalStyles.Contains(s)) == true) return true;
    
    // Title patterns
    if (Regex.IsMatch(r.Title ?? "", @"\b(Symphony|Concerto|Sonata|Quartet|Op\.|BWV|K\.)\b"))
        return true;
    
    // Label indicators
    if (ClassicalLabels.Contains(r.Labels?.FirstOrDefault()?.Name ?? ""))
        return true;
    
    return false;
}

static readonly HashSet<string> ClassicalStyles = [
    "Baroque", "Romantic", "Modern Classical", "Contemporary Classical",
    "Opera", "Choral", "Chamber Music", "Orchestral"
];

static readonly HashSet<string> ClassicalLabels = [
    "Deutsche Grammophon", "Decca", "Chandos", "Naxos", "Hyperion",
    "Sony Classical", "EMI Classics", "Philips", "RCA Red Seal"
];
```

---

## Default Columns Per Mode

### Pop + Discogs
| Column | Source          |
| ------ | --------------- |
| Artist | Artists[0].Name |
| Title  | Title           |
| Year   | Year            |
| Label  | Labels[0].Name  |
| Format | Formats[0].Name |
| ID     | Id              |

### Pop + MusicBrainz
| Column | Source                  |
| ------ | ----------------------- |
| Artist | ArtistCredit[0].Name    |
| Title  | Title                   |
| Date   | Date                    |
| Label  | LabelInfo[0].Label.Name |
| Format | Media[0].Format         |
| MBID   | MbId                    |

### Classical + Discogs
| Column     | Source                                               |
| ---------- | ---------------------------------------------------- |
| Composer   | ExtraArtists[role=Composed By] or parse from Artists |
| Work       | Extract from Title                                   |
| Performers | Artists minus composer                               |
| Year       | Year                                                 |
| Label      | Labels[0].Name                                       |
| ID         | Id                                                   |

### Classical + MusicBrainz
| Column     | Source                               |
| ---------- | ------------------------------------ |
| Composer   | Work.Relations[type=composer].Artist |
| Work       | Work.Title                           |
| Performers | ArtistCredit                         |
| Conductor  | Relations[type=conductor].Artist     |
| Date       | Date                                 |
| MBID       | MbId                                 |

---

## Testing Strategy

1. **Pop mode tests**
   - Search "Beatles Abbey Road"
   - Verify artist/title/year extraction

2. **Classical mode tests**
   - Search "Beethoven Symphony No. 9 Karajan"
   - Search "Ormandy Brahms 1"
   - Verify composer/work/conductor extraction

3. **Edge cases**
   - Film soundtracks (composer + songs)
   - Jazz (multiple modes valid)
   - World music compilations
