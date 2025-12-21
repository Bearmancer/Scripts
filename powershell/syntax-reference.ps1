<#
.SYNOPSIS
    Comprehensive PowerShell Syntax Reference with Detailed Explanations

.DESCRIPTION
    This file serves as a complete reference guide to PowerShell syntax, covering:
    - All keywords, operators, and symbols
    - Indentation and formatting conventions
    - Design principles and philosophy
    - Style guide recommendations
    - Comparison notes with other languages
    
    PowerShell Design Philosophy:
    - Object-oriented pipeline (passes .NET objects, not text)
    - Verb-Noun naming convention for discoverability
    - Consistent parameter naming across cmdlets
    - Built on .NET framework with full access to .NET classes
    - Case-insensitive by default
    - Designed for system administration and automation
    
.NOTES
    All examples use semantically meaningful variable names for clarity.
#>

#region VARIABLES_AND_DATA_TYPES
<#
    VARIABLE DECLARATION AND SCOPING
    
    PowerShell variables are prefixed with $ and are dynamically typed by default.
    They can be strongly typed using type constraints in square brackets.
    
    Scoping:
    - $variable          : Local scope (current scope only)
    - $Script:variable   : Script scope (entire script file)
    - $Global:variable   : Global scope (entire session)
    - $Private:variable  : Private scope (current scope, not inherited)
    - $using:variable    : Used in remote sessions and parallel foreach
#>

# BASIC VARIABLE DECLARATION (no type constraint, dynamically typed)
$userName = "JohnDoe"                    # String type inferred
$userAge = 30                            # Integer type inferred
$accountBalance = 1234.56                # Double type inferred
$isAccountActive = $true                 # Boolean type

# STRONGLY TYPED VARIABLES (type constraint in square brackets)
[string]$emailAddress = "john@example.com"
[int]$loginAttempts = 0
[double]$transactionAmount = 99.99
[bool]$hasAdminPrivileges = $false
[datetime]$accountCreatedDate = Get-Date

# SPECIAL AUTOMATIC VARIABLES (read-only, populated by PowerShell)
# $PSVersionTable    - PowerShell version information
# $_                 - Current object in pipeline (also $PSItem)
# $args              - Array of unbound parameters
# $Error             - Array of most recent errors
# $LASTEXITCODE      - Exit code of last native executable
# $PSScriptRoot      - Directory containing the current script
# $PWD               - Current working directory
# $null              - Represents absence of value

# ARRAYS (collections indexed from 0)
$serverNames = @("WebServer01", "WebServer02", "DatabaseServer01")
$portNumbers = @(80, 443, 8080, 8443)
$mixedTypeArray = @("text", 123, $true, (Get-Date))

# HASH TABLES / DICTIONARIES (key-value pairs)
$userConfiguration = @{
    UserName        = "JohnDoe"
    EmailAddress    = "john@example.com"
    Department      = "Engineering"
    AccessLevel     = "Standard"
    AccountCreated  = Get-Date
}

# ORDERED HASH TABLES (preserves insertion order)
$serverConfiguration = [ordered]@{
    ServerName      = "WebServer01"
    IpAddress       = "192.168.1.100"
    OperatingSystem = "Windows Server 2022"
    Role            = "Web"
}

# CUSTOM OBJECTS (PSCustomObject for structured data)
$employeeRecord = [PSCustomObject]@{
    EmployeeId      = 12345
    FullName        = "John Doe"
    Department      = "Engineering"
    HireDate        = Get-Date "2020-01-15"
    Salary          = 75000
    IsFullTime      = $true
}

#endregion

#region OPERATORS
<#
    ARITHMETIC OPERATORS
    + (addition)
    - (subtraction)
    * (multiplication)
    / (division)
    % (modulus/remainder)
    
    PowerShell performs type coercion automatically.
#>

$totalItems = 10 + 5                     # Result: 15
$itemsRemaining = 100 - 25               # Result: 75
$productPrice = 19.99 * 3                # Result: 59.97
$averageScore = 200 / 4                  # Result: 50
$remainderValue = 17 % 5                 # Result: 2

<#
    COMPARISON OPERATORS (case-insensitive by default)
    
    -eq  : Equal to
    -ne  : Not equal to
    -gt  : Greater than
    -ge  : Greater than or equal to
    -lt  : Less than
    -le  : Less than or equal to
    
    Add 'c' prefix for case-sensitive: -ceq, -cne, -cgt, etc.
    Add 'i' prefix for explicit case-insensitive: -ieq, -ine, etc.
#>

$currentAge = 25
$minimumAge = 18
$isOldEnough = $currentAge -ge $minimumAge      # Result: $true

$enteredPassword = "Secret123"
$storedPassword = "secret123"
$passwordMatches = $enteredPassword -eq $storedPassword        # Result: $true (case-insensitive)
$exactPasswordMatch = $enteredPassword -ceq $storedPassword    # Result: $false (case-sensitive)

<#
    LOGICAL OPERATORS
    
    -and : Logical AND
    -or  : Logical OR
    -not : Logical NOT (can also use ! prefix)
    -xor : Logical exclusive OR
#>

$hasValidLicense = $true
$hasCurrentSubscription = $true
$canAccessPremiumFeatures = $hasValidLicense -and $hasCurrentSubscription

$isWeekend = $false
$isHoliday = $false
$isDayOff = $isWeekend -or $isHoliday

$serviceIsRunning = $true
$needsRestart = -not $serviceIsRunning          # Also valid: !$serviceIsRunning

<#
    STRING OPERATORS
    
    -like     : Wildcard matching (*, ?)
    -notlike  : Negated wildcard matching
    -match    : Regular expression matching
    -notmatch : Negated regex matching
    -contains : Collection contains item
    -in       : Item in collection
    -replace  : String replacement (regex-based)
    -split    : Split string into array
    -join     : Join array into string
#>

$fileName = "report_2024_Q1.xlsx"
$isReportFile = $fileName -like "report_*.xlsx"        # Result: $true

$logEntry = "ERROR: Connection timeout at 10:45 AM"
$containsError = $logEntry -match "ERROR:"              # Result: $true

$availableColors = @("Red", "Green", "Blue", "Yellow")
$hasGreenOption = $availableColors -contains "Green"    # Result: $true
$isValidColor = "Blue" -in $availableColors             # Result: $true

# STRING MANIPULATION
$serverList = "Server01,Server02,Server03"
$serverArray = $serverList -split ","                   # Split into array
$rejoinedList = $serverArray -join "; "                 # Join with semicolon

$originalText = "The quick brown fox"
$replacedText = $originalText -replace "brown", "red"   # Regex replacement

<#
    TYPE OPERATORS
    
    -is     : Test if object is of specified type
    -isnot  : Test if object is not of specified type
    -as     : Convert to specified type (returns null if conversion fails)
#>

$numericValue = 42
$isInteger = $numericValue -is [int]                    # Result: $true
$isNotString = $numericValue -isnot [string]            # Result: $true

$textNumber = "123"
$convertedNumber = $textNumber -as [int]                # Result: 123 (integer)

<#
    ASSIGNMENT OPERATORS
    
    =   : Simple assignment
    +=  : Add and assign
    -=  : Subtract and assign
    *=  : Multiply and assign
    /=  : Divide and assign
    %=  : Modulus and assign
#>

$totalScore = 0
$totalScore += 10                                        # Equivalent to: $totalScore = $totalScore + 10
$totalScore *= 2                                         # Multiply by 2 and assign

<#
    RANGE OPERATOR
    
    .. : Creates a range of integers
#>

$numberSequence = 1..10                                  # Array: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10
$countdownSequence = 10..1                               # Reverse: 10, 9, 8, 7, 6, 5, 4, 3, 2, 1

#endregion

#region CONTROL_FLOW
<#
    IF-ELSEIF-ELSE STATEMENT
    
    Syntax:
        if (condition) {
            # Code block when condition is true
        }
        elseif (another_condition) {
            # Code block when another_condition is true
        }
        else {
            # Code block when all conditions are false
        }
    
    Indentation: 4 spaces (standard convention)
    Braces: Opening brace on same line as statement (K&R style)
#>

$accountBalance = 150.00
$minimumBalance = 100.00
$transactionAmount = 75.00

if ($accountBalance -gt ($minimumBalance + $transactionAmount)) {
    Write-Host "Transaction approved. Sufficient funds available."
    $accountBalance -= $transactionAmount
}
elseif ($accountBalance -gt $transactionAmount) {
    Write-Host "Transaction approved with warning. Balance approaching minimum."
    $accountBalance -= $transactionAmount
}
else {
    Write-Host "Transaction declined. Insufficient funds."
}

<#
    SWITCH STATEMENT
    
    PowerShell switch is more powerful than many languages:
    - Can match against strings, numbers, wildcards, regex
    - Can process arrays
    - Supports multiple conditions per case
    - Has 'default' for unmatched cases
#>

$dayOfWeek = (Get-Date).DayOfWeek

switch ($dayOfWeek) {
    "Monday" {
        Write-Host "Start of work week"
    }
    "Friday" {
        Write-Host "Last work day of the week"
    }
    { $_ -in @("Saturday", "Sunday") } {
        Write-Host "Weekend day"
    }
    default {
        Write-Host "Midweek day"
    }
}

# SWITCH WITH WILDCARD MATCHING
$logLevel = "ERROR_CRITICAL"

switch -Wildcard ($logLevel) {
    "INFO*" {
        Write-Host "Informational message"
    }
    "WARN*" {
        Write-Host "Warning message"
    }
    "ERROR*" {
        Write-Host "Error message - requires attention"
    }
    default {
        Write-Host "Unknown log level"
    }
}

<#
    FOR LOOP
    
    Traditional C-style loop with initialization, condition, and iterator.
#>

# Iterate through array indices
$serverNames = @("WebServer01", "DatabaseServer01", "CacheServer01")

for ($serverIndex = 0; $serverIndex -lt $serverNames.Count; $serverIndex++) {
    $currentServerName = $serverNames[$serverIndex]
    Write-Host "Processing server $($serverIndex + 1) of $($serverNames.Count): $currentServerName"
}

<#
    FOREACH LOOP
    
    Simpler iteration over collections. Two forms:
    1. foreach statement (control flow)
    2. ForEach-Object cmdlet (pipeline)
#>

# FOREACH STATEMENT (faster for arrays, doesn't support pipeline)
$fileExtensions = @(".txt", ".log", ".csv", ".json")

foreach ($currentExtension in $fileExtensions) {
    Write-Host "Processing files with extension: $currentExtension"
}

# FOREACH-OBJECT CMDLET (works in pipeline, more flexible)
Get-Process | ForEach-Object {
    Write-Host "Process: $($_.Name) - ID: $($_.Id) - Memory: $($_.WorkingSet64)"
}

# FOREACH-OBJECT with -Parallel (PowerShell 7+)
1..10 | ForEach-Object -Parallel {
    $jobNumber = $_
    Write-Host "Processing job $jobNumber in parallel"
    Start-Sleep -Seconds 1
} -ThrottleLimit 5

<#
    WHILE LOOP
    
    Executes while condition is true (condition checked before each iteration).
#>

$attemptCount = 0
$maxAttempts = 5
$connectionSuccessful = $false

while (-not $connectionSuccessful -and $attemptCount -lt $maxAttempts) {
    $attemptCount++
    Write-Host "Connection attempt $attemptCount of $maxAttempts"
    
    # Simulate connection attempt
    $connectionSuccessful = Get-Random -Minimum 0 -Maximum 2
    
    if (-not $connectionSuccessful) {
        Start-Sleep -Seconds 2
    }
}

<#
    DO-WHILE / DO-UNTIL LOOPS
    
    do-while: Executes at least once, continues while condition is true
    do-until: Executes at least once, continues until condition is true
#>

# DO-WHILE (continues while condition is true)
$retryCount = 0
do {
    $retryCount++
    Write-Host "Retry attempt: $retryCount"
} while ($retryCount -lt 3)

# DO-UNTIL (continues until condition becomes true)
$confirmationReceived = $false
$promptCount = 0
do {
    $promptCount++
    Write-Host "Waiting for confirmation (attempt $promptCount)"
    $confirmationReceived = Get-Random -Minimum 0 -Maximum 2
    Start-Sleep -Milliseconds 500
} until ($confirmationReceived -or $promptCount -ge 5)

<#
    BREAK AND CONTINUE
    
    break    : Exits the current loop immediately
    continue : Skips remaining code in current iteration, continues to next
#>

# Process only even numbers, stop at 15
foreach ($currentNumber in 1..20) {
    if ($currentNumber % 2 -ne 0) {
        continue    # Skip odd numbers
    }
    
    if ($currentNumber -gt 15) {
        break       # Stop processing after 15
    }
    
    Write-Host "Processing even number: $currentNumber"
}

#endregion

#region FUNCTIONS
<#
    FUNCTION DEFINITION
    
    PowerShell functions follow verb-noun naming convention.
    Approved verbs: Get, Set, New, Remove, Test, Invoke, etc.
    
    Function components:
    - CmdletBinding: Enables advanced function features
    - Parameters: Defined in param() block
    - Begin/Process/End blocks: For pipeline processing
    - Return: Explicit return (or implicit output)
#>

function Get-UserAccountStatus {
    <#
    .SYNOPSIS
        Retrieves account status information for a user.
    
    .DESCRIPTION
        Checks various aspects of a user account including:
        - Account existence
        - Active/disabled status
        - Last login time
        - Password expiration
    
    .PARAMETER UserName
        The username to check. Mandatory parameter.
    
    .PARAMETER IncludeDetails
        Switch parameter to include detailed information.
    
    .EXAMPLE
        Get-UserAccountStatus -UserName "jdoe"
        
        Retrieves basic status for user jdoe.
    
    .EXAMPLE
        Get-UserAccountStatus -UserName "jdoe" -IncludeDetails
        
        Retrieves detailed status for user jdoe.
    
    .OUTPUTS
        PSCustomObject with account status information.
    #>
    
    [CmdletBinding()]    # Enables advanced function features
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string]$UserName,
        
        [Parameter(Mandatory = $false)]
        [switch]$IncludeDetails
    )
    
    # Function body
    Write-Verbose "Checking account status for user: $UserName"
    
    # Create result object
    $accountStatus = [PSCustomObject]@{
        UserName      = $UserName
        AccountExists = $true
        IsActive      = $true
        LastLogin     = (Get-Date).AddDays(-5)
        CheckedAt     = Get-Date
    }
    
    if ($IncludeDetails) {
        Add-Member -InputObject $accountStatus -MemberType NoteProperty -Name "DetailedInfo" -Value "Extended account information"
    }
    
    # Return object (implicitly output to pipeline)
    return $accountStatus
}

<#
    PARAMETER VALIDATION ATTRIBUTES
    
    PowerShell provides built-in parameter validation:
    - ValidateNotNull
    - ValidateNotNullOrEmpty
    - ValidatePattern (regex)
    - ValidateRange (numeric range)
    - ValidateSet (allowed values)
    - ValidateScript (custom validation)
    - ValidateCount (array length)
    - ValidateLength (string length)
#>

function New-ServerConnection {
    [CmdletBinding()]
    param(
        # Must not be null or empty
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$ServerName,
        
        # Must be between 1 and 65535
        [Parameter(Mandatory = $false)]
        [ValidateRange(1, 65535)]
        [int]$PortNumber = 443,
        
        # Must be one of these values
        [Parameter(Mandatory = $false)]
        [ValidateSet("HTTP", "HTTPS", "FTP", "SFTP")]
        [string]$Protocol = "HTTPS",
        
        # Must match IP address pattern
        [Parameter(Mandatory = $false)]
        [ValidatePattern("^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$")]
        [string]$IpAddress,
        
        # Custom validation script
        [Parameter(Mandatory = $false)]
        [ValidateScript({ Test-Path $_ })]
        [string]$CertificatePath
    )
    
    Write-Host "Connecting to $ServerName on port $PortNumber using $Protocol"
}

<#
    PARAMETER SETS
    
    Allows functions to have different parameter combinations.
    Only one parameter set can be used at a time.
#>

function Connect-RemoteSystem {
    [CmdletBinding(DefaultParameterSetName = "ByComputerName")]
    param(
        [Parameter(Mandatory = $true, ParameterSetName = "ByComputerName")]
        [string]$ComputerName,
        
        [Parameter(Mandatory = $true, ParameterSetName = "ByIpAddress")]
        [string]$IpAddress,
        
        [Parameter(Mandatory = $true, ParameterSetName = "BySessionId")]
        [int]$SessionId,
        
        [Parameter(Mandatory = $false)]
        [PSCredential]$Credential
    )
    
    switch ($PSCmdlet.ParameterSetName) {
        "ByComputerName" {
            Write-Host "Connecting to computer: $ComputerName"
        }
        "ByIpAddress" {
            Write-Host "Connecting to IP: $IpAddress"
        }
        "BySessionId" {
            Write-Host "Connecting to session: $SessionId"
        }
    }
}

<#
    PIPELINE FUNCTIONS (Begin/Process/End blocks)
    
    Begin:   Executed once before processing pipeline input
    Process: Executed once for each pipeline input object
    End:     Executed once after all pipeline input is processed
#>

function Measure-StringLength {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [string]$InputString
    )
    
    begin {
        Write-Verbose "Starting string length measurement"
        $totalLength = 0
        $stringCount = 0
    }
    
    process {
        $currentLength = $InputString.Length
        $totalLength += $currentLength
        $stringCount++
        
        [PSCustomObject]@{
            String = $InputString
            Length = $currentLength
        }
    }
    
    end {
        Write-Verbose "Processed $stringCount strings with total length: $totalLength"
        Write-Host "Average length: $($totalLength / $stringCount)"
    }
}

# Usage: "Hello", "World", "PowerShell" | Measure-StringLength

#endregion

#region ERROR_HANDLING
<#
    TRY-CATCH-FINALLY
    
    PowerShell's exception handling mechanism:
    - try: Code that might throw an exception
    - catch: Handles specific or general exceptions
    - finally: Always executes, regardless of exception
    
    Error types:
    - Terminating errors: Stop execution (throw, cmdlet with -ErrorAction Stop)
    - Non-terminating errors: Continue execution (default for most cmdlets)
#>

function Read-ConfigurationFile {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConfigFilePath
    )
    
    try {
        Write-Verbose "Attempting to read configuration file: $ConfigFilePath"
        
        # This will throw if file doesn't exist
        $configurationContent = Get-Content -Path $ConfigFilePath -ErrorAction Stop
        
        # Simulate parsing
        if ([string]::IsNullOrWhiteSpace($configurationContent)) {
            throw "Configuration file is empty"
        }
        
        Write-Host "Configuration loaded successfully"
        return $configurationContent
    }
    catch [System.IO.FileNotFoundException] {
        Write-Error "Configuration file not found at: $ConfigFilePath"
        return $null
    }
    catch [System.UnauthorizedAccessException] {
        Write-Error "Access denied to configuration file: $ConfigFilePath"
        return $null
    }
    catch {
        # Generic catch for all other exceptions
        Write-Error "Unexpected error reading configuration: $($_.Exception.Message)"
        Write-Verbose "Error details: $($_.Exception.GetType().FullName)"
        return $null
    }
    finally {
        # Always executes, whether exception occurred or not
        Write-Verbose "Completed configuration file read attempt"
    }
}

<#
    THROW STATEMENT
    
    Explicitly throws an exception to stop execution.
#>

function Set-DatabaseConnection {
    param(
        [string]$ConnectionString
    )
    
    if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
        throw "ConnectionString cannot be null or empty"
    }
    
    Write-Host "Connection established"
}

<#
    ERROR PREFERENCE VARIABLES
    
    $ErrorActionPreference: Controls default behavior for non-terminating errors
    - Continue (default): Display error and continue
    - Stop: Treat as terminating error
    - SilentlyContinue: Suppress error and continue
    - Inquire: Prompt user
    
    -ErrorAction parameter: Overrides $ErrorActionPreference for specific cmdlet
#>

# Set preference for current scope
$ErrorActionPreference = "Stop"

# Override for specific cmdlet
Get-Process -Name "NonExistentProcess" -ErrorAction SilentlyContinue

#endregion

#region SCRIPT_BLOCKS_AND_CLOSURES
<#
    SCRIPT BLOCKS
    
    First-class functions represented by curly braces { }.
    Can be stored in variables, passed as parameters, invoked with & or .Invoke()
#>

# Define script block
$calculationBlock = {
    param($firstNumber, $secondNumber)
    return $firstNumber + $secondNumber
}

# Invoke script block using call operator (&)
$sumResult = & $calculationBlock 10 20    # Result: 30

# Invoke using .Invoke() method
$productResult = $calculationBlock.Invoke(5, 6)

<#
    CLOSURES
    
    Script blocks can capture variables from their defining scope.
#>

function New-Counter {
    param([int]$InitialValue = 0)
    
    $currentCount = $InitialValue
    
    # Return script block that captures $currentCount
    return {
        $currentCount++
        return $currentCount
    }
}

$counter1 = New-Counter -InitialValue 0
$counter2 = New-Counter -InitialValue 100

& $counter1    # Result: 1
& $counter1    # Result: 2
& $counter2    # Result: 101
& $counter2    # Result: 102

#endregion

#region OBJECT_ORIENTED_FEATURES
<#
    CLASS DEFINITION (PowerShell 5.0+)
    
    PowerShell supports class-based object-oriented programming:
    - Classes with properties and methods
    - Constructors (overloaded)
    - Inheritance
    - Static members
    - Enumerations
#>

# ENUMERATION
enum AccountType {
    Standard
    Premium
    Enterprise
}

enum TransactionStatus {
    Pending
    Approved
    Declined
    Cancelled
}

# CLASS DEFINITION
class BankAccount {
    # PROPERTIES
    [string]$AccountNumber
    [string]$AccountHolderName
    [AccountType]$Type
    [double]$Balance
    [datetime]$CreatedDate
    [bool]$IsActive
    
    # STATIC PROPERTY
    static [int]$TotalAccountsCreated = 0
    
    # DEFAULT CONSTRUCTOR
    BankAccount() {
        $this.CreatedDate = Get-Date
        $this.IsActive = $true
        [BankAccount]::TotalAccountsCreated++
    }
    
    # PARAMETERIZED CONSTRUCTOR
    BankAccount([string]$accountNumber, [string]$holderName, [AccountType]$accountType) {
        $this.AccountNumber = $accountNumber
        $this.AccountHolderName = $holderName
        $this.Type = $accountType
        $this.Balance = 0.0
        $this.CreatedDate = Get-Date
        $this.IsActive = $true
        [BankAccount]::TotalAccountsCreated++
    }
    
    # INSTANCE METHODS
    [void]Deposit([double]$depositAmount) {
        if ($depositAmount -le 0) {
            throw "Deposit amount must be positive"
        }
        
        if (-not $this.IsActive) {
            throw "Cannot deposit to inactive account"
        }
        
        $this.Balance += $depositAmount
        Write-Host "Deposited $depositAmount. New balance: $($this.Balance)"
    }
    
    [TransactionStatus]Withdraw([double]$withdrawalAmount) {
        if ($withdrawalAmount -le 0) {
            throw "Withdrawal amount must be positive"
        }
        
        if (-not $this.IsActive) {
            return [TransactionStatus]::Declined
        }
        
        if ($this.Balance -ge $withdrawalAmount) {
            $this.Balance -= $withdrawalAmount
            Write-Host "Withdrew $withdrawalAmount. New balance: $($this.Balance)"
            return [TransactionStatus]::Approved
        }
        else {
            Write-Host "Insufficient funds for withdrawal"
            return [TransactionStatus]::Declined
        }
    }
    
    [string]GetAccountSummary() {
        return "Account: $($this.AccountNumber) | Holder: $($this.AccountHolderName) | Balance: $($this.Balance) | Type: $($this.Type)"
    }
    
    # STATIC METHOD
    static [int]GetTotalAccounts() {
        return [BankAccount]::TotalAccountsCreated
    }
}

# INHERITANCE
class SavingsAccount : BankAccount {
    [double]$InterestRate
    [int]$MinimumBalance
    
    SavingsAccount([string]$accountNumber, [string]$holderName, [double]$interestRate) : base($accountNumber, $holderName, [AccountType]::Standard) {
        $this.InterestRate = $interestRate
        $this.MinimumBalance = 100
    }
    
    [void]ApplyInterest() {
        $interestAmount = $this.Balance * ($this.InterestRate / 100)
        $this.Balance += $interestAmount
        Write-Host "Applied interest: $interestAmount. New balance: $($this.Balance)"
    }
}

# USING CLASSES
$checkingAccount = [BankAccount]::new("CHK-12345", "John Doe", [AccountType]::Premium)
$checkingAccount.Deposit(1000.00)
$withdrawalStatus = $checkingAccount.Withdraw(250.00)

$savingsAccount = [SavingsAccount]::new("SAV-67890", "Jane Smith", 2.5)
$savingsAccount.Deposit(5000.00)
$savingsAccount.ApplyInterest()

Write-Host "Total accounts created: $([BankAccount]::GetTotalAccounts())"

#endregion

#region MODULES_AND_SCOPE
<#
    MODULES
    
    PowerShell code is organized into modules (.psm1 files):
    - Script modules: .psm1 files with functions
    - Manifest modules: .psd1 files describing module
    - Binary modules: Compiled .NET assemblies
    
    Module structure:
    - Public functions (exported)
    - Private functions (internal use)
    - Module-scoped variables
#>

# Example module structure would be in separate .psm1 file
# This demonstrates module concepts

# EXPORT FUNCTIONS (in module manifest or using Export-ModuleMember)
# Export-ModuleMember -Function Get-*, Set-*, New-*
# Export-ModuleMember -Alias *

# IMPORT MODULE
# Import-Module -Name ModuleName

# MODULE AUTO-LOADING (PowerShell 3.0+)
# Modules in $env:PSModulePath are automatically loaded on first use

#endregion

#region ADVANCED_TOPICS

<#
    SPLATTING
    
    Technique for passing multiple parameters using a hash table.
    Makes code more readable when many parameters are needed.
#>

$connectionParameters = @{
    ComputerName  = "Server01"
    Credential    = Get-Credential
    Port          = 5986
    UseSSL        = $true
    Authentication = "Kerberos"
}

# Splat parameters using @
Invoke-Command @connectionParameters -ScriptBlock { Get-Process }

<#
    HERE-STRINGS
    
    Multi-line strings preserving formatting.
    @ and closing @ must be on their own lines.
#>

$emailTemplate = @"
Dear $userName,

Your account has been successfully created.

Account Details:
  Account Number: $accountNumber
  Created Date: $accountCreatedDate
  Status: Active

Thank you for choosing our service.
"@

# Single-quoted here-string (no variable expansion)
$literalTemplate = @'
This is literal text.
$variables are not expanded.
'@

<#
    PIPELINE ADVANCED FEATURES
    
    PowerShell pipeline passes objects, not text (unlike Unix shells).
    Objects preserve type information and properties.
#>

# Pipeline with Where-Object and ForEach-Object
Get-Process | 
    Where-Object { $_.WorkingSet64 -gt 100MB } |
    ForEach-Object { 
        [PSCustomObject]@{
            ProcessName = $_.Name
            MemoryMB = [math]::Round($_.WorkingSet64 / 1MB, 2)
        }
    } |
    Sort-Object -Property MemoryMB -Descending |
    Select-Object -First 10

<#
    REGULAR EXPRESSIONS
    
    PowerShell uses .NET regex engine (very powerful).
    Available through -match operator and [regex] type.
#>

$logEntry = "2024-01-15 14:30:45 ERROR Connection timeout (Server: 192.168.1.100)"

# Simple match
if ($logEntry -match "ERROR") {
    Write-Host "Found error in log entry"
}

# Capture groups
if ($logEntry -match "(\d{4}-\d{2}-\d{2}) (\d{2}:\d{2}:\d{2}) (\w+)") {
    $entryDate = $matches[1]
    $entryTime = $matches[2]
    $logLevel = $matches[3]
    Write-Host "Date: $entryDate, Time: $entryTime, Level: $logLevel"
}

# Named capture groups
$ipPattern = "Server: (?<ipaddress>\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})"
if ($logEntry -match $ipPattern) {
    $serverIp = $matches['ipaddress']
    Write-Host "Server IP: $serverIp"
}

<#
    JOBS AND BACKGROUND PROCESSING
    
    PowerShell supports parallel execution through jobs:
    - Start-Job: Background job (separate process)
    - Invoke-Command -AsJob: Remote job
    - Start-ThreadJob: Thread-based job (faster startup)
#>

# Start background job
$backgroundJob = Start-Job -ScriptBlock {
    $processCount = (Get-Process).Count
    Start-Sleep -Seconds 5
    return $processCount
}

# Wait for job completion
$jobResult = Wait-Job $backgroundJob | Receive-Job
Write-Host "Total processes: $jobResult"

# Clean up
Remove-Job $backgroundJob

<#
    REMOTING
    
    PowerShell Remoting uses WS-Management protocol (WinRM).
    Allows executing commands on remote computers.
#>

# Single command on remote computer
Invoke-Command -ComputerName "Server01" -ScriptBlock {
    Get-Service | Where-Object { $_.Status -eq "Running" }
}

# Interactive session
# $remoteSession = New-PSSession -ComputerName "Server01"
# Enter-PSSession $remoteSession
# Exit-PSSession
# Remove-PSSession $remoteSession

#endregion

#region COMPARISON_WITH_OTHER_LANGUAGES
<#
    POWERSHELL vs PYTHON vs C#
    
    === Character Count Comparison for Common Operations ===
    
    1. VARIABLE ASSIGNMENT
       PowerShell: $userName = "John"              (22 chars)
       Python:     user_name = "John"              (21 chars)
       C#:         string userName = "John";       (28 chars)
       Winner: Python (shortest)
    
    2. ARRAY/LIST CREATION
       PowerShell: $items = @(1, 2, 3)             (23 chars)
       Python:     items = [1, 2, 3]               (20 chars)
       C#:         var items = new[] {1, 2, 3};    (32 chars)
       Winner: Python (shortest)
    
    3. DICTIONARY/HASH TABLE
       PowerShell: $dict = @{Name="John"}          (26 chars)
       Python:     dict = {"Name": "John"}         (26 chars)
       C#:         var dict = new Dictionary...    (60+ chars)
       Winner: Tie (PowerShell/Python)
    
    4. FUNCTION DEFINITION (simple)
       PowerShell: function Get-Data {...}         (27 chars + body)
       Python:     def get_data(): ...             (19 chars + body)
       C#:         public void GetData() {...}     (30 chars + body)
       Winner: Python (shortest)
    
    5. LOOP THROUGH ARRAY
       PowerShell: foreach ($item in $array)       (29 chars)
       Python:     for item in array:              (21 chars)
       C#:         foreach (var item in array)     (31 chars)
       Winner: Python (shortest)
    
    6. CONDITIONAL STATEMENT
       PowerShell: if ($x -eq 5)                   (13 chars)
       Python:     if x == 5:                      (10 chars)
       C#:         if (x == 5)                     (11 chars)
       Winner: Python (shortest)
    
    === Design Philosophy Comparison ===
    
    POWERSHELL:
    - Object-oriented pipeline (passes .NET objects)
    - Verb-Noun naming convention for discoverability
    - Designed for system administration
    - Case-insensitive
    - Rich cmdlet ecosystem
    - Direct .NET framework integration
    
    PYTHON:
    - General-purpose, multi-paradigm
    - "Batteries included" philosophy
    - Emphasis on readability ("There should be one obvious way")
    - Snake_case naming convention
    - Indentation-based syntax (no braces)
    - Extensive third-party packages (pip)
    
    C#:
    - Strongly typed, object-oriented
    - Compiled language (JIT compilation)
    - Full .NET framework/Core integration
    - PascalCase naming convention
    - Explicit type declarations
    - Enterprise-grade tooling and IDE support
    
    === Key Syntactic Differences ===
    
    VARIABLES:
    - PowerShell: $variable (sigil required)
    - Python:     variable (no sigil)
    - C#:         variable (with type declaration)
    
    COMMENTING:
    - PowerShell: # single line, <# #> block
    - Python:     # single line, ''' ''' docstring
    - C#:         // single line, /* */ block, /// XML doc
    
    INDENTATION:
    - PowerShell: 4 spaces (convention, not enforced)
    - Python:     4 spaces (enforced by syntax)
    - C#:         4 spaces (convention, not enforced)
    
    BOOLEAN VALUES:
    - PowerShell: $true, $false
    - Python:     True, False
    - C#:         true, false
    
    NULL/NONE:
    - PowerShell: $null
    - Python:     None
    - C#:         null
    
    COMPARISON OPERATORS:
    - PowerShell: -eq, -ne, -gt, -lt (English words)
    - Python:     ==, !=, >, <       (Symbols)
    - C#:         ==, !=, >, <       (Symbols)
    
    LOGICAL OPERATORS:
    - PowerShell: -and, -or, -not    (English words)
    - Python:     and, or, not       (English words)
    - C#:         &&, ||, !           (Symbols)
    
    === Performance Characteristics ===
    
    STARTUP TIME:
    - PowerShell: Slower (loads .NET framework)
    - Python:     Fast (lightweight interpreter)
    - C#:         Fast once compiled (compilation overhead)
    
    EXECUTION SPEED:
    - PowerShell: Moderate (interpreted, but .NET JIT)
    - Python:     Moderate (interpreted)
    - C#:         Fast (compiled to native code)
    
    MEMORY USAGE:
    - PowerShell: Higher (.NET framework overhead)
    - Python:     Lower (lightweight runtime)
    - C#:         Moderate to high (depends on app)
#>

#endregion

#region STYLE_GUIDE_SUMMARY
<#
    POWERSHELL STYLE GUIDE BEST PRACTICES
    
    NAMING CONVENTIONS:
    - Functions: PascalCase with Verb-Noun pattern (Get-Process, Set-Item)
    - Variables: camelCase for local ($userName, $itemCount)
    - Constants: ALL_CAPS with underscores (rarely used, prefer [const])
    - Private functions: Use verb-noun but don't export
    - Parameters: PascalCase
    
    FORMATTING:
    - Indentation: 4 spaces (no tabs)
    - Line length: 115 characters maximum (recommendation)
    - Braces: Opening brace on same line (K&R style)
    - Operators: Spaces around operators ($a -eq $b, not $a-eq$b)
    - Parameter alignment: Align parameters vertically for readability
    
    COMMENTS:
    - Use comment-based help for functions (.SYNOPSIS, .DESCRIPTION, etc.)
    - Single-line comments: # before the line
    - Block comments: <# #> for multi-line
    - Document complex logic, not obvious code
    
    ERROR HANDLING:
    - Use try-catch for expected exceptions
    - Use -ErrorAction parameter to control cmdlet behavior
    - Always clean up resources in finally block
    - Throw meaningful error messages
    
    FUNCTIONS:
    - Use [CmdletBinding()] for advanced features
    - Validate parameters with attributes
    - Support pipeline input when appropriate
    - Include comment-based help
    - Return strongly-typed objects
    
    VARIABLES:
    - Use meaningful descriptive names (not $x, $temp)
    - Declare scope explicitly when needed ($Script:, $Global:)
    - Initialize variables before use
    - Use strongly-typed variables for clarity [type]$variable
    
    PIPELINE:
    - Prefer pipeline over loops when appropriate
    - Use Where-Object and ForEach-Object for filtering/transformation
    - Chain cmdlets for readability
    - Format at the end of pipeline (Format-Table, Format-List)
    
    PERFORMANCE:
    - Use .NET methods for intensive operations
    - Prefer ArrayList over += for array building
    - Use foreach statement over ForEach-Object for large collections
    - Profile code with Measure-Command
    
    SECURITY:
    - Never store passwords in plain text
    - Use SecureString for sensitive data
    - Validate input from users
    - Use -WhatIf and -Confirm for destructive operations
    - Follow principle of least privilege
#>

#endregion

<#
    === FINAL NOTES ===
    
    This comprehensive reference covers PowerShell syntax from basics to advanced topics.
    All examples use semantically meaningful variable names for clarity and maintainability.
    
    PowerShell excels at:
    - System administration and automation
    - Working with structured data (objects)
    - Integration with Windows and .NET
    - Remote management
    - Pipeline-based data processing
    
    Key takeaways:
    1. PowerShell is object-oriented, not text-based
    2. Verb-Noun naming improves discoverability
    3. Rich type system from .NET
    4. Powerful cmdlet ecosystem
    5. Strong scripting and automation capabilities
    
    For expanding this code:
    - Variable names clearly indicate purpose
    - Functions follow single responsibility principle
    - Comments explain "why" not "what"
    - Modular structure supports easy enhancement
#>
