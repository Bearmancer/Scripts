---
name: csharp
description: Use when writing or refactoring modern C# code (C# 13/14/15+). Covers C# Architect role, language features, and idiomatic patterns.
---

# C# Architect & Language

## Execution Protocol
| Trigger         | Mode       | Section   |
| --------------- | ---------- | --------- |
| New feature     | `DEV`      | §DEV      |
| Code cleanup    | `JANITOR`  | §JANITOR  |
| Guidance/review | `ADVISORY` | §ADVISORY |
| Tests           | `TESTER`   | §TESTER   |

Pre: Scan `Directory.Build.*`, verify latest .NET / C# target.
Post: Run `csharpier format .` and tests.

## Code Constraints
| Rule             | Enforcement                                     |
| ---------------- | ----------------------------------------------- |
| Interfaces       | ONLY for external deps/testing                  |
| Access modifiers | `private` > `internal` > `protected` > `public` |
| Records          | Prefer for DTOs; primary constructors for DI    |
| Async naming     | ALL async methods end with `Async`              |
| Cancellation     | `CancellationToken ct` on ALL async methods     |
| Sync-over-async  | FORBIDDEN — never `.Result`, `.Wait()`          |
| Primary Constructors | Mandatory for DI and record types unless state is complex |

## Playbooks & Reference
- [Modern C# Features (C# 13-15+)](playbooks/modern-features.md)
- **REQUIRED SKILL:** Use `csharp-regex` for all C# regular expression syntax and validation.

## Modern Syntax (Quick Reference)
- **Field-Backed Properties:** `set => field = value.Trim();`
- **Union Types:** `[Union] partial record Result;`
- **Collection Expressions:** `int[] x = [1, 2, 3];`
- **Extension Members:** `extension<T>(IEnumerable<T> s) { ... }`
- **Unbound nameof:** `nameof(List<>)`
- **Implicit Span:** `ReadOnlySpan<char> s = "abc";`
- **Partial Events/Ctors:** `partial class T { partial T(); }`

## Library Matrix
| Purpose          | Library                         |
| ---------------- | ------------------------------- |
| Test Framework   | `TUnit` (Prefer over xUnit)     |
| Assertions       | `FluentAssertions`              |
| Mocking          | `NSubstitute`                   |
| CLI              | `Spectre.Console.CLI`           |
| JSON             | `System.Text.Json`              |
| Resiliency       | `Polly`                         |

## §TESTER
- **Project naming:** `[ProjectName].Tests`
- **Test naming:** `WhenCatMeowsThenCatDoorOpens`
- **One behavior:** Per test — no multiple assertions.
- **Workflow:** one failing test → fix → `dotnet test` → next.

## §JANITOR
1. `dotnet build` baseline → fix warnings → modernize → `csharpier format .`
2. Preserve all behavior. Incremental changes only.

## Red Flags
- Explicit backing fields where `field` keyword suffices.
- `new List<T>()` or `new[] { ... }` (use `[]`).
- Block-scoped namespaces (use file-scoped `namespace Project;`).
- Missing `Async` suffix or `CancellationToken ct`.
- Catching base `Exception` or silent catches.
