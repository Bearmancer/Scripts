# JSON Output for Scripting

This document explains when and why to use JSON output instead of native C# processing.

---

## The Question

> "JSON output for scripting -- can't C# handle it natively?"

**Answer**: Yes, C# can handle everything natively. JSON output is for **external tool integration**, not internal C# processing.

---

## When to Use Each Approach

### Native C# (Default)

Use when:
- Processing within the same application
- Building on results programmatically
- Performance is critical
- Type safety matters

```csharp
// Native C# - type-safe, fast, integrated
var results = await discogsService.SearchAsync("Beatles");
var filtered = results.Where(r => r.Year > 1965).ToList();
```

### JSON Output

Use when:
- Piping to external tools (jq, PowerShell, Python)
- Storing for later analysis
- Sharing with other systems
- Shell scripting workflows

```powershell
# PowerShell piping
scripts music search "Beatles" --output json | ConvertFrom-Json | Where-Object { $_.Year -gt 1965 }

# jq processing (bash/zsh)
scripts music search "Beatles" --output json | jq '.[] | select(.Year > 1965)'

# Python processing
scripts music search "Beatles" --output json | python -c "import json,sys; print([r for r in json.load(sys.stdin) if r['Year'] > 1965])"
```

---

## Use Case Examples

### 1. PowerShell Data Pipeline

```powershell
# Search, filter, and export to CSV
scripts music search "Deutsche Grammophon Beethoven" --output json `
    | ConvertFrom-Json `
    | Where-Object { $_.Format -eq "CD" } `
    | Select-Object Artist, Title, Year `
    | Export-Csv -Path "results.csv"
```

### 2. Batch Processing

```powershell
# Process multiple queries
$queries = @("Bach", "Mozart", "Beethoven")
$results = @()

foreach ($q in $queries) {
    $json = scripts music search $q --output json --limit 5
    $results += ($json | ConvertFrom-Json)
}

$results | Group-Object Label | Sort-Object Count -Descending
```

### 3. Cross-Tool Integration

```powershell
# Search Discogs, then lookup in MusicBrainz
$discogsResults = scripts music search "Abbey Road" --source discogs --output json | ConvertFrom-Json

foreach ($release in $discogsResults) {
    # Use barcode to find in MusicBrainz
    if ($release.Barcode) {
        scripts music search $release.Barcode --source musicbrainz
    }
}
```

### 4. Data Storage

```powershell
# Archive search results
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
scripts music search "Karajan" --output json --limit 100 | Out-File "searches/$timestamp-karajan.json"
```

---

## Why Not Just Use C# for Everything?

### C# Strengths
- Type safety and IntelliSense
- Performance
- Complex logic

### Shell/JSON Strengths
- Ad-hoc queries without code changes
- Quick one-liners
- Integration with existing shell workflows
- No recompilation needed

---

## JSON Schema

The JSON output follows this structure:

```json
[
  {
    "Id": "24047",
    "Artist": "The Beatles",
    "Title": "Abbey Road",
    "Year": 1969,
    "Label": "Apple Records",
    "Format": "Vinyl",
    "Country": "UK",
    "Genres": ["Rock"],
    "Styles": ["Pop Rock", "Psychedelic Rock"],
    "Barcode": "077774644518",
    "CatalogNumber": "PCS 7088"
  }
]
```

---

## Implementation Notes

### Adding JSON Output

```csharp
if (settings.Output == "json")
{
    var json = JsonSerializer.Serialize(results, new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    });
    System.Console.WriteLine(json);
    return 0;
}

// Default: Spectre.Console table output
```

### Avoiding Spectre Markup in JSON

When outputting JSON, bypass Spectre.Console entirely to avoid ANSI escape sequences:

```csharp
// Use System.Console, not Spectre Console
System.Console.WriteLine(json);
```

---

## Summary

| Approach     | Best For                                   |
| ------------ | ------------------------------------------ |
| Table output | Interactive use, human reading             |
| JSON output  | Scripting, piping, storage, external tools |
| Native C#    | Internal processing, complex logic         |

JSON output is a **bridge** between your C# application and the broader scripting ecosystem.
