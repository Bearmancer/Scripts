# Zsh ScriptsToolkit

> **Shell:** Zsh (Z Shell)
> **Version Target:** 5.0+
> **Philosophy:** Interactive power user shell with robust globbing and array handling.

---

## Complete Implementation (`ScriptsToolkit.zsh`)

Save as `C:\Users\Lance\Dev\powershell\ScriptsToolkit.zsh`.

```zsh
#!/usr/bin/env zsh
#
# ScriptsToolkit.zsh
# Full implementation of Scripts Toolkit
#

# ==============================================================================
# OPTIONS & CONFIGURATION
# ==============================================================================

# ERR_EXIT: Exit on error (equivalent to set -e)
# PIPE_FAIL: Return error if any part of pipe fails
# NO_UNSET: Error on unset vars (set -u)
# EXTENDED_GLOB: Enable #, ~, ^ operators in patterns
# NULL_GLOB: If glob matches nothing, remove pattern instead of literal string
setopt ERR_EXIT PIPE_FAIL NO_UNSET EXTENDED_GLOB NULL_GLOB

# ${0:A}: Absolute path of the script
# :h: Head modifier (dirname). Applied twice to get parent of parent.
# This resolves to the repo root assuming script is in powershell/ subfolder.
local SCRIPT_DIR="${0:A:h}"
local REPO_ROOT="${SCRIPT_DIR:h}"

readonly PYTHON_TOOLKIT="$REPO_ROOT/python/toolkit/cli.py"
readonly CSHARP_ROOT="$REPO_ROOT/csharp"
readonly LOG_DIR="$REPO_ROOT/logs"

# Zsh prompt expansion for colors (%F{color})
readonly C_GREEN='%F{green}'
readonly C_CYAN='%F{cyan}'
readonly C_YELLOW='%F{yellow}'
readonly C_RED='%F{red}'
readonly C_RESET='%f'

# ==============================================================================
# HELPER FUNCTIONS
# ==============================================================================

function log_info() {
    # print -P expands prompt sequences like %F{...}
    print -P "${C_CYAN}[INFO]${C_RESET} $*"
}

function log_success() {
    print -P "${C_GREEN}[DONE]${C_RESET} $*"
}

function log_error() {
    print -P "${C_RED}[FAIL]${C_RESET} $*" >&2
}

function invoke_python() {
    # ${commands[python3]}: Associative array mapping command names to paths.
    # fall back to 'python' if 'python3' not found in path
    local python_exe=${commands[python3]:-python}
    
    "$python_exe" "$PYTHON_TOOLKIT" "$@"
    
    # $? is strictly checked by ERR_EXIT
}

# ==============================================================================
# UTILITIES
# ==============================================================================

function tkfn() {
    print -P "\n${C_CYAN}ScriptsToolkit Functions (Zsh)${C_RESET}"
    print "=================================="

    # Function Registry: "Category:Name:Description"
    local -a funcs=(
        "Filesystem:dirs:List directories with sizes"
        "Filesystem:tree:List all items with sizes"
        "Filesystem:torrent:Create .torrent files"
        "Video:remux:Remux disc folders to MKV"
        "Video:compress:Batch compress videos"
        "Video:chapters:Extract video chapters"
        "Video:res:Report video resolutions"
        "Audio:audio:Convert audio files"
        "Audio:tomp3:Convert to MP3"
        "Audio:toflac:Convert to FLAC"
        "Audio:rename:Rename music files"
        "Audio:artsize:Report art sizes"
        "Transcription:whisp:Transcribe file/folder"
        "Transcription:wpf:Transcribe folder"
        "Transcription:wpj:Transcribe Japanese"
        "Sync:syncyt:Sync YouTube playlists"
        "Sync:synclf:Sync Last.fm scrobbles"
        "Sync:syncall:Run all syncs"
    )

    local current_cat=""
    for entry in $funcs; do
        # Zsh splitting: ${(s/:/)var} splits var on ':'
        local parts=(${(s/:/)entry})
        local cat=$parts[1]
        local cmd=$parts[2]
        local desc=$parts[3]

        if [[ "$cat" != "$current_cat" ]]; then
            print -P "\n${C_YELLOW}$cat${C_RESET}"
            current_cat=$cat
        fi
        printf "${C_GREEN}%-12s${C_RESET} %s\n" "$cmd" "$desc"
    done
    print
}

function hist() {
    local hfile=${HISTFILE:-$HOME/.zsh_history}
    ${EDITOR:-code} "$hfile"
}

# ==============================================================================
# FILESYSTEM
# ==============================================================================

function dirs() {
    invoke_python filesystem tree --directory "${1:-.}" --sort "${2:-size}"
}

function tree() {
    invoke_python filesystem tree --directory "${1:-.}" --include-files
}

function torrent() {
    local -a args=("filesystem" "torrents" "--directory" "${1:-.}")
    # Pattern matching in comparison
    if [[ "${2:-}" == (-r|--recursive) ]]; then
        args+=("--include-subdirectories")
    fi
    invoke_python "${args[@]}"
}

# ==============================================================================
# VIDEO Commands
# ==============================================================================

function remux() {
    invoke_python video remux --path "${1:-.}"
}

function compress() {
    invoke_python video compress --directory "${1:-.}"
}

function chapters() {
    invoke_python video chapters --path "${1:-.}"
}

function res() {
    invoke_python video resolutions --path "${1:-.}"
}

# ==============================================================================
# AUDIO Commands
# ==============================================================================

function audio() {
    invoke_python audio convert --directory "${1:-.}" \
        --mode convert --format "${2:-all}"
}

function tomp3() { audio "${1:-.}" mp3 }
function toflac() { audio "${1:-.}" flac }
function rename() { invoke_python audio rename --directory "${1:-.}" }
function artsize() { invoke_python audio art-report --directory "${1:-.}" }

# ==============================================================================
# TRANSCRIPTION
# ==============================================================================

function whisp() {
    local path=$1
    shift
    
    # zparseopts: Zsh util for parsing options
    # -D: Remove used opts from $@
    # -E: Stop at first non-opt
    # -A: Store in associative array 'opts'
    local -A opts
    zparseopts -D -E -A opts \
        l:=language \
        m:=model \
        t=translate \
        f=force \
        o:=output
    
    local lang=${opts[-l]:-}
    local mod=${opts[-m]:-large-v3}
    local out=${opts[-o]:-.}
    local do_trans=${opts[-t]:-}
    local do_force=${opts[-f]:-}

    # Modifiers: :t = filename, :r = no_extension
    local fname=${path:t}
    local froot=${fname:r}
    local srt="$out/$froot.srt"

    if [[ -f "$srt" && -z "$do_force" ]]; then
        print -P "${C_YELLOW}Skipped: $fname (SRT exists)${C_RESET}"
        return 0
    fi

    log_info "Transcribing: $fname"
    
    local -a cargs=(--model "$mod" --output_format srt --output_dir "$out")
    [[ -n "$lang" ]] && cargs+=(--language "$lang")
    [[ -n "$do_trans" ]] && cargs+=(--task translate)
    
    whisper-ctranslate2 "${cargs[@]}" "$path"
    log_success "Completed: $fname"
}

function wpf() {
    local -a files=("${1:-.}"/**/*.(mp4|mkv|mp3|flac)(.N))
    
    if [[ ${#files} -eq 0 ]]; then
        log_error "No media files found"
        return 0
    fi
    
    # Pass rest of args to whisp
    for f in $files; do
        whisp "$f" "${@:2}"
    done
}

function wpj() { whisp "$1" -l ja "${@:2}" }

# ==============================================================================
# SYNC
# ==============================================================================

function syncyt() {
    pushd "$CSHARP_ROOT" >/dev/null
    local -a cmd=(run -- sync yt)
    [[ "${1:-}" == (-f|--force) ]] && cmd+=(--force)
    dotnet "${cmd[@]}"
    popd >/dev/null
}

function synclf() {
    pushd "$CSHARP_ROOT" >/dev/null
    local -a cmd=(run -- sync lastfm)
    [[ -n "${1:-}" ]] && cmd+=(--since "$1")
    dotnet "${cmd[@]}"
    popd >/dev/null
}

function syncall() {
    print -P "\n${C_CYAN}[YouTube Sync]${C_RESET}"
    syncyt
    print -P "\n${C_CYAN}[Last.fm Sync]${C_RESET}"
    synclf
    log_success "All syncs complete!"
}
```

---

## Symbol & Feature Glossary (Zsh 5.0+)

| Feature               | Symbol / Syntax      | Detailed Explanation                                                                    |
| --------------------- | -------------------- | --------------------------------------------------------------------------------------- |
| **Modifiers**         | `${var:t}`           | **Tail**. Gets filename from path. `:h` (head/dirname), `:r` (root/no ext), `:e` (ext). |
| **Flags**             | `${(s/:/)var}`       | **Split Flag**. Splits `var` on string `:`. Returns array.                              |
| **Recursive Glob**    | `**/*.txt`           | Matches files recursively.                                                              |
| **Glob Qualifiers**   | `*(.N)`              | `.` selects plain files. `N` (NullGlob) prevents error if no matches.                   |
| **Print -P**          | `print -P "%F{red}"` | Prints with Prompt Expansion. Allows `%F` codes instead of ANSI escapes.                |
| **Opts Parsing**      | `zparseopts`         | Built-in CLI argument parser. Maps flags to variables or associative arrays.            |
| **Associative Array** | `local -A map`       | Key-value dictionary.                                                                   |
| **Commands Map**      | `${commands[cmd]}`   | System map of command names to full paths.                                              |
| **Pattern Match**     | `(-r                 | --recursive)`                                                                           | Used in `[[ ]]`. Matches if string is `-r` OR `--recursive`. |
