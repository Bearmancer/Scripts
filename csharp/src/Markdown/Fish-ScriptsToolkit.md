# Fish ScriptsToolkit

> **Shell:** Fish (Friendly Interactive Shell)
> **Version Target:** 3.0+
> **Philosophy:** User-friendly, explicit, colors-by-default, no hidden configuration.

---

## Complete Implementation (`ScriptsToolkit.fish`)

Save as `C:\Users\Lance\Dev\powershell\ScriptsToolkit.fish`.

```fish
#!/usr/bin/env fish
#
# ScriptsToolkit.fish
# Full implementation of Scripts Toolkit
#

# ==============================================================================
# CONFIGURATION
# ==============================================================================

# dirname (status filename): Gets the directory of the currently currently file.
# status is a builtin command to query shell status.
set -g SCRIPT_DIR (dirname (status filename))
set -g REPO_ROOT (dirname $SCRIPT_DIR)
set -g PYTHON_TOOLKIT "$REPO_ROOT/python/toolkit/cli.py"
set -g CSHARP_ROOT "$REPO_ROOT/csharp"
set -g LOG_DIR "$REPO_ROOT/logs"

# ==============================================================================
# HELPERS
# ==============================================================================

function log_info
    # set_color: Builtin for managing terminal colors
    set_color cyan
    echo -n "[INFO] "
    set_color normal
    echo $argv
end

function log_success
    set_color green
    echo -n "[DONE] "
    set_color normal
    echo $argv
end

function log_error
    set_color red
    echo -n "[FAIL] "
    set_color normal
    echo $argv >&2
end

function invoke_python
    # 'command -v': Checks for existence of executable.
    # '; or': Executes if previous command FAILED (non-zero exit).
    set -l python_exe (command -v python3; or command -v python)
    
    # $argv: The list of arguments passed to function
    $python_exe $PYTHON_TOOLKIT $argv
    
    # $status: The exit code of the last command
    if test $status -ne 0
        log_error "Python toolkit exited with code $status"
        return $status
    end
end

# ==============================================================================
# UTILITIES
# ==============================================================================

function tkfn --description "List toolkit functions"
    echo
    set_color cyan; echo "ScriptsToolkit Functions (Fish)"; set_color normal
    echo "==================================="

    # Define a list of strings "Category:Name"
    # Fish lists are space separated
    set -l funcs \
      "Filesystem:dirs" \
      "Filesystem:tree" \
      "Video:remux" \
      "Video:compress" \
      "Audio:audio" \
      "Audio:tomp3" \
      "Transcription:whisp" \
      "Sync:syncapp"

    # NOTE: Fish doesn't have associative arrays natively (dictionaries).
    # We just print categories manually for cleaner code in this implementation.
    
    set_color yellow; echo "Filesystem"; set_color normal
    echo "  dirs         List directories with sizes"
    echo "  tree         List all items with sizes"
    
    set_color yellow; echo "Video"; set_color normal
    echo "  remux        Remux disc folders to MKV"
    echo "  compress     Batch compress videos"
    
    set_color yellow; echo "Audio"; set_color normal
    echo "  audio        Convert audio files"
    echo "  tomp3        Convert to MP3"
    
    set_color yellow; echo "Transcription"; set_color normal
    echo "  whisp        Transcribe file/folder"
    echo "  wpf          Transcribe folder"
    
    set_color yellow; echo "Sync"; set_color normal
    echo "  syncyt       Sync YouTube"
    echo "  synclf       Sync Last.fm"
    echo "  syncall      Run all syncs"
    echo
end

# ==============================================================================
# COMMANDS
# ==============================================================================

function dirs --description "List directories with sizes"
    # set -q check if variable is set.
    # argv[1] is 1-indexed access.
    # 'test -n' checks non-empty string.
    # 'and echo ..; or echo ..' is the ternary operator equivalent
    set -l dir (test -n "$argv[1]"; and echo $argv[1]; or echo ".")
    set -l sort (test -n "$argv[2]"; and echo $argv[2]; or echo "size")
    invoke_python filesystem tree --directory $dir --sort $sort
end

function tree
    set -l dir (test -n "$argv[1]"; and echo $argv[1]; or echo ".")
    invoke_python filesystem tree --directory $dir --include-files
end

function torrent
    # argparse parses arguments.
    # 'r/recursive' means -r or --recursive sets $_flag_recursive or $_flag_r
    argparse 'r/recursive' -- $argv
    
    set -l dir (test -n "$argv[1]"; and echo $argv[1]; or echo ".")
    set -l args filesystem torrents --directory $dir
    
    # 'set -q' tests if the flag variable exists (is set)
    if set -q _flag_recursive
        set -a args --include-subdirectories
    end
    
    invoke_python $args
end

function remux
    invoke_python video remux --path (test -n "$argv[1]"; and echo $argv[1]; or echo ".")
end

function compress
    invoke_python video compress --directory (test -n "$argv[1]"; and echo $argv[1]; or echo ".")
end

function audio
    set -l dir (test -n "$argv[1]"; and echo $argv[1]; or echo ".")
    set -l fmt (test -n "$argv[2]"; and echo $argv[2]; or echo "all")
    invoke_python audio convert --directory $dir --mode convert --format $fmt
end

function tomp3
    audio $argv[1] mp3
end

function whisp --description "Transcribe audio"
    # Argument definition: name=type (l/language=?)
    # =? means optional value
    argparse 'l/language=' 'm/model=' 't/translate' 'f/force' 'o/output=' -- $argv
    or return 1 # Exit if parsing fails
    
    set -l path $argv[1]
    
    # Use variable from flag or default
    set -l model $_flag_model; or set model "large-v3"
    set -l out $_flag_output; or set out "."
    
    # string split: splits filename extensions using '.'
    # This is complex in fish without external tools like basename
    # We'll use basename/dirname for simplicity
    set -l fname (basename "$path")
    # string replace -r: regex replace
    set -l name (string replace -r '\.[^.]*$' '' -- $fname)
    set -l srt "$out/$name.srt"
    
    if test -f "$srt"; and not set -q _flag_force
        set_color yellow; echo "Skipped: $fname (SRT exists)"; set_color normal
        return 0
    end
    
    log_info "Transcribing: $fname"
    
    set -l args --model $model --output_format srt --output_dir $out
    
    if set -q _flag_language
        set -a args --language $_flag_language
    end
    
    if set -q _flag_translate
        set -a args --task translate
    end
    
    whisper-ctranslate2 $args $path
    log_success "Completed: $fname"
end

function wpf --description "Transcribe folder"
    set -l dir (test -n "$argv[1]"; and echo $argv[1]; or echo ".")
    
    # Globbing: fish supports ** recursively by default if enabled, 
    # but here we just do flat folder by default logic.
    # wildcards for extensions
    for f in $dir/*.{mp4,mkv,mp3,flac}
        # Check if file exists (if glob matches nothing, loop might run once with literal string in older fish, but checked here)
        if test -f "$f"
           # Pass all other arguments ($argv[2..-1]) to whisp
           whisp "$f" $argv[2..-1]
        end
    end
end

function syncyt
    # pushd/popd work as expected
    pushd $CSHARP_ROOT
    
    set -l cmd run -- sync yt
    if contains -- -f $argv; or contains -- --force $argv
        set -a cmd --force
    end
    
    dotnet $cmd
    popd
end

function synclf
    pushd $CSHARP_ROOT
    set -l cmd run -- sync lastfm
    if test -n "$argv[1]"
        set -a cmd --since $argv[1]
    end
    dotnet $cmd
    popd
end

function syncall
    set_color cyan; echo "[YouTube Sync]"; set_color normal
    syncyt
    
    set_color cyan; echo "[Last.fm Sync]"; set_color normal
    synclf
    
    log_success "All syncs complete!"
end

# ==============================================================================
# LOAD
# ==============================================================================

log_success "ScriptsToolkit (Fish) loaded."
```

---

## Symbol & Feature Glossary (Fish 3.0+)

| Feature            | Symbol / Syntax               | Detailed Explanation                                                                                                            |
| ------------------ | ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| **Shebang**        | `#!/usr/bin/env fish`         | Defines interpreter.                                                                                                            |
| **Set**            | `set -g name val`             | Sets a **g**lobal variable. `-l` for **l**ocal variables (function scoped). No `=` used.                                        |
| **Command Sub**    | `(cmd)`                       | Parentheses are used for command substitution. `$(cmd)` is illegal.                                                             |
| **Path**           | `(dirname (status filename))` | `status filename` gives path to current script. Nested execution calculates Dir.                                                |
| **Logic**          | `; or` / `; and`              | Fish combinators. Executes next command based on exit status of previous.                                                       |
| **Condition (If)** | `if test -f $f ... end`       | Logic blocks must end with `end`. `test` is the primary conditional command.                                                    |
| **Arg Parse**      | `argparse`                    | Native, powerful argument parser. Validates options according to spec string (e.g. `l/language=`). Sets `_flag_name` variables. |
| **List Append**    | `set -a var val`              | Appends `val` to list variable `var`.                                                                                           |
| **Range**          | `$argv[2..-1]`                | Slice notation. `-1` denotes the end of the list.                                                                               |
| **String Op**      | `string replace -r`           | Native string manipulation command. Replaces need for `sed`/`awk` in many cases.                                                |
| **Globbing**       | `*.{mp4,mkv}`                 | Brace expansion handles multiple extensions.                                                                                    |
| **No Quotes**      | `set name val`                | Quotes often unnecessary for simple assignments, unlike Bash.                                                                   |
| **Colors**         | `set_color cyan`              | State-based color management. `set_color normal` resets.                                                                        |
