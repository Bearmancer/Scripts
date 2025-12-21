# ==============================================================================
# NuShell Implementation of ScriptsToolkit Module
# ==============================================================================
#
# This is a native NuShell implementation of the PowerShell ScriptsToolkit module,
# demonstrating how the same functionality can be achieved in NuShell with detailed
# explanations of syntax differences.
#
# COMPARISON WITH POWERSHELL:
# - NuShell: Structured data (tables), modern syntax, cross-platform from start
# - PowerShell: Object-based (.NET objects), mature ecosystem, Windows roots
# - NuShell: Table-oriented pipeline, immutable by default
# - PowerShell: Object pipeline, mutable variables
# - NuShell: Plugin system for extensions
# - PowerShell: .NET framework integration
#
# ==============================================================================

# ------------------------------------------------------------------------------
# MODULE VARIABLES (equivalent to PowerShell script-scoped variables)
# ------------------------------------------------------------------------------

# PowerShell: $Script:RepositoryRoot = Split-Path -Path $PSScriptRoot -Parent
# NuShell: Use environment variables or constants
#
# COMPARISON:
# - PowerShell: $Script: scope prefix for script-level variables
# - NuShell: No scope prefixes, lexically scoped
# - PowerShell: Split-Path cmdlet for path manipulation
# - NuShell: path dirname for path operations
#
# Note: In NuShell, we'd typically define these as part of config.nu or env.nu
# For this module, we'll use let with path operations

let repository_root = ($env.PWD | path dirname)
let python_toolkit = ($repository_root | path join "python" "toolkit" "cli.py")
let csharp_root = ($repository_root | path join "csharp")
let log_directory = ($repository_root | path join "logs")

# ------------------------------------------------------------------------------
# FUNCTION: invoke-toolkit-python
# PowerShell equivalent: Invoke-ToolkitPython
# ------------------------------------------------------------------------------
#
# SYNTAX COMPARISON:
#   PowerShell: function Invoke-ToolkitPython {
#                   [CmdletBinding()]
#                   param(
#                       [Parameter(Mandatory)]
#                       [string[]]$ArgumentList
#                   )
#                   & $python @arguments
#               }
#   
#   NuShell:    def invoke-toolkit-python [
#                   ...argument_list: string  # Required rest parameter
#               ] {
#                   python $python_toolkit ...$argument_list
#               }
#
# KEY DIFFERENCES:
# - PowerShell: Verbose param() block with attributes
# - NuShell: Inline parameter list with type annotations
# - PowerShell: [CmdletBinding()] for advanced features
# - NuShell: Built-in help with descriptions
# - PowerShell: [Parameter(Mandatory)] for required params
# - NuShell: Parameters without default are required
# - PowerShell: @ for array splatting
# - NuShell: ... for spread operator
#
# CHARACTER COUNT:
#   PowerShell with param: 120+ chars (full declaration)
#   NuShell: 45 chars (def with param)
#   Winner: NuShell (75+ chars shorter)
#
def invoke-toolkit-python [
    ...argument_list: string  # Rest parameter for variable arguments
]: nothing {
    # NuShell function description (like PowerShell comment-based help)
    # PowerShell: .SYNOPSIS, .DESCRIPTION in comment block
    # NuShell: Description after function signature (not shown here but can add with --help)
    
    # Check if arguments provided
    # PowerShell: [Parameter(Mandatory)] handles automatically
    # NuShell: Rest parameters are always arrays, check length
    if ($argument_list | length) == 0 {
        error make {
            msg: "ArgumentList parameter is required"
        }
    }
    
    # Find python command
    # PowerShell: $python = (Get-Command -Name python).Source
    # NuShell: which command returns path
    #
    # COMPARISON:
    # - PowerShell: Get-Command with .Source property (30+ chars)
    # - NuShell: which command (15 chars)
    # - Winner: NuShell (15+ chars shorter)
    let python_path = (
        try {
            (which python3).path.0
        } catch {
            (which python).path.0
        }
    )
    
    # Execute Python with toolkit script and arguments
    # PowerShell: & $python @arguments
    # NuShell: ^$python_path ...$argument_list
    #
    # COMPARISON:
    # - PowerShell: & operator for external commands, @ for splatting
    # - NuShell: ^ for external commands, ... for spreading
    # - Both: Similar concepts, different syntax
    # - NuShell: More explicit with ^ prefix
    #
    # Run command and capture exit code
    let result = (
        do {
            ^$python_path $python_toolkit ...$argument_list
        } | complete
    )
    
    # Check exit code
    # PowerShell: $LASTEXITCODE automatic variable
    # NuShell: Use 'complete' command to get exit code
    #
    # COMPARISON:
    # - PowerShell: $LASTEXITCODE after command (automatic)
    # - NuShell: Must use 'complete' to capture exit code
    # - PowerShell: Simpler automatic variable
    # - NuShell: More explicit with 'complete'
    if $result.exit_code != 0 {
        error make {
            msg: $"Python toolkit exited with code ($result.exit_code)"
        }
    }
}

# Alias (like PowerShell [Alias])
# PowerShell: [Alias('tkpy')]
# NuShell: alias tkpy = invoke-toolkit-python
alias tkpy = invoke-toolkit-python

# ------------------------------------------------------------------------------
# FUNCTION: get-toolkit-functions
# PowerShell equivalent: Get-ToolkitFunctions
# ------------------------------------------------------------------------------
#
# SYNTAX COMPARISON - Data Structures:
#   PowerShell: $functions = @(
#                   @{ Category = 'Utilities'; Name = 'Get-Functions'; ... }
#                   @{ Category = 'Logs'; Name = 'Show-SyncLog'; ... }
#               )
#
#   NuShell:    let function_data = [
#                   {category: "Utilities", name: "get-toolkit-functions", ...}
#                   {category: "Logs", name: "show-sync-log", ...}
#               ]
#
# KEY DIFFERENCES:
# - PowerShell: @{} for hash tables, @() for arrays
# - NuShell: {} for records, [] for lists
# - PowerShell: Keys use any casing, accessed with dot notation
# - NuShell: Keys lowercase, accessed with dot notation
# - PowerShell: Semicolons between key-value pairs
# - NuShell: Commas between key-value pairs
# - Both: Similar structure, NuShell more JSON-like
#
def get-toolkit-functions []: nothing {
    # Function metadata as list of records
    # PowerShell uses hash tables @{}
    # NuShell uses records {} in lists []
    #
    # COMPARISON:
    # - PowerShell: @() wrapper for array
    # - NuShell: [] native list syntax
    # - NuShell: More concise and JSON-like
    let function_data = [
        {category: "Utilities", name: "get-toolkit-functions", alias: "tkfn", description: "List all toolkit functions"}
        {category: "Utilities", name: "open-command-history", alias: "hist", description: "Open shell history file"}
        {category: "Utilities", name: "show-toolkit-help", alias: "tkhelp", description: "Open toolkit documentation"}
        {category: "Utilities", name: "invoke-toolkit-analyzer", alias: "tklint", description: "Run linting tools"}
        {category: "Logs", name: "show-sync-log", alias: "synclog", description: "View JSONL sync logs"}
        {category: "Sync", name: "invoke-youtube-sync", alias: "syncyt", description: "Sync YouTube playlists"}
        {category: "Sync", name: "invoke-lastfm-sync", alias: "synclf", description: "Sync Last.fm scrobbles"}
        {category: "Sync", name: "invoke-all-syncs", alias: "syncall", description: "Run all daily syncs"}
        {category: "Filesystem", name: "get-directories", alias: "dirs", description: "List directories with sizes"}
        {category: "Filesystem", name: "get-files-and-dirs", alias: "tree", description: "List all items"}
        {category: "Video", name: "start-disc-remux", alias: "remux", description: "Remux video discs"}
        {category: "Video", name: "start-batch-compression", alias: "compress", description: "Compress videos"}
        {category: "Audio", name: "convert-audio", alias: "audio", description: "Convert audio files"}
        {category: "Audio", name: "convert-to-mp3", alias: "tomp3", description: "Convert to MP3"}
        {category: "Audio", name: "convert-to-flac", alias: "toflac", description: "Convert to FLAC"}
    ]
    
    # Print header with colors
    # PowerShell: Write-Host "`nScriptsToolkit Functions" -ForegroundColor Cyan
    # NuShell: print with ANSI codes or use styling
    #
    # COMPARISON:
    # - PowerShell: -ForegroundColor parameter
    # - NuShell: ANSI escape codes in strings
    # - Both can achieve colored output
    # - PowerShell syntax more integrated
    print "\nScriptsToolkit Functions (NuShell)"
    print "=================================="
    print ""
    
    # Group by category and display
    # PowerShell: $functions | Group-Object -Property Category | ForEach-Object {...}
    # NuShell: $function_data | group-by category | transpose | each {...}
    #
    # COMPARISON:
    # - PowerShell: Group-Object cmdlet (29 chars)
    # - NuShell: group-by command (15 chars)
    # - Winner: NuShell (14 chars shorter)
    # - Both: Pipeline-based grouping
    # - NuShell: More concise command names
    #
    $function_data
    | group-by category
    | transpose category items
    | each { |row|
        # Print category header
        print $"($row.category)"
        
        # Print each function in category
        $row.items | each { |func|
            # Format with padding
            # PowerShell: "$($_.Alias.PadRight(10))"
            # NuShell: Use format or fill for padding
            #
            # COMPARISON:
            # - PowerShell: .PadRight() method
            # - NuShell: fill command or format strings
            # - Both achieve same result
            let alias_padded = ($func.alias | fill -a right -w 10)
            let name_padded = ($func.name | fill -a right -w 30)
            
            print $"  ($alias_padded) ($name_padded) ($func.description)"
        }
        print ""
    }
}

# Alias
alias tkfn = get-toolkit-functions

# ------------------------------------------------------------------------------
# FUNCTION: show-sync-log
# PowerShell equivalent: Show-SyncLog
# ------------------------------------------------------------------------------
#
# SYNTAX COMPARISON - Parameters with Defaults:
#   PowerShell: param(
#                   [ValidateSet('youtube', 'lastfm', 'all')]
#                   [string]$Service = 'all',
#                   [int]$Sessions = 10
#               )
#
#   NuShell:    def show-sync-log [
#                   service: string = "all"    # Default value
#                   num_sessions: int = 10     # Default value
#               ]
#
# KEY DIFFERENCES:
# - PowerShell: ValidateSet for automatic validation and tab completion
# - NuShell: Manual validation required, but cleaner syntax
# - PowerShell: Verbose param() block
# - NuShell: Inline parameters with types and defaults
# - PowerShell: 60+ chars for param block
# - NuShell: 35 chars for same parameters
# - Winner: NuShell (25+ chars shorter)
#
def show-sync-log [
    service: string = "all"      # Service to show logs for
    num_sessions: int = 10       # Number of sessions to display
]: nothing {
    # Validate service parameter
    # PowerShell: [ValidateSet(...)] handles automatically
    # NuShell: Manual match expression
    #
    # COMPARISON:
    # - PowerShell: Validation built into parameter
    # - NuShell: Validation in function body
    # - PowerShell: More concise for validation
    # - NuShell: More flexible control flow
    if not ($service in ["youtube", "lastfm", "all"]) {
        error make {
            msg: "Service must be 'youtube', 'lastfm', or 'all'"
        }
    }
    
    # Determine log files based on service
    # PowerShell: Uses switch statement
    # NuShell: Uses match expression
    #
    # COMPARISON:
    # - PowerShell: switch ($var) { pattern { ... } }
    # - NuShell: match $var { pattern => ... }
    # - NuShell: More concise with => syntax
    let log_files = match $service {
        "youtube" => [$"($log_directory)/youtube.jsonl"],
        "lastfm" => [$"($log_directory)/lastfm.jsonl"],
        "all" => [$"($log_directory)/youtube.jsonl", $"($log_directory)/lastfm.jsonl"],
        _ => []
    }
    
    # Check if log files exist
    # PowerShell: Test-Path cmdlet
    # NuShell: path exists command
    #
    # COMPARISON:
    # - PowerShell: Test-Path (9 chars)
    # - NuShell: path exists (11 chars)
    # - Similar functionality
    let existing_files = (
        $log_files
        | where { |file| $file | path exists }
    )
    
    if ($existing_files | length) == 0 {
        error make {
            msg: $"No log files found in ($log_directory)"
        }
    }
    
    print $"Showing last ($num_sessions) sessions for ($service)"
    print $"Log files: ($log_files | str join ', ')"
    
    # Read and parse JSONL files
    # PowerShell: Get-Content | ConvertFrom-Json
    # NuShell: open command with automatic format detection
    #
    # COMPARISON:
    # - PowerShell: Get-Content | ConvertFrom-Json (40 chars)
    # - NuShell: open file (9 chars)
    # - Winner: NuShell (31 chars shorter!)
    # - NuShell: Automatic format detection
    # - PowerShell: Manual conversion needed
    #
    $existing_files | each { |log_file|
        print $"\n--- (basename $log_file) ---"
        
        # Read JSONL file (one JSON object per line)
        # NuShell can parse JSON natively
        open $log_file
        | lines
        | each { |line| $line | from json }
        | where event == "SessionStarted" or event == "SessionEnded"
        | last $num_sessions
        | select timestamp event session_id service
        | table
    }
}

# Alias
alias synclog = show-sync-log

# ------------------------------------------------------------------------------
# FUNCTION: open-command-history
# PowerShell equivalent: Open-CommandHistory
# ------------------------------------------------------------------------------
#
# SYNTAX COMPARISON - External Command Execution:
#   PowerShell: & code "$env:APPDATA\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt"
#   NuShell:    ^code $nu.history-path
#
# KEY DIFFERENCES:
# - PowerShell: & operator for external commands
# - NuShell: ^ operator for external commands
# - PowerShell: $env:APPDATA for environment variables
# - NuShell: $env.APPDATA for environment variables
# - PowerShell: Long path to history file (80+ chars)
# - NuShell: $nu.history-path built-in variable (17 chars)
# - Winner: NuShell (63+ chars shorter)
#
def open-command-history []: nothing {
    # NuShell has built-in $nu.history-path variable
    # Much simpler than PowerShell's path construction
    let history_file = $nu.history-path
    
    # Check for available editors
    # PowerShell: & operator
    # NuShell: ^ operator
    #
    # COMPARISON:
    # - PowerShell: & code (7 chars)
    # - NuShell: ^code (5 chars)
    # - Winner: NuShell (2 chars shorter)
    if (which code | length) > 0 {
        ^code $history_file
    } else if (which vim | length) > 0 {
        ^vim $history_file
    } else if (which nano | length) > 0 {
        ^nano $history_file
    } else {
        error make {
            msg: "No editor found (tried: code, vim, nano)"
        }
    }
}

# Alias
alias hist = open-command-history

# ------------------------------------------------------------------------------
# FUNCTION: get-directories
# PowerShell equivalent: Get-Directories
# ------------------------------------------------------------------------------
#
# SYNTAX COMPARISON - Directory Listing:
#   PowerShell: Get-ChildItem -Directory | 
#               Select-Object Name, @{N='Size'; E={(Get-ChildItem $_.FullName -Recurse | 
#                   Measure-Object -Property Length -Sum).Sum}} |
#               Format-Table
#
#   NuShell:    ls | where type == dir | 
#               insert size { |row| du $row.name | get apparent } |
#               select name size
#
# KEY DIFFERENCES:
# - PowerShell: Get-ChildItem (14 chars), multiple cmdlets
# - NuShell: ls (2 chars), fewer commands needed
# - PowerShell: Complex calculated properties with hash tables
# - NuShell: insert command to add columns
# - PowerShell: 150+ chars for full pipeline
# - NuShell: 80 chars for same result
# - Winner: NuShell (70+ chars shorter)
#
def get-directories [
    target_dir: string = "."  # Directory to list
]: nothing {
    print $"Directories in ($target_dir) with sizes:"
    print ""
    
    # Use ls to list directories
    # Then add size information using du
    # NuShell's structured pipeline makes this elegant
    ls $target_dir
    | where type == "dir"
    | insert size { |row|
        try {
            (du $row.name | where name == $row.name | get apparent.0)
        } catch {
            "N/A"
        }
    }
    | select name size
    | sort-by size
    | table
}

# Alias
alias dirs = get-directories

# ------------------------------------------------------------------------------
# CHARACTER COUNT SUMMARY
# ------------------------------------------------------------------------------
#
# COMPARISON: NuShell vs PowerShell for common operations
#
# 1. FUNCTION DECLARATION WITH PARAMETERS:
#    NuShell:     def name [param: type = default] { }  (38 chars)
#    PowerShell:  function Name { param([type]$Param = 'default') }
#                                                        (55+ chars)
#    Winner: NuShell (17+ chars shorter)
#
# 2. GROUPING DATA:
#    NuShell:     | group-by field                      (16 chars)
#    PowerShell:  | Group-Object -Property Field       (29 chars)
#    Winner: NuShell (13 chars shorter)
#
# 3. READING JSON:
#    NuShell:     open file                             (9 chars)
#    PowerShell:  Get-Content file | ConvertFrom-Json  (40 chars)
#    Winner: NuShell (31 chars shorter!)
#
# 4. EXTERNAL COMMAND:
#    NuShell:     ^command                              (8 chars)
#    PowerShell:  & command                             (9 chars)
#    Winner: NuShell (1 char shorter)
#
# 5. HISTORY FILE PATH:
#    NuShell:     $nu.history-path                      (17 chars)
#    PowerShell:  "$env:APPDATA\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt"
#                                                        (80+ chars)
#    Winner: NuShell (63+ chars shorter!)
#
# 6. LISTING DIRECTORIES:
#    NuShell:     ls | where type == "dir"              (24 chars)
#    PowerShell:  Get-ChildItem -Directory              (28 chars)
#    Winner: NuShell (4 chars shorter)
#
# OVERALL: NuShell wins 6/6 comparisons for syntax brevity
#
# TRADE-OFFS:
# - NuShell: Shorter, modern syntax, structured data everywhere
# - PowerShell: More verbose but mature ecosystem
# - NuShell: Table-oriented pipeline, immutable by default
# - PowerShell: Object pipeline, .NET integration
# - NuShell: Automatic format detection (JSON, CSV, etc.)
# - PowerShell: Manual conversion needed
# - NuShell: Younger ecosystem, learning curve
# - PowerShell: Established, extensive documentation
# - NuShell: Best for data manipulation
# - PowerShell: Best for Windows administration
#
# ==============================================================================

print "ScriptsToolkit NuShell Module Loaded"
print "Type 'tkfn' to see available functions"
