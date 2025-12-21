#!/usr/bin/env fish
# ==============================================================================
# Comprehensive Fish Shell Syntax Reference with PowerShell Comparisons
# ==============================================================================
#
# This file provides a complete Fish syntax reference with:
# - All keywords, operators, and symbols explained
# - Indentation and style guide conventions
# - Design principles and philosophy
# - Detailed comparisons with PowerShell cmdlets
# - Character count analysis for common operations
# - Semantically meaningful variable names throughout
#
# FISH DESIGN PHILOSOPHY:
# - User-friendly interactive shell
# - "Out of the box" excellent experience
# - Syntax highlighting and autosuggestions
# - No POSIX compatibility (simpler, cleaner syntax)
# - Case-sensitive
# - Web-based configuration
# - Designed for interactive use first, scripting second
#
# KEY DIFFERENCES FROM POWERSHELL:
# - Fish: Text-based pipeline | PowerShell: Object-based pipeline
# - Fish: Autosuggestions from history | PowerShell: Tab completion
# - Fish: Web-based config | PowerShell: Profile scripts
# - Fish: No subshells | PowerShell: Full .NET integration
# - Fish: Simple syntax | PowerShell: Verb-Noun cmdlets
#
# ==============================================================================

# ------------------------------------------------------------------------------
# VARIABLES AND DATA TYPES
# ------------------------------------------------------------------------------

# VARIABLE DECLARATION (set command, no $ when setting)
# Comparison with PowerShell:
#     Fish:        set user_name "John"
#     PowerShell:  $userName = "John"
# Fish is 23 chars, PowerShell is 22 chars (PowerShell shorter by 1)

set user_name "JohnDoe"                  # String variable
set user_age 30                          # Numeric (stored as string)
set account_balance 1234.56              # Float (stored as string)
set is_account_active true               # Boolean (stored as string)

# ACCESSING VARIABLES (requires $ prefix)
echo "User: $user_name"
echo "Age: $user_age"

# VARIABLE SCOPES
# -g or --global    : Global variable
# -l or --local     : Local variable (function scope)
# -x or --export    : Exported variable (environment)
# -U or --universal : Universal variable (persists across sessions)

set -g global_variable "Available everywhere"
set -l local_variable "Only in this function"
set -x PATH /usr/local/bin $PATH        # Add to PATH
set -U preferred_editor vim             # Persists across sessions

# ERASING VARIABLES
set -e variable_name                     # Erase variable

# LISTS (arrays in other shells)
# Fish treats everything as lists by default
# Comparison with PowerShell:
#     Fish:        set servers "Web" "DB" "Cache"
#     PowerShell:  $servers = @("Web", "DB", "Cache")
# Fish is 35 chars, PowerShell is 39 chars (Fish shorter by 4)

set server_names "WebServer01" "WebServer02" "DatabaseServer01"
set port_numbers 80 443 8080 8443

# LIST ACCESS (1-indexed, not 0-indexed!)
set first_server $server_names[1]        # First element (index 1)
set second_server $server_names[2]       # Second element
set last_server $server_names[-1]        # Last element
set all_servers $server_names            # All elements

# LIST LENGTH
set array_length (count $server_names)

# LIST SLICING
set first_two $server_names[1..2]        # First two elements
set from_second $server_names[2..-1]     # From second to end

# APPENDING TO LISTS
set -a server_names "CacheServer01"      # Append element
set server_names $server_names "Cache02" # Also append

# LIST OPERATIONS
set combined_list $server_names $port_numbers  # Concatenate lists

# COMMAND SUBSTITUTION (parentheses)
set current_date (date +%Y-%m-%d)
set current_time (date +%H:%M:%S)

# ------------------------------------------------------------------------------
# OPERATORS
# ------------------------------------------------------------------------------

# ARITHMETIC OPERATIONS
# Fish uses math command or math expression in parentheses
# Comparison with PowerShell:
#     Fish:        math "10 + 5"
#     PowerShell:  10 + 5
# Fish is 14 chars, PowerShell is 7 chars (PowerShell shorter by 7)

set total_items (math "10 + 5")          # Addition: 15
set items_remaining (math "100 - 25")    # Subtraction: 75
set product_price (math "20 * 3")        # Multiplication: 60
set average_score (math "200 / 4")       # Division: 50
set remainder_value (math "17 % 5")      # Modulus: 2

# FLOATING POINT ARITHMETIC
set precise_average (math "100 / 3")     # Automatic float: 33.333...

# COMPARISONS
# Comparison operators in test command or [ ]
# -eq  : Equal to
# -ne  : Not equal to
# -gt  : Greater than
# -ge  : Greater than or equal to
# -lt  : Less than
# -le  : Less than or equal to

set current_age 25
set minimum_age 18

if test $current_age -ge $minimum_age
    echo "Old enough"
end

# STRING COMPARISONS
# =    : Equal
# !=   : Not equal

set entered_password "Secret123"
set stored_password "secret123"

if test "$entered_password" = "$stored_password"
    echo "Passwords match"
else
    echo "Passwords don't match (Fish is case-sensitive!)"
end

# STRING MATCHING
# string match pattern string          - Pattern matching
# string match -r regex string         - Regex matching

if string match -q "*ERROR*" "This is an ERROR message"
    echo "Contains ERROR"
end

# LOGICAL OPERATORS
# -a or and   : Logical AND
# -o or or    : Logical OR
# not or !    : Logical NOT

set has_valid_license true
set has_current_subscription true

if test "$has_valid_license" = "true" -a "$has_current_subscription" = "true"
    echo "Can access premium features"
end

# FILE TEST OPERATORS
# -e   : File exists
# -f   : Is a regular file
# -d   : Is a directory
# -r   : Is readable
# -w   : Is writable
# -x   : Is executable
# -s   : File exists and is not empty

set config_file "/etc/myapp/config.conf"

if test -f $config_file
    echo "Config file exists"
end

if test -d /var/log
    echo "Log directory exists"
end

# ------------------------------------------------------------------------------
# CONTROL FLOW
# ------------------------------------------------------------------------------

# IF-ELSE IF-ELSE STATEMENT
# Fish uses 'end' instead of 'fi' or braces
# Comparison with PowerShell:
#     Fish:        if condition
#                      # code
#                  end
#     PowerShell:  if ($condition) {
#                      # code
#                  }
# Fish is 32 chars, PowerShell is 42 chars (Fish shorter by 10)

set account_balance 150.00
set minimum_balance 100.00
set transaction_amount 75.00

if test (math "$account_balance > $minimum_balance + $transaction_amount") -eq 1
    echo "Transaction approved. Sufficient funds."
    set account_balance (math "$account_balance - $transaction_amount")
else if test (math "$account_balance > $transaction_amount") -eq 1
    echo "Transaction approved with warning."
    set account_balance (math "$account_balance - $transaction_amount")
else
    echo "Transaction declined. Insufficient funds."
end

# SWITCH STATEMENT
# Comparison with PowerShell:
#     Fish:        switch $var; case pattern
#     PowerShell:  switch ($var) { pattern {
# Fish is 28 chars, PowerShell is 28 chars (Tie)

set day_of_week (date +%A)

switch $day_of_week
    case Monday
        echo "Start of work week"
    case Friday
        echo "Last work day of the week"
    case Saturday Sunday
        echo "Weekend day"
    case '*'
        echo "Midweek day"
end

# SWITCH WITH WILDCARDS
set log_level "ERROR_CRITICAL"

switch $log_level
    case 'INFO*'
        echo "Informational message"
    case 'WARN*'
        echo "Warning message"
    case 'ERROR*'
        echo "Error message - requires attention"
    case '*'
        echo "Unknown log level"
end

# ------------------------------------------------------------------------------
# LOOPS
# ------------------------------------------------------------------------------

# FOR LOOP
# Comparison with PowerShell:
#     Fish:        for item in $array
#     PowerShell:  foreach ($item in $array)
# Fish is 20 chars, PowerShell is 27 chars (Fish shorter by 7)

set file_extensions ".txt" ".log" ".csv" ".json"

for current_extension in $file_extensions
    echo "Processing files with extension: $current_extension"
end

# FOR LOOP WITH RANGE
for number in (seq 1 10)
    echo "Number: $number"
end

# FOR LOOP WITH INDEX
for i in (seq 1 (count $server_names))
    echo "Server $i: $server_names[$i]"
end

# WHILE LOOP
set attempt_count 0
set max_attempts 5
set connection_successful false

while test "$connection_successful" != "true" -a $attempt_count -lt $max_attempts
    set attempt_count (math "$attempt_count + 1")
    echo "Connection attempt $attempt_count of $max_attempts"
    
    # Simulate connection
    if test (random 0 1) -eq 1
        set connection_successful true
    end
    
    if test "$connection_successful" != "true"
        sleep 2
    end
end

# BREAK AND CONTINUE
for current_number in (seq 1 20)
    # Skip odd numbers
    if test (math "$current_number % 2") -ne 0
        continue
    end
    
    # Stop after 15
    if test $current_number -gt 15
        break
    end
    
    echo "Processing even number: $current_number"
end

# ------------------------------------------------------------------------------
# FUNCTIONS
# ------------------------------------------------------------------------------

# FUNCTION DEFINITION
# Fish uses 'function' keyword and 'end'
# Comparison with PowerShell:
#     Fish:        function get_user_data
#     PowerShell:  function Get-UserData {
# Fish is 26 chars, PowerShell is 26 chars (Tie)

function get_user_account_status
    set user_name $argv[1]
    set include_details $argv[2]
    
    if test -z "$include_details"
        set include_details false
    end
    
    echo "Checking account status for user: $user_name"
    
    set account_status "User: $user_name, Active: true"
    
    if test "$include_details" = "true"
        set account_status "$account_status, Details: Extended information"
    end
    
    echo $account_status
    return 0
end

# FUNCTION WITH NAMED ARGUMENTS (using argparse)
function create_server_connection
    argparse 'h/help' 's/server=' 'p/port=' -- $argv
    or return
    
    set -q _flag_server; or set _flag_server "localhost"
    set -q _flag_port; or set _flag_port 443
    
    echo "Connecting to $_flag_server:$_flag_port"
end

# Call function
create_server_connection --server server01 --port 8080

# FUNCTION ARGUMENTS
# $argv       - All arguments as list
# $argv[1]    - First argument
# $argv[2..-1]- Second to last arguments
# (count $argv) - Number of arguments

function calculate_sum
    set sum 0
    for number in $argv
        set sum (math "$sum + $number")
    end
    echo $sum
end

# Call function
set result (calculate_sum 10 20 30 40)
echo "Sum: $result"

# FUNCTION WITH DESCRIPTION (shows in help)
function process_data -d "Process data with custom logic"
    echo "Processing: $argv"
end

# ------------------------------------------------------------------------------
# STRING MANIPULATION
# ------------------------------------------------------------------------------

# STRING OPERATIONS (using 'string' command)
set original_text "  Python Programming  "

# STRING LENGTH
set text_length (string length "$original_text")

# SUBSTRING
set substring (string sub -s 3 -l 6 "$original_text")  # Start at 3, length 6

# STRING REPLACEMENT
set replaced_text (string replace "Python" "Fish" "$original_text")
set all_replaced (string replace -a "m" "M" "$original_text")

# CASE CONVERSION
set lowercase_text (string lower "$original_text")
set uppercase_text (string upper "$original_text")

# TRIMMING
set trimmed_text (string trim "$original_text")
set left_trimmed (string trim -l "$original_text")
set right_trimmed (string trim -r "$original_text")

# STRING SPLITTING
set server_list "Server01,Server02,Server03"
set server_array (string split "," $server_list)

for server in $server_array
    echo "Server: $server"
end

# STRING JOINING
set words "one" "two" "three"
set joined_string (string join "," $words)

# STRING MATCHING
if string match -q "*ERROR*" "This is an ERROR"
    echo "Contains ERROR"
end

# REGULAR EXPRESSIONS
set log_entry "2024-01-15 14:30:45 ERROR Connection timeout"

if string match -qr "ERROR" $log_entry
    echo "Log contains error"
end

# REGEX CAPTURE
set date (string replace -r '(\d{4}-\d{2}-\d{2}).*' '$1' $log_entry)
echo "Date: $date"

# ------------------------------------------------------------------------------
# PIPELINES
# ------------------------------------------------------------------------------

# PIPELINE (text-based, like Bash)
# Comparison with PowerShell:
#     Fish:        cat file | grep pattern | sort
#     PowerShell:  Get-Content file | Where-Object {$_ -match pattern} | Sort-Object
# Fish is 31 chars, PowerShell is 73 chars (Fish much shorter)

# Basic pipeline
cat /var/log/syslog | grep ERROR | tail -10

# Using Fish builtins
string match -r "ERROR" < /var/log/syslog | tail -10

# Pipeline with each
seq 1 10 | while read number
    echo "Processing: $number"
end

# ------------------------------------------------------------------------------
# INPUT/OUTPUT
# ------------------------------------------------------------------------------

# OUTPUT REDIRECTION
echo "Hello" > output.txt                # Overwrite
echo "World" >> output.txt               # Append

# INPUT REDIRECTION
while read -l line
    echo "Line: $line"
end < input.txt

# ERROR REDIRECTION
command 2> error.log                     # Stderr to file
command 2>&1                             # Stderr to stdout
command &> output.log                    # Both to file

# DISCARD OUTPUT
command > /dev/null 2>&1

# HERE STRING (not supported, use echo and pipe)
echo "Search this string" | grep "pattern"

# ------------------------------------------------------------------------------
# PROCESS MANAGEMENT
# ------------------------------------------------------------------------------

# BACKGROUND PROCESSES
command &                                # Run in background
set background_pid $last_pid             # Get PID

# JOB CONTROL
# Ctrl+Z     - Suspend
# bg         - Background
# fg         - Foreground
# jobs       - List jobs

# PARALLEL EXECUTION
for server in $server_names
    ping -c 1 $server &
end
wait                                     # Wait for all

# ------------------------------------------------------------------------------
# FISH-SPECIFIC FEATURES
# ------------------------------------------------------------------------------

# AUTOSUGGESTIONS
# Fish suggests commands from history as you type
# Press → or Ctrl+F to accept suggestion

# SYNTAX HIGHLIGHTING
# Fish highlights commands in real-time:
# - Valid commands: colored
# - Invalid commands: red
# - Strings: different color

# TAB COMPLETION
# Intelligent completion for:
# - Commands
# - File paths
# - Command options
# - Git branches
# - Custom completions

# WEB CONFIGURATION
# Run: fish_config
# Opens web interface for:
# - Prompt configuration
# - Color schemes
# - Function editing
# - Variable management

# ABBREVIATIONS (like aliases but expand)
abbr -a gs 'git status'
abbr -a gp 'git push'
abbr -a gc 'git commit'

# Use: Type 'gs' and press space, it expands to 'git status'

# PROMPT CUSTOMIZATION
function fish_prompt
    set_color blue
    echo -n (prompt_pwd)
    set_color yellow
    echo -n ' ❯ '
    set_color normal
end

# RIGHT PROMPT (optional)
function fish_right_prompt
    set_color green
    echo -n (date +%H:%M:%S)
    set_color normal
end

# GREETING (customize welcome message)
set fish_greeting "Welcome to Fish Shell!"

# ------------------------------------------------------------------------------
# EVENT HANDLERS
# ------------------------------------------------------------------------------

# FUNCTION EVENTS
# Run function when variable changes
function on_variable_change --on-variable PWD
    echo "Directory changed to: $PWD"
end

# Run function when signal received
function on_signal --on-signal SIGINT
    echo "Caught Ctrl+C"
end

# ------------------------------------------------------------------------------
# CONFIGURATION
# ------------------------------------------------------------------------------

# CONFIGURATION FILES
# ~/.config/fish/config.fish           - User configuration
# ~/.config/fish/functions/*.fish      - Function definitions
# ~/.config/fish/completions/*.fish    - Custom completions
# ~/.config/fish/conf.d/*.fish         - Additional config snippets

# UNIVERSAL VARIABLES
# Set once, persist across all sessions
set -U fish_user_paths /usr/local/bin $fish_user_paths

# ENVIRONMENT VARIABLES
set -x EDITOR vim
set -x LANG en_US.UTF-8

# ------------------------------------------------------------------------------
# CHARACTER COUNT COMPARISON SUMMARY
# ------------------------------------------------------------------------------

# FISH vs POWERSHELL - Common Operations:
#
# 1. VARIABLE ASSIGNMENT
#    Fish:        set user_name "John"               (23 chars)
#    PowerShell:  $userName = "John"                 (22 chars)
#    Winner: PowerShell (1 char shorter)
#
# 2. LIST CREATION
#    Fish:        set names "A" "B" "C"              (25 chars)
#    PowerShell:  $names = @("A", "B", "C")          (30 chars)
#    Winner: Fish (5 chars shorter)
#
# 3. FOR LOOP
#    Fish:        for item in $array                 (20 chars)
#    PowerShell:  foreach ($item in $array)          (27 chars)
#    Winner: Fish (7 chars shorter)
#
# 4. IF STATEMENT
#    Fish:        if test $x -eq 5                   (19 chars)
#    PowerShell:  if ($x -eq 5) {                    (16 chars)
#    Winner: PowerShell (3 chars shorter)
#
# 5. FUNCTION DEFINITION
#    Fish:        function get_data                  (21 chars)
#    PowerShell:  function Get-Data {                (21 chars)
#    Winner: Tie
#
# 6. PIPELINE OPERATIONS
#    Fish:        cat file | grep pattern            (24 chars)
#    PowerShell:  Get-Content file | Where-Object... (40+ chars)
#    Winner: Fish (much shorter)
#
# OVERALL: Fish wins 3/6, PowerShell wins 2/6, Tie 1/6
# Fish is shorter for loops and lists
# PowerShell is shorter for conditionals
# Both are similar for functions

# ------------------------------------------------------------------------------
# FISH STYLE GUIDE
# ------------------------------------------------------------------------------

# NAMING CONVENTIONS:
# - Variables: lowercase_with_underscores
# - Functions: lowercase_with_underscores
# - Private functions: __private_function (double underscore)

# INDENTATION:
# - 4 spaces (standard)
# - No tabs

# QUOTING:
# - Quote variables when necessary
# - Double quotes for strings with spaces

# COMMENTS:
# - # for single-line comments
# - Document functions with -d flag

# FUNCTION ORGANIZATION:
# - One function per file in ~/.config/fish/functions/
# - Or group related functions in config.fish

# ------------------------------------------------------------------------------
# FISH ADVANTAGES OVER POWERSHELL
# ------------------------------------------------------------------------------

# 1. USER-FRIENDLY: Excellent out-of-box experience
# 2. AUTOSUGGESTIONS: Learns from history, suggests as you type
# 3. SYNTAX HIGHLIGHTING: Real-time command validation
# 4. WEB CONFIGURATION: Easy visual configuration
# 5. SIMPLE SYNTAX: Clean, readable syntax
# 6. SMART COMPLETIONS: Context-aware tab completion
# 7. NO POSIX BAGGAGE: Modern design without legacy constraints

# ------------------------------------------------------------------------------
# POWERSHELL ADVANTAGES OVER FISH
# ------------------------------------------------------------------------------

# 1. OBJECT PIPELINE: Passes structured objects, not text
# 2. DISCOVERABILITY: Verb-Noun naming, Get-Command, Get-Help
# 3. TYPE SYSTEM: Strong typing with .NET objects
# 4. CROSS-PLATFORM: Windows, Linux, macOS support
# 5. SCRIPTING POWER: More suitable for complex scripts
# 6. ENTERPRISE READY: Windows administration, Active Directory
# 7. .NET INTEGRATION: Full access to .NET framework

# ==============================================================================
# END OF FISH SYNTAX REFERENCE
# ==============================================================================

echo "Fish Shell Syntax Reference - Complete Implementation"
echo "Compare with PowerShell, Bash, and other shell references"
echo "All examples use semantically meaningful variable names"
