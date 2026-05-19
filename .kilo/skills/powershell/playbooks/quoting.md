# Playbook: String Quoting Examples

## Anti-Pattern 1: Double Quotes for Constant Strings

```powershell
# BAD: Double quotes imply expansion but no variables
$path = "C:\Users\Public\Scripts"

# GOOD: Single quotes signal constant intent
$path = 'C:\Users\Public\Scripts'
```

## Anti-Pattern 2: PowerShell Escapes vs Regex Escapes

```powershell
# BAD: "`r`n" replaces actual CR+LF byte chars, not regex \r\n
$text -replace "`r`n", ' '

# GOOD: '\r\n' is regex pattern matching carriage-return + newline
$text -replace '\r\n', ' '

# KEY DISTINCTION:
# "`r" = literal carriage return byte 0x0D PowerShell escape
# "\r" = two characters backslash + r
# '\r' = two literal characters use for regex
```

## Anti-Pattern 3: Unescaped `$` in Regex Replacements

```powershell
# BAD: $1 interpreted as regex backreference by the .NET regex engine
$text -replace 'old(pattern)', '$1.00'

# BAD: Backticks escape PowerShell variables, but do NOT escape .NET Regex engine variables.
$text -replace 'old(pattern)', '`$1.00'

# BAD: [regex]::Escape() is for PATTERNS, not replacement strings. It adds backslashes (\$).
$text -replace 'old(pattern)', [regex]::Escape('$1.00')

# GOOD: Use $$ to insert a literal $ in a .NET Regex replacement string
$text -replace 'old(pattern)', '$$1.00'
```

## Anti-Pattern 4: `$matches` in Loops

```powershell
# BAD: $matches overwritten each iteration
foreach ($line in $lines) {
    if ($line -match '(pattern)') {
        $val = $matches[1]  # BUG: from wrong iteration
    }
}

# GOOD: [regex]::Matches with persistent MatchCollection
$pattern = [regex]'(pattern)'
foreach ($match in $pattern.Matches($text)) {
    $val = $match.Groups[1].Value
}
```

## Anti-Pattern 5: Regex Metacharacters in `-replace`

```powershell
# BAD: '.' matches ANY character, not literal dot
'foo.bar' -replace '.', 'X'         # Result: 'XXXXXXX'

# GOOD: Escape the dot
'foo.bar' -replace '\.', 'X'        # Result: 'fooXbar'
'foo.bar' -replace [regex]::Escape('.'), 'X'
```

## Anti-Pattern 6: Here-String Closing Delimiter

```powershell
# BAD: Closing "@ not on its own line
$s = @"
data
"@trailing                          # Syntax error

# GOOD: Closing "@ on own line, no chars before or after
$s = @"
data
"@
```

## Anti-Pattern 7: Nested Quoting for Native Commands

```powershell
# BAD: PowerShell strips quotes passing to native
cmd /c echo "hello world"

# GOOD: --% stop-parsing token
cmd /c --% echo "hello world"

# GOOD: single-quotes inside double-quotes
cmd /c echo '"hello world"'
```
