---
name: dotnet
description: Use when managing .NET SDK, runtime, core libraries (System.Text.Json, LINQ), or project configuration.
---

# .NET (SDK & Runtime)

## Skill Handoff Logic
- **ACTIVATE `csharp`** for language syntax, advanced types (`Span`, `Unions`), and idiomatic refactoring.
- **ACTIVATE `ef-core`** for data modeling, migrations, and LINQ to Entities.

## Core Reference
- [Modern .NET Library & CLI Playbook](playbooks/library-features.md)
- [Universal .NET Syntax & Logic](playbooks/universal-syntax.md)
- [Universal Regex Engine Behavior](playbooks/dotnet-regex-engine.md)
- [Universal CRLF & Line Stability](playbooks/regex-crlf.md)

## Modern Implementation (Quick Reference)
- **Shared Operators:** `? :`, `??`, `??=` (See Universal Syntax playbook)
- **JSON Naming:** `PropertyNamingPolicy = JsonNamingPolicy.PascalCase`
- **JSON Schema:** `JsonSchemaExporter.GetJsonSchema(typeof(T))`
- **JSON Duplicate:** `AllowDuplicateProperties = false`
- **LINQ Aggs:** `items.CountBy(k)` / `items.AggregateBy(k, s, f)`
- **LINQ Index:** `foreach (var (idx, val) in items.Index())`
- **CLI Env:** `dotnet run -e VAR=VAL`
- **CLI Tab:** `dotnet completions script pwsh`
- **Workloads:** `dotnet workload config --update-mode workload-set`

## Project Configuration (.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net11.0</TargetFramework>
    <LangVersion>preview</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

## SDK & CLI Patterns
- **Noun-First CLI:** `dotnet package add` > `dotnet add package`.
- **One-Shot Tools:** `dotnet tool exec <pkg>` or `dnx <pkg>`.
- **Roll-Forward:** `dotnet tool install --allow-roll-forward`.

## Red Flags
- Using `new List<T>()` instead of collection expressions `[]`.
- Manual `GroupBy` for frequency (use `CountBy`).
- Targeting obsolete runtimes without justification.
- Missing `CancellationToken` in async calls.
