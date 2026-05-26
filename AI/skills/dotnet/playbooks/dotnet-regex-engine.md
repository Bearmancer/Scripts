# .NET Regex Engine Core Reference

This document covers the **Regex Engine** layer of the .NET `System.Text.RegularExpressions` library. These rules are identical for C#, PowerShell, and any other .NET host language.

## Separation of Concerns
- **`dotnet` skill (This file):** Owns the regex engine behavior. This includes pattern syntax (`.`, `^`, `$`), flags (`(?m)`), timeout behavior, match collections, and replacement string tokens (like `$$` or `$1`).
- **`csharp-regex` skill:** Owns how C# passes strings to the engine (e.g., verbatim `@""` vs raw `""" """` strings, escaping `\` in C# strings) and C# integration like `[GeneratedRegex]`.
- **`powershell` skill:** Owns how PowerShell passes strings to the engine (e.g., single quotes `'...'` vs double quotes `"..."`) and PowerShell operators (`-match`, `-replace`).

## The Anchor/CRLF Conflict
The `$` anchor in multiline mode (`(?m)`) matches before `\n` (LF) but **not** before `\r` (CR).
- **Symptom:** Regex patterns ending in `$` fail silently on Windows files (`\r\n`).
- **Fix:** Always strip `\r` (`-replace "\r", ""` or `.Replace("\r", "")`) before processing multiline strings.

## Engine Behavior Matrix

| Token | Meaning | Notes |
|-------|---------|-------|
| `.` | Any char except `\n` | **Warning:** Includes `\r`. |
| `^` | Start of line/string | Matches after `\n` in multiline. |
| `$` | End of line/string | Matches before `\n` in multiline. |
| `\A` | Absolute start | Ignores multiline mode. |
| `\z` | Absolute end | Ignores multiline mode. |
| `\Z` | End or before `\n` at end | Useful for single-line files with trailing LF. |
| `(?m)` | Multiline mode | `^` and `$` match line boundaries. |
| `(?s)` | Singleline mode | `.` matches `\n` (LF). |
| `(?i)` | Case-insensitive | Default for PWSH `-replace`. |

## Replacement String Tokens
Special characters in the **replacement** string (the "to" part):

| Token | Meaning | Example |
|-------|---------|---------|
| `$1`, `$2` | Numbered group | `Price: $1` |
| `${name}` | Named group | `Price: ${amount}` |
| `$$` | **Literal Dollar Sign** | `Cost: $$10.00` -> `Cost: $10.00` |
| `$&` | The entire match | |
| `` $` `` | Text before match | |
| `$'` | Text after match | |
| `$+` | Last captured group | |

## Backreference "Conflict Zone"
If you need a literal `$` followed by a group (e.g., `$100`), you **must** escape the dollar sign using `$$`.
- **Wrong:** `$1.00` (Engine looks for group 1).
- **Wrong:** `$$1.00` (Engine sees `$$` as literal `$` and then literal `1.00`. Group 1 is ignored).
- **Right:** `$$$1` (Engine sees `$$` as literal `$` and then `$1` as Group 1).
