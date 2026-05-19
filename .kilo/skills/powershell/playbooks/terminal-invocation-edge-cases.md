# PowerShell — Terminal Invocation Edge-Cases

## Nested `powershell -Command` quote collapse

```powershell
# FAILS — inner quotes stripped, variable unexpanded
powershell -Command "Write-Host "Value is $env:PATH""

# FIX A — base64-encode the command
$cmd     = 'Write-Host "Value is $env:PATH"'
$bytes   = [Text.Encoding]::Unicode.GetBytes($cmd)
$encoded = [Convert]::ToBase64String($bytes)
powershell -EncodedCommand $encoded

# FIX B — temp script file
$script = Join-Path $env:TEMP 'script.ps1'
'Write-Host "Value is $env:PATH"' | Set-Content $script
powershell -File $script
```

---

## Native command argument stripping (`$PSNativeCommandArgumentPassing`)

```powershell
# FAILS in PS 7.3+ — quotes stripped
cmd /c echo "a|b"
icacls X:\VMS /grant Dom\HVAdmin:(CI)(OI)F

# FIX A — stop-parsing token
cmd /c --% echo "a|b"
icacls X:\VMS --% /grant Dom\HVAdmin:(CI)(OI)F /T

# FIX B — revert to legacy mode (session-scoped)
$PSNativeCommandArgumentPassing = 'Legacy'
```

**`--%` constraints:**

- Terminates at newline or pipe character.
- Expands `%VAR%` environment variables only — no PowerShell `$var` or subexpressions.
- No escape sequences after the token.

---

## Argument mode vs. expression mode

| Invocation           | Mode       | Result                     |
| -------------------- | ---------- | -------------------------- |
| `Write-Output 2+2`   | Argument   | `"2+2"` (string)           |
| `Write-Output (2+2)` | Expression | `4` (int)                  |
| `Write-Output $a+2`  | Argument   | `"4+2"` (string)           |
| `Write-Output a$a`   | Argument   | `"a4"` (variable expanded) |
| `Write-Output a'$a'` | Argument   | `"a$a"` (verbatim)         |

```powershell
# FAILS — scope specifier error
"$HOME: where the heart is."

# FIX — braces isolate variable
"${HOME}: where the heart is."

# FAILS — positional string not evaluated
Write-Output 2+2

# FIX — parens force expression context
Write-Output (2+2)
```

---

## `--` vs. `--%`

| Token | Scope                      | Purpose                                                    |
| ----- | -------------------------- | ---------------------------------------------------------- |
| `--`  | PowerShell cmdlets         | Stops parameter binding; remaining tokens passed as values |
| `--%` | Native (external) commands | Passes everything after literally; only `%VAR%` expanded   |

```powershell
# -- for PowerShell — prevents -InputObject being treated as a switch
Write-Output -- -InputObject       # outputs the string "-InputObject"

# --% for native — passes raw string to cmd
cmd /c --% echo "Hello World"
```
