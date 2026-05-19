# Playbook: .NET Regex & CRLF Consistency

This playbook addresses the common pitfall where the `$` anchor fails on Windows-style `\r\n` line endings. The behavior is identical across C# and PowerShell because both utilize the .NET `System.Text.RegularExpressions` engine.

## The Problem: `$` Anchor with `\r\n`

In multiline mode (`(?m)`), the `$` anchor matches before `\n` (LF), but it **does not** match before `\r` (CR). On Windows, lines end in `\r\n`, so a pattern ending in `$` will fail to match lines because the character immediately following the match target is `\r`, not `\n`.

### C# Failure Case
```csharp
string content = "import java.util.List;\r\nline2";
// This returns 0 matches because of the \r before \n
var matches = Regex.Matches(content, @"(?m)^import\s+(.+?);$");
```

### PowerShell Failure Case
```powershell
$content = "import java.util.List;`r`nline2"
# FAILS: No matches found
$matches = [regex]::Matches($content, '(?m)^import\s+(.+?);$')
```

---

## Recommended Fixes

### 1. Strip `\r` Before Processing (Preferred)
The most robust approach is to normalize the string to Unix-style `\n` line endings before applying regex.

**C#:**
```csharp
string normalized = content.Replace("\r", "");
var matches = Regex.Matches(normalized, @"(?m)^import\s+(.+?);$");
```

**PowerShell:**
```powershell
$normalized = $content -replace "`r", ""
$matches = [regex]::Matches($normalized, '(?m)^import\s+(.+?);$')
```

### 2. Handle Optional `\r` in Pattern
Add `\r?` before the `$` anchor.

**Pattern:** `(?m)^import\s+(.+?);\r?$`

### 3. Use `\z` for End-of-String
If you are matching the entire content as a single block and don't need multiline anchors.

---

## .NET Regex Engine Behavior Matrix

| Feature | Behavior |
|---------|----------|
| `$` with `(?m)` | Matches before `\n` ONLY. Fails if `\r` precedes `\n`. |
| `^` with `(?m)` | Matches after `\n` or at start of string. |
| `.` (Default) | Matches everything EXCEPT `\n`. It **includes** `\r`. |
| `(?s)` (Singleline) | Makes `.` match `\n` as well. |
| `\A` | Matches start of entire string (ignores `(?m)`). |
| `\Z` | Matches end of string or before `\n` at the very end. |
| `\z` | Matches absolute end of string only. |

## Cross-Cutting Patterns: HtmlToMarkdown

Verified patterns for converting LeetCode HTML to Markdown using .NET Regex.

| Element | Pattern | Replacement |
|---------|---------|-------------|
| **Bold** | `<(?:b|strong)>(.*?)</(?:b|strong)>` | `**$1**` |
| **Italic** | `<(?:i|em)>(.*?)</(?:i|em)>` | `*$1*` |
| **Inline Code** | `<code>(.*?)</code>` | `` `$1` `` |
| **List Item** | `<li>(.*?)</li>` | `- $1` |
| **Headings** | `<h(\d)[^>]*>(.*?)</h\1>` | `${1} ${2}` (with logic for `#` count) |

### Pre-Block Protection
Always extract `<pre>` blocks into placeholders *before* running general HTML-to-Markdown replacements to avoid corrupting code content.

```csharp
// 1. Extract
var preMatches = Regex.Matches(html, @"(?s)<pre>(.*?)</pre>");
// 2. Replace tags in non-pre content
// 3. Re-insert pre blocks
```
