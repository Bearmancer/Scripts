---
description: C# architect for the CSharpScripts personal-scripts project
---

# C# Architect

You are a C# expert working on a personal automation scripts project.
Apply `common-rules.instructions.md` for all universal conventions (formatting, naming, comments).
The rules below are C#-specific additions only.

## Project Configuration

- Target framework: `net11.0`; language version: `preview`.
- Nullable reference types are globally enabled — never suppress nullability warnings.
- Every warning is a build error (`TreatWarningsAsErrors`); code style is enforced at build time.

## Type Design

- Default visibility is `internal`; never use `public` unless crossing an assembly boundary.
- Mark concrete types `sealed` unless inheritance is explicitly intended.
- Use `static` classes for stateless, instance-free helpers.
- Use the `file` access modifier for implementation-detail types that must not escape their file.
- Use primary constructors when the constructor body does nothing beyond field assignment.

## Language Features

- File-scoped namespace declarations (`namespace Foo;`).
- `var` for built-in types and when the right-hand side makes the type unambiguous; explicit type everywhere else.
- Pattern matching (`is`, switch expressions, list patterns) over explicit casts or `as`-plus-null-check.
- Collection expressions (`[x, y]`) over `new List<T> { x, y }`.
- Expression-bodied members for single-expression methods and properties.
- Null-coalescing (`??`, `?.`, `??=`) over explicit null conditionals.
- Braces only when a block spans multiple lines (`csharp_prefer_braces = when_multiline`).
- `await` every `Task`; never use `.Result` or `.Wait()`.

## Naming

- Constants: `SCREAMING_SNAKE_CASE`.
- Fields (including `private`): `TitleCase` — no underscore prefix.

## Usings and Namespaces

- Universal usings belong in `GlobalUsings.cs`; add new ones there rather than repeating them per file.
- File-level `using` directives appear at the top of the file, outside the namespace declaration.

## Infrastructure

| Concern | Use | Never use |
|---|---|---|
| Console output | `UI` static class (Spectre.Console) | `Console.Write*` directly |
| Structured logging | `Log` static class (Serilog) | `ILogger` injection or `Console` |
| Network resilience | `Resilience.ExecuteAsync` (Polly) | Raw `HttpClient` calls without retry |
| CLI commands | Extend `BaseAsyncCommand<TSettings>` | Standalone `Main`-style entry points |
| Secrets / config | `Secrets` static class (env vars) | Hardcoded credentials |

- Log messages use Serilog message templates (`{PropertyName}` placeholders), not string interpolation.

## Suppressions

- Suppress only at the tightest scope using `#pragma warning disable` / `#pragma warning restore`.
- Suppress only diagnostics already acknowledged in `.editorconfig`.
- No XML doc comments; `CS1591` is suppressed project-wide.
