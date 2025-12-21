#!/bin/bash
# ==============================================================================
# Bash Implementation of ScriptsToolkit Module
# ==============================================================================
#
# This is a native Bash implementation of the PowerShell ScriptsToolkit module,
# demonstrating how the same functionality can be achieved in Bash with detailed
# explanations of syntax differences.
#
# COMPARISON WITH POWERSHELL:
# - Bash: Text-based, procedural, requires external tools (jq, awk, grep)
# - PowerShell: Object-based, integrated cmdlets, .NET framework
# - Bash: Simpler syntax for basic operations, less verbose
# - PowerShell: More discoverable (Verb-Noun), better for complex data
#
# ==============================================================================

# ------------------------------------------------------------------------------
# MODULE VARIABLES (equivalent to PowerShell script-scoped variables)
# ------------------------------------------------------------------------------

# PowerShell: $Script:RepositoryRoot = Split-Path -Path $PSScriptRoot -Parent
# Bash: Get script directory and parent
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(dirname "$SCRIPT_DIR")"
PYTHON_TOOLKIT="$REPOSITORY_ROOT/python/toolkit/cli.py"
CSHARP_ROOT="$REPOSITORY_ROOT/csharp"
LOG_DIRECTORY="$REPOSITORY_ROOT/logs"

# ------------------------------------------------------------------------------
# FUNCTION: invoke_toolkit_python
# PowerShell equivalent: Invoke-ToolkitPython
# ------------------------------------------------------------------------------
#
# SYNTAX COMPARISON:
#   PowerShell: function Invoke-ToolkitPython {
#                   [CmdletBinding()]
#                   param([Parameter(Mandatory)] [string[]]$ArgumentList)
#                   # function body
#               }
#   
#   Bash:       invoke_toolkit_python() {
#                   local argument_list=("$@")
#                   # function body
#               }
#
# KEY DIFFERENCES:
# - PowerShell: Strong typing with [string[]], parameter validation
# - Bash: Dynamic typing, all parameters accessible via $@
# - PowerShell: CmdletBinding enables advanced features (verbose, debug)
# - Bash: No built-in advanced parameter handling
# - PowerShell: Explicit parameter names with Mandatory attribute
# - Bash: Positional parameters, manual validation needed
#
# CHARACTER COUNT:
#   PowerShell function declaration: 89 chars (with CmdletBinding and param)
#   Bash function declaration: 31 chars
#   Winner: Bash (58 chars shorter)
#
invoke_toolkit_python() {
    # Capture all arguments passed to function
    local argument_list=("$@")
    
    # Check if arguments provided (manual validation, unlike PowerShell's Mandatory)
    if [[ ${#argument_list[@]} -eq 0 ]]; then
        echo "Error: ArgumentList parameter is required" >&2
        return 1
    fi
    
    # Find python command (equivalent to Get-Command -Name python)
    # PowerShell: $python = (Get-Command -Name python).Source
    # Bash: Use 'command -v' or 'which'
    local python_path
    python_path=$(command -v python3 || command -v python)
    
    if [[ -z "$python_path" ]]; then
        echo "Error: Python not found in PATH" >&2
        return 1
    fi
    
    # Execute Python with toolkit script and arguments
    # PowerShell: & $python @arguments
    # Bash: "$python_path" "$PYTHON_TOOLKIT" "${argument_list[@]}"
    #
    # COMPARISON:
    # - PowerShell: @arguments is splatting (array expansion)
    # - Bash: "${argument_list[@]}" expands array elements
    # - Both achieve same result, Bash syntax is more explicit
    "$python_path" "$PYTHON_TOOLKIT" "${argument_list[@]}"
    local exit_code=$?
    
    # Check exit code and throw error if non-zero
    # PowerShell: if ($LASTEXITCODE -ne 0) { throw "..." }
    # Bash: if [[ $exit_code -ne 0 ]]; then return/exit
    #
    # COMPARISON:
    # - PowerShell: $LASTEXITCODE is automatic variable
    # - Bash: $? captures last exit code, must save immediately
    # - PowerShell: 'throw' creates terminating error
    # - Bash: 'return' exits function with code, 'exit' exits script
    if [[ $exit_code -ne 0 ]]; then
        echo "Error: Python toolkit exited with code $exit_code" >&2
        return "$exit_code"
    fi
}

# Alias for shorter command (equivalent to PowerShell [Alias('tkpy')])
# PowerShell: [Alias('tkpy')]
# Bash: alias tkpy='invoke_toolkit_python'
alias tkpy='invoke_toolkit_python'

# ------------------------------------------------------------------------------
# FUNCTION: get_toolkit_functions
# PowerShell equivalent: Get-ToolkitFunctions
# ------------------------------------------------------------------------------
#
# SYNTAX COMPARISON - Function Data Structures:
#   PowerShell: $functions = @(
#                   @{ Category = 'Utilities'; Name = 'Get-ToolkitFunctions'; ... }
#                   @{ Category = 'Logs'; Name = 'Show-SyncLog'; ... }
#               )
#
#   Bash:       # Arrays of colon-separated strings (no native hash tables in Bash 3)
#               function_data=(
#                   "Utilities:get_toolkit_functions:tkfn:List all toolkit functions"
#                   "Logs:show_sync_log:synclog:View JSONL sync logs as table"
#               )
#
# KEY DIFFERENCES:
# - PowerShell: Native hash table support with @{} syntax
# - Bash: Must use delimited strings or associative arrays (Bash 4+)
# - PowerShell: Clean syntax for nested structures
# - Bash: More manual string manipulation required
# - PowerShell: Type-safe with strong typing
# - Bash: Everything is strings, requires parsing
#
get_toolkit_functions() {
    # Function metadata as colon-separated strings
    # Format: Category:FunctionName:Alias:Description
    local function_data=(
        "Utilities:get_toolkit_functions:tkfn:List all toolkit functions"
        "Utilities:open_command_history:hist:Open shell history file"
        "Utilities:show_toolkit_help:tkhelp:Open toolkit documentation"
        "Utilities:invoke_toolkit_analyzer:tklint:Run linting tools"
        "Logs:show_sync_log:synclog:View JSONL sync logs as table"
        "Sync:invoke_youtube_sync:syncyt:Sync YouTube playlists"
        "Sync:invoke_lastfm_sync:synclf:Sync Last.fm scrobbles"
        "Sync:invoke_all_syncs:syncall:Run all daily syncs"
        "Filesystem:get_directories:dirs:List directories with sizes"
        "Filesystem:get_files_and_directories:tree:List all items with sizes"
        "Video:start_disc_remux:remux:Remux video discs to MKV"
        "Video:start_batch_compression:compress:Compress videos in batch"
        "Audio:convert_audio:audio:Convert audio files"
        "Audio:convert_to_mp3:tomp3:Convert to MP3"
        "Audio:convert_to_flac:toflac:Convert to FLAC"
    )
    
    # Print header
    # PowerShell: Write-Host "`nScriptsToolkit Functions" -ForegroundColor Cyan
    # Bash: Use ANSI color codes
    #
    # COMPARISON:
    # - PowerShell: -ForegroundColor parameter, built-in colors
    # - Bash: ANSI escape sequences (\033[36m for cyan)
    # - PowerShell: More readable parameter syntax
    # - Bash: More portable with ANSI standards
    echo -e "\n\033[36mScriptsToolkit Functions (Bash)\033[0m"
    echo -e "\033[36m================================\033[0m\n"
    
    # Group by category and display
    # PowerShell: $functions | Group-Object -Property Category | ForEach-Object { ... }
    # Bash: Sort and process with loops
    #
    # COMPARISON:
    # - PowerShell: Pipeline with Group-Object cmdlet, very concise
    # - Bash: Manual grouping with associative arrays or sorting
    # - PowerShell: 42 chars for grouping: "| Group-Object -Property Category"
    # - Bash: Requires more code for same functionality
    
    local current_category=""
    
    # Sort by category (first field)
    for entry in $(printf '%s\n' "${function_data[@]}" | sort); do
        # Split colon-separated values
        # PowerShell: Would use .Split(':') on string object
        # Bash: Use IFS (Internal Field Separator) and read
        IFS=':' read -r category name alias description <<< "$entry"
        
        # Print category header if changed
        if [[ "$category" != "$current_category" ]]; then
            current_category="$category"
            echo -e "\033[33m$category\033[0m"  # Yellow
        fi
        
        # Print function info with padding
        # PowerShell: "$($_.Alias.PadRight(10))"
        # Bash: printf with width specifier "%-10s"
        #
        # COMPARISON:
        # - PowerShell: .PadRight() method on string
        # - Bash: printf with format specifiers
        # - Both achieve same result
        # - Bash printf is more powerful for formatting
        printf "  \033[32m%-10s\033[0m" "$alias"           # Green alias
        printf "\033[37m%-30s\033[0m" "$name"              # White name
        printf "\033[90m%s\033[0m\n" "$description"        # Dark gray desc
    done
    
    echo ""
}

# Alias
alias tkfn='get_toolkit_functions'

# ------------------------------------------------------------------------------
# FUNCTION: show_sync_log
# PowerShell equivalent: Show-SyncLog
# ------------------------------------------------------------------------------
#
# SYNTAX COMPARISON - Parameter Handling:
#   PowerShell: [Parameter()]
#               [ValidateSet('youtube', 'lastfm', 'all')]
#               [string]$Service = 'all'
#
#   Bash:       local service="${1:-all}"
#               case "$service" in
#                   youtube|lastfm|all) ;;
#                   *) echo "Invalid service"; return 1 ;;
#               esac
#
# KEY DIFFERENCES:
# - PowerShell: ValidateSet provides automatic validation and tab completion
# - Bash: Manual validation with case statement required
# - PowerShell: Named parameters (-Service value)
# - Bash: Positional parameters, need getopts for named
# - PowerShell: Default values in parameter declaration
# - Bash: Parameter expansion with default ${var:-default}
#
show_sync_log() {
    # Parse parameters with default values
    local service="${1:-all}"
    local num_sessions="${2:-10}"
    
    # Validate service parameter
    case "$service" in
        youtube|lastfm|all)
            : # Valid, do nothing
            ;;
        *)
            echo "Error: Service must be 'youtube', 'lastfm', or 'all'" >&2
            return 1
            ;;
    esac
    
    # Determine which log files to read
    local log_files=()
    case "$service" in
        youtube)
            log_files=("$LOG_DIRECTORY/youtube.jsonl")
            ;;
        lastfm)
            log_files=("$LOG_DIRECTORY/lastfm.jsonl")
            ;;
        all)
            log_files=("$LOG_DIRECTORY/youtube.jsonl" "$LOG_DIRECTORY/lastfm.jsonl")
            ;;
    esac
    
    # Check if log files exist
    local found_files=0
    for log_file in "${log_files[@]}"; do
        if [[ -f "$log_file" ]]; then
            ((found_files++))
        fi
    done
    
    if [[ $found_files -eq 0 ]]; then
        echo "No log files found in $LOG_DIRECTORY" >&2
        return 1
    fi
    
    echo "Showing last $num_sessions sessions for $service"
    echo "Log files: ${log_files[*]}"
    
    # Read and parse JSONL files
    # PowerShell: Get-Content | ConvertFrom-Json
    # Bash: Use jq for JSON parsing
    #
    # COMPARISON:
    # - PowerShell: Built-in ConvertFrom-Json cmdlet
    # - Bash: Requires external tool (jq) for JSON
    # - PowerShell: Native object pipeline
    # - Bash: Text-based, requires more parsing
    #
    # CHARACTER COUNT for reading JSON:
    #   PowerShell: "Get-Content file | ConvertFrom-Json" (40 chars)
    #   Bash: "jq -r '.' file" (16 chars with jq)
    #   Winner: Bash (if jq is available)
    
    if command -v jq &> /dev/null; then
        # Use jq to parse JSON and extract fields
        for log_file in "${log_files[@]}"; do
            if [[ -f "$log_file" ]]; then
                echo -e "\n--- $(basename "$log_file") ---"
                
                # Get last N sessions
                # PowerShell: Would use Group-Object and Where-Object
                # Bash: Use jq with filters
                jq -r --arg num "$num_sessions" '
                    select(.event == "SessionStarted" or .event == "SessionEnded") |
                    "\(.timestamp // "N/A") | \(.event) | \(.session_id // "N/A") | \(.service // "N/A")"
                ' "$log_file" | tail -n "$((num_sessions * 2))"
            fi
        done
    else
        echo "Warning: jq not installed, showing raw log lines" >&2
        # Fallback: show raw lines
        for log_file in "${log_files[@]}"; do
            if [[ -f "$log_file" ]]; then
                echo -e "\n--- $(basename "$log_file") ---"
                tail -n "$num_sessions" "$log_file"
            fi
        done
    fi
}

# Alias
alias synclog='show_sync_log'

# ------------------------------------------------------------------------------
# FUNCTION: open_command_history
# PowerShell equivalent: Open-CommandHistory
# ------------------------------------------------------------------------------
#
# SYNTAX COMPARISON - External Command Execution:
#   PowerShell: & code "$env:APPDATA\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt"
#   Bash:       code ~/.bash_history
#
# KEY DIFFERENCES:
# - PowerShell: & operator for command execution
# - Bash: Direct command execution (no operator needed)
# - PowerShell: $env:APPDATA environment variable access
# - Bash: $HOME or ~ for home directory
# - PowerShell: Longer path to history file
# - Bash: Simple ~/.bash_history location
#
# CHARACTER COUNT:
#   PowerShell: 80+ chars (with env variable and path)
#   Bash: 22 chars (code ~/.bash_history)
#   Winner: Bash (58 chars shorter)
#
open_command_history() {
    # Determine history file based on shell
    local history_file="$HOME/.bash_history"
    
    # Check if VSCode is available
    if command -v code &> /dev/null; then
        code "$history_file"
    elif command -v vim &> /dev/null; then
        vim "$history_file"
    elif command -v nano &> /dev/null; then
        nano "$history_file"
    else
        echo "No editor found (tried: code, vim, nano)" >&2
        return 1
    fi
}

# Alias
alias hist='open_command_history'

# ------------------------------------------------------------------------------
# FUNCTION: get_directories
# PowerShell equivalent: Get-Directories
# ------------------------------------------------------------------------------
#
# SYNTAX COMPARISON - Directory Listing:
#   PowerShell: Get-ChildItem -Directory | 
#               Select-Object Name, @{N='Size'; E={...}} |
#               Format-Table
#
#   Bash:       du -sh */ | sort -h
#
# KEY DIFFERENCES:
# - PowerShell: Multiple cmdlets piped together (Get-ChildItem, Select-Object, Format-Table)
# - Bash: Single du command with options
# - PowerShell: Calculated properties with hash tables
# - Bash: Built-in tool does calculation
# - PowerShell: More verbose but more flexible
# - Bash: Concise but less customizable
#
# CHARACTER COUNT:
#   PowerShell: 100+ chars (with Select-Object and calculated property)
#   Bash: 20 chars (du -sh */ | sort -h)
#   Winner: Bash (80+ chars shorter)
#
get_directories() {
    local target_dir="${1:-.}"
    
    echo "Directories in $target_dir with sizes:"
    echo ""
    
    # Use du to get directory sizes
    # -s: summarize (don't show subdirectories)
    # -h: human-readable sizes
    # */ : only directories
    du -sh "$target_dir"/*/ 2>/dev/null | sort -h
}

# Alias
alias dirs='get_directories'

# ------------------------------------------------------------------------------
# CHARACTER COUNT SUMMARY
# ------------------------------------------------------------------------------
#
# COMPARISON: Bash vs PowerShell for common operations
#
# 1. FUNCTION DECLARATION:
#    Bash:        function_name() { }                    (21 chars)
#    PowerShell:  function Function-Name { param(...) }  (45+ chars)
#    Winner: Bash (24+ chars shorter)
#
# 2. PARAMETER WITH DEFAULT:
#    Bash:        local var="${1:-default}"              (27 chars)
#    PowerShell:  [string]$Var = 'default'               (27 chars)
#    Winner: Tie
#
# 3. CALLING EXTERNAL COMMAND:
#    Bash:        command arg1 arg2                      (17 chars)
#    PowerShell:  & command arg1 arg2                    (19 chars)
#    Winner: Bash (2 chars shorter)
#
# 4. DIRECTORY LISTING WITH SIZES:
#    Bash:        du -sh */ | sort -h                    (20 chars)
#    PowerShell:  Get-ChildItem -Directory | Select...   (50+ chars)
#    Winner: Bash (30+ chars shorter)
#
# 5. JSON PARSING (with jq):
#    Bash:        jq -r '.' file                         (15 chars)
#    PowerShell:  Get-Content file | ConvertFrom-Json   (40 chars)
#    Winner: Bash (25 chars shorter)
#
# 6. COLOR OUTPUT:
#    Bash:        echo -e "\033[32mtext\033[0m"          (31 chars)
#    PowerShell:  Write-Host "text" -ForegroundColor Green (45 chars)
#    Winner: Bash (14 chars shorter)
#
# OVERALL: Bash wins 5/6 comparisons for syntax brevity
#
# TRADE-OFFS:
# - Bash: Shorter syntax, requires external tools (jq, du, etc.)
# - PowerShell: Longer syntax, but more discoverable and consistent
# - Bash: Text-based pipeline, fast for simple tasks
# - PowerShell: Object pipeline, better for complex data manipulation
# - Bash: Manual validation and error handling
# - PowerShell: Built-in parameter validation and rich error handling
#
# ==============================================================================

# Export functions (make them available when script is sourced)
export -f invoke_toolkit_python
export -f get_toolkit_functions
export -f show_sync_log
export -f open_command_history
export -f get_directories

echo "ScriptsToolkit Bash Module Loaded"
echo "Type 'tkfn' to see available functions"
