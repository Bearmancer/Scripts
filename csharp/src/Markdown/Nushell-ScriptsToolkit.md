# Nushell ScriptsToolkit

> **Shell:** Nushell (Nu)
> **Version Target:** 0.80+
> **Philosophy:** Structured data pipelines, types, and error correctness.

---

## Complete Implementation (`ScriptsToolkit.nu`)

Save as `C:\Users\Lance\Dev\powershell\ScriptsToolkit.nu`.

```nu
# ScriptsToolkit.nu
# Full implementation of Scripts Toolkit
#

# ==============================================================================
# CONSTANTS & CONFIGURATION
# ==============================================================================

# path join: Platform agnostic path joining
# $nu.home-path: Built-in variable for user home
const REPO_ROOT = ($nu.home-path | path join "Dev")
const PYTHON_TOOLKIT = ($REPO_ROOT | path join "python/toolkit/cli.py")
const CSHARP_ROOT = ($REPO_ROOT | path join "csharp")
const LOG_DIR = ($REPO_ROOT | path join "logs")

# ==============================================================================
# HELPERS
# ==============================================================================

# Custom commands defined with 'def'
# [msg: string] defines a typed parameter
def log_info [msg: string] { 
    # ansi commands output color codes
    print $"(ansi cyan)[INFO](ansi reset) ($msg)" 
}

def log_success [msg: string] { 
    print $"(ansi green)[DONE](ansi reset) ($msg)" 
}

def log_error [msg: string] { 
    print $"(ansi red)[FAIL](ansi reset) ($msg)" 
}

# '...args' is a rest parameter (collects remaining args)
def invoke_python [...args: string] {
    # 'complete' captures stdout/stderr/exit_code into a record
    let result = (python $PYTHON_TOOLKIT ...$args | complete)
    
    if $result.exit_code != 0 {
        # String interpolation $"..."
        log_error $"Python toolkit failed: ($result.stderr)"
        # 'error make' creates a structured error
        error make {msg: "Python toolkit failed"}
    }
    # Return stdout transparently
    $result.stdout
}

# ==============================================================================
# UTILITIES
# ==============================================================================

# 'export def' makes this command available when module is imported 'use ScriptsToolkit.nu'
export def tkfn [] {
    print ""
    print $"(ansi cyan)ScriptsToolkit Functions (Nushell)(ansi reset)"
    print "=========================================="
    
    # Create a literal table (list of records)
    let funcs = [
        [category name description]; # Header row
        [Filesystem dirs "List directories with sizes"]
        [Filesystem tree "List all items with sizes"]
        [Filesystem torrent "Create .torrent files"]
        [Video remux "Remux disc folders"]
        [Video compress "Batch compress videos"]
        [Video chapters "Extract chapters"]
        [Video res "Report resolutions"]
        [Audio audio "Convert audio"]
        [Audio tomp3 "Convert to MP3"]
        [Audio toflac "Convert to FLAC"]
        [Transcription whisp "Transcribe file"]
        [Transcription wpf "Transcribe folder"]
        [Sync syncall "Run all syncs"]
    ]
    
    # Pipeline: group -> transpose -> loop
    $funcs | group-by category | transpose category items | each {|g|
        print $"(ansi yellow)($g.category)(ansi reset)"
        $g.items | each {|f|
            # fill command pads string
            print $"  (ansi green)($f.name | fill -w 12)(ansi reset)($f.description)"
        }
    }
    print ""
}

# ? denotes optional parameter. = "." sets default value.
export def dirs [
    directory?: path = "."
    --sort (-s): string = "size" # Named flag with type
] {
    invoke_python filesystem tree --directory $directory --sort $sort
}

export def tree [directory?: path = "."] {
    invoke_python filesystem tree --directory $directory --include-files
}

export def torrent [
    directory?: path = "."
    --recursive (-r) # Switch flag (boolean)
] {
    # 'mut' declares a mutable variable
    mut args = [filesystem torrents --directory $directory]
    if $recursive { 
        # append returns a new list
        $args = ($args | append --include-subdirectories) 
    }
    invoke_python ...$args
}

# ==============================================================================
# MEDIA PROCESSING
# ==============================================================================

export def remux [directory?: path = "."] {
    invoke_python video remux --path $directory
}

export def compress [directory?: path = "."] {
    invoke_python video compress --directory $directory
}

export def audio [
    directory?: path = "."
    format?: string = "all"
] {
    invoke_python audio convert --directory $directory --mode convert --format $format
}

export def tomp3 [directory?: path = "."] { 
    audio $directory mp3 
}

# ==============================================================================
# TRANSCRIPTION
# ==============================================================================

export def whisp [
    path: path
    --language (-l): string
    --model (-m): string = "large-v3"
    --translate (-t)
    --force (-f)
    --output (-o): path = "."
] {
    let basename = ($path | path basename)
    # path parse splits extension, stem, etc.
    let stem = ($path | path parse | get stem)
    let srt_path = ($output | path join $"($stem).srt")
    
    # path exists returns bool
    if ($srt_path | path exists) and (not $force) {
        print $"(ansi yellow)Skipped: ($basename) (SRT exists)(ansi reset)"
        return
    }

    log_info $"Transcribing: ($basename)"
    
    mut args = [--model $model --output_format srt --output_dir $output]
    
    # $language != null checks optional presence
    if $language != null { $args = ($args | append [--language $language]) }
    if $translate { $args = ($args | append [--task translate]) }
    
    # ^ executes external command (bypassing Nu aliases)
    ^whisper-ctranslate2 ...$args $path
    
    log_success $"Completed: ($basename)"
}

export def wpf [
    directory?: path = "."
    --language (-l): string
] {
    # glob returns a list of matching paths
    let files = (glob ($directory | path join "*.{mp4,mkv,mp3,flac}"))
    
    for file in $files {
        # Pass conditional args
        if $language != null {
            whisp $file -l $language
        } else {
            whisp $file
        }
    }
}

# ==============================================================================
# SYNC
# ==============================================================================

export def syncyt [--force (-f)] {
    # cd changes directory for the block scope if used in do block, 
    # but here changes shell state.
    cd $CSHARP_ROOT
    mut args = [run -- sync yt]
    if $force { $args = ($args | append --force) }
    ^dotnet ...$args
}

export def synclf [since?: string] {
    cd $CSHARP_ROOT
    mut args = [run -- sync lastfm]
    if $since != null { $args = ($args | append [--since $since]) }
    ^dotnet ...$args
}

export def syncall [] {
    print $"(ansi cyan)[YouTube Sync](ansi reset)"
    syncyt
    print $"(ansi cyan)[Last.fm Sync](ansi reset)"
    synclf
    log_success "All syncs complete!"
}
```

---

## Symbol & Feature Glossary (Nushell)

| Feature                  | Symbol / Syntax        | Detailed Explanation                                                                                    |
| ------------------------ | ---------------------- | ------------------------------------------------------------------------------------------------------- |
| **Path Type**            | `directory?: path`     | Declares parameter as a path. Nu handles OS path separators automatically. `?` means optional.          |
| **Pipeline**             | `                      | `                                                                                                       | Passes data (tables/records/lists) to the next command. Unlike other shells, this passes **Structured Objects**, not text. |
| **String Interpolation** | `$"($var)"`            | Double quotes with `$` prefix. Expressions inside `()` are evaluated.                                   |
| **Constants**            | `const NAME = val`     | Compile-time constants.                                                                                 |
| **Table Literal**        | `[[hdr]; [val]]`       | Creates a table. First row is headers (semicolon separated), subsequent rows are data.                  |
| **Closure**              | `{                     | x                                                                                                       | ... }`                                                                                                                     | Anonymous function definition. Used in `each`, `filter`, `reduce`. |
| **Flags**                | `--flag (-f)`          | Defines a boolean flag `flag` with short alias `f`. If passed, var `$flag` is true.                     |
| **External Command**     | `^cmd`                 | Explicitly calls an external executable, bypassing any internal aliases/definitions with the same name. |
| **Path Parsing**         | `path parse`           | Breaks a file path into `parent`, `stem`, `extension`.                                                  |
| **Rest Args**            | `...args`              | Captures all remaining arguments as a list. Also used to spread a list into arguments: `...$list`.      |
| **Complete**             | `                      | complete`                                                                                               | Captures the output of an external command into a record `{ stdout, stderr, exit_code }`.                                  |
| **Error**                | `error make {msg: ..}` | Throws a structured error, halting execution.                                                           |

### Formatting
- **Indent:** 2 or 4 spaces.
- **Camel-kebab:** Nushell prefers `kebab-case` for commands and variables.
- **Blocks:** Use `{ ... }`.
