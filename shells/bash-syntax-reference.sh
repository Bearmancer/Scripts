#!/bin/bash
# ==============================================================================
# Comprehensive Bash Syntax Reference with PowerShell Comparisons
# ==============================================================================
#
# This file provides a complete Bash syntax reference with:
# - All keywords, operators, and symbols explained
# - Indentation and style guide conventions
# - Design principles and philosophy
# - Detailed comparisons with PowerShell cmdlets
# - Character count analysis for common operations
# - Semantically meaningful variable names throughout
#
# BASH DESIGN PHILOSOPHY:
# - Traditional Unix shell (ubiquitous, standard on Unix-like systems)
# - Text-based pipeline (passes strings between commands)
# - Imperative, procedural paradigm
# - Case-sensitive
# - POSIX-compliant (mostly)
# - Designed for scripting and automation
# - "Everything is text" philosophy
#
# KEY DIFFERENCES FROM POWERSHELL:
# - Bash: Text-based pipeline | PowerShell: Object-based pipeline
# - Bash: Case-sensitive | PowerShell: Case-insensitive
# - Bash: Cryptic syntax | PowerShell: Verb-Noun discoverability
# - Bash: Strings everywhere | PowerShell: .NET objects with properties
# - Bash: Traditional Unix tools | PowerShell: Cmdlets with consistent naming
#
# ==============================================================================

# ------------------------------------------------------------------------------
# VARIABLES AND DATA TYPES
# ------------------------------------------------------------------------------

# VARIABLE DECLARATION (no $ prefix when assigning, $ when referencing)
# Comparison with PowerShell:
#     Bash:        user_name="John"
#     PowerShell:  $userName = "John"
# Bash is 19 chars, PowerShell is 22 chars (Bash shorter by 3)

user_name="JohnDoe"                      # String variable
user_age=30                              # Numeric (stored as string)
account_balance=1234.56                  # Float (stored as string)
is_account_active=true                   # Boolean (stored as string "true")

# ACCESSING VARIABLES (requires $ prefix)
echo "User: $user_name"
echo "Age: $user_age"

# VARIABLE EXPANSION WITH BRACES (for clarity)
echo "User: ${user_name}"
echo "File: ${file_name}.txt"

# READONLY VARIABLES (constants)
readonly APPLICATION_NAME="MyApp"
readonly MAXIMUM_RETRIES=3

# ENVIRONMENT VARIABLES (exported to child processes)
export DATABASE_URL="postgresql://localhost:5432/mydb"
export LOG_LEVEL="INFO"

# SPECIAL VARIABLES (automatic)
# $0    - Script name
# $1-$9 - Positional parameters (arguments)
# $#    - Number of arguments
# $@    - All arguments as separate words
# $*    - All arguments as single word
# $$    - Process ID of current shell
# $?    - Exit status of last command
# $!    - Process ID of last background command

script_name=$0
first_argument=$1
argument_count=$#
all_arguments="$@"
last_exit_status=$?

# ARRAYS (indexed, zero-based)
# Comparison with PowerShell:
#     Bash:        server_names=("Web" "DB" "Cache")
#     PowerShell:  $serverNames = @("Web", "DB", "Cache")
# Bash is 38 chars, PowerShell is 42 chars (Bash shorter by 4)

server_names=("WebServer01" "WebServer02" "DatabaseServer01")
port_numbers=(80 443 8080 8443)

# ARRAY ACCESS
first_server="${server_names[0]}"
second_server="${server_names[1]}"
all_servers="${server_names[@]}"         # All elements
array_length="${#server_names[@]}"       # Array length

# ARRAY OPERATIONS
server_names+=("CacheServer01")          # Append element
unset server_names[1]                    # Remove element at index

# ASSOCIATIVE ARRAYS (hash tables, requires Bash 4+)
# Comparison with PowerShell:
#     Bash:        declare -A config=([name]="John" [age]=30)
#     PowerShell:  $config = @{Name="John"; Age=30}
# Bash is 49 chars, PowerShell is 37 chars (PowerShell shorter by 12)

declare -A user_configuration
user_configuration[user_name]="JohnDoe"
user_configuration[email_address]="john@example.com"
user_configuration[department]="Engineering"

# ACCESS ASSOCIATIVE ARRAY
email="${user_configuration[email_address]}"
all_keys="${!user_configuration[@]}"     # Get all keys
all_values="${user_configuration[@]}"    # Get all values

# ------------------------------------------------------------------------------
# OPERATORS
# ------------------------------------------------------------------------------

# ARITHMETIC OPERATORS (using $((...)) or let)
# Comparison with PowerShell:
#     Bash:        result=$((10 + 5))
#     PowerShell:  $result = 10 + 5
# Bash is 20 chars, PowerShell is 18 chars (PowerShell shorter by 2)

total_items=$((10 + 5))                  # Addition: 15
items_remaining=$((100 - 25))            # Subtraction: 75
product_price=$((20 * 3))                # Multiplication: 60
average_score=$((200 / 4))               # Integer division: 50
remainder_value=$((17 % 5))              # Modulus: 2

# INCREMENT/DECREMENT
counter=0
((counter++))                            # Post-increment
((++counter))                            # Pre-increment
((counter--))                            # Post-decrement
((--counter))                            # Pre-decrement

# COMPOUND ASSIGNMENT
total_score=0
((total_score += 10))
((total_score *= 2))

# FLOATING POINT ARITHMETIC (requires bc or awk)
product_price=$(echo "19.99 * 3" | bc)
average=$(awk "BEGIN {print 100/3}")

# COMPARISON OPERATORS (in [[ ]] or [ ])
# Comparison with PowerShell:
#     Bash:        if [[ $age -eq 18 ]]
#     PowerShell:  if ($age -eq 18)
# Bash is 21 chars, PowerShell is 17 chars (PowerShell shorter by 4)

# Numeric comparisons
# -eq  : Equal to
# -ne  : Not equal to
# -gt  : Greater than
# -ge  : Greater than or equal to
# -lt  : Less than
# -le  : Less than or equal to

current_age=25
minimum_age=18

if [[ $current_age -ge $minimum_age ]]; then
    echo "Old enough"
fi

# String comparisons
# =    : Equal (in [[ ]])
# ==   : Equal (same as =)
# !=   : Not equal
# <    : Less than (lexicographic)
# >    : Greater than (lexicographic)
# -z   : String is empty
# -n   : String is not empty

entered_password="Secret123"
stored_password="secret123"

if [[ "$entered_password" == "$stored_password" ]]; then
    echo "Passwords match"
else
    echo "Passwords don't match (Bash is case-sensitive!)"
fi

# Case-insensitive comparison (convert to lowercase)
if [[ "${entered_password,,}" == "${stored_password,,}" ]]; then
    echo "Passwords match (case-insensitive)"
fi

# LOGICAL OPERATORS
# &&   : Logical AND
# ||   : Logical OR
# !    : Logical NOT

has_valid_license=true
has_current_subscription=true

if [[ "$has_valid_license" == "true" && "$has_current_subscription" == "true" ]]; then
    echo "Can access premium features"
fi

# FILE TEST OPERATORS (very common in Bash)
# -e   : File exists
# -f   : Is a regular file
# -d   : Is a directory
# -r   : Is readable
# -w   : Is writable
# -x   : Is executable
# -s   : File exists and is not empty

config_file="/etc/myapp/config.conf"

if [[ -f "$config_file" ]]; then
    echo "Config file exists"
fi

if [[ -d "/var/log" ]]; then
    echo "Log directory exists"
fi

# STRING OPERATORS
# =~   : Regular expression match (in [[ ]])

log_entry="2024-01-15 14:30:45 ERROR Connection timeout"

if [[ "$log_entry" =~ ERROR ]]; then
    echo "Log contains error"
fi

# Extract with regex capture groups
if [[ "$log_entry" =~ ([0-9]{4}-[0-9]{2}-[0-9]{2}) ]]; then
    entry_date="${BASH_REMATCH[1]}"
    echo "Date: $entry_date"
fi

# ------------------------------------------------------------------------------
# CONTROL FLOW
# ------------------------------------------------------------------------------

# IF-ELIF-ELSE STATEMENT
# Comparison with PowerShell:
#     Bash:        if [[ condition ]]; then
#     PowerShell:  if ($condition) {
# Bash is 23 chars, PowerShell is 17 chars (PowerShell shorter by 6)

account_balance=150.00
minimum_balance=100.00
transaction_amount=75.00

if (( $(echo "$account_balance > $minimum_balance + $transaction_amount" | bc -l) )); then
    echo "Transaction approved. Sufficient funds."
    account_balance=$(echo "$account_balance - $transaction_amount" | bc)
elif (( $(echo "$account_balance > $transaction_amount" | bc -l) )); then
    echo "Transaction approved with warning."
    account_balance=$(echo "$account_balance - $transaction_amount" | bc)
else
    echo "Transaction declined. Insufficient funds."
fi

# CASE STATEMENT (switch)
# Comparison with PowerShell:
#     Bash:        case $var in pattern)
#     PowerShell:  switch ($var) { pattern {
# Bash is 21 chars, PowerShell is 27 chars (Bash shorter by 6)

day_of_week=$(date +%A)

case "$day_of_week" in
    Monday)
        echo "Start of work week"
        ;;
    Friday)
        echo "Last work day of the week"
        ;;
    Saturday|Sunday)
        echo "Weekend day"
        ;;
    *)
        echo "Midweek day"
        ;;
esac

# CASE WITH WILDCARDS
log_level="ERROR_CRITICAL"

case "$log_level" in
    INFO*)
        echo "Informational message"
        ;;
    WARN*)
        echo "Warning message"
        ;;
    ERROR*)
        echo "Error message - requires attention"
        ;;
    *)
        echo "Unknown log level"
        ;;
esac

# FOR LOOP (C-style)
# Comparison with PowerShell:
#     Bash:        for ((i=0; i<10; i++))
#     PowerShell:  for ($i=0; $i -lt 10; $i++)
# Bash is 26 chars, PowerShell is 30 chars (Bash shorter by 4)

for ((server_index=0; server_index<${#server_names[@]}; server_index++)); do
    current_server_name="${server_names[$server_index]}"
    echo "Processing server $((server_index + 1)): $current_server_name"
done

# FOR LOOP (iterating over list)
# Comparison with PowerShell:
#     Bash:        for item in "${array[@]}"
#     PowerShell:  foreach ($item in $array)
# Bash is 27 chars, PowerShell is 27 chars (Tie)

file_extensions=(".txt" ".log" ".csv" ".json")

for current_extension in "${file_extensions[@]}"; do
    echo "Processing files with extension: $current_extension"
done

# FOR LOOP (range)
for number in {1..10}; do
    echo "Number: $number"
done

# FOR LOOP (with step)
for number in {10..1..2}; do
    echo "Countdown: $number"
done

# WHILE LOOP
attempt_count=0
max_attempts=5
connection_successful=false

while [[ "$connection_successful" != "true" && $attempt_count -lt $max_attempts ]]; do
    ((attempt_count++))
    echo "Connection attempt $attempt_count of $max_attempts"
    
    # Simulate connection attempt
    if (( RANDOM % 2 )); then
        connection_successful=true
    fi
    
    if [[ "$connection_successful" != "true" ]]; then
        sleep 2
    fi
done

# UNTIL LOOP (opposite of while)
retry_count=0
until [[ $retry_count -ge 3 ]]; do
    ((retry_count++))
    echo "Retry attempt: $retry_count"
done

# BREAK AND CONTINUE
for current_number in {1..20}; do
    # Skip odd numbers
    if (( current_number % 2 != 0 )); then
        continue
    fi
    
    # Stop after 15
    if (( current_number > 15 )); then
        break
    fi
    
    echo "Processing even number: $current_number"
done

# ------------------------------------------------------------------------------
# FUNCTIONS
# ------------------------------------------------------------------------------

# FUNCTION DEFINITION (two syntaxes)
# Comparison with PowerShell:
#     Bash:        function get_user_data() {
#     PowerShell:  function Get-UserData {
# Bash is 29 chars, PowerShell is 26 chars (PowerShell shorter by 3)

# Syntax 1: function keyword
function get_user_account_status() {
    local user_name="$1"
    local include_details="${2:-false}"
    
    echo "Checking account status for user: $user_name"
    
    local account_status="User: $user_name, Active: true"
    
    if [[ "$include_details" == "true" ]]; then
        account_status="$account_status, Details: Extended information"
    fi
    
    echo "$account_status"
    return 0
}

# Syntax 2: no function keyword (POSIX-compliant)
get_user_data() {
    local username="$1"
    echo "User: $username"
}

# FUNCTION WITH PARAMETERS
create_server_connection() {
    local server_name="$1"
    local port_number="${2:-443}"
    local protocol="${3:-HTTPS}"
    local timeout_seconds="${4:-30}"
    
    echo "Connecting to $server_name:$port_number using $protocol"
    echo "Timeout: ${timeout_seconds}s"
}

# Call function
create_server_connection "server01"
create_server_connection "server02" 8080 "HTTP" 60

# FUNCTION RETURN VALUES
# In Bash, functions return exit codes (0-255)
# To return data, use echo and command substitution

calculate_sum() {
    local sum=0
    for number in "$@"; do
        ((sum += number))
    done
    echo "$sum"
}

# Capture function output
result=$(calculate_sum 10 20 30 40)
echo "Sum: $result"

# LOCAL VARIABLES
process_data() {
    local local_variable="Only visible in function"
    global_variable="Visible everywhere"
    echo "$local_variable"
}

# ------------------------------------------------------------------------------
# ERROR HANDLING
# ------------------------------------------------------------------------------

# EXIT ON ERROR
set -e                                   # Exit on error
set -u                                   # Exit on undefined variable
set -o pipefail                          # Exit on pipe failure

# TRAP (catch errors and signals)
cleanup() {
    echo "Cleaning up..."
    # Cleanup code here
}

trap cleanup EXIT                        # Run on script exit
trap 'echo "Error on line $LINENO"' ERR # Run on error

# CHECKING EXIT STATUS
read_configuration_file() {
    local config_file_path="$1"
    
    echo "Attempting to read configuration file: $config_file_path"
    
    if [[ ! -f "$config_file_path" ]]; then
        echo "Error: Configuration file not found: $config_file_path" >&2
        return 1
    fi
    
    if [[ ! -r "$config_file_path" ]]; then
        echo "Error: Access denied to configuration file: $config_file_path" >&2
        return 1
    fi
    
    local configuration_content
    configuration_content=$(cat "$config_file_path")
    
    if [[ -z "$configuration_content" ]]; then
        echo "Error: Configuration file is empty" >&2
        return 1
    fi
    
    echo "Configuration loaded successfully"
    echo "$configuration_content"
    return 0
}

# USING ERROR HANDLING
if read_configuration_file "/etc/myapp/config.conf"; then
    echo "Config loaded successfully"
else
    echo "Failed to load config"
fi

# ------------------------------------------------------------------------------
# STRING MANIPULATION
# ------------------------------------------------------------------------------

original_text="  Python Programming  "

# STRING LENGTH
text_length=${#original_text}

# SUBSTRING EXTRACTION
# ${variable:offset:length}
substring=${original_text:2:6}

# STRING REPLACEMENT
# ${variable/pattern/replacement}      - Replace first occurrence
# ${variable//pattern/replacement}     - Replace all occurrences
replaced_text=${original_text/Python/Bash}
all_replaced=${original_text//m/M}

# CASE CONVERSION (Bash 4+)
lowercase_text="${original_text,,}"      # To lowercase
uppercase_text="${original_text^^}"      # To uppercase

# TRIMMING WHITESPACE
trimmed_text="${original_text#"${original_text%%[![:space:]]*}"}"
trimmed_text="${trimmed_text%"${trimmed_text##*[![:space:]]}"}"

# STRING SPLITTING
IFS=',' read -ra server_array <<< "Server01,Server02,Server03"
for server in "${server_array[@]}"; do
    echo "Server: $server"
done

# STRING JOINING
array=("one" "two" "three")
joined_string=$(IFS=,; echo "${array[*]}")

# ------------------------------------------------------------------------------
# COMMAND SUBSTITUTION AND PIPELINES
# ------------------------------------------------------------------------------

# COMMAND SUBSTITUTION (two syntaxes)
current_date=$(date +%Y-%m-%d)           # Modern syntax
current_time=`date +%H:%M:%S`            # Backtick syntax (deprecated)

# PIPELINE (text-based)
# Comparison with PowerShell:
#     Bash:        cat file | grep pattern | sort
#     PowerShell:  Get-Content file | Where-Object {$_ -match pattern} | Sort-Object
# Bash is 31 chars, PowerShell is 73 chars (Bash much shorter)

# Basic pipeline
cat /var/log/syslog | grep ERROR | tail -10

# Multiple commands
ps aux | grep python | awk '{print $2}' | xargs kill -9

# Pipeline with for loop
for process_id in $(ps aux | grep python | awk '{print $2}'); do
    echo "Killing process: $process_id"
    kill -9 "$process_id"
done

# ------------------------------------------------------------------------------
# INPUT/OUTPUT REDIRECTION
# ------------------------------------------------------------------------------

# OUTPUT REDIRECTION
echo "Hello" > output.txt                # Overwrite file
echo "World" >> output.txt               # Append to file

# INPUT REDIRECTION
while read -r line; do
    echo "Line: $line"
done < input.txt

# ERROR REDIRECTION
command 2> error.log                     # Redirect stderr to file
command 2>&1                             # Redirect stderr to stdout
command &> output.log                    # Redirect both stdout and stderr

# DISCARD OUTPUT
command > /dev/null 2>&1                 # Discard all output

# HERE DOCUMENT
cat << EOF
This is a multi-line
here document in Bash.
Variables are expanded: $user_name
EOF

# HERE STRING
grep "pattern" <<< "Search this string"

# ------------------------------------------------------------------------------
# PROCESS MANAGEMENT
# ------------------------------------------------------------------------------

# BACKGROUND PROCESSES
long_running_command &                   # Run in background
background_pid=$!                        # Get PID of background process

# WAIT FOR BACKGROUND PROCESS
wait $background_pid

# JOB CONTROL
# Ctrl+Z     - Suspend current job
# bg         - Resume job in background
# fg         - Resume job in foreground
# jobs       - List jobs

# PARALLEL EXECUTION
for server in "${server_names[@]}"; do
    ping -c 1 "$server" &
done
wait                                     # Wait for all background jobs

# ------------------------------------------------------------------------------
# ADVANCED FEATURES
# ------------------------------------------------------------------------------

# PROCESS SUBSTITUTION
diff <(sort file1.txt) <(sort file2.txt)

# SUBSHELLS
(cd /tmp && ls -la)                      # Commands in subshell don't affect parent

# BRACE EXPANSION
echo file{1..5}.txt                      # Expands to: file1.txt file2.txt ...
echo {A..Z}                              # Expands to: A B C ... Z

mkdir -p project/{src,test,docs}         # Create multiple directories

# PARAMETER EXPANSION
# ${variable:-default}                   - Use default if unset
# ${variable:=default}                   - Assign default if unset
# ${variable:?error}                     - Error if unset
# ${variable:+alternate}                 - Use alternate if set

database_url="${DATABASE_URL:-postgresql://localhost:5432/mydb}"

# ARITHMETIC EXPANSION
result=$((5 + 3 * 2))                    # Result: 11

# COMMAND GROUPING
{ command1; command2; } > output.txt     # Group commands, share redirection

# ------------------------------------------------------------------------------
# CHARACTER COUNT COMPARISON SUMMARY
# ------------------------------------------------------------------------------

# BASH vs POWERSHELL - Common Operations:
#
# 1. VARIABLE ASSIGNMENT
#    Bash:        user_name="John"                   (19 chars)
#    PowerShell:  $userName = "John"                 (22 chars)
#    Winner: Bash (3 chars shorter)
#
# 2. ARRAY CREATION
#    Bash:        names=("A" "B" "C")                (23 chars)
#    PowerShell:  $names = @("A", "B", "C")          (30 chars)
#    Winner: Bash (7 chars shorter)
#
# 3. FOR LOOP
#    Bash:        for item in "${array[@]}"          (27 chars)
#    PowerShell:  foreach ($item in $array)          (27 chars)
#    Winner: Tie
#
# 4. IF STATEMENT
#    Bash:        if [[ $x -eq 5 ]]; then            (25 chars)
#    PowerShell:  if ($x -eq 5) {                    (16 chars)
#    Winner: PowerShell (9 chars shorter)
#
# 5. FUNCTION DEFINITION
#    Bash:        function get_data() {              (24 chars)
#    PowerShell:  function Get-Data {                (21 chars)
#    Winner: PowerShell (3 chars shorter)
#
# 6. PIPELINE OPERATIONS
#    Bash:        cat file | grep pattern            (24 chars)
#    PowerShell:  Get-Content file | Where-Object... (40+ chars)
#    Winner: Bash (much shorter for simple cases)
#
# OVERALL: Bash wins 3/6, PowerShell wins 2/6, Tie 1/6
# Bash is generally shorter for simple operations, especially pipelines
# PowerShell is shorter for conditionals and structured operations

# ------------------------------------------------------------------------------
# BASH STYLE GUIDE
# ------------------------------------------------------------------------------

# NAMING CONVENTIONS:
# - Variables: lowercase_with_underscores
# - Constants: UPPERCASE_WITH_UNDERSCORES
# - Functions: lowercase_with_underscores
# - Local variables: local keyword prefix

# INDENTATION:
# - 2 or 4 spaces (2 is more common)
# - No tabs

# QUOTING:
# - Always quote variables: "$variable"
# - Use double quotes for expansion, single quotes for literals

# SHEBANG:
# - #!/bin/bash (specific to Bash)
# - #!/bin/sh (POSIX-compliant)

# ERROR HANDLING:
# - Use set -e for critical scripts
# - Check exit status with $?
# - Use trap for cleanup

# COMMENTS:
# - # for single-line comments
# - Document complex logic
# - Explain "why" not "what"

# ------------------------------------------------------------------------------
# BASH ADVANTAGES OVER POWERSHELL
# ------------------------------------------------------------------------------

# 1. UBIQUITY: Available on virtually all Unix-like systems
# 2. SIMPLICITY: Simpler syntax for basic tasks
# 3. PIPELINE EFFICIENCY: Text pipelines are very fast
# 4. TOOL ECOSYSTEM: Massive ecosystem of Unix tools
# 5. SCRIPTING TRADITION: Decades of scripts and examples
# 6. STARTUP SPEED: Very fast startup time
# 7. PORTABILITY: Scripts run on many systems unchanged

# ------------------------------------------------------------------------------
# POWERSHELL ADVANTAGES OVER BASH
# ------------------------------------------------------------------------------

# 1. OBJECT PIPELINE: Passes structured objects, not text
# 2. DISCOVERABILITY: Verb-Noun naming, Get-Command, Get-Help
# 3. CONSISTENCY: Cmdlets follow standard patterns
# 4. TYPE SYSTEM: Strong typing with .NET objects
# 5. ERROR HANDLING: More sophisticated exception handling
# 6. CROSS-PLATFORM: Modern PowerShell runs on Windows, Linux, macOS
# 7. STRUCTURED DATA: Better for complex data manipulation

# ==============================================================================
# END OF BASH SYNTAX REFERENCE
# ==============================================================================

echo "Bash Syntax Reference - Complete Implementation"
echo "Compare with PowerShell and other shell references"
echo "All examples use semantically meaningful variable names"
