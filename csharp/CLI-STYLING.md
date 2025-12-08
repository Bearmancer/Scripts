# CLI Styling Policy

This document defines the styling conventions for command-line help text and output formatting. Following .NET CLI conventions (used by `dotnet`, Entity Framework, and modern Microsoft tooling).

## Style Choice: .NET CLI

| Style      | Examples                      | Characteristics                                      |
| ---------- | ----------------------------- | ---------------------------------------------------- |
| .NET CLI   | `dotnet`, `ef`, `nuget`       | Clean, concise, PascalCase commands, lowercase flags |
| PowerShell | `Get-Command`, `Set-Location` | Verb-Noun, verbose, `-ParameterName` style           |
| UNIX       | `ls`, `grep`, `curl`          | Terse, single-letter flags, cryptic                  |

**.NET CLI was chosen because:**
- Modern, clean aesthetic
- Balances discoverability with brevity
- Consistent with the tooling ecosystem (`dotnet`, `ef`, `nuget`)
- Hierarchical commands (`cli sync yt`, `cli clean purge`)

---

## Command Structure

```
cli <group> <command> [arguments] [options]
```

**Examples:**
```
cli sync yt --verbose
cli music search --artist "Pink Floyd" --album "Animals"
cli clean purge lastfm
```

---

## Help Text Format

### Section Headers
Use **bold white** uppercase titles followed by a blank line:

```
USAGE
  cli <group> <command> [options]

COMMANDS
  sync        Sync data to Google Sheets
  clean       Delete state, cache, and remote data
  music       Search Discogs and MusicBrainz
```

### Options Format
```
  -s, --short <VALUE>           Description text. [default: value]
      --long-only <VALUE>       When no short form exists.
  -f, --flag                    Boolean flag. [default: False]
```

**Alignment rules:**
- Short flag: 2-space indent, `-x`
- Long flag: `, --name`
- Value placeholder: `<UPPERCASE>` in cyan
- Description: Starts at column 32 (minimum 2-space gap)
- Defaults/allowed values: `[dim]` style at end

### Arguments Format
```
ARGUMENTS
  <service>                     Required argument. yt, lastfm, or all.
  [service]                     Optional argument (brackets).
```

---

## Console Output Levels

Log messages use fixed 4-character level codes (industry standard approach):

```
 [[DBG!]] 14:32:01: Verbose diagnostic information
 [[INFO]] 14:32:01: Starting sync operation...
 [[OKAY]] 14:32:15: Sync complete
 [[WARN]] 14:32:05: Rate limit approaching
 [[ERR!]] 14:32:10: Connection failed
 [[CRIT]] 14:32:20: Fatal error, aborting
 [[PROG]] 14:32:03: Processing 150/1000...
```

**Format:** `[{color}] [[{CODE}]][/] [dim]{timestamp}:[/] {message}`

| Level    | Code | Color    | Usage                                   |
| -------- | ---- | -------- | --------------------------------------- |
| Debug    | DBG! | grey     | Verbose diagnostic info                 |
| Info     | INFO | blue     | Normal operational messages             |
| Success  | OKAY | green    | Completion, positive outcomes           |
| Warning  | WARN | yellow   | Non-fatal issues, user attention needed |
| Error    | ERR! | red      | Failures requiring action               |
| Fatal    | CRIT | red bold | Critical failures, immediate abort      |
| Progress | PROG | cyan     | Long-running operation updates          |

**Design rationale:**
- Fixed 4-character codes eliminate padding/whitespace complexity
- Follows industry standards (serilog, log4net, structlog)
- Exclamation marks (`!`) add visual weight to critical levels
- Consistent width ensures clean column alignment in logs

---

## Description Text Guidelines

### Do
- Start with a verb: "Sync", "Delete", "Search", "Show"
- Be concise: max ~60 characters
- Use sentence case (capitalize first word only)
- Include allowed values inline: `yt, lastfm, all`
- Specify defaults when non-obvious

### Don't
- Don't end with periods (unless multi-sentence)
- Don't use articles ("the", "a") unnecessarily  
- Don't repeat the command name in the description
- Don't use jargon without context

### Examples

**Good:**
```csharp
[Description("Enable debug logging")]
[Description("Re-sync from date (yyyy/MM/dd)")]
[Description("yt, lastfm, all (default: all)")]
[Description("Max results per source")]
```

**Bad:**
```csharp
[Description("This option enables verbose debug logging output.")]  // Too verbose
[Description("The service")]  // Too vague
[Description("LASTFM or YOUTUBE")]  // Wrong case
```

---

## Spectre.Console CLI Attributes

### Command Options
```csharp
[CommandOption("-v|--verbose")]
[Description("Enable debug logging")]
public bool Verbose { get; init; }

[CommandOption("-s|--source")]
[Description("musicbrainz, discogs, both")]
[DefaultValue("both")]
public string Source { get; init; } = "both";
```

### Command Arguments
```csharp
[CommandArgument(0, "<id>")]        // Required
[Description("Release ID")]
public required string Id { get; init; }

[CommandArgument(0, "[service]")]   // Optional
[Description("yt, lastfm (omit for all)")]
public string? Service { get; init; }
```

### Branch Configuration
```csharp
config.AddBranch("sync", sync =>
{
    sync.SetDescription("Sync data to Google Sheets");
    sync.AddCommand<SyncYouTubeCommand>("yt")
        .WithDescription("Sync YouTube playlists to Google Sheets");
});
```

---

## Status Indicators

For operation status without log levels:

| Indicator | Symbol | Color | Usage               |
| --------- | ------ | ----- | ------------------- |
| Starting  | →      | blue  | Operation beginning |
| Complete  | ✓      | green | Success             |
| Failed    | ✗      | red   | Failure             |

```csharp
Console.Starting("Connecting to YouTube API...");
Console.Complete("Fetched 150 videos");
Console.Failed("Authentication failed");
```

---

## Rich Output

### Key-Value Pairs
```csharp
Console.KeyValue("Address", account.Address);
// Output: Address: user@example.com
```

### Rules (Section Dividers)
```csharp
Console.Rule("MusicBrainz Results");
// Output: ──────────── MusicBrainz Results ────────────
```

### Dimmed Text
```csharp
Console.Dim("  ID: abc123");  // Supplementary info
Console.Tip("Use 'cli status' to check progress");  // Hints
```

---

## Quick Reference

| Element            | Format                             |
| ------------------ | ---------------------------------- |
| Command names      | lowercase, hyphenated (`sync-all`) |
| Option flags       | lowercase (`--verbose`, `-v`)      |
| Value placeholders | UPPERCASE in `<angle brackets>`    |
| Optional arguments | `[square brackets]`                |
| Required arguments | `<angle brackets>`                 |
| Allowed values     | Comma-separated, lowercase         |
| Defaults           | `[default: value]` suffix          |
| Log level width    | 8 characters, right-aligned        |
| Timestamp format   | `HH:mm:ss`                         |
