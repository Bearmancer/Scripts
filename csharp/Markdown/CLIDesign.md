# CLI Architecture Design

Respect MB/Discogs casing

1. Look into C# in built enhancement of autofilling in .NET 10: https://learn.microsoft.com/en-us/dotnet/core/tools/enable-tab-autocomplete
2. Check current autofilling library/packages/code used and remove it to replace with .NET 10 varaint.
3. Ensure compliance with updates name of fields
4. In verose mode always show all fields/tags returned by either services

## Command Structure

```
scripts <branch> <command> [arguments] [options]
```

### Global Options

| Flag            | Description                                        |
| --------------- | -------------------------------------------------- |
| `-v, --verbose` | Enable debug logging (sets Console.Level globally) |
| `--help`        | Show help for command                              |

(Ditch using version everywhere possible unless Spectre mandated -- do not need it as CLI option)

---

## Branch: `music`

### Command: `music search`

```
scripts music search <query> [options]
```

**Arguments:**
| Name    | Required | Description                         |
| ------- | -------- | ----------------------------------- |
| `query` | Yes      | Search string (artist, album, etc.) |

**Options:**
| Flag       | Type                   | Default   | Description                  |
| ---------- | ---------------------- | --------- | ---------------------------- |
| `--source` | `Discogs\|MusicBrainz` | `discogs` | Data source                  |
| `--mode`   | `pop\|classical`       | `pop`     | Metadata interpretation mode |
| `--limit`  | int                    | `25`      | Number of results            |
| `--fields` | string                 | `default` | Columns to display           |
| `--output` | `table\|json`          | `table`   | Output format                |

**`--fields` Special Values:**
- `default` — Mode-specific default columns
- `all` — All available columns

**Available Fields:**
```
artist, album, release year, record year, label, format, barcode, catalog_number, id
```

> **Note**: Barcode (UPC/EAN) and catalog number are different:
> - **Barcode**: Universal product code (e.g., `077774644518`)
> - **Catalog Number**: Label-specific identifier (e.g., `PCS 7088`)

**Examples:**
```powershell
# Basic search
scripts music search "Beatles Abbey Road"

# MusicBrainz source with 50 results
scripts music search "Beethoven Symphony" --source musicbrainz --limit 50

# Classical mode with custom fields
scripts music search "Bach" --mode classical --fields composer,work,year,id

# JSON output for scripting -- write separate markdown to show where this would be used and why it is useful for scripting --> can't C# handle it natively?
scripts music search "Miles Davis" --output json | jq '.[].artist'


```

> **See also**: [LookupVsSearch.md](LookupVsSearch.md) for explanation of search vs lookup vs schema purposes

---

### Command: `music lookup`

```
scripts music lookup <id> [options]
```

**Arguments:**
| Name | Required | Description       |
| ---- | -------- | ----------------- |
| `id` | Yes      | Release/Master ID |

**Options:**
| Flag       | Type                   | Default   | Description   |
| ---------- | ---------------------- | --------- | ------------- |
| `--source` | `discogs\|musicbrainz` | `discogs` | Data source   |
| `--type`   | `release\|master`      | `release` | Entity type   |
| `--output` | `table\|json`          | `table`   | Output format |

---

### Command: `music schema` 

I've removed options as it is superfluous

```
scripts music schema
```

Lists all available metadata fields from both services.

---

## Mode Behavior

Refer to classicalmodeimplementation to see default rows

### Pop Mode (Default)

Optimized for popular music: rock, pop, electronic, hip-hop, etc.

**Default columns:**
| Discogs | MusicBrainz |
| ------- | ----------- |
| Artist  | Artist      |
| Title   | Title       |
| Year    | Date        |
| Label   | Label       |
| Format  | Format      |
| ID      | MBID        |

**Field interpretation:**
- `artist` = Primary credited artist
- `title` = Release title

### Classical Mode

Optimized for classical music: orchestral, chamber, opera, etc.

**Default columns:**
| Discogs    | MusicBrainz |
| ---------- | ----------- |
| Composer   | Composer    |
| Work       | Work        |
| Performers | Performers  |
| Year       | Date        |
| Label      | Label       |
| ID         | MBID        |

**Field interpretation:**
- `composer` = Work composer (from credits/relations)
- `work` = Composition title (e.g., "Symphony No. 9")
- `performers` = Conductor, orchestra, soloists

> **Discogs `work` implementation**: Discogs does not have a native `work` field. Extract work titles from:
> 1. Release title parsing (regex for "Symphony No.", "Concerto", "Op.", "BWV", etc.)
> 2. ExtraArtists credits (composer credits)
> 3. Title patterns like "Composer: Work Title"

---

## Search Scope

**Allowed entity types:**
- ✅ Album
- ✅ EP
- ✅ Single
- ✅ Compilation
- ✅ Soundtrack
- ❌ Track (individual recordings)

**Rationale:** Focus on *collections* for metadata quality and curation. Individual track searches lead to noise and duplicate results.

---

## Output Formats

### Table (Default)

Uses Spectre.Console rich tables with:
- Column alignment
- Color coding
- **Clickable hyperlinks** (ANSI escape sequences)

```csharp
// Hyperlink implementation
table.AddRow($"[link=https://discogs.com/release/{id}]{id}[/]");
```

### JSON

Machine-readable output for scripting:

```json
[
  {
    "artist": "The Beatles",
    "title": "Abbey Road",
    "year": 1969,
    "label": "Apple Records",
    "id": "24047"
  }
]
```

---

## 4-Mode Matrix

> **Purpose**: This matrix shows how search behavior and defaults vary across all mode/source combinations.
> Each cell represents a distinct use case with optimized settings.

| Mode                    | Source      | Use Case              | Default Sort |
| ----------------------- | ----------- | --------------------- | ------------ |
| Pop + Discogs           | Default     | Vinyl/CD collectors   | Year (desc)  |
| Pop + MusicBrainz       | Tagging     | Music library tagging | Relevance    |
| Classical + Discogs     | Specialist  | Classical collectors  | Composer     |
| Classical + MusicBrainz | Composition | Work-level research   | Work title   |

---

## Tab Completion

> **See also**: [DotNetTabCompletion.md](DotNetTabCompletion.md) for .NET 10 tab completion details

### Installation

```powershell
scripts completion install
. $PROFILE  # Reload profile
```

### Available Completions

```
scripts <TAB>
→ sync, clean, music, mail, completion

scripts music <TAB>
→ search, lookup, schema

scripts music search <query> --<TAB>
→ --source, --mode, --limit, --fields, --output

scripts music search "test" --source <TAB>
→ discogs, musicbrainz

scripts music search "test" --fields <TAB>
→ default, all, artist, album, year, label, country, format, barcode, genre, style, id
```

---

## Implementation Notes

### Hyperlinks in Spectre.Console

> **Testing**: Always test table formatting in Windows Terminal, not IDE console.
> IDE consoles may not render ANSI hyperlinks correctly.

> **Rule**: No fallback values. If a field is empty, display empty — never use placeholders like "Unknown".

```csharp
// ANSI escape sequence format
var link = $"\x1b]8;;{url}\x1b\\{displayText}\x1b]8;;\x1b\\";

// Or using Spectre markup
table.AddRow($"[link={url}]{displayText}[/]");
```

**Terminal support:**
- ✅ Windows Terminal
- ✅ iTerm2
- ✅ VS Code terminal
- ⚠️ conhost.exe (limited)

### Field Name Mapping

```csharp
static readonly Dictionary<string, Func<SearchResult, string>> FieldAccessors = new()
{
    ["artist"] = r => r.Artist ?? "",
    ["album"] = r => r.Title ?? "",
    ["title"] = r => r.Title ?? "",  // alias
    ["year"] = r => r.Year?.ToString() ?? "",
    ["label"] = r => r.Labels?.FirstOrDefault()?.Name ?? "",
    ["country"] = r => r.Country ?? "",
    ["format"] = r => r.Formats?.FirstOrDefault()?.Name ?? "",
    ["barcode"] = r => r.Barcodes?.FirstOrDefault() ?? "",
    ["genre"] = r => string.Join(", ", r.Genres ?? []),
    ["style"] = r => string.Join(", ", r.Styles ?? []),
    ["id"] = r => r.Id?.ToString() ?? "",
};
```
