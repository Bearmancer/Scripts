# ==============================================================================
# Comprehensive NuShell Syntax Reference with PowerShell Comparisons
# ==============================================================================
#
# This file provides a complete NuShell syntax reference with:
# - All keywords, operators, and symbols explained
# - Indentation and style guide conventions
# - Design principles and philosophy
# - Detailed comparisons with PowerShell cmdlets
# - Character count analysis for common operations
# - Semantically meaningful variable names throughout
#
# NUSHELL DESIGN PHILOSOPHY:
# - Structured data shell (everything is typed data, not text)
# - Table-oriented pipeline (like PowerShell but different)
# - Modern syntax (Rust-inspired)
# - Cross-platform from the start
# - Plugin system for extensibility
# - Designed for data manipulation and analysis
# - Type-aware commands
#
# KEY DIFFERENCES FROM POWERSHELL:
# - NuShell: Tables everywhere | PowerShell: Objects everywhere
# - NuShell: Rust-inspired syntax | PowerShell: C#-inspired syntax
# - NuShell: No cmdlet concept | PowerShell: Verb-Noun cmdlets
# - NuShell: Immutable by default | PowerShell: Mutable variables
# - NuShell: External plugins | PowerShell: .NET integration
#
# ==============================================================================

# ------------------------------------------------------------------------------
# VARIABLES AND DATA TYPES
# ------------------------------------------------------------------------------

# VARIABLE DECLARATION (let command, immutable by default)
# Comparison with PowerShell:
#     NuShell:     let user_name = "John"
#     PowerShell:  $userName = "John"
# NuShell is 24 chars, PowerShell is 22 chars (PowerShell shorter by 2)

let user_name = "JohnDoe"                # Immutable string
let user_age = 30                        # Integer
let account_balance = 1234.56            # Float
let is_account_active = true             # Boolean

# MUTABLE VARIABLES (mut keyword)
mut counter = 0                          # Mutable variable
$counter += 1                            # Can be modified

# VARIABLE SCOPING
# Variables are lexically scoped (block scope)
# No global variables by default

# DATA TYPES (structured, typed)
# - int: Integers
# - float: Floating point numbers
# - string: Text strings
# - bool: true/false
# - date: Date and time
# - duration: Time spans
# - filesize: File sizes with units
# - range: Numeric ranges
# - list: Ordered collections
# - record: Key-value pairs (like objects)
# - table: Tables of records
# - binary: Binary data

# LISTS (ordered collections)
# Comparison with PowerShell:
#     NuShell:     let servers = ["Web", "DB", "Cache"]
#     PowerShell:  $servers = @("Web", "DB", "Cache")
# NuShell is 41 chars, PowerShell is 39 chars (PowerShell shorter by 2)

let server_names = ["WebServer01", "WebServer02", "DatabaseServer01"]
let port_numbers = [80, 443, 8080, 8443]
let mixed_list = ["text", 123, true]     # Can hold different types

# LIST ACCESS (0-indexed)
let first_server = $server_names.0       # First element
let second_server = $server_names.1      # Second element
let last_server = ($server_names | last) # Last element

# LIST LENGTH
let array_length = ($server_names | length)

# LIST OPERATIONS
let combined_list = ($server_names | append $port_numbers)
let filtered_list = ($port_numbers | where $it > 80)
let mapped_list = ($port_numbers | each { |port| $port * 2 })

# RECORDS (key-value pairs, like objects/dictionaries)
# Comparison with PowerShell:
#     NuShell:     let config = {name: "John", age: 30}
#     PowerShell:  $config = @{Name="John"; Age=30}
# NuShell is 42 chars, PowerShell is 37 chars (PowerShell shorter by 5)

let user_configuration = {
    user_name: "JohnDoe",
    email_address: "john@example.com",
    department: "Engineering",
    access_level: "Standard"
}

# RECORD ACCESS
let email = $user_configuration.email_address
let all_keys = ($user_configuration | columns)
let all_values = ($user_configuration | values)

# TABLES (primary data structure in NuShell)
# Tables are lists of records
let user_table = [
    {name: "Alice", age: 30, department: "Engineering"},
    {name: "Bob", age: 25, department: "Sales"},
    {name: "Charlie", age: 35, department: "Engineering"}
]

# TABLE OPERATIONS
$user_table | where department == "Engineering"
$user_table | select name age
$user_table | sort-by age
$user_table | reverse

# RANGES
let number_range = 1..10                 # Range from 1 to 10
let countdown = 10..1                    # Reverse range

# DATES AND DURATIONS
let current_date = (date now)
let tomorrow = ($current_date + 1day)
let one_hour = 1hr
let five_minutes = 5min

# FILE SIZES
let file_size = 1.5MB
let disk_space = 500GB

# ------------------------------------------------------------------------------
# OPERATORS
# ------------------------------------------------------------------------------

# ARITHMETIC OPERATORS
# Comparison with PowerShell:
#     NuShell:     10 + 5
#     PowerShell:  10 + 5
# Both: 7 chars (Tie)

let total_items = 10 + 5                 # Addition: 15
let items_remaining = 100 - 25           # Subtraction: 75
let product_price = 19.99 * 3            # Multiplication: 59.97
let average_score = 200 / 4              # Division: 50.0
let remainder_value = 17 mod 5           # Modulus: 2
let power_result = 2 ** 8                # Exponentiation: 256

# COMPARISON OPERATORS
# ==   : Equal to
# !=   : Not equal to
# >    : Greater than
# >=   : Greater than or equal to
# <    : Less than
# <=   : Less than or equal to
# =~   : Regex match
# !~   : Regex not match
# in   : Membership test
# not-in : Not in collection

let current_age = 25
let minimum_age = 18
let is_old_enough = ($current_age >= $minimum_age)

# STRING COMPARISONS (case-sensitive)
let entered_password = "Secret123"
let stored_password = "secret123"

if $entered_password == $stored_password {
    print "Passwords match"
} else {
    print "Passwords don't match (NuShell is case-sensitive!)"
}

# REGEX MATCHING
let log_entry = "2024-01-15 14:30:45 ERROR Connection timeout"

if $log_entry =~ "ERROR" {
    print "Log contains error"
}

# LOGICAL OPERATORS
# and  : Logical AND
# or   : Logical OR
# not  : Logical NOT

let has_valid_license = true
let has_current_subscription = true

if $has_valid_license and $has_current_subscription {
    print "Can access premium features"
}

# MEMBERSHIP
let available_colors = ["Red", "Green", "Blue", "Yellow"]
let has_green = ("Green" in $available_colors)

# ------------------------------------------------------------------------------
# CONTROL FLOW
# ------------------------------------------------------------------------------

# IF-ELSE STATEMENT
# NuShell uses braces and modern syntax
# Comparison with PowerShell:
#     NuShell:     if condition { } else { }
#     PowerShell:  if ($condition) { } else { }
# NuShell is 28 chars, PowerShell is 33 chars (NuShell shorter by 5)

let account_balance = 150.00
let minimum_balance = 100.00
let transaction_amount = 75.00

if $account_balance > ($minimum_balance + $transaction_amount) {
    print "Transaction approved. Sufficient funds."
    mut account_balance = ($account_balance - $transaction_amount)
} else if $account_balance > $transaction_amount {
    print "Transaction approved with warning."
    mut account_balance = ($account_balance - $transaction_amount)
} else {
    print "Transaction declined. Insufficient funds."
}

# MATCH EXPRESSION (pattern matching)
# More powerful than switch
let day_of_week = (date now | format date "%A")

match $day_of_week {
    "Monday" => { print "Start of work week" },
    "Friday" => { print "Last work day of the week" },
    "Saturday" | "Sunday" => { print "Weekend day" },
    _ => { print "Midweek day" }
}

# ------------------------------------------------------------------------------
# LOOPS
# ------------------------------------------------------------------------------

# FOR LOOP (each command)
# Comparison with PowerShell:
#     NuShell:     $array | each { |item| ... }
#     PowerShell:  $array | ForEach-Object { $_ ... }
# NuShell is 29 chars, PowerShell is 36 chars (NuShell shorter by 7)

let file_extensions = [".txt", ".log", ".csv", ".json"]

$file_extensions | each { |current_extension|
    print $"Processing files with extension: ($current_extension)"
}

# FOR LOOP WITH INDEX
$file_extensions | enumerate | each { |item|
    print $"[$($item.index)] ($item.item)"
}

# RANGE LOOP
1..10 | each { |number|
    print $"Number: ($number)"
}

# WHILE LOOP (loop with condition)
mut attempt_count = 0
let max_attempts = 5
mut connection_successful = false

loop {
    $attempt_count += 1
    print $"Connection attempt ($attempt_count) of ($max_attempts)"
    
    # Simulate connection
    $connection_successful = (random bool)
    
    if $connection_successful or ($attempt_count >= $max_attempts) {
        break
    }
    
    sleep 2sec
}

# FILTER (where command - like continue)
1..20 | where ($it mod 2 == 0) and ($it <= 15) | each { |current_number|
    print $"Processing even number: ($current_number)"
}

# ------------------------------------------------------------------------------
# FUNCTIONS (CUSTOM COMMANDS)
# ------------------------------------------------------------------------------

# FUNCTION DEFINITION
# Comparison with PowerShell:
#     NuShell:     def get-user-data [] { ... }
#     PowerShell:  function Get-UserData { ... }
# NuShell is 29 chars, PowerShell is 29 chars (Tie)

def get-user-account-status [
    user_name: string           # Required parameter with type
    --include-details           # Optional flag
]: string {                     # Return type
    print $"Checking account status for user: ($user_name)"
    
    let account_status = $"User: ($user_name), Active: true"
    
    if $include_details {
        $account_status + ", Details: Extended information"
    } else {
        $account_status
    }
}

# Call function
get-user-account-status "JohnDoe"
get-user-account-status "JohnDoe" --include-details

# FUNCTION WITH DEFAULT VALUES
def create-server-connection [
    server_name: string
    port_number: int = 443      # Default value
    protocol: string = "HTTPS"  # Default value
    --timeout: int = 30         # Optional with default
] {
    print $"Connecting to ($server_name):($port_number) using ($protocol)"
    print $"Timeout: ($timeout)s"
}

# Call function
create-server-connection "server01"
create-server-connection "server02" 8080 "HTTP" --timeout 60

# FUNCTION WITH REST PARAMETERS
def calculate-sum [...numbers: int]: int {
    $numbers | reduce { |number, acc| $acc + $number }
}

# Call function
let result = (calculate-sum 10 20 30 40)
print $"Sum: ($result)"

# CLOSURES (anonymous functions)
let square = { |x| $x * $x }
let result = (do $square 5)              # Result: 25

# ------------------------------------------------------------------------------
# STRING MANIPULATION
# ------------------------------------------------------------------------------

let original_text = "  Python Programming  "

# STRING LENGTH
let text_length = ($original_text | str length)

# SUBSTRING
let substring = ($original_text | str substring 2..8)

# STRING REPLACEMENT
let replaced_text = ($original_text | str replace "Python" "NuShell")
let all_replaced = ($original_text | str replace -a "m" "M")

# CASE CONVERSION
let lowercase_text = ($original_text | str downcase)
let uppercase_text = ($original_text | str upcase)

# TRIMMING
let trimmed_text = ($original_text | str trim)

# STRING SPLITTING
let server_list = "Server01,Server02,Server03"
let server_array = ($server_list | split row ",")

$server_array | each { |server|
    print $"Server: ($server)"
}

# STRING JOINING
let words = ["one", "two", "three"]
let joined_string = ($words | str join ",")

# STRING INTERPOLATION
let name = "Alice"
let age = 30
let greeting = $"Hello, ($name)! You are ($age) years old."

# ------------------------------------------------------------------------------
# PIPELINES (STRUCTURED DATA)
# ------------------------------------------------------------------------------

# STRUCTURED PIPELINE (the key feature of NuShell)
# Comparison with PowerShell:
#     NuShell:     ls | where size > 1MB | sort-by size
#     PowerShell:  Get-ChildItem | Where-Object {$_.Length -gt 1MB} | Sort-Object Length
# NuShell is 39 chars, PowerShell is 77 chars (NuShell much shorter!)

# List files, filter by size, sort
ls | where size > 1MB | sort-by size | reverse

# Process data in pipeline
[
    {name: "Alice", age: 30, salary: 75000},
    {name: "Bob", age: 25, salary: 65000},
    {name: "Charlie", age: 35, salary: 85000}
]
| where age > 26
| select name salary
| sort-by salary -r
| first 2

# PIPELINE COMMANDS
# where     : Filter rows
# select    : Choose columns
# sort-by   : Sort by column
# reverse   : Reverse order
# first     : Take first N
# last      : Take last N
# skip      : Skip N items
# take      : Take N items
# each      : Transform each item
# reduce    : Aggregate
# group-by  : Group rows
# merge     : Merge tables

# GROUPING AND AGGREGATION
$user_table
| group-by department
| transpose department employees
| insert count { |row| $row.employees | length }
| select department count

# ------------------------------------------------------------------------------
# FILE OPERATIONS
# ------------------------------------------------------------------------------

# READ FILE
let config_content = (open /etc/myapp/config.conf)

# READ STRUCTURED DATA
let json_data = (open data.json)        # Automatic parsing
let csv_data = (open data.csv)          # Returns table
let toml_config = (open config.toml)    # Returns record

# WRITE FILE
"Hello World" | save output.txt
$user_table | to json | save users.json

# FILE SYSTEM OPERATIONS
ls                                       # List directory
ls *.txt                                 # Glob pattern
cd /path/to/dir                          # Change directory
mkdir new_directory                      # Create directory
mv old.txt new.txt                       # Move/rename
cp source.txt dest.txt                   # Copy
rm file.txt                              # Remove

# ------------------------------------------------------------------------------
# ERROR HANDLING
# ------------------------------------------------------------------------------

# TRY-CATCH (error handling)
try {
    let content = (open /nonexistent/file.txt)
    print $content
} catch {
    print "Error: File not found"
}

# OPTIONAL VALUES (handling null)
let maybe_value = (
    $user_configuration | get optional_field? | default "default_value"
)

# ------------------------------------------------------------------------------
# ENVIRONMENT AND CONFIGURATION
# ------------------------------------------------------------------------------

# ENVIRONMENT VARIABLES
$env.PATH                                # Access env variable
$env.HOME
$env.USER

# SET ENVIRONMENT VARIABLE
$env.MY_VAR = "value"

# ADD TO PATH
$env.PATH = ($env.PATH | split row (char esep) | append "/usr/local/bin")

# CONFIGURATION FILE
# ~/.config/nushell/config.nu          - Main configuration
# ~/.config/nushell/env.nu             - Environment setup

# ALIASES
alias ll = ls -l
alias gs = git status
alias gp = git push

# ------------------------------------------------------------------------------
# PLUGINS AND EXTENSIBILITY
# ------------------------------------------------------------------------------

# PLUGINS (external programs that extend NuShell)
# Plugins provide new commands

# Register plugin
register /path/to/plugin

# Built-in plugin commands:
# - nu_plugin_query: Query structured data
# - nu_plugin_formats: Additional format support
# - nu_plugin_polars: DataFrame operations

# ------------------------------------------------------------------------------
# TABLE OPERATIONS (NUSHELL SPECIALTY)
# ------------------------------------------------------------------------------

# CREATE TABLE FROM SCRATCH
let employee_table = [
    [name, department, salary];
    ["Alice", "Engineering", 75000],
    ["Bob", "Sales", 65000],
    ["Charlie", "Engineering", 85000]
]

# TABLE TRANSFORMATIONS
$employee_table
| where salary > 70000
| insert bonus { |row| $row.salary * 0.1 }
| update salary { |row| $row.salary + $row.bonus }
| select name department salary

# PIVOT TABLE
$employee_table
| group-by department
| transpose department employees

# JOIN TABLES
let departments = [
    [dept_name, manager];
    ["Engineering", "Dr. Smith"],
    ["Sales", "Ms. Johnson"]
]

$employee_table
| join $departments dept_name department
| select name department manager salary

# ------------------------------------------------------------------------------
# CHARACTER COUNT COMPARISON SUMMARY
# ------------------------------------------------------------------------------

# NUSHELL vs POWERSHELL - Common Operations:
#
# 1. VARIABLE ASSIGNMENT
#    NuShell:     let user_name = "John"             (24 chars)
#    PowerShell:  $userName = "John"                 (22 chars)
#    Winner: PowerShell (2 chars shorter)
#
# 2. LIST CREATION
#    NuShell:     let names = ["A", "B", "C"]        (29 chars)
#    PowerShell:  $names = @("A", "B", "C")          (30 chars)
#    Winner: NuShell (1 char shorter)
#
# 3. FOR EACH LOOP
#    NuShell:     $array | each { |item| ... }       (29 chars)
#    PowerShell:  $array | ForEach-Object { $_ ... } (36 chars)
#    Winner: NuShell (7 chars shorter)
#
# 4. IF STATEMENT
#    NuShell:     if condition { }                   (18 chars)
#    PowerShell:  if ($condition) { }                (20 chars)
#    Winner: NuShell (2 chars shorter)
#
# 5. FUNCTION DEFINITION
#    NuShell:     def get-data [] { }                (21 chars)
#    PowerShell:  function Get-Data { }              (23 chars)
#    Winner: NuShell (2 chars shorter)
#
# 6. PIPELINE FILTERING
#    NuShell:     ls | where size > 1MB              (23 chars)
#    PowerShell:  Get-ChildItem | Where-Object...    (40+ chars)
#    Winner: NuShell (much shorter!)
#
# OVERALL: NuShell wins 5/6, PowerShell wins 1/6
# NuShell is generally shorter, especially for data operations
# NuShell's structured pipeline is more concise than PowerShell

# ------------------------------------------------------------------------------
# NUSHELL STYLE GUIDE
# ------------------------------------------------------------------------------

# NAMING CONVENTIONS:
# - Commands: kebab-case (get-user-data, create-server)
# - Variables: snake_case (user_name, server_list)
# - Constants: UPPER_SNAKE_CASE (MAX_RETRIES)

# INDENTATION:
# - 4 spaces (standard)
# - No tabs

# COMMENTS:
# - # for single-line comments
# - Document functions with parameter descriptions

# TYPES:
# - Use type annotations for function parameters
# - Leverage structural typing

# IMMUTABILITY:
# - Prefer 'let' over 'mut'
# - Use transformations instead of mutations

# ------------------------------------------------------------------------------
# NUSHELL ADVANTAGES OVER POWERSHELL
# ------------------------------------------------------------------------------

# 1. STRUCTURED DATA: Everything is typed, tables everywhere
# 2. CONCISE SYNTAX: Shorter, more readable commands
# 3. MODERN DESIGN: No legacy constraints, fresh start
# 4. CROSS-PLATFORM: Built for Linux/Mac/Windows from day one
# 5. PERFORMANCE: Written in Rust, fast execution
# 6. DATA ANALYSIS: Better for data manipulation tasks
# 7. PLUGIN SYSTEM: Extensible through plugins

# ------------------------------------------------------------------------------
# POWERSHELL ADVANTAGES OVER NUSHELL
# ------------------------------------------------------------------------------

# 1. MATURITY: Decades of development, stable
# 2. ECOSYSTEM: Huge library of cmdlets and modules
# 3. ENTERPRISE READY: Windows administration, Active Directory
# 4. .NET INTEGRATION: Full access to .NET framework
# 5. DISCOVERABILITY: Verb-Noun naming, comprehensive help
# 6. COMMUNITY: Large user base, extensive documentation
# 7. SCRIPTING: Better for complex automation scripts

# ==============================================================================
# END OF NUSHELL SYNTAX REFERENCE
# ==============================================================================

print "NuShell Syntax Reference - Complete Implementation"
print "Compare with PowerShell, Bash, Fish, and other shell references"
print "All examples use semantically meaningful variable names"
