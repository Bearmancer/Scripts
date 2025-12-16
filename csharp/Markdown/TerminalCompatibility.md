# Terminal Compatibility

This document describes terminal support for the CLI features.

---

## Hyperlink Support

The music search command uses ANSI escape sequences for clickable hyperlinks.

### Supported Terminals

| Terminal               | Hyperlinks | ANSI Colors | Notes                        |
| ---------------------- | ---------- | ----------- | ---------------------------- |
| Windows Terminal       | ✅ Full     | ✅ Full      | Recommended for Windows      |
| iTerm2                 | ✅ Full     | ✅ Full      | Recommended for macOS        |
| VS Code Terminal       | ✅ Full     | ✅ Full      | Works in integrated terminal |
| PowerShell 7 (conhost) | ⚠️ Partial  | ✅ Full      | Links may not be clickable   |
| CMD (conhost)          | ❌ None     | ⚠️ Partial   | Use Windows Terminal instead |
| macOS Terminal.app     | ⚠️ Partial  | ✅ Full      | Limited hyperlink support    |

### Testing Hyperlinks

Run the following to test hyperlink rendering:

```powershell
scripts music search "Beatles" --source musicbrainz --limit 3
```

If IDs are clickable and open the correct URLs, hyperlinks are working.

---

## Table Rendering

### Best Experience

For optimal table rendering:
1. Use Windows Terminal (Windows) or iTerm2 (macOS)
2. Use a monospace font
3. Terminal width should be at least 120 characters for debug mode

### Table Border Styles

The CLI uses Spectre.Console rounded borders:
- Standard mode: 5 columns (Artist, Title, Year, Type, ID)
- Debug mode: 13 columns (includes Score, Country, Genres, etc.)

---

## Color Themes

The CLI uses the following color scheme:

| Element            | Color  | Usage                   |
| ------------------ | ------ | ----------------------- |
| Discogs source     | Yellow | `[yellow]Discogs[/]`    |
| MusicBrainz source | Cyan   | `[cyan]MusicBrainz[/]`  |
| Missing data       | Dim    | `[dim]—[/]`             |
| Success messages   | Green  | Via `Console.Success()` |
| Warnings           | Yellow | Via `Console.Warning()` |
| Errors             | Red    | Via `Console.Error()`   |

---

## Testing Recommendations

### IDE Console Limitations

⚠️ **Do not test visual features in IDE console** (Visual Studio, VS Code debug console).

IDE consoles may:
- Strip ANSI color codes
- Not render hyperlinks
- Have limited width

### Recommended Testing

1. Open Windows Terminal (or equivalent)
2. Navigate to project directory
3. Run commands directly:

```powershell
# Normal mode
dotnet run -- music search "Beatles Abbey Road"

# Debug mode (all fields)
dotnet run -- music search "Beethoven Symphony" --source musicbrainz --debug
```

---

## Troubleshooting

### Colors/Links Not Working

1. Verify terminal supports ANSI:
   ```powershell
   $env:TERM
   ```
   
2. Check if running in conhost vs Windows Terminal:
   ```powershell
   $host.Name
   ```

3. For Windows Terminal, ensure version 1.7+ for hyperlink support

### Table Too Wide

Use `--output json` for machine-readable output that doesn't depend on terminal width:

```powershell
scripts music search "Beatles" --output json
```
