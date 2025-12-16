# Discogs API Search Sorting

The Discogs API search lacks built-in relevancy sorting for database searches, unlike MusicBrainz.

---

## API vs Website Discrepancy

### Website Search
- Uses proprietary relevancy algorithm
- Exact matches appear first
- Considers user engagement metrics
- Smart fuzzy matching

### API Search
- Returns results in **arbitrary order**
- No relevance score provided
- No sorting parameter for database search
- Results may differ significantly from website

---

## Available Sorting (Limited Contexts)

### Marketplace Only
```
sort=price&sort_order=asc
sort=listed&sort_order=desc
sort=seller
```

### Collection Only
```
sort=added&sort_order=desc
sort=artist
sort=title
sort=year
sort=rating
```

### Database Search
**No sorting available** — results come in arbitrary order.

---

## GitHub Community Discussions

### Key Issues Reported

| Issue                                | Source           | Status           |
| ------------------------------------ | ---------------- | ---------------- |
| "Search results don't match website" | Discogs Forums   | Known limitation |
| "No relevance scoring in API"        | Developer Forums | Won't fix        |
| "Exact match buried in results"      | GitHub Issues    | Acknowledged     |

### Developer Workarounds

1. **Over-fetch and re-sort locally**
   ```csharp
   var results = await discogs.SearchAsync(query, limit: 100);
   var sorted = results.OrderBy(LevenshteinDistance(query, r.Title));
   ```

2. **Use specific query parameters**
   ```
   ?artist=beatles&release_title=abbey+road&year=1969
   ```

3. **Master releases preference**
   - Search for `type=master` instead of `type=release`
   - Reduces duplicate pressings

---

## Implemented Solution

### Custom Relevance Scoring

```csharp
static int CalculateRelevance(string query, SearchResult result)
{
    int score = 0;
    
    // Exact title match
    if (result.Title.Equals(query, StringComparison.OrdinalIgnoreCase))
        score += 100;
    
    // Title contains query
    else if (result.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
        score += 50;
    
    // Artist match (bonus)
    if (result.Artist.Contains(query.Split(' ')[0], StringComparison.OrdinalIgnoreCase))
        score += 25;
    
    // Prefer masters over releases
    if (result.Type == "master")
        score += 10;
    
    // Prefer items with images
    if (result.ThumbUrl is not null)
        score += 5;
    
    return score;
}
```

### Levenshtein Distance Alternative

```csharp
var sorted = results
    .Select(r => (Result: r, Distance: LevenshteinDistance(query, r.Title)))
    .OrderBy(x => x.Distance)
    .Select(x => x.Result);
```

---

## ParkSquare.Discogs Considerations

The ParkSquare.Discogs library wraps the API but does not add sorting:

```csharp
// Returns unsorted results
var criteria = new SearchCriteria { Query = "Beatles" };
var results = await client.SearchAsync(criteria);

// Must sort manually
var sorted = results.Results
    .OrderBy(r => string.Equals(r.Title, "Beatles", OrdinalIgnoreCase) ? 0 : 1)
    .ThenBy(r => r.Year);
```

---

## Recommendations

### Do's and Don'ts

**DO:**
```csharp
// Use specific parameters
var criteria = new SearchCriteria 
{
    Artist = "Beatles",
    ReleaseTitle = "Abbey Road",
    Year = 1969
};

// Sort results locally by relevance
var sorted = results.OrderBy(r => LevenshteinDistance(query, r.Title));

// Use master type to reduce duplicates
var criteria = new SearchCriteria { Type = "master" };
```

**DON'T:**
```csharp
// Don't use only free-text query for exact matches
var criteria = new SearchCriteria { Query = "Beatles Abbey Road 1969" };  // Arbitrary order!

// Don't assume results are sorted by relevance
var first = results.First();  // May not be the best match!

// Don't use fallback values
var year = result.Year ?? 0;  // WRONG - use nullable or empty
```

---

## Discogs Master Releases

> **Clarification**: Discogs DOES have Master Releases, similar to MusicBrainz Release Groups.

| Concept           | Discogs       | MusicBrainz    |
| ----------------- | ------------- | -------------- |
| Canonical version | Master        | Release Group  |
| Specific pressing | Release       | Release        |
| Search parameter  | `type=master` | `releasegroup` |

```csharp
// Search for masters to avoid duplicate pressings
var criteria = new SearchCriteria 
{
    Query = "Dark Side of the Moon",
    Type = "master"
};
```

---

## Pop vs Classical Search Examples

### Pop Search Example

```powershell
scripts music search "Beatles Abbey Road" --source discogs --mode pop
```

**Expected fields extracted:**
| Field  | Value         |
| ------ | ------------- |
| Artist | The Beatles   |
| Title  | Abbey Road    |
| Year   | 1969          |
| Label  | Apple Records |
| Format | Vinyl, LP     |
| ID     | 24047         |

### Classical Search Example

```powershell
scripts music search "Karajan Beethoven Symphony 9" --source discogs --mode classical
```

**Expected fields extracted:**
| Field      | Source                               |
| ---------- | ------------------------------------ |
| Composer   | ExtraArtists: "Ludwig van Beethoven" |
| Work       | Parsed from title: "Symphony No. 9"  |
| Performers | Artist: "Berliner Philharmoniker"    |
| Conductor  | ExtraArtists: "Herbert von Karajan"  |
| Year       | 1962                                 |
| Label      | Deutsche Grammophon                  |
| ID         | 398712                               |

**Work extraction regex:**
```csharp
// Pattern matches: Symphony No. 9, Concerto No. 1, Op. 125, BWV 565, K. 545
var workPattern = @"(Symphony|Concerto|Sonata|Quartet|Overture)\s+No\.?\s*\d+|Op\.?\s*\d+|BWV\s*\d+|K\.?\s*\d+";
```

---

## Single-Source vs Dual-Source Searching

### Single Source (Default)

Use when you know which database has better data for your use case:

```powershell
# Discogs: Better for vinyl collectors, pressings, marketplace
scripts music search "Pink Floyd" --source discogs

# MusicBrainz: Better for tagging, ISRCs, work relationships
scripts music search "Pink Floyd" --source musicbrainz
```

### Cross-Source Lookup

Use search results from one source to find in another:

```powershell
# 1. Search Discogs
$discogs = scripts music search "Abbey Road" --source discogs --output json | ConvertFrom-Json

# 2. Use barcode to find in MusicBrainz
scripts music search $discogs[0].Barcode --source musicbrainz
```

---

## Standard Recommendations

1. **Always use specific parameters** instead of free-text `query`
2. **Search for `master` type** to reduce duplicates
3. **Implement client-side scoring** for display order
4. **Limit pagination** — later pages are less relevant
5. **Never cache without validation** — API results may change
