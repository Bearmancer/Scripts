# C# Implementation Plan - CLI & Services

*Last Updated: December 25, 2025*
*Based on Microsoft docs, Spectre.Console documentation, and community research*

---

## Executive Summary

| Metric | Current | Target |
|--------|---------|--------|
| **Files >300 lines without regions** | 10 | 0 |
| **Console.cs wrapper violations** | 1 | 0 |
| **Compiler warnings** | 0 | 0 ✅ |
| **basedpyright errors** | 0 | 0 ✅ |

---

## Research Sources

### 1. Spectre.Console Official Documentation
**Source:** https://spectreconsole.net/

**Key Findings for Progress Bars (CS-009):**

```csharp
// Best practice: Multiple columns with timing
AnsiConsole.Progress()
    .Columns(
        new TaskDescriptionColumn(),
        new ProgressBarColumn(),
        new PercentageColumn(),
        new ElapsedTimeColumn(),
        new RemainingTimeColumn())
    .Start(ctx => {
        var task = ctx.AddTask("Processing", maxValue: 100);

        // Update description dynamically
        task.Description = $"Processing file {i} of {total}";
        task.Increment(1);
    });
```

**Important Patterns:**
- `IsIndeterminate()` - Use when total unknown, switch to determinate later
- `AutoClear(true)` - Remove progress display when complete
- `HideCompleted(true)` - Remove finished tasks from view
- **⚠️ Progress is NOT thread-safe** - Don't mix with prompts/status

---

### 2. Spectre.Console.Cli Commands
**Source:** https://spectreconsole.net/cli/how-to/defining-commands-and-arguments

**Best Practices:**

```csharp
public class Settings : CommandSettings
{
    // Required positional argument (angle brackets)
    [CommandArgument(0, "<source>")]
    [Description("The source file to process")]
    public string Source { get; init; } = string.Empty;

    // Optional positional argument (square brackets)
    [CommandArgument(1, "[destination]")]
    public string? Destination { get; init; }

    // Short and long option forms
    [CommandOption("-f|--force")]
    [Description("Overwrite existing files")]
    public bool Force { get; init; }

    // Option with default value
    [CommandOption("-b|--buffer-size")]
    [DefaultValue(64)]
    public int BufferSize { get; init; } = 64;
}
```

---

### 3. GitHub Discussions Findings
**Source:** https://github.com/spectreconsole/spectre.console/discussions

**Common Issues & Solutions:**

| Issue | Solution |
|-------|----------|
| Spinners not showing | Set `Console.OutputEncoding = Encoding.UTF8` |
| Emojis not rendering | UTF8 encoding required |
| Cancellation handling | Use `CancellationToken` with async commands |
| Nested Progress/Status | Not supported - use one at a time |

---

## High-Priority Tasks

### CS-003: Add Regions to Large Files

**Files >300 Lines Requiring Regions:**

| File | Lines | Suggested Regions |
|------|-------|-------------------|
| GoogleSheetsService.cs | 1230 | Auth, Read, Write, Sync, Export |
| MusicBrainzService.cs | 1041 | Search, Lookup, Parse, Format |
| MusicSearchCommand.cs | 1035 | Settings, Search Modes, Rendering |
| YouTubePlaylistOrchestrator.cs | 1029 | Fetch, Compare, Sync, Progress |
| DiscogsService.cs | 633 | Auth, Search, Parse, Format |
| MusicFillCommand.cs | 612 | Settings, TSV Processing, Output |
| SyncCommands.cs | 459 | YouTube, LastFm, Status, Help |
| Console.cs | 414 | Logging, Progress, Tables, Formatting |
| MailTmService.cs | 385 | Auth, Messages, Parse |
| Logger.cs | 331 | Config, File, Console, Format |

**Region Naming Convention:**
```csharp
#region Search Operations
// Related methods here
#endregion

#region Data Formatting
// Related methods here
#endregion
```

---

### CS-009: Live Progress Bar Implementation

**Current State:** No live progress during music fill

**Target Implementation:**

```csharp
public async Task<int> ExecuteAsync(CommandContext context, Settings settings)
{
    var records = LoadTsvRecords(settings.InputPath);

    await AnsiConsole.Progress()
        .Columns(
            new TaskDescriptionColumn(),
            new ProgressBarColumn(),
            new PercentageColumn(),
            new ElapsedTimeColumn(),
            new RemainingTimeColumn())
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask("Processing records", maxValue: records.Count);

            foreach (var record in records)
            {
                task.Description = $"Searching: {record.Work} - {record.Composer}";

                var result = await SearchBothServicesAsync(record);
                WriteResultToTsv(result);

                task.Increment(1);
            }
        });

    return 0;
}
```

---

### CS-019: Console.cs Wrapper Enforcement

**Current Violation (CompletionCommands.cs:55-66):**

```csharp
// ❌ BAD: Direct Spectre.Console usage
var panel = new Panel(
    new Markup(
        $"[bold green]✓ Tab completion installed successfully![/]\n\n"
            + $"[dim]Profile:[/]\n[link=file:///{psProfilePath}]{psProfilePath}[/]"
    )
)
{
    Border = BoxBorder.Rounded,
    Padding = new Spectre.Console.Padding(1, 1),
    Header = new PanelHeader("[blue]System Configuration[/]"),
};
```

**Required Fix - Add to Console.cs:**

```csharp
public static void WritePanel(string header, string content, Color? borderColor = null)
{
    var panel = new Panel(new Markup(Markup.Escape(content)))
    {
        Border = BoxBorder.Rounded,
        Padding = new Padding(1, 1),
        Header = new PanelHeader($"[blue]{Markup.Escape(header)}[/]"),
    };

    if (borderColor.HasValue)
        panel.BorderColor(borderColor.Value);

    AnsiConsole.Write(panel);
}
```

**Then refactor CompletionCommands.cs:**

```csharp
// ✅ GOOD: Use wrapper
Console.WritePanel(
    "System Configuration",
    $"✓ Tab completion installed successfully!\n\nProfile:\n{psProfilePath}\n\nAction Required:\nRestart PowerShell or run: . $PROFILE"
);
```

---

## Music Command Improvements (CS-007 to CS-016)

### CS-007: Search Both Services

```csharp
public async Task<MergedSearchResult> SearchBothServicesAsync(MusicRecord record)
{
    // Parallel queries
    var discogsTask = _discogsService.SearchAsync(record);
    var musicBrainzTask = _musicBrainzService.SearchAsync(record);

    await Task.WhenAll(discogsTask, musicBrainzTask);

    // Merge and sort by confidence
    return MergeResults(discogsTask.Result, musicBrainzTask.Result)
        .OrderByDescending(r => r.Confidence)
        .ToList();
}
```

### CS-008: Improved Output Format

**Current (Bad):**
```
Symphony No. 7 - Bruckner, Anton
Label:
50% Victor (Discogs)
```

**Target:**
```
Recording: Symphony No. 7
Composer: Anton Bruckner

┌─ Label ─────────────────────┐
│ 50% Victor (Discogs)        │
│ 40% DG (MusicBrainz)        │
└──────────────────────────────┘
```

---

## Python to C# Migration (CS-024, CS-025)

### Migration Strategy

**Python toolkit modules (61 functions):**

| Module | Functions | C# Equivalent |
|--------|-----------|---------------|
| audio.py | 17 | AudioService.cs |
| video.py | 17 | VideoService.cs |
| cli.py | 12 | CLI commands |
| filesystem.py | 6 | FileSystemService.cs |
| cuesheet.py | 5 | CuesheetService.cs |
| lastfm.py | 4 | LastFmService.cs (exists) |

**Key Libraries:**
- PIL/Pillow → **SixLabors.ImageSharp**
- subprocess → **CliWrap** or Process.Start
- mutagen → **TagLib#**
- requests → **HttpClient**

### ImageSharp vs PIL Differences

| Feature | PIL (Python) | ImageSharp (C#) |
|---------|--------------|-----------------|
| Load image | `Image.open(path)` | `Image.Load(path)` |
| Resize | `img.resize((w, h))` | `img.Mutate(x => x.Resize(w, h))` |
| Save | `img.save(path)` | `img.Save(path)` |
| Pixel access | `img.getpixel((x,y))` | `img[x, y]` |
| Memory model | Mutable | Immutable by default |

---

## Implementation Priority

### Immediate (This Week)
1. **CS-003:** Add regions to GoogleSheetsService.cs, MusicBrainzService.cs
2. **CS-019:** Fix Console.cs wrapper, refactor CompletionCommands.cs
3. **CS-009:** Basic progress bar for music fill

### Short-term (This Month)
4. **CS-007:** Parallel Discogs + MusicBrainz search
5. **CS-008:** Improved search result formatting
6. **CS-011:** Real-time TSV writing

### Long-term (Next Quarter)
7. **CS-024:** Python scrobble integration
8. **CS-025:** Full migration plan
9. **CS-031:** New CLI structure

---

## References

1. **Spectre.Console Docs:** https://spectreconsole.net/
2. **Spectre.Console GitHub:** https://github.com/spectreconsole/spectre.console
3. **ImageSharp Docs:** https://docs.sixlabors.com/
4. **CliWrap:** https://github.com/Tyrrrz/CliWrap
5. **Microsoft .NET Docs:** https://learn.microsoft.com/en-us/dotnet/

---

*Document generated from verified research and documentation review.*
