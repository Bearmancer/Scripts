# Bash ScriptsToolkit

> **Shell:** GNU Bash (Bourne Again SHell)
> **Version Target:** 4.0+ (for associative arrays)
> **Philosophy:** Portable, text-stream processing, strict POSIX compliance when needed.

---

## Complete Implementation (`ScriptsToolkit.bash`)

Save as `C:\Users\Lance\Dev\powershell\ScriptsToolkit.bash`.

```bash
#!/usr/bin/env bash
#
# scripts_toolkit.bash
# Full implementation of Scripts Toolkit
#

# ==============================================================================
# STRICT MODE & CONFIGURATION
# ==============================================================================

# -e: Exit immediately if a command exits with a non-zero status.
# -u: Treat unset variables as an error when substituting.
# -o pipefail: The return value of a pipeline is the status of the last command 
#              to exit with a non-zero status, or zero if no command failed.
set -euo pipefail

# ${BASH_SOURCE[0]}: The path to the script itself.
# dirname: Extracts directory path.
# $(...): Command substitution - runs command and replaces with output.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

# Read-only variables for constants
readonly PYTHON_TOOLKIT="$REPO_ROOT/python/toolkit/cli.py"
readonly CSHARP_ROOT="$REPO_ROOT/csharp"
readonly LOG_DIR="$REPO_ROOT/logs"

# ANSI Color Codes
# \033 is the ESC character. [0;32m sets text color to green.
readonly GREEN='\033[0;32m'
readonly CYAN='\033[0;36m'
readonly YELLOW='\033[0;33m'
readonly RED='\033[0;31m'
readonly NC='\033[0m' # No Color

# ==============================================================================
# HELPER FUNCTIONS
# ==============================================================================

# Internal logging function
# $1: The message to print
function _log_info() {
    # echo -e enables interpretation of backslash escapes (colors)
    echo -e "${CYAN}[INFO]${NC} $1"
}

function _log_success() {
    echo -e "${GREEN}[DONE]${NC} $1"
}

function _log_error() {
    # >&2 redirects output to Standard Error (stderr)
    echo -e "${RED}[FAIL]${NC} $1" >&2
}

# Wrapper for Python execution
# "$@": Expands to all arguments passed to the function, individually quoted.
function invoke_python() {
    # Check for python3 or python
    local python_cmd
    if command -v python3 &> /dev/null; then
        python_cmd="python3"
    else
        python_cmd="python"
    fi

    "$python_cmd" "$PYTHON_TOOLKIT" "$@"
    
    # $?: The exit status of the last executed command.
    local status=$?
    if [[ $status -ne 0 ]]; then
        _log_error "Python toolkit exited with code $status"
        return $status
    fi
}

# ==============================================================================
# UTILITIES
# ==============================================================================

function tkfn() {
    echo -e "\n${CYAN}ScriptsToolkit Functions (Bash)${NC}"
    echo "=================================="
    
    # Associative array for categories isn't strictly ordered in Bash < 4,
    # so we just print manually for consistent layout.
    
    echo -e "\n${YELLOW}Filesystem${NC}"
    # printf allows formatted columns. %-12s means string padded to 12 chars left-aligned.
    printf "  ${GREEN}%-12s${NC} %s\n" "dirs" "List directories with sizes"
    printf "  ${GREEN}%-12s${NC} %s\n" "tree" "List all items with sizes"
    printf "  ${GREEN}%-12s${NC} %s\n" "torrent" "Create .torrent files"

    echo -e "\n${YELLOW}Video${NC}"
    printf "  ${GREEN}%-12s${NC} %s\n" "remux" "Remux disc folders to MKV"
    printf "  ${GREEN}%-12s${NC} %s\n" "compress" "Batch compress videos"
    printf "  ${GREEN}%-12s${NC} %s\n" "chapters" "Extract video chapters"
    printf "  ${GREEN}%-12s${NC} %s\n" "res" "Report video resolution"

    echo -e "\n${YELLOW}Audio${NC}"
    printf "  ${GREEN}%-12s${NC} %s\n" "audio" "Convert audio files"
    printf "  ${GREEN}%-12s${NC} %s\n" "tomp3" "Convert to MP3"
    printf "  ${GREEN}%-12s${NC} %s\n" "toflac" "Convert to FLAC"
    printf "  ${GREEN}%-12s${NC} %s\n" "rename" "Rename music files"
    printf "  ${GREEN}%-12s${NC} %s\n" "artsize" "Report album art sizes"

    echo -e "\n${YELLOW}Transcription${NC}"
    printf "  ${GREEN}%-12s${NC} %s\n" "whisp" "Transcribe file/folder"
    printf "  ${GREEN}%-12s${NC} %s\n" "wpf" "Transcribe folder"
    printf "  ${GREEN}%-12s${NC} %s\n" "wpj" "Transcribe Japanese"

    echo -e "\n${YELLOW}Sync${NC}"
    printf "  ${GREEN}%-12s${NC} %s\n" "syncyt" "Sync YouTube playlists"
    printf "  ${GREEN}%-12s${NC} %s\n" "synclf" "Sync Last.fm scrobbles"
    printf "  ${GREEN}%-12s${NC} %s\n" "syncall" "Run all syncs"
    echo ""
}

function hist() {
    # ${VAR:-default}: If VAR is unset or null, use default.
    local histfile="${HISTFILE:-$HOME/.bash_history}"
    # Open in VS Code (or fallback to EDITOR or vi)
    "${EDITOR:-code}" "$histfile"
}

# ==============================================================================
# FILESYSTEM COMMANDS
# ==============================================================================

function dirs() {
    # Default directory to "." if $1 is empty
    local directory="${1:-.}"
    local sort="${2:-size}"
    invoke_python "filesystem" "tree" "--directory" "$directory" "--sort" "$sort"
}

function tree() {
    local directory="${1:-.}"
    invoke_python "filesystem" "tree" "--directory" "$directory" "--include-files"
}

function torrent() {
    local directory="${1:-.}"
    # Check if second arg is recursive flag
    local recursive=false
    if [[ "${2:-}" == "-r" ]] || [[ "${2:-}" == "--recursive" ]]; then
        recursive=true
    fi

    # Build array of arguments
    local args=("filesystem" "torrents" "--directory" "$directory")
    if $recursive; then
        # Append to array
        args+=("--include-subdirectories")
    fi

    invoke_python "${args[@]}"
}

# ==============================================================================
# VIDEO COMMANDS
# ==============================================================================

function remux() {
    local directory="${1:-.}"
    invoke_python "video" "remux" "--path" "$directory"
}

function compress() {
    local directory="${1:-.}"
    invoke_python "video" "compress" "--directory" "$directory"
}

function chapters() {
    local directory="${1:-.}"
    invoke_python "video" "chapters" "--path" "$directory"
}

function res() {
    local directory="${1:-.}"
    invoke_python "video" "resolutions" "--path" "$directory"
}

# ==============================================================================
# AUDIO COMMANDS
# ==============================================================================

function audio() {
    local directory="${1:-.}"
    local format="${2:-all}"
    invoke_python "audio" "convert" "--directory" "$directory" "--mode" "convert" "--format" "$format"
}

function tomp3() {
    # Call internal function
    audio "${1:-.}" "mp3"
}

function toflac() {
    audio "${1:-.}" "flac"
}

function sacd() {
    local directory="${1:-.}"
    invoke_python "audio" "convert" "--directory" "$directory" "--mode" "extract"
}

function rename() {
    local directory="${1:-.}"
    invoke_python "audio" "rename" "--directory" "$directory"
}

function artsize() {
    local directory="${1:-.}"
    invoke_python "audio" "art-report" "--directory" "$directory"
}

# ==============================================================================
# TRANSCRIPTION COMMANDS
# ==============================================================================

function whisp() {
    local filepath="$1"
    # shift removes $1 from argument list, shifting $2 to $1, etc.
    shift 

    # Argument parsing loop
    local language=""
    local model="large-v3"
    local translate=false
    local force=false
    local output_dir="."

    while [[ $# -gt 0 ]]; do
        case "$1" in
            -l|--language)
                language="$2"
                shift 2
                ;;
            -m|--model)
                model="$2"
                shift 2
                ;;
            -t|--translate)
                translate=true
                shift
                ;;
            -f|--force)
                force=true
                shift
                ;;
            -o|--output)
                output_dir="$2"
                shift 2
                ;;
            *)
                echo "Unknown option: $1"
                return 1
                ;;
        esac
    done

    # Base filename processing
    # basename: gets filename from path
    local filename=$(basename "$filepath")
    # ${filename%.*}: param substitution, removes suffix starting with last dot
    local name="${filename%.*}"
    local srt_path="$output_dir/$name.srt"

    # [[ -f path ]] checks if file exists
    if [[ -f "$srt_path" ]] && [[ "$force" == "false" ]]; then
        echo -e "${YELLOW}Skipped: $filename (SRT exists)${NC}"
        # return 0 means success in shell
        return 0
    fi

    _log_info "Transcribing: $filename"
    
    # Construct args array
    local cmd_args=("--model" "$model" "--output_format" "srt" "--output_dir" "$output_dir")
    
    # -n tests if string is Non-empty
    if [[ -n "$language" ]]; then
        cmd_args+=("--language" "$language")
    fi

    if [[ "$translate" == "true" ]]; then
        cmd_args+=("--task" "translate")
    fi

    # Execute directly
    whisper-ctranslate2 "${cmd_args[@]}" "$filepath"
    _log_success "Completed: $filename"
}

function wpf() {
    local directory="${1:-.}"
    shift
    
    # Use find to get files. 
    # -maxdepth 1 so we don't recurse unless intended (mimicking PowerShell script implied logic)
    # -print0 handles filenames with spaces correctly when paired with read -d ''
    while IFS= read -r -d '' file; do
        whisp "$file" "$@"
    done < <(find "$directory" -maxdepth 1 -type f \( -name "*.mp4" -o -name "*.mkv" -o -name "*.mp3" -o -name "*.flac" \) -print0)
}

function wpj() {
    # Prepend language arg
    # "${@:2}" expands to all arguments starting from the 2nd one
    whisp "$1" --language "ja" "${@:2}"
}

# ==============================================================================
# SYNC COMMANDS
# ==============================================================================

function syncyt() {
    # pushd adds current dir to stack and changes dir. 
    # > /dev/null suppresses the stack print output
    pushd "$CSHARP_ROOT" > /dev/null || return
    
    local args=("run" "--" "sync" "yt")
    if [[ "${1:-}" == "-f" ]] || [[ "${1:-}" == "--force" ]]; then
        args+=("--force")
    fi

    dotnet "${args[@]}"
    
    # popd returns to original dir
    popd > /dev/null
}

function synclf() {
    pushd "$CSHARP_ROOT" > /dev/null || return
    
    local args=("run" "--" "sync" "lastfm")
    if [[ -n "${1:-}" ]]; then
        args+=("--since" "$1")
    fi
    
    dotnet "${args[@]}"
    popd > /dev/null
}

function syncall() {
    echo -e "\n${CYAN}[YouTube Sync]${NC}"
    syncyt
    
    echo -e "\n${CYAN}[Last.fm Sync]${NC}"
    synclf
    
    _log_success "All syncs complete!"
}

# ==============================================================================
# INITIALIZATION
# ==============================================================================

_log_success "ScriptsToolkit loaded. Type 'tkfn' for commands."
```

---

## Symbol & Feature Glossary (Bash 4.0+)

| Feature                  | Symbol / Syntax                      | Detailed Explanation                                                                                                      |
| ------------------------ | ------------------------------------ | ------------------------------------------------------------------------------------------------------------------------- |
| **Shebang**              | `#!/usr/bin/env bash`                | Tells the OS to execute this file using `bash` found in the `$PATH`.                                                      |
| **Strict Mode**          | `set -euo pipefail`                  | Critical hygiene. Stops script on error (`-e`), undefined vars (`-u`), or pipe failure (`-o pipefail`).                   |
| **Command Substitution** | `$(cmd)`                             | Executes `cmd` and replaces the expression with the stdout result. Preferable to backticks `` `cmd` ``.                   |
| **Arithmetic**           | `$(( 1 + 1 ))`                       | Performs integer math.                                                                                                    |
| **Variable Expansion**   | `${VAR}`                             | Explicit variable access. allows `${VAR}_suffix`.                                                                         |
| **Default Value**        | `${VAR:-default}`                    | If `VAR` is unset/null, returns `default`.                                                                                |
| **String Trim Suffix**   | `${VAR%.*}`                          | Removes the shortest match of `.*` from the end of `VAR` (e.g. extension removal).                                        |
| **String Trim Prefix**   | `${VAR##*/}`                         | Removes the longest match of `*/` from the start of `VAR` (e.g. dirname removal -> basename).                             |
| **Array Declaration**    | `arr=("val1" "val2")`                | Creates an indexed array.                                                                                                 |
| **Array Expansion**      | `"${arr[@]}"`                        | Expands to all elements, treating each as a separate quoted word. Handles spaces in elements correctly.                   |
| **Array Append**         | `arr+=("val3")`                      | Adds an element to the end of the array.                                                                                  |
| **Conditional (Test)**   | `[[ -f file ]]`                      | "New" test command. Checks if `file` exists and is a regular file. Safer than `[ ... ]`.                                  |
| **Conditional (String)** | `[[ -n str ]]`                       | Checks if string is **n**on-zero length.                                                                                  |
| **Conditional (Math)**   | `[[ $a -ne 0 ]]`                     | Checks if integer `$a` is **n**ot **e**qual to 0.                                                                         |
| **Case Statement**       | `case "$1" in pattern) cmds ;; esac` | Switch-like structure for string matching. `;;` breaks the case.                                                          |
| **Function def**         | `function name() { ... }`            | Defines command logic. Arguments accessed via `$1`..`$9`.                                                                 |
| **Shift**                | `shift`                              | Discards `$1`, moves `$2` to `$1`, `$3` to `$2`, etc. Used for iterating arguments.                                       |
| **Process Subst**        | `<(cmd)`                             | Runs `cmd` and presents its output as a temporary file descriptor (e.g. `/dev/fd/63`). Used in `done < <(find ...)` loop. |
| **Null Device**          | `/dev/null`                          | Bit bucket. Redirect output here `> /dev/null` to silence it.                                                             |
| **Here Doc**             | `<<EOF`                              | Not used in this specific script, but common for multi-line strings.                                                      |

### Indentation Rules
- **2 or 4 spaces** are standard. This script uses 4.
- `then` and `do` usually go on the same line as `if` and `for` (e.g. `if [[ ]]; then`).
