# MusicBrainz Search Scoring System

MusicBrainz uses Apache Solr (built on Lucene) for its indexed search. The scoring system determines result relevance.

## Accessing Score from API

The MetaBrainz.MusicBrainz library exposes the score through search results:

```csharp
// Search returns scored results
var results = await query.FindReleasesAsync("Beatles Abbey Road");

foreach (var result in results.Results)
{
    // result.Score is 0-100
    Console.WriteLine($"[{result.Score,3}] {result.Item.Title}");
}
```

**Display in verbose mode:**
```csharp
if (verbose)
{
    table.AddColumn("Score");
    foreach (var result in results)
        row.Add(result.Score.ToString());
}
```

---

## Scoring Algorithm

### Lucene TF-IDF Formula

The relevance score is calculated using:

```
score = tf × idf × fieldNorm × boost
```

| Component                            | Description                                  |
| ------------------------------------ | -------------------------------------------- |
| **tf** (Term Frequency)              | How often the term appears in the field      |
| **idf** (Inverse Document Frequency) | Rarity of the term across all documents      |
| **fieldNorm**                        | Shorter fields rank higher                   |
| **boost**                            | Optional per-field or per-document weighting |

### Practical Impact

| Factor                      | Effect on Score                      |
| --------------------------- | ------------------------------------ |
| Exact match in short field  | Very high                            |
| Partial match in long field | Lower                                |
| Rare term match             | Higher (unique terms matter more)    |
| Common term match           | Lower ("the", "and" contribute less) |

---

## MusicBrainz-Specific Behavior

### Score Attribute

API responses include a `score` attribute (0-100):

```json
{
  "id": "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d",
  "name": "The Beatles",
  "score": 100
}
```

### No Popularity Weighting

Unlike Spotify or Apple Music, MusicBrainz does **not** factor in:
- Play counts
- Chart positions
- Artist popularity
- Release sales

Popular artists may appear higher due to:
- More complete metadata (shorter, cleaner names)
- Better data quality
- More exact matches

### Website vs API Differences

The website search and API may return different scores for the same query because:
- Different default query parsers
- Website may apply additional filters
- Caching behavior differences

---

## Search Query Syntax

MusicBrainz supports Lucene query syntax:

### Basic Operators

| Syntax | Example                             | Description         |
| ------ | ----------------------------------- | ------------------- |
| `AND`  | `artist:beatles AND release:abbey`  | Both terms required |
| `OR`   | `artist:lennon OR artist:mccartney` | Either term         |
| `NOT`  | `artist:beatles NOT release:live`   | Exclude term        |
| `""`   | `"abbey road"`                      | Exact phrase        |
| `*`    | `beet*`                             | Wildcard            |
| `~`    | `beethoven~`                        | Fuzzy match         |

### Field-Specific Search

```
artist:Beatles AND release:"Abbey Road" AND date:1969
```

| Field          | Description         |
| -------------- | ------------------- |
| `artist`       | Artist name         |
| `release`      | Release title       |
| `recording`    | Track title         |
| `releasegroup` | Release group title |
| `label`        | Record label        |
| `catno`        | Catalog number      |
| `barcode`      | UPC/EAN             |
| `isrc`         | ISRC code           |
| `tag`          | User tags           |
| `type`         | Release type        |
| `status`       | Release status      |
| `country`      | Release country     |
| `date`         | Release date        |

---

## Boosting

Lucene allows boosting specific terms or fields:

```
artist:beatles^2 release:help
```

The `^2` multiplies the score contribution of "beatles" by 2.

### Default Field Weights

MusicBrainz likely applies internal boosts (exact values not public):
- Name/Title fields: Higher weight
- Aliases: Medium weight
- Disambiguation: Lower weight

---

## Optimizing Search Results

### For Best Results

1. **Use specific fields**: `artist:` instead of free text
2. **Include multiple terms**: More context improves matching
3. **Avoid common words**: "The", "Symphony No." add noise
4. **Use exact phrases**: Quotes for multi-word names

### Example Queries

```
# Find Beethoven's 9th Symphony recordings
recording:"Symphony No. 9" AND artist:beethoven

# Find Abbey Road album
release:"Abbey Road" AND artist:beatles AND date:1969*
```

---

## Score Interpretation

| Score Range | Interpretation                 |
| ----------- | ------------------------------ |
| 90-100      | Excellent match (likely exact) |
| 70-89       | Good match (minor differences) |
| 50-69       | Partial match                  |
| Below 50    | Weak match (may be irrelevant) |

**Note**: Scores are relative within a result set, not absolute quality indicators.
