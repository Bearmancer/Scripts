---
name: csharp-regex
description: Use when writing or debugging C# regular expressions. Focuses on Valid vs Invalid syntax, verbatim strings, and source generation.
---

# C# Regex Syntax & Validation

## Skill Handoff Logic
- **ACTIVATE `dotnet`** for universal **.NET Regex Engine behavior** (like syntax, anchors, multiline, timeouts, and `$$` in replacements).
- This skill focuses purely on how to safely integrate Regex into the **C# language**.

## Core Reference
- [C# Regex Host Syntax (Verbatim & Raw)](references/syntax.md)
- **REQUIRED SKILL:** Use `dotnet` for universal engine behavior and CRLF rules.

## Valid vs. Invalid Syntax (Quick Check)

| Feature | Valid (C#) | Invalid / Red Flag |
| :--- | :--- | :--- |
| **Literal Strings** | `@"\d+"` (Verbatim) | `"\d+"` (Requires double escape `\\d`) |
| **Quotes in Regex** | `"""<a href="([^"]+)">"""` (Raw) | `"\""` (Messy escaping) |
| **Source Gen** | `[GeneratedRegex(@"\b\w\b")]` | `new Regex(...)` in hot loops |
| **Timeouts** | `TimeSpan.FromSeconds(1)` | Missing timeout (DoS risk) |
| **Named Groups** | `(?<name>subpattern)` | Non-named groups for complex logic |

## Implementation Patterns

### Source Generated (Preferred)
```csharp
[GeneratedRegex(@"^(?<protocol>https?)://", RegexOptions.IgnoreCase, "en-US")]
private static partial Regex ProtocolRegex();
```

### Match & Extract
```csharp
var match = ProtocolRegex().Match(input);
if (match.Success) {
    var proto = match.Groups["protocol"].Value;
}
```

### Replace with Logic
```csharp
string result = Regex.Replace(input, pattern, m => m.Value.ToUpper());
```

## Red Flags
- Using `RegexOptions.Compiled` outside of static instances (memory leak).
- Forgetting `RegexOptions.IgnoreCase` when parsing web/user data.
- Hardcoding `\n` (Use `\r?\n` via `dotnet` skill).
