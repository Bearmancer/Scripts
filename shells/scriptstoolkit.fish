#!/usr/bin/env fish
# ==============================================================================
# Fish Implementation of ScriptsToolkit Module
# ==============================================================================
#
# This is a native Fish implementation of the PowerShell ScriptsToolkit module,
# demonstrating how the same functionality can be achieved in Fish with detailed
# explanations of syntax differences.
#
# COMPARISON WITH POWERSHELL:
# - Fish: User-friendly, autosuggestions, text-based pipeline
# - PowerShell: Object-based, Verb-Noun cmdlets, .NET integration
# - Fish: Clean syntax, no POSIX baggage, modern design
# - PowerShell: More powerful for enterprise automation
#
# ==============================================================================

# ------------------------------------------------------------------------------
# MODULE VARIABLES (equivalent to PowerShell script-scoped variables)
# ------------------------------------------------------------------------------

# PowerShell: $Script:RepositoryRoot = Split-Path -Path $PSScriptRoot -Parent
# Fish: Use functions and command substitution
set -g REPOSITORY_ROOT (dirname (status -f | dirname))
set -g PYTHON_TOOLKIT "$REPOSITORY_ROOT/python/toolkit/cli.py"
set -g CSHARP_ROOT "$REPOSITORY_ROOT/csharp"
set -g LOG_DIRECTORY "$REPOSITORY_ROOT/logs"

# ------------------------------------------------------------------------------
# FUNCTION: invoke_toolkit_python
# PowerShell equivalent: Invoke-ToolkitPython
# ------------------------------------------------------------------------------
#
# SYNTAX COMPARISON:
#   PowerShell: function Invoke-ToolkitPython {
#                   [CmdletBinding()]
#                   param([Parameter(Mandatory)] [string[]]$ArgumentList)
#                   & $python @arguments
#               }
#   
#   Fish:       function invoke_toolkit_python
#                   set argument_list $argv
#                   $python_path $PYTHON_TOOLKIT $argument_list
#               end
#
# KEY DIFFERENCES:
# - PowerShell: Requires [CmdletBinding()] and param() block
# - Fish: Simple 'function' and 'end' keywords, no param declaration
# - PowerShell: Arguments in $ArgumentList parameter
# - Fish: Arguments automatically in $argv
# - PowerShell: Need & operator to execute command
# - Fish: Commands execute directly, no operator needed
#
# CHARACTER COUNT:
#   PowerShell function with param: 89 chars
#   Fish function: 32 chars
#   Winner: Fish (57 chars shorter)
#
function invoke_toolkit_python
    # Fish functions description (equivalent to PowerShell comment-based help)
    # PowerShell: .SYNOPSIS, .DESCRIPTION, .PARAMETER
    # Fish: -d flag inline description
    
    # Check if arguments provided
    # PowerShell: [Parameter(Mandatory)] handles this automatically
    # Fish: Manual check needed
    if test (count $argv) -eq 0
        echo "Error: ArgumentList parameter is required" >&2
        return 1
    end
    
    # Find python command
    # PowerShell: $python = (Get-Command -Name python).Source
    # Fish: Use 'command -s' which returns path
    #
    # COMPARISON:
    # - PowerShell: Get-Command is verbose but discoverable
    # - Fish: 'command -s' is concise Unix-style
    set python_path (command -s python3; or command -s python)
    
    if test -z "$python_path"
        echo "Error: Python not found in PATH" >&2
        return 1
    end
    
    # Execute Python with toolkit script and arguments
    # PowerShell: & $python @arguments (splatting)
    # Fish: $python_path $PYTHON_TOOLKIT $argv (automatic array expansion)
    #
    # COMPARISON:
    # - PowerShell: Requires @ for splatting arrays
    # - Fish: Arrays expand naturally, no special syntax
    # - Fish: 40 chars vs PowerShell: 22 chars
    # - But PowerShell includes param handling above
    $python_path $PYTHON_TOOLKIT $argv
    set exit_code $status
    
    # Check exit code
    # PowerShell: $LASTEXITCODE automatic variable
    # Fish: $status automatic variable
    #
    # COMPARISON:
    # - Both have automatic exit code variables
    # - Fish: $status is shorter than $LASTEXITCODE
    # - PowerShell: Uses 'throw' for errors
    # - Fish: Uses 'return' with exit code
    if test $exit_code -ne 0
        echo "Error: Python toolkit exited with code $exit_code" >&2
        return $exit_code
    end
end

# Abbreviation (like PowerShell alias but expands on space)
# PowerShell: [Alias('tkpy')]
# Fish: abbr -a tkpy invoke_toolkit_python
abbr -a tkpy invoke_toolkit_python

# ------------------------------------------------------------------------------
# FUNCTION: get_toolkit_functions
# PowerShell equivalent: Get-ToolkitFunctions
# ------------------------------------------------------------------------------
#
# SYNTAX COMPARISON - Data Structures:
#   PowerShell: $functions = @(
#                   @{ Category = 'Utilities'; Name = 'Get-ToolkitFunctions' }
#               )
#
#   Fish:       set function_data \
#                   "Utilities:get_toolkit_functions:tkfn:List functions" \
#                   "Logs:show_sync_log:synclog:View sync logs"
#
# KEY DIFFERENCES:
# - PowerShell: Native hash table support with @{}
# - Fish: Use colon-separated strings (no native hash tables)
# - PowerShell: @() creates array
# - Fish: 'set' with multiple values creates list
# - PowerShell: Cleaner nested structure syntax
# - Fish: Simpler but requires string parsing
#
function get_toolkit_functions -d "List all toolkit functions"
    # Function metadata as colon-separated strings
    set function_data \
        "Utilities:get_toolkit_functions:tkfn:List all toolkit functions" \
        "Utilities:open_command_history:hist:Open shell history file" \
        "Utilities:show_toolkit_help:tkhelp:Open toolkit documentation" \
        "Utilities:invoke_toolkit_analyzer:tklint:Run linting tools" \
        "Logs:show_sync_log:synclog:View JSONL sync logs as table" \
        "Sync:invoke_youtube_sync:syncyt:Sync YouTube playlists" \
        "Sync:invoke_lastfm_sync:synclf:Sync Last.fm scrobbles" \
        "Sync:invoke_all_syncs:syncall:Run all daily syncs" \
        "Filesystem:get_directories:dirs:List directories with sizes" \
        "Filesystem:get_files_and_directories:tree:List all items" \
        "Video:start_disc_remux:remux:Remux video discs to MKV" \
        "Video:start_batch_compression:compress:Compress videos" \
        "Audio:convert_audio:audio:Convert audio files" \
        "Audio:convert_to_mp3:tomp3:Convert to MP3" \
        "Audio:convert_to_flac:toflac:Convert to FLAC"
    
    # Print header with colors
    # PowerShell: Write-Host "`nScriptsToolkit Functions" -ForegroundColor Cyan
    # Fish: set_color cyan; echo "..."; set_color normal
    #
    # COMPARISON:
    # - PowerShell: -ForegroundColor parameter (24 chars)
    # - Fish: set_color command (15 chars for cyan + normal)
    # - Fish: More explicit color control
    # - PowerShell: Integrated into Write-Host
    set_color cyan
    echo -e "\nScriptsToolkit Functions (Fish)"
    echo "================================"
    set_color normal
    echo ""
    
    # Group by category and display
    # PowerShell: $functions | Group-Object -Property Category
    # Fish: Manual grouping with loop and tracking
    #
    # COMPARISON:
    # - PowerShell: Group-Object cmdlet (25 chars)
    # - Fish: Manual loop required (more code)
    # - PowerShell: More concise for grouping
    # - Fish: More control over output format
    
    set current_category ""
    
    # Sort by category (first field) using Fish's sort
    for entry in (printf '%s\n' $function_data | sort)
        # Split colon-separated values
        # PowerShell: $entry.Split(':')
        # Fish: string split ':' $entry
        #
        # COMPARISON:
        # - PowerShell: .Split() method on string object
        # - Fish: 'string split' command
        # - Fish: More explicit, follows command pattern
        set parts (string split ':' $entry)
        set category $parts[1]
        set name $parts[2]
        set alias $parts[3]
        set description $parts[4]
        
        # Print category header if changed
        if test "$category" != "$current_category"
            set current_category $category
            set_color yellow
            echo "$category"
            set_color normal
        end
        
        # Print function info with padding
        # PowerShell: "$($_.Alias.PadRight(10))"
        # Fish: printf "%-10s" $alias
        #
        # COMPARISON:
        # - PowerShell: .PadRight() method (16 chars)
        # - Fish: printf with format (14 chars)
        # - Both achieve same result
        # - Fish printf is standard Unix tool
        set_color green
        printf "  %-10s" $alias
        set_color white
        printf "%-30s" $name
        set_color brblack
        printf "%s\n" $description
        set_color normal
    end
    
    echo ""
end

# Abbreviation
abbr -a tkfn get_toolkit_functions

# ------------------------------------------------------------------------------
# FUNCTION: show_sync_log
# PowerShell equivalent: Show-SyncLog
# ------------------------------------------------------------------------------
#
# SYNTAX COMPARISON - Parameter Validation:
#   PowerShell: [ValidateSet('youtube', 'lastfm', 'all')]
#               [string]$Service = 'all'
#
#   Fish:       set service (test -n "$argv[1]"; and echo $argv[1]; or echo "all")
#               switch $service
#                   case youtube lastfm all
#                   case '*'
#                       echo "Invalid service"; return 1
#               end
#
# KEY DIFFERENCES:
# - PowerShell: ValidateSet provides automatic validation
# - Fish: Manual switch statement for validation
# - PowerShell: Named parameters with defaults
# - Fish: Positional with ternary-like default
# - PowerShell: Tab completion for ValidateSet
# - Fish: No built-in parameter validation
#
function show_sync_log -d "View JSONL sync logs"
    # Parse parameters with defaults
    # PowerShell: param([string]$Service = 'all')
    # Fish: Use test and or/and for default values
    #
    # COMPARISON:
    # - PowerShell: Default in param declaration (15 chars)
    # - Fish: Inline with test (35 chars for default logic)
    # - PowerShell: Clearer intent
    # - Fish: More verbose but flexible
    set service (test -n "$argv[1]"; and echo $argv[1]; or echo "all")
    set num_sessions (test -n "$argv[2]"; and echo $argv[2]; or echo "10")
    
    # Validate service parameter
    switch $service
        case youtube lastfm all
            # Valid, continue
        case '*'
            echo "Error: Service must be 'youtube', 'lastfm', or 'all'" >&2
            return 1
    end
    
    # Determine which log files to read
    set log_files
    switch $service
        case youtube
            set log_files "$LOG_DIRECTORY/youtube.jsonl"
        case lastfm
            set log_files "$LOG_DIRECTORY/lastfm.jsonl"
        case all
            set log_files "$LOG_DIRECTORY/youtube.jsonl" "$LOG_DIRECTORY/lastfm.jsonl"
    end
    
    # Check if log files exist
    set found_files 0
    for log_file in $log_files
        if test -f $log_file
            set found_files (math "$found_files + 1")
        end
    end
    
    if test $found_files -eq 0
        echo "No log files found in $LOG_DIRECTORY" >&2
        return 1
    end
    
    echo "Showing last $num_sessions sessions for $service"
    echo "Log files: $log_files"
    
    # Read and parse JSONL files
    # PowerShell: Get-Content | ConvertFrom-Json
    # Fish: Use jq for JSON parsing
    #
    # COMPARISON:
    # - PowerShell: Built-in JSON support (40 chars)
    # - Fish: Requires jq but very concise (15 chars)
    # - PowerShell: Native object manipulation
    # - Fish: Text-based with external tool
    
    if command -v jq > /dev/null
        for log_file in $log_files
            if test -f $log_file
                echo -e "\n--- "(basename $log_file)" ---"
                
                # Use jq to parse and format JSON
                # Fish string handling is cleaner than Bash
                jq -r --arg num $num_sessions '
                    select(.event == "SessionStarted" or .event == "SessionEnded") |
                    "\(.timestamp // "N/A") | \(.event) | \(.session_id // "N/A") | \(.service // "N/A")"
                ' $log_file | tail -n (math "$num_sessions * 2")
            end
        end
    else
        echo "Warning: jq not installed, showing raw log lines" >&2
        for log_file in $log_files
            if test -f $log_file
                echo -e "\n--- "(basename $log_file)" ---"
                tail -n $num_sessions $log_file
            end
        end
    end
end

# Abbreviation
abbr -a synclog show_sync_log

# ------------------------------------------------------------------------------
# FUNCTION: open_command_history
# PowerShell equivalent: Open-CommandHistory
# ------------------------------------------------------------------------------
#
# SYNTAX COMPARISON - File Path Access:
#   PowerShell: "$env:APPDATA\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt"
#   Fish:       "$HOME/.local/share/fish/fish_history"
#
# KEY DIFFERENCES:
# - PowerShell: Complex Windows-specific path with env variable
# - Fish: Simple Unix-style path
# - PowerShell: 80+ chars for path
# - Fish: 40 chars for path
# - Winner: Fish (40 chars shorter)
#
function open_command_history -d "Open shell history file"
    # Fish history location
    set history_file "$HOME/.local/share/fish/fish_history"
    
    # Check for available editors
    # PowerShell: Uses & operator for command execution
    # Fish: Direct command execution
    #
    # COMPARISON:
    # - PowerShell: & code (7 chars)
    # - Fish: code (4 chars)
    # - Fish: No operator needed (3 chars shorter)
    if command -v code > /dev/null
        code $history_file
    else if command -v vim > /dev/null
        vim $history_file
    else if command -v nano > /dev/null
        nano $history_file
    else
        echo "No editor found (tried: code, vim, nano)" >&2
        return 1
    end
end

# Abbreviation
abbr -a hist open_command_history

# ------------------------------------------------------------------------------
# FUNCTION: get_directories
# PowerShell equivalent: Get-Directories
# ------------------------------------------------------------------------------
#
# SYNTAX COMPARISON - Directory Operations:
#   PowerShell: Get-ChildItem -Directory | 
#               Select-Object Name, @{N='Size'; E={...}} |
#               Format-Table
#
#   Fish:       du -sh */ | sort -h
#
# KEY DIFFERENCES:
# - PowerShell: 3 cmdlets piped (Get-ChildItem, Select-Object, Format-Table)
# - Fish: 2 commands piped (du, sort)
# - PowerShell: Calculated properties with hash table syntax
# - Fish: du command handles size calculation natively
# - PowerShell: 100+ chars for full pipeline
# - Fish: 20 chars for same result
# - Winner: Fish (80+ chars shorter)
#
function get_directories -d "List directories with sizes"
    set target_dir (test -n "$argv[1]"; and echo $argv[1]; or echo ".")
    
    echo "Directories in $target_dir with sizes:"
    echo ""
    
    # Use du to get directory sizes
    # -s: summarize
    # -h: human-readable
    du -sh $target_dir/*/ 2>/dev/null | sort -h
end

# Abbreviation
abbr -a dirs get_directories

# ------------------------------------------------------------------------------
# CHARACTER COUNT SUMMARY
# ------------------------------------------------------------------------------
#
# COMPARISON: Fish vs PowerShell for common operations
#
# 1. FUNCTION DECLARATION:
#    Fish:        function name; end                     (19 chars)
#    PowerShell:  function Name { param(...) }           (30+ chars)
#    Winner: Fish (11+ chars shorter)
#
# 2. PARAMETER WITH DEFAULT:
#    Fish:        set var (test -n "$argv[1]"; and echo $argv[1]; or echo "default")
#                                                         (65 chars)
#    PowerShell:  [string]$Var = 'default'               (27 chars)
#    Winner: PowerShell (38 chars shorter)
#
# 3. COMMAND EXECUTION:
#    Fish:        command arg1 arg2                      (17 chars)
#    PowerShell:  & command arg1 arg2                    (19 chars)
#    Winner: Fish (2 chars shorter)
#
# 4. COLOR OUTPUT:
#    Fish:        set_color cyan; echo "text"; set_color normal
#                                                         (47 chars)
#    PowerShell:  Write-Host "text" -ForegroundColor Cyan (43 chars)
#    Winner: PowerShell (4 chars shorter)
#
# 5. DIRECTORY LISTING:
#    Fish:        du -sh */ | sort -h                    (20 chars)
#    PowerShell:  Get-ChildItem -Directory | Select...   (50+ chars)
#    Winner: Fish (30+ chars shorter)
#
# 6. ARRAY ACCESS:
#    Fish:        $array[1]                              (9 chars)
#    PowerShell:  $array[0]                              (9 chars)
#    Tie (but Fish is 1-indexed, PowerShell is 0-indexed)
#
# OVERALL: Fish wins 3/6, PowerShell wins 2/6, Tie 1/6
#
# TRADE-OFFS:
# - Fish: Cleaner syntax for simple operations, user-friendly
# - PowerShell: Better parameter handling and validation
# - Fish: Autosuggestions and syntax highlighting
# - PowerShell: Object pipeline and .NET integration
# - Fish: Excellent interactive experience
# - PowerShell: Better for complex scripting and automation
# - Fish: No POSIX baggage, modern design
# - PowerShell: Enterprise-ready with rich ecosystem
#
# ==============================================================================

echo "ScriptsToolkit Fish Module Loaded"
echo "Type 'tkfn' to see available functions"
