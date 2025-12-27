# PowerShell Completion Architecture
*Created: January 2025*
*Updated: December 2025 - Benchmarks verified*

## Performance Summary

| Scenario | Time | Delta |
|----------|------|-------|
| pwsh -NoProfile | ~284ms | baseline |
| PSCompletions alone (fresh) | ~314ms | module import only |
| PSFzf alone (fresh) | ~489ms | module import only |
| Carapace init | ~324ms | completer registration |
| **Current Profile** | **~2,342ms** | **+2,058ms** |

**Verified Component Times (Dec 26, 2025):**

| Component | Import Time | Notes |
|-----------|-------------|-------|
| PSFzf | **489ms** | ✅ Lazy loaded (saves 489ms) |
| Carapace | **324ms** | 670 completers |
| PSCompletions | **314ms** | Tab handler + menu |
| ScriptsToolkit | **128ms** | Custom functions |
| PSReadLine | **73ms** | Pre-loaded by PowerShell |
| argc | **61ms** | dotnet, Continwhisper completers |
| ThreadJob | **24ms** | Used by profile |

#!/usr/bin/env dotnet fsi

#r "nuget: Spectre.Console"
#r "nuget: Polly"

open System
open System.Diagnostics
open System.IO
open System.Text.Json
open Spectre.Console
open Polly

let models = [| "tiny.en"; "distil-large-v3.5"; "distil-medium.en"; "distil-small.en" |]
let implementations = [| "whisper-ctranslate2"; "faster-whisper"; "whisper-vulkan"; "faster-whisper-xxl" |]

let log level message =
    let color = match level with "SUCCESS" -> Color.Green | "WARNING" -> Color.Yellow | "ERROR" | "CRITICAL" -> Color.Red | _ -> Color.Blue
    AnsiConsole.MarkupLine($"[{color}]{DateTime.Now:yyyy/MM/dd HH:mm:ss} | {level,-11} | {message}[/]")

let run command args =
    Policy.Handle<Exception>(fun ex -> ex.Message.Contains("timeout") || ex.Message.Contains("connection"))
        .WaitAndRetry(10, fun i -> TimeSpan.FromSeconds(min (3.0 * pown 2.0 i) 300.0))
        .Execute(fun () ->
            log "INFORMATION" $"Executing: {command} {String.concat " " args}"
            use proc = Process.Start(ProcessStartInfo(command, String.concat " " args, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false))
            proc.WaitForExit()
            if proc.ExitCode <> 0 then failwith (proc.StandardError.ReadToEnd()))

let commandExists command =
    try
        use proc = Process.Start(ProcessStartInfo(command, "--version", RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true))
        proc.WaitForExit()
        proc.ExitCode = 0
    with _ -> false

let ensureDependencies () =
    Environment.GetEnvironmentVariable("HF_TOKEN") |> function
    | null -> failwith "HF_TOKEN not set. Create at: https://huggingface.co/settings/tokens"
    | _ -> log "SUCCESS" "HF_TOKEN found"

    implementations |> Array.iter (fun impl ->
        if not (commandExists impl) then
            log "INFORMATION" $"Installing {impl}..."
            run "python" [| "-m"; "pip"; "install"; impl; "--upgrade" |]
            log "SUCCESS" $"Installed {impl}")

let parseSubtitles path =
    if File.Exists(path) then
        File.ReadAllText(path).Split([| "\n\n"; "\r\n\r\n" |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun s -> s.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        |> Array.filter (fun l -> l.Length >= 3)
        |> Array.map (fun l -> l |> Array.skip 2 |> String.concat " ")
        |> fun segs -> let words = segs |> Array.collect (fun t -> t.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                       (words.Length, segs.Length, if segs.Length > 0 then segs |> Array.averageBy (float << String.length) else 0.0)
    else (0, 0, 0.0)

let benchmark impl model input =
    let output = $"output_{impl}_{model}.srt"
    let args = if impl = "whisper-vulkan" then [| "--output-srt"; "--language"; "en"; "--model"; model; input |]
               else [| "--output_format"; "srt"; "--language"; "en"; "--model"; model; "--output_dir"; "."; input |]
    log "INFORMATION" $"Running {impl}/{model}..."
    let sw = Stopwatch.StartNew()
    try
        run impl args; sw.Stop()
        let (wc, sc, avg) = parseSubtitles output
        log "SUCCESS" $"Completed {impl}/{model} in {sw.Elapsed.TotalSeconds:F2}s"
        (impl, model, sw.Elapsed.TotalSeconds, wc, sc, avg, output, true, "")
    with ex ->
        sw.Stop(); log "ERROR" $"Failed {impl}/{model}: {ex.Message}"
        (impl, model, sw.Elapsed.TotalSeconds, 0, 0, 0.0, "", false, ex.Message)

let display results =
    let baseline = results |> List.filter (fun (_, _, _, _, _, _, _, success, _) -> success) |> List.map (fun (_, _, dur, _, _, _, _, _, _) -> dur) |> List.min
    let table = Table(Border = TableBorder.Rounded, Title = TableTitle("Whisper Implementation Benchmark Results"))
    table.AddColumns("Implementation", "Model", "Duration (s)", "Multiplier", "Words", "Segments", "Avg Seg Len", "Status") |> ignore

    results |> List.sortBy (fun (_, _, dur, _, _, _, _, success, _) -> (not success, dur / baseline))
    |> List.iter (fun (impl, model, dur, wc, sc, avg, _, success, err) ->
        let (color, mult, durStr, status) =
            if success then (Color.Green, $"{dur / baseline:F2}x", $"{dur:F2}", "✓")
            else (Color.Red, "N/A", "N/A", $"✗ {err.Substring(0, min 30 err.Length)}")
        table.AddRow(Markup(impl), Markup(model), Markup(durStr, Style(color)), Markup(mult, Style(color)),
                     Markup(string wc, Style(color)), Markup(string sc, Style(color)), Markup($"{avg:F1}", Style(color)), Markup(status, Style(color))) |> ignore)

    AnsiConsole.Write(table)
    File.WriteAllText("benchmark_results.json", JsonSerializer.Serialize(results, JsonSerializerOptions(WriteIndented = true)))
    log "SUCCESS" "Results saved to benchmark_results.json"

let input = fsi.CommandLineArgs |> Array.skip 1 |> Array.tryHead |> Option.defaultWith (fun () -> failwith "Usage: dotnet fsi benchmark.fsx <input-file>")
if not (File.Exists(input)) then failwith $"Input not found: {input}"

log "INFORMATION" $"Starting benchmark: {input}"
ensureDependencies ()
implementations |> Array.collect (fun impl -> models |> Array.map (fun model -> benchmark impl model input)) |> Array.toList |> display
log "INFORMATION" "All implementations use OpenAI Whisper models with different backends (CTranslate2/Vulkan)"**Key Optimization Applied:** PSFzf lazy loading saves ~489ms at startup.

## Overview

The completion system consists of three layers working together:

```
┌─────────────────────────────────────────────────────────────┐
│                     TAB KEY PRESS                           │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│              PSCompletions (Menu UI Layer)                  │
│  • Intercepts Tab via Set-PSReadLineKeyHandler              │
│  • Calls TabExpansion2 for completion data                  │
│  • Renders TUI menu with enable_menu_enhance=1              │
│  • Owns: psc, winget completions                            │
└──────────────────────────┬──────────────────────────────────┘
                           │ TabExpansion2
                           ▼
┌─────────────────────────────────────────────────────────────┐
│           Register-ArgumentCompleter Sources                │
├─────────────────────────────────────────────────────────────┤
│  Carapace (670 commands)     │  argc (2 commands)           │
│  • git, gh, docker, kubectl  │  • dotnet                    │
│  • npm, cargo, go, az        │  • whisper-ctranslate2       │
│  • Custom: whisper-ctranslate2.yaml                         │
└─────────────────────────────────────────────────────────────┘
```

## Component Details

### 1. PSCompletions v6.2.2 (Menu UI)

**Role:** Tab key handler and TUI menu renderer

**Current Completions (2):**
- `psc` - PSCompletions management
- `winget` - Windows Package Manager (Carapace lacks this)

**How it Works:**
1. `Import-Module PSCompletions` registers Tab key handler
2. On Tab press, calls `TabExpansion2` for completion data
3. Aggregates results from ALL registered completers
4. Renders results in TUI menu (when >1 match)

**Configuration:**
```powershell
$PSCompletions.config.enable_menu          # 1 = TUI menu enabled
$PSCompletions.config.enable_menu_enhance  # 1 = Enhanced tooltips
$PSCompletions.config.trigger_key          # Tab
$PSCompletions.config.completion_suffix    # "" (empty)
```

### 2. Carapace v1.5.7 (Completion Data Provider)

**Role:** Primary source of completion data for 670+ commands

**How it Works:**
```powershell
carapace _carapace | Out-String | Invoke-Expression
```
This registers `Register-ArgumentCompleter` for each supported command.

**Environment:**
```powershell
$env:CARAPACE_BRIDGES = 'zsh,fish,bash,inshellisense'
```

**Not Covered:** `winget` (use PSCompletions or argc)

### 3. argc (Additional Completions)

**Role:** Fills gaps not covered by Carapace

**Currently Registered:**
- `dotnet` - .NET CLI (better than Carapace)
- `whisper-ctranslate2` - Custom spec

**How it Works:**
```powershell
argc --argc-completions powershell dotnet | Out-String | Invoke-Expression
```

## Data Flow: Tab Completion

```
User types: git checkout ma<Tab>

1. PSCompletions intercepts Tab
2. Calls TabExpansion2('git checkout ma', cursorColumn=16)
3. PowerShell finds ArgumentCompleter for 'git' (Carapace)
4. Carapace spawns: carapace git powershell 'checkout' 'ma'
5. Carapace returns: [main, master, maintenance]
6. PSCompletions renders menu with options
7. User selects, PSCompletions inserts selection
```

## PSReadLine Integration

PSReadLine handles:
- **Inline Prediction:** `PredictionSource History` shows gray inline suggestions
- **History Navigation:** UpArrow/DownArrow cycle through history
- **Native History Search:** Ctrl+R (ReverseSearchHistory), F8 (HistorySearchBackward)

PSCompletions overrides:
- **Tab:** Custom handler for TUI menu

## PSFzf (Optional - NOT in current profile)

**Role:** Fuzzy finder integration

**Key Bindings (when enabled):**
- `Ctrl+R` - Fuzzy history search
- `Ctrl+T` - Fuzzy file/directory search
- `Alt+C` - Fuzzy cd to directory
- `Ctrl+Space` - Fuzzy tab completion

**Impact:** ~421ms import time

**Lazy Loading Option:**
```powershell
#region PSFzf - Lazy loaded on first Ctrl+R
Set-PSReadLineKeyHandler -Key 'Ctrl+r' -ScriptBlock {
    if (-not (Get-Module PSFzf)) {
        Import-Module PSFzf
        Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t'
        Set-PsFzfOption -PSReadlineChordReverseHistory 'Ctrl+r'
    }
    Invoke-FzfPsReadlineHandlerHistory
}
Set-PSReadLineKeyHandler -Key 'Ctrl+t' -ScriptBlock {
    if (-not (Get-Module PSFzf)) {
        Import-Module PSFzf
        Set-PsFzfOption -PSReadlineChordProvider 'Ctrl+t'
        Set-PsFzfOption -PSReadlineChordReverseHistory 'Ctrl+r'
    }
    Invoke-FzfPsReadlineHandlerProvider
}
#endregion
```

## History List Alternatives to PSFzf

### Option 1: PSReadLine Native (No module needed)
| Key | Function | Description |
|-----|----------|-------------|
| `Ctrl+R` | ReverseSearchHistory | Incremental text search |
| `F8` | HistorySearchBackward | Match by prefix |
| `UpArrow` | PreviousHistory | Cycle through history |

### Option 2: Out-GridView F7 Handler (No module needed)
```powershell
Set-PSReadLineKeyHandler -Key F7 -BriefDescription 'ShowHistory' -ScriptBlock {
    $line = $null
    $cursor = $null
    [Microsoft.PowerShell.PSConsoleReadLine]::GetBufferState([ref]$line, [ref]$cursor)
    $selection = Get-History | Out-GridView -Title 'Select Command' -PassThru
    if ($selection) {
        [Microsoft.PowerShell.PSConsoleReadLine]::ReplaceLine($selection.CommandLine)
        [Microsoft.PowerShell.PSConsoleReadLine]::SetCursorPosition($selection.CommandLine.Length)
    }
}
```

### Option 3: F7History Module (~471ms)
```powershell
Install-Module F7History -Scope CurrentUser
Import-Module F7History
```
Provides console-based GUI popup on F7.

### Recommendation
- **If you rarely use Ctrl+R:** Use PSReadLine native Ctrl+R
- **If you want visual list:** Use Out-GridView F7 handler (zero overhead)
- **If you want fuzzy search:** Use PSFzf with lazy loading pattern

## Benchmark Results

| Scenario | Time (ms) | Notes |
|----------|-----------|-------|
| pwsh -NoProfile | 273 | Baseline |
| PSCompletions alone (fresh) | 1,537 | +1264ms (includes PSReadLine) |
| Carapace alone (fresh) | 1,245 | +972ms (registers 670 completers) |
| **Current Profile** | **1,887** | **+1614ms** |
| Profile + PSFzf (eager) | ~2,300 | +~400ms additional |
| Profile + PSFzf (lazy) | 1,887 | 0ms startup, ~180ms first use |

**Verified December 2025** - Fresh shell measurements with 5-iteration averages.

## Known Issues

### COMP-001: Tab Double Space
**Symptom:** Tab inserts completion + extra space
**Cause:** PSCompletions may add space AND completer adds space
**Fix:** Set `$PSCompletions.config.completion_suffix = ""`

### COMP-002: Tab Selects Instead of Filling ✅ FIXED
**Symptom:** Tab navigates menu instead of inserting selection
**Expected:** Single match should auto-insert
**Fix Applied:** `psc menu config enable_enter_when_single 1`

### COMP-003: winget Dynamic Search Broken
**Symptom:** `winget install je--<Tab>` doesn't search packages
**Cause:** PSCompletions winget doesn't support dynamic API search
**Fix:** Use `winget search` then copy package name

## Current Profile Order

```powershell
1. UTF-8 + Module Path      # Essential config
2. ScriptsToolkit           # Custom functions (~49ms)
3. PSCompletions            # Tab handler + menu (~350ms)
4. PSReadLine               # Prediction + colors (~5ms)
5. Carapace                 # 670 completions (~168ms)
6. argc                     # dotnet, whisper (~117ms)
7. PSFzf (LAZY)             # Ctrl+R/Ctrl+T (0ms startup)
```

This order ensures:
- PSCompletions gets Tab before PSReadLine's default
- Carapace/argc completers registered before any Tab press
- All completers available to PSCompletions TabExpansion2
