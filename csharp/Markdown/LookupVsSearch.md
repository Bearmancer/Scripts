# Search vs Lookup vs Schema

This document explains the purpose of each command when they might seem redundant.

---

## Command Overview

| Command  | Purpose                      | Input             | Output                 |
| -------- | ---------------------------- | ----------------- | ---------------------- |
| `search` | Find releases by text query  | Search string     | Multiple results       |
| `lookup` | Get full details by exact ID | Release/Master ID | Single detailed result |
| `schema` | List all available fields    | None              | Field reference        |

---

## Why Three Commands?

### Search: Discovery

**Purpose**: Find releases when you don't know the exact ID.

```powershell
scripts music search "Beatles Abbey Road"
```

**Returns**: List of potential matches with summary information:
- Artist, Title, Year, Label, Format, ID

**Limitations**:
- Returns limited fields to reduce API calls
- Multiple results may include variations/duplicates
- Not all metadata is available in search results

### Lookup: Full Details

**Purpose**: Get complete metadata for a specific release when you have its ID.

```powershell
scripts music lookup 24047 --source discogs
scripts music lookup b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d --source musicbrainz
```

**Returns**: Complete release information including:
- Full artist credits and roles
- Complete track listing
- All labels and catalog numbers
- Barcodes, formats, notes
- Relationships (MusicBrainz)
- Extra artists/credits (Discogs)

**Use cases**:
1. After finding a release via search, get full details
2. When you have an ID from external source
3. For data export or tagging workflows

### Schema: Field Discovery

**Purpose**: See all available metadata fields from both services.

```powershell
scripts music schema
```

**Returns**: Reference of all fields:
- Field name, type, description
- Whether field is searchable
- Source (MusicBrainz/Discogs/both)

**Use cases**:
1. Learning what data is available
2. Choosing which `--fields` to display
3. Understanding differences between services

---

## Workflow Example

### Typical Research Flow

```powershell
# 1. Discovery: Find the release
scripts music search "Karajan Beethoven 9" --limit 10

# Output shows multiple matches with IDs

# 2. Inspection: Get full details of best match
scripts music lookup 12345678 --source discogs

# Output shows complete metadata

# 3. Reference: Check what fields exist for classical mode
scripts music schema
```

### Why Not Combine Them?

1. **API Efficiency**: Search endpoints return limited data. Getting full details requires additional API calls per result. Separating allows control over API usage.

2. **User Intent**: Sometimes you want a quick list (search), sometimes full detail (lookup), sometimes reference (schema).

3. **Rate Limits**: Both MusicBrainz (1 req/sec) and Discogs have rate limits. Fetching full details for 25 search results would be slow and wasteful if you only need one.

---

## Search vs Lookup Data Comparison

### Discogs

| Field            | Search | Lookup |
| ---------------- | ------ | ------ |
| Title            | ✅      | ✅      |
| Artist           | ✅      | ✅      |
| Year             | ✅      | ✅      |
| Label            | ✅      | ✅      |
| Catalog Number   | ✅      | ✅      |
| Format           | ✅      | ✅      |
| Genre/Style      | ✅      | ✅      |
| **Tracklist**    | ❌      | ✅      |
| **ExtraArtists** | ❌      | ✅      |
| **Notes**        | ❌      | ✅      |
| **Images**       | ❌      | ✅      |
| **Videos**       | ❌      | ✅      |

### MusicBrainz

| Field             | Search | Lookup |
| ----------------- | ------ | ------ |
| Title             | ✅      | ✅      |
| Artist Credit     | ✅      | ✅      |
| Date              | ✅      | ✅      |
| Label             | ✅      | ✅      |
| Country           | ✅      | ✅      |
| Barcode           | ✅      | ✅      |
| **Media/Tracks**  | ❌      | ✅      |
| **Relationships** | ❌      | ✅      |
| **Work Links**    | ❌      | ✅      |
| **Annotations**   | ❌      | ✅      |

---

## Summary

- **Search** = "What releases match this query?"
- **Lookup** = "Tell me everything about this specific release"
- **Schema** = "What fields exist for me to query?"

Each serves a distinct purpose in a metadata research workflow.
