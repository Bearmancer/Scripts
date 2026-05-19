---
name: entity-framework-core
description: >
  Modern Entity Framework Core patterns for PostgreSQL (Npgsql provider). Use whenever the
  user is writing EF Core queries, configuring a DbContext, designing a model, working with
  migrations, or asking about performance. Trigger on any mention of EF Core, EntityFramework,
  DbContext, LINQ-to-SQL, migrations, owned types, complex types, JSON columns, vector search,
  pgvector, full-text search, parameterized queries, ILike, or JsonPathExists. Also trigger
  when the user asks how to do something with a PostgreSQL database in .NET — even if they
  don't say "EF Core" — to steer them toward modern patterns instead of outdated ones.
---

# EF Core Modern Practices — PostgreSQL

Produce the most terse, performant, and idiomatic EF Core code possible. Always prefer the
All code examples live in `ref/ef-core-references.md`.
newest API that solves the problem. Never suggest a pattern that has been superseded.

> **Provider:** `Npgsql.EntityFrameworkCore.PostgreSQL`
> **Target:** EF11 exclusively (requires .NET 11 runtime; assert `<TargetFramework>net11.0</TargetFramework>` before Phase 18+ tasks).

---

## 1. LINQ & Query Translation

**LeftJoin / RightJoin (.NET 10+ / EF10+)**
First-class LINQ operators — replace the `GroupJoin` + `SelectMany` + `DefaultIfEmpty`
ceremony entirely. Use `LeftJoin` for every outer-join query. See ref §1.1.

**Named query filters (EF10+)**
Assign string names: `HasQueryFilter("SoftDelete", predicate)`. Disable selectively with
`IgnoreQueryFilters(["SoftDelete"])` — never disable all filters when only one is needed.
Multiple named filters per entity are supported. See ref §1.2.

**ExecuteUpdateAsync — non-expression overload (EF10+)**
Accepts a regular lambda body with conditionals:

```csharp
await context.Blogs.ExecuteUpdateAsync(s =>
{
    s.SetProperty(b => b.Views, 8);
    if (nameChanged) s.SetProperty(b => b.Name, "foo");
});
```

Always use this over the expression-tree overload. See ref §1.3.

**MaxByAsync / MinByAsync (EF11+)**
Replace `OrderBy(...).FirstAsync()` for the entity with the highest or lowest projected
value. Translates to a single efficient query. See ref §1.4.

**Order() / OrderDescending() (EF9+)**
Natural-order sort without a key selector. Use for string/number columns where
`OrderBy(x => x)` is redundant. Not the same as `OrderBy`. See ref §1.5.

**Any() over Count() > 0**
Write `Any()` explicitly. EF9+ auto-translates `Count() > 0` to EXISTS, but `Any()` is
the correct intent signal and avoids reader ambiguity.

**Inlined uncorrelated subqueries (EF9+)**
An `IQueryable` referenced inside another query is now inlined into a single SQL round-trip
rather than executed as a separate database call. No annotation required — this is the
default. Avoid manually splitting such queries with `ToListAsync()` mid-chain.

**Negation push-down (EF9+)**
EF9 pushes `!` into comparisons and flattens nested `CASE/WHEN NOT` blocks:
- `!col.Contains("x")` → `col NOT LIKE '%x%'`
- `!(cond ? false : true)` → single flat `CASE`

No action required — informational for debugging generated SQL.

**GREATEST / LEAST (EF9+)**
`Math.Max(a, b)` / `Math.Min(a, b)` on two column-backed expressions translate to
`GREATEST` / `LEAST`. Use them; do not fake with conditionals. See ref §1.7.

**ToHashSetAsync (EF9+)**
Returns query results as a `HashSet<T>`. Use when uniqueness is the intent; prefer over
`.ToListAsync()` + manual `.Distinct()`. See ref §1.8.

**Enum ToString translation (EF9+)**
`ToString()` on an enum property translates to SQL. Use directly in projections and
`WHERE` clauses — no manual string conversion needed. See ref §1.9.

**C# null semantics (EF9+)**
Nullable comparisons now follow C# semantics by default. After upgrading from EF8,
re-test any nullable equality checks — previously they relied on SQL three-value logic. See ref §1.10.

**Split queries (EF11+)**
Use `AsSplitQuery()` for collection navigations that produce cartesian explosion.
EF11 automatically:
- prunes redundant to-one joins from collection split queries
- removes redundant ORDER BY keys from reference navigations (functional dependency is respected)

Measured 29% perf improvement on common split-query patterns. Call `AsSplitQuery()` to opt in;
EF11 handles the pruning. EF10 also fixed split-query ordering consistency — subqueries now
carry the full ORDER BY, preventing non-deterministic results.

---

## 2. Complex Types

Prefer complex types over owned entities for JSON columns and table-splitting.

- **Value semantics:** no primary key, equality by content, assignable like a struct.
- **Owned entities** have identity bugs: can't be referenced twice from the same entity,
  compared by reference in LINQ, silently wrong in bulk operations.

| Version | Addition                                                                                                                                       |
| ------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| EF8     | Basic complex types (table-splitting)                                                                                                          |
| EF10    | Optional complex types (`Address? BillingAddress`), `.ToJson()` mapping, struct complex types, `ExecuteUpdateAsync` on JSON complex properties |
| EF11    | Complex types on TPT and TPC inheritance hierarchies; full Cosmos DB support                                                                   |

**Declaration:** `[ComplexType]` attribute or fluent `ComplexProperty(...)`.
**JSON persistence:** call `.ToJson()` inside `ComplexProperty(...)`.
**Optional:** declare the property nullable (`Address? BillingAddress`); requires at least one
required property on the complex type.
**Struct support:** complex types may be .NET structs — aligns with value-object semantics.
Collections of structs are not yet supported.
**TPT/TPC (EF11):** complex types and JSON columns are now usable on entity hierarchies
using table-per-type or table-per-concrete-type strategies. No workaround required. See ref §2.

---

## 3. JSON Columns (PostgreSQL jsonb)

Npgsql maps complex types and primitive collections to `jsonb` columns.
Call `.ToJson()` on a complex property to store the sub-object as a single `jsonb` value.

**Partial JSON update via ExecuteUpdateAsync (EF10+)**
Set individual sub-properties inside a `jsonb` column without loading the entity:

```csharp
await context.Blogs.ExecuteUpdateAsync(s =>
    s.SetProperty(b => b.Details.Views, b => b.Details.Views + 1));
```

This is the required pattern for single-field updates on JSON documents — never
load-modify-save. Works with complex types only; does not work with owned entities.

**EF.Functions.JsonPathExists (EF11 — SQL Server only)**
`EF.Functions.JsonPathExists(col, "$.Prop")` translates to SQL Server's `JSON_PATH_EXISTS`.
For PostgreSQL/Npgsql, use `EF.Functions.JsonTypeof` or a raw-SQL predicate. Do not
use the SQL Server API against Npgsql — it will not translate. See §11 for Npgsql JSON. See ref §3.

---

## 4. Primitive Collections

EF8+ maps `List<T>`, `int[]`, etc. directly to `jsonb` array columns in PostgreSQL.
EF9 adds `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, and `ReadOnlyCollection<T>`.

Use them directly in:
- `Contains()` — translates to `ANY(column)` / `= ANY(param)` in PostgreSQL
- `Any()` / `All()` subquery predicates
- Projection and ordering

**Parameterized collection translation default changed in EF10:**

| Version | Default translation                                                                 |
| ------- | ----------------------------------------------------------------------------------- |
| EF8     | JSON array single parameter (`OPENJSON`)                                            |
| EF9     | JSON array single parameter (with manual override via `EF.Constant`/`EF.Parameter`) |
| EF10    | **Individual scalar parameters with cardinality padding**                           |

EF10 pads the parameter list to bucket sizes (e.g. 8 items → 10 parameters) to reduce
unique SQL shapes and improve plan-cache hit rates while still exposing cardinality.

**Override strategies:**

```csharp
// Force JSON array (single param) — use for very large lists
EF.Parameter(ids).Contains(e.Id)

// Force inline constants — use when cardinality dramatically affects the plan
EF.Constant(ids).Contains(e.Id)
```

**Global configuration:**

```csharp
// Force constants globally
optionsBuilder.UseNpgsql(..., o => o.UseParameterizedCollectionMode(ParameterTranslationMode.Constant));
```

`TranslateParameterizedCollectionsToConstants` and `TranslateParameterizedCollectionsToParameters`
are the EF9 context option names (superseded by `UseParameterizedCollectionMode` in EF10).
Per-query `EF.Parameter()` / `EF.Constant()` always override the global setting.

Do not use `string`-serialized arrays or comma-separated column patterns. See ref §4.

---

## 5. Vector Search (pgvector)

Requires: `pgvector` PostgreSQL extension + `Pgvector.EntityFrameworkCore` NuGet.
Call `HasPostgresExtension("vector")` in `OnModelCreating` and `UseVector()` on the
Npgsql options builder.

Always store embeddings as `Vector` (Pgvector type), not `float[]`.

**Exact distance (EF10, GA):**

```csharp
.OrderBy(e => EF.Functions.VectorCosineDistance(e.Embedding, queryVector))
```

**Approximate nearest-neighbor via vector index (EF11):**

```csharp
// Model configuration
modelBuilder.Entity<Blog>().HasVectorIndex(b => b.Embedding, "cosine");

// Query — returns VectorSearchResult<TEntity> with .Distance
await context.Blogs.VectorSearch(b => b.Embedding, embedding, "cosine", topN: 5).ToListAsync();
```

**Vector columns excluded from SELECT by default (EF11):**
`Vector` columns are not projected when materializing entities. Loading embeddings on
every query produced 9–22× overhead in benchmarks. Always explicit-project:

```csharp
// Vector excluded automatically
var blogs = await context.Blogs.OrderBy(b => b.Name).ToListAsync();

// Explicit projection when needed
var embeddings = await context.Blogs.Select(b => new { b.Id, b.Embedding }).ToListAsync();
```

Vector columns remain usable in `WHERE` and `ORDER BY` without triggering a load.
See ref §5.

---

## 6. Full-Text Search (PostgreSQL)

Use Npgsql's built-in FTS translations — no external packages needed.

Key functions: `EF.Functions.ToTsVector`, `EF.Functions.ToTsQuery`,
`EF.Functions.WebSearchToTsQuery`, `EF.Functions.PlainToTsQuery`, `Matches` (`@@`).

In production: store a pre-computed `tsvector` column maintained with a
`GENERATED ALWAYS AS` expression or application-level trigger. Add a GIN index over
the `tsvector` column in migrations. Never call `to_tsvector` per-query on tables queried at scale.
See ref §6.

---

## 7. Migrations

**Concurrent migration locking (EF9)**
EF serializes concurrent migration runs automatically. No application-level locking needed.

**Multi-operation transaction warning (EF9)**
EF9 warns when a migration contains multiple operations where at least one cannot be wrapped
in a transaction (e.g. `CREATE INDEX CONCURRENTLY` in PostgreSQL). Isolate such operations
into their own migration immediately on seeing this warning.

**Seeding (EF9+)**
Use `UseSeeding` / `UseAsyncSeeding` on `DbContextOptionsBuilder`.
`UseAsyncSeeding` runs outside migrations and is idempotent — always guard with an existence
check before inserting. Do **not** use `HasData` for anything that changes after initial schema
creation. See ref §7.1.

**Named default constraints (EF10)**
Two options:

```csharp
// Per-property
builder.Property(p => p.CreatedDate).HasDefaultValueSql("now()", "DF_Post_CreatedDate");

// Global — names all default constraints; next migration renames every existing one
modelBuilder.UseNamedDefaultConstraints();
```

Required for clean diffs in environments with strict schema comparison tooling. See ref §7.2.

**FK constraint exclusion (EF11)**
Keeps the EF relationship (queries, change tracking, navigation) and the database index,
but suppresses the constraint itself:

```csharp
modelBuilder.Entity<Blog>()
    .HasMany(e => e.Posts).WithOne(e => e.Blog)
    .HasForeignKey(e => e.BlogId)
    .ExcludeForeignKeyFromMigrations();
```

Use for cross-schema references or application-enforced referential integrity. See ref §7.3.

**Fill-factor for keys and indexes (EF9+)**
```csharp
modelBuilder.Entity<User>().HasKey(e => e.Id).HasFillFactor(80);
modelBuilder.Entity<User>().HasIndex(e => e.Name).HasFillFactor(80);
```

Applied to write-heavy tables to reduce page splits. See ref §7.4.

**Migration snapshot records latest migration ID (EF11)**
The model snapshot now embeds the last migration ID. Divergent branches both mutate this
field, producing a merge conflict that surfaces the divergence before it becomes a data issue.
Resolve by discarding one migration tree and creating a new unified migration.

**Create + apply in one step (EF11)**
```
dotnet ef database update InitialCreate --add
dotnet ef database update AddProducts --add --output-dir Migrations/Products
```

Scaffolds, Roslyn-compiles, and applies in a single command. Migration files still written
to disk. Useful in Aspire and containerized pipelines.

**Offline migration removal (EF11)**
```
dotnet ef migrations remove --offline            # no DB connection required
dotnet ef migrations remove --connection "..."   # explicit connection string
dotnet ef database drop --connection "..."       # drop by connection string
```

`--offline` and `--force` are mutually exclusive: `--force` requires a live DB to check
applied state before reverting.

**`dotnet-ef.json` configuration (EF11)**
Place at `.config/dotnet-ef.json` (searched upward from cwd). Eliminates repeated
`--project` / `--startup-project` flags:

```json
{
  "project": "src/App.Infrastructure",
  "startupProject": "src/App.Api",
  "context": "AppDbContext"
}
```

Explicit CLI flags always take precedence. See ref §7.5.

---

## 8. Compiled Models

**Auto-detection (EF9+):** compiled model is detected and used automatically when the
`DbContext` and compiled model are in the same assembly. **Never add `.UseModel(...)` explicitly
— it is an anti-pattern since EF9.**

**Generate:**
```
dotnet ef dbcontext optimize
```

**MSBuild auto-rebuild (EF9+):**
Install `Microsoft.EntityFrameworkCore.Tasks` NuGet, then add to `.csproj`:

```xml
<PropertyGroup>
  <EFOptimizeContext>true</EFOptimizeContext>
  <EFScaffoldModelStage>build</EFScaffoldModelStage>
</PropertyGroup>
```

The compiled model is regenerated automatically on every project build when the model changes.
Without this, the compiled model silently goes stale. Required for NativeAOT. See ref §8.

---

## 9. Security

**Inlined constant redaction (EF10+)**
When EF inlines a value into SQL (e.g. via `EF.Constant()`), it now logs `?` in place of
the actual value by default. Enable full logging only in development:

```csharp
optionsBuilder.EnableSensitiveDataLogging(); // dev only
```

**Raw SQL concatenation analyzer (EF10+)**
EF ships a Roslyn analyzer that emits a warning when string concatenation appears inside
`FromSqlRaw`, `ExecuteSqlRaw`, or similar "raw" API call sites:

```csharp
// WARNING: SQL injection risk — analyzer fires here
context.Users.FromSqlRaw("SELECT * FROM Users WHERE [" + fieldName + "] IS NULL");
```

Suppress only when `fieldName` is guaranteed to be from a trusted, controlled source.
Prefer `FromSql` (safe interpolation) wherever possible.

---

## 10. Npgsql-Specific Extensions

**ILike — case-insensitive pattern matching**
```csharp
// Translates to PostgreSQL ILIKE
.Where(e => EF.Functions.ILike(e.Name, "%searchTerm%"))
```

Use instead of `EF.Functions.Like` when case-insensitivity is required. Do not use
`ToLower()` + `Like` — `ILike` is index-aware and correct. See ref §10.1.

**JSON path queries (PostgreSQL)**
`EF.Functions.JsonPathExists` in EF11 targets SQL Server (`JSON_PATH_EXISTS`). For
Npgsql/PostgreSQL, use:
- `EF.Functions.JsonTypeof(col, "$.Prop") != null` — checks key existence
- `EF.Functions.JsonContains(col, value)` — Npgsql's `@>` operator
- Raw SQL via `FromSql` for complex path predicates

See ref §10.2.

**pg_trgm trigram similarity**
```csharp
// Fuzzy search — requires pg_trgm extension
.Where(e => EF.Functions.TrigramsSimilarity(e.Name, term) > 0.3)
```

Add `HasPostgresExtension("pg_trgm")` in `OnModelCreating`. Pair with a GIN trigram
index in migrations. See ref §10.3.

**ApplyConfigurationsFromAssembly — non-public constructors (EF9+)**
Private nested `IEntityTypeConfiguration<T>` classes are now instantiated correctly.
No workaround needed — `ApplyConfigurationsFromAssembly` reaches non-public constructors.
See ref §10.4.

---

## 11. Performance Checklist

| Concern                           | Modern approach                                                                                                                                        |
| --------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Large `IN` list                   | EF10 default (individual scalar params + padding) — override with `EF.Parameter()` only for very large lists                                           |
| Bulk update                       | `ExecuteUpdateAsync` non-expression overload (EF10)                                                                                                    |
| Bulk delete                       | `ExecuteDeleteAsync`                                                                                                                                   |
| Partial JSON update               | `ExecuteUpdateAsync` on complex type JSON props — never load-modify-save                                                                               |
| Vector columns in SELECT          | Excluded by default in EF11 — always explicit-project                                                                                                  |
| Collection cartesian explosion    | `AsSplitQuery()` — EF11 prunes to-one joins + redundant ORDER BY keys                                                                                  |
| Split query ordering consistency  | EF10 fix applied automatically — no annotation needed                                                                                                  |
| Startup cost                      | Compiled models: `dotnet ef dbcontext optimize` + `<EFOptimizeContext>true</EFOptimizeContext>` + `<EFScaffoldModelStage>build</EFScaffoldModelStage>` |
| Seed data                         | `UseAsyncSeeding` — not `HasData`                                                                                                                      |
| Log safety                        | Inlined constants redacted by default (EF10) — `EnableSensitiveDataLogging()` dev only                                                                 |
| Nullable comparisons post-upgrade | Re-test after EF8 → EF9: C# null semantics changed default behavior                                                                                    |
| Uncorrelated subquery round-trips | Inlined automatically in EF9+ — do not split with mid-chain `ToListAsync()`                                                                            |
| Case-insensitive lookup           | `EF.Functions.ILike` — not `ToLower()` + `Like`                                                                                                        |
| Write-heavy index pages           | `HasFillFactor(n)` on keys and indexes                                                                                                                 |

---

## 12. Anti-Patterns

| Anti-pattern                                                  | Fix                                                                                        |
| ------------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| `OrderBy(x => x.Prop).FirstAsync()` for max/min entity        | `MaxByAsync` / `MinByAsync` (EF11)                                                         |
| `GroupJoin` + `SelectMany` + `DefaultIfEmpty`                 | `LeftJoin` (EF10 / .NET 10)                                                                |
| Owned entities for JSON / table-split                         | Complex types                                                                              |
| Single unnamed filter + `IgnoreQueryFilters()`                | Named filters, disable selectively (EF10)                                                  |
| `HasData` for seed data                                       | `UseAsyncSeeding` (EF9)                                                                    |
| `OrderBy(x => x)` for natural-order sort                      | `Order()` / `OrderDescending()` (EF9)                                                      |
| `Count() > 0`                                                 | `Any()`                                                                                    |
| `FromSqlRaw` with string concatenation                        | `FromSql` (safe interpolation) — EF10 analyzer warns                                       |
| `.UseModel(compiledModel)` explicit call                      | Remove — anti-pattern since EF9, auto-detected                                             |
| `float[]` column for embeddings                               | `Vector` (Pgvector type)                                                                   |
| Load entity to update one JSON sub-property                   | `ExecuteUpdateAsync` on JSON complex property (EF10)                                       |
| `Skip(n * size).Take(size)` at large offsets                  | Keyset / cursor pagination                                                                 |
| `ToLower()` + `Like` for case-insensitive match               | `EF.Functions.ILike` (Npgsql)                                                              |
| `EF.Functions.JsonPathExists` on PostgreSQL                   | Npgsql JSON functions (`JsonTypeof`, `JsonContains`) — `JsonPathExists` targets SQL Server |
| `UseModel(compiledModel)` in Task 19.3                        | Replace with `<EFOptimizeContext>true` in `.csproj`; assert compiled model files exist     |
| Expression-tree `ExecuteUpdateAsync` with conditionals        | Non-expression lambda overload (EF10)                                                      |
| Mid-chain `ToListAsync()` to work around subquery round-trips | Remove — EF9+ inlines uncorrelated subqueries automatically                                |