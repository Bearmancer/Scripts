# C# Regex Host Layer Reference

This document covers the **Host Layer** (String Literals and Syntax) when using Regex in C#.

## The "Gold Standard": Verbatim Strings
Always use **Verbatim Strings** (`@""`) for regex patterns.
- **Why:** Verbatim strings treat backslashes (`\`) as literal characters. Without `@`, you must double-escape every backslash (`"\\d"`).

| Task | Pattern Layer | C# Host Layer |
|-------|---------------|---------------|
| Literal Dot | `\.` | `@"\."` |
| Literal Dollar | `\$` | `@"\$"` |
| Backreference | `$1` | `"$1"` |
| Literal Backslash | `\\` | @"\\" |

## Raw String Literals (C# 11+)
For complex patterns or those containing quotes, use **Raw String Literals** (`"""..."""`).

```csharp
// No need to escape " inside the pattern
var pattern = """<a href="([^"]+)">""";
```

## Source Generated Regex (.NET 7+)
For high-performance scenarios, use `[GeneratedRegex]`.

```csharp
[GeneratedRegex(@"\b\w{5}\b", RegexOptions.Compiled)]
private static partial Regex FiveCharWord();
```

## Replacement with Logic (Evaluators)
When replacement logic depends on the match content, use a `MatchEvaluator` (lambda).

```csharp
string result = Regex.Replace(html, @"<h(\d)>(.*?)</h\1>", m => {
    int level = int.Parse(m.Groups[1].Value);
    return new string('#', level) + " " + m.Groups[2].Value;
});
```

## Valid vs Invalid Patterns

### Valid (Recommended)
- **Source Gen:** `[GeneratedRegex(@"...")]`
- **Lazy Match:** `.*?`
- **Named Groups:** `(?<name>...)`

### Invalid / Problematic
- **Compiled on the fly:** `new Regex(p, RegexOptions.Compiled)` (Expensive, permanent memory)
- **Missing Timeout:** `new Regex(p)` (Vulnerable to DoS)
- **Double Escaping:** `"\\d"` (Hard to read, use `@"\d"`)
