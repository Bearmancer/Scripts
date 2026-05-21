# EF Core Reference — PostgreSQL Code Examples

All examples target **EF10 + Npgsql.EntityFrameworkCore.PostgreSQL** unless a section
header specifies otherwise. Version requirements are noted inline where they differ.

---

## §1 LINQ & Query Translation

### §1.1 LeftJoin / RightJoin (EF10+)

```csharp
// ❌ old — three-step ceremony
var q = context.Students
    .GroupJoin(context.Departments,
        s => s.DeptId, d => d.Id,
        (s, ds) => new { s, ds })
    .SelectMany(x => x.ds.DefaultIfEmpty(),
        (x, d) => new { x.s.Name, Dept = d!.Name ?? "NONE" });

// ✅ EF10+
var q = context.Students.LeftJoin(
    context.Departments,
    s => s.DepartmentId,
    d => d.Id,
    (s, d) => new { s.Name, Department = d!.Name ?? "[NONE]" });
```

---

### §1.2 Named Query Filters (EF10+)

```csharp
// Model configuration
modelBuilder.Entity<Post>()
    .HasQueryFilter("SoftDelete", p => !p.IsDeleted)
    .HasQueryFilter("Tenant",     p => p.TenantId == currentTenantId);

// Disable only the soft-delete filter — tenant filter still applies
var allForTenant = await context.Posts
    .IgnoreQueryFilters(["SoftDelete"])
    .ToListAsync();

// Disable both
var all = await context.Posts
    .IgnoreQueryFilters(["SoftDelete", "Tenant"])
    .ToListAsync();
```

---

### §1.3 ExecuteUpdateAsync — Non-Expression Overload (EF10+)

```csharp
// ❌ old — expression tree only, no conditionals
// required manual Expression<Func<SetPropertyCalls<Blog>, ...>> manipulation

// ✅ EF10+ — regular lambda, conditionals allowed
await context.Posts
    .Where(p => p.AuthorId == authorId)
    .ExecuteUpdateAsync(s =>
    {
        s.SetProperty(p => p.Views, p => p.Views + 1);
        if (titleChanged) s.SetProperty(p => p.Title, newTitle);
    });
```

---

### §1.4 MaxByAsync / MinByAsync (EF11+)

```csharp
// ❌ old
var top = await context.Posts
    .OrderByDescending(p => p.Views)
    .FirstAsync();

// ✅ EF11+
var top = await context.Posts.MaxByAsync(p => p.Views);
var bottom = await context.Posts.MinByAsync(p => p.Views);
```

---

### §1.5 Order() / OrderDescending() (EF9+)

```csharp
// ❌ redundant key selector
var tags = await context.Tags.OrderBy(t => t.Name).ToListAsync();

// ✅ EF9+ — when natural order on the property is what you mean
var tags = await context.Tags.Order().ToListAsync();
var tagsDesc = await context.Tags.OrderDescending().ToListAsync();

// Order() is for the entity's natural comparison — not a substitute for OrderBy
// when sorting by a specific property other than the natural order.
```

---

### §1.6 Parameterized Collections (EF10 defaults)

```csharp
int[] ids = [1, 2, 3, 4, 5];

// Default (EF10): each value → individual scalar param → best plan cache
// Generates: WHERE id = $1 OR id = $2 OR id = $3 (PostgreSQL uses ANY in practice)
var posts = await context.Posts
    .Where(p => ids.Contains(p.Id))
    .ToListAsync();

// Force JSON array param — good for large lists (EF9+)
var posts2 = await context.Posts
    .Where(p => EF.Parameter(ids).Contains(p.Id))
    .ToListAsync();

// Force inline constants — use when cardinality matters for the query plan (EF8+)
var posts3 = await context.Posts
    .Where(p => EF.Constant(ids).Contains(p.Id))
    .ToListAsync();

// Global override in DbContext options
optionsBuilder.UseNpgsql(connectionString, o =>
    o.UseParameterizedCollectionMode(ParameterTranslationMode.Constant));
```

---

### §1.7 GREATEST / LEAST (EF9+)

```csharp
// Translates to SQL: GREATEST(a.Views, b.Views)
var combined = await context.Posts
    .Select(p => new
    {
        p.Title,
        EffectiveScore = Math.Max(p.UpvoteCount, p.CommentCount)
    })
    .ToListAsync();
```

### §1.8 ToHashSetAsync (EF9+)

```csharp
// ❌ old — allocates a list then converts
var tags = (await context.Posts.Select(p => p.Category).ToListAsync())
    .ToHashSet();

// ✅ EF9+ — translates to the same SQL but materialises directly into a HashSet
HashSet<string> tags = await context.Posts
    .Select(p => p.Category)
    .ToHashSetAsync();
```

Use when the result is consumed with `.Contains()` or uniqueness is the intent. Avoids the
intermediate `List<T>` allocation.

### §1.9 Enum ToString Translation (EF9+)

```csharp
public enum PostStatus { Draft, Published, Archived }

// ✅ EF9+ — ToString() translates to SQL; no helper column or conversion needed
var published = await context.Posts
    .Where(p => p.Status.ToString() == "Published")
    .ToListAsync();

// Also works in projections
var statuses = await context.Posts
    .Select(p => new { p.Title, StatusLabel = p.Status.ToString() })
    .ToListAsync();
```

Store enum columns as `text` (or a named PostgreSQL enum) in migrations. Using `int`
storage still requires `ToString()` to translate — verify the generated SQL.

### §1.10 C# Null Semantics Default Change (EF9+)

EF9 changed the default null comparison behaviour to match C# semantics rather than
SQL three-value logic. The practical effect: EF no longer auto-generates
`column IS NULL OR column = @p` guard clauses for nullable comparisons.

```csharp
// EF8 generated: WHERE (col = @p) OR (col IS NULL AND @p IS NULL)
// EF9+ generates: WHERE col = @p   (C# semantics — matches .NET behaviour)
var results = await context.Posts
    .Where(p => p.Category == category)   // category may be null
    .ToListAsync();
```

If upgrading from EF8, audit every nullable equality check in `WHERE` clauses and
verify generated SQL with `EnableSensitiveDataLogging()` in a dev environment.

---

## §2 Complex Types

### §2.1 Basic Definition and Table-Splitting (EF8+)

```csharp
// Entity
public class Order
{
    public int     Id              { get; set; }
    public required Address ShippingAddress { get; set; }
    public Address? BillingAddress  { get; set; }  // optional — EF10+
}

// Complex type — no DbSet, no primary key
[ComplexType]
public class Address
{
    public required string Street { get; set; }
    public required string City   { get; set; }
    public required string Zip    { get; set; }
}

// Fluent equivalent (no attribute needed)
modelBuilder.Entity<Order>(b =>
{
    b.ComplexProperty(o => o.ShippingAddress);
    b.ComplexProperty(o => o.BillingAddress);
});
```

### §2.2 Struct Complex Types (EF10+)

```csharp
public struct Money
{
    public decimal Amount   { get; init; }
    public string  Currency { get; init; }
}

// Fluent
modelBuilder.Entity<Invoice>()
    .ComplexProperty(i => i.Total);
```

### §2.3 JSON Column Mapping via ToJson() (EF10+)

```csharp
// Stores ShippingAddress as a jsonb column, not table-split columns
modelBuilder.Entity<Order>(b =>
{
    b.ComplexProperty(o => o.ShippingAddress, c => c.ToJson());
    b.ComplexProperty(o => o.BillingAddress,  c => c.ToJson());
});
```

### §2.4 ExecuteUpdateAsync on JSON Complex Properties (EF10+)

```csharp
// Update a single field inside a jsonb column — no entity load required
await context.Orders
    .Where(o => o.CustomerId == customerId)
    .ExecuteUpdateAsync(s =>
        s.SetProperty(o => o.ShippingAddress.City, "Edinburgh"));
```

---

## §3 JSON Columns (PostgreSQL jsonb)

### §3.1 Raw jsonb Column (Npgsql)

```csharp
public class Product
{
    public int    Id       { get; set; }
    public string Name     { get; set; } = "";

    [Column(TypeName = "jsonb")]
    public JsonDocument? Metadata { get; set; }
}
```

### §3.2 Querying jsonb Properties (Npgsql EF functions)

```csharp
// Filter on a jsonb path value
var heavyItems = await context.Products
    .Where(p => EF.Functions.JsonContains(p.Metadata!, @"{""weight"": 10}"))
    .ToListAsync();

// Project a scalar from jsonb
var names = await context.Products
    .Select(p => EF.Functions.JsonValue(p.Metadata!, "$.sku"))
    .ToListAsync();
```

---

## §4 Primitive Collections

### §4.1 Basic Mapping (EF8+)

```csharp
public class Post
{
    public int      Id   { get; set; }
    public string   Title { get; set; } = "";
    public List<string> Tags { get; set; } = [];      // → jsonb array column
    public int[]    CategoryIds { get; set; } = [];   // → jsonb array column
}
```

### §4.2 Read-Only Collection Interface (EF9+)

```csharp
public class Article
{
    public int                   Id     { get; set; }
    public IReadOnlyList<string> Labels { get; set; } = [];
}
```

### §4.3 Querying Primitive Collections

```csharp
// Contains — translates to PostgreSQL ANY / @>
var tagged = await context.Posts
    .Where(p => p.Tags.Contains("ef-core"))
    .ToListAsync();

// Any with predicate — subquery
var hasShortTag = await context.Posts
    .Where(p => p.Tags.Any(t => t.Length < 5))
    .ToListAsync();

// Projection
var allTags = await context.Posts
    .SelectMany(p => p.Tags)
    .Distinct()
    .ToListAsync();
```

---

## §5 Vector Search (pgvector)

### §5.1 Setup

```csharp
// Packages: Pgvector.EntityFrameworkCore, Npgsql.EntityFrameworkCore.PostgreSQL

// DbContext options
optionsBuilder.UseNpgsql(connectionString, o => o.UseVector());

// OnModelCreating
modelBuilder.HasPostgresExtension("vector");
```

### §5.2 Entity Mapping

```csharp
using Pgvector;

public class Document
{
    public int    Id        { get; set; }
    public string Content   { get; set; } = "";

    [Column(TypeName = "vector(1536)")]
    public Vector? Embedding { get; set; }
}
```

### §5.3 Exact Nearest-Neighbor Search (EF10+)

```csharp
Vector queryVector = new(embeddings);  // float[] from your model

var nearest = await context.Documents
    .OrderBy(d => EF.Functions.VectorCosineDistance(d.Embedding!, queryVector))
    .Take(5)
    .Select(d => new { d.Id, d.Content })   // never project Embedding unless needed
    .ToListAsync();
```

Available distance functions (Npgsql): `VectorCosineDistance`, `VectorL2Distance`,
`VectorInnerProduct`, `VectorL1Distance`.

### §5.4 Approximate Nearest-Neighbor with Index (EF11+)

```csharp
// Model configuration — creates an HNSW index
modelBuilder.Entity<Document>()
    .HasIndex(d => d.Embedding)
    .HasMethod("hnsw")
    .HasOperators("vector_cosine_ops");

// Query — same as exact, optimizer uses the index automatically
var nearest = await context.Documents
    .VectorSearch(d => d.Embedding!, queryVector, "cosine", topN: 10)
    .Select(d => new { d.Id, d.Content })
    .ToListAsync();
```

### §5.5 Never SELECT the Vector Column

```csharp
// ❌ loads 1536 floats per row — catastrophic at scale
var docs = await context.Documents.ToListAsync();

// ✅ project only what you need
var docs = await context.Documents
    .Select(d => new { d.Id, d.Content })
    .ToListAsync();

// ✅ load embedding only when explicitly required
var doc = await context.Documents
    .Where(d => d.Id == id)
    .Select(d => new { d.Content, d.Embedding })
    .FirstAsync();
```

---

## §6 Full-Text Search (PostgreSQL)

### §6.1 Ad-Hoc FTS Query (simple / prototyping)

```csharp
var results = await context.Posts
    .Where(p => EF.Functions.ToTsVector("english", p.Title + " " + p.Body)
        .Matches(EF.Functions.ToTsQuery("english", "entity & framework")))
    .ToListAsync();
```

### §6.2 Stored tsvector Column with GIN Index (production)

```csharp
// Entity
public class Post
{
    public int    Id         { get; set; }
    public string Title      { get; set; } = "";
    public string Body       { get; set; } = "";
    public NpgsqlTsVector SearchVector { get; set; } = null!;  // computed
}

// Model configuration
modelBuilder.Entity<Post>(b =>
{
    b.HasGeneratedTsVectorColumn(
        p => p.SearchVector,
        "english",
        p => new { p.Title, p.Body });

    b.HasIndex(p => p.SearchVector)
     .HasMethod("GIN");
});

// Query — fast, uses GIN index
var results = await context.Posts
    .Where(p => p.SearchVector.Matches(
        EF.Functions.WebSearchToTsQuery("english", userInput)))
    .OrderByDescending(p => p.SearchVector.Rank(
        EF.Functions.WebSearchToTsQuery("english", userInput)))
    .Take(20)
    .ToListAsync();
```

### §6.3 Query Builder Variants

```csharp
// Phrase search
EF.Functions.PhraseToTsQuery("english", "quick brown fox")

// Web-style (Google-like, safe for user input)
EF.Functions.WebSearchToTsQuery("english", userInput)

// Plain (no operators, safest for untrusted input)
EF.Functions.PlainToTsQuery("english", userInput)

// Structured (requires operators — only for trusted input)
EF.Functions.ToTsQuery("english", "cats & (dogs | fish)")
```

---

## §7 Migrations

### §7.1 Seeding with UseAsyncSeeding (EF9+)

```csharp
// ❌ old — HasData embeds data in migrations, breaks on any data change
modelBuilder.Entity<Role>().HasData(
    new Role { Id = 1, Name = "Admin" });

// ✅ EF9+ — runs outside migrations, idempotent, no migration regeneration
optionsBuilder.UseNpgsql(connectionString)
    .UseAsyncSeeding(async (context, _, ct) =>
    {
        if (!await context.Set<Role>().AnyAsync(ct))
        {
            context.Set<Role>().AddRange(
                new Role { Name = "Admin" },
                new Role { Name = "User" });
            await context.SaveChangesAsync(ct);
        }
    });

// Trigger during app startup (e.g. in Program.cs)
await using var scope = app.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.MigrateAsync();
// UseAsyncSeeding runs as part of EnsureCreated / Migrate
```

### §7.2 Named Default Constraints (EF10+)

```csharp
// Named individual constraint
modelBuilder.Entity<Post>()
    .Property(p => p.CreatedAt)
    .HasDefaultValueSql("NOW()", "DF_Post_CreatedAt");

// Name all default constraints automatically (recommended for PostgreSQL envs
// that use strict schema diffing tools)
modelBuilder.UseNamedDefaultConstraints();
```

### §7.3 FK Constraint Exclusion (EF11+)

```csharp
// Keeps the index, drops the FK constraint from migrations.
// Use for cross-schema references or app-layer-enforced integrity.
modelBuilder.Entity<Post>()
    .HasOne(p => p.Author).WithMany(a => a.Posts)
    .HasForeignKey(p => p.AuthorId)
    .ExcludeForeignKeyFromMigrations();
```

### §7.4 HasFillFactor for Keys and Indexes (EF9+)

```csharp
// Reduce page splits on write-heavy tables
modelBuilder.Entity<User>()
    .HasKey(e => e.Id)
    .HasFillFactor(80);

modelBuilder.Entity<User>()
    .HasIndex(e => e.Email)
    .HasFillFactor(80);
```

Translated to `WITH (FILLFACTOR = 80)` in PostgreSQL DDL. Typical value: 70–90 on
tables with frequent updates/deletes; 100 for append-only tables.

### §7.5 dotnet-ef.json Config (EF11+)

```json
// .config/dotnet-ef.json  (searched upward from cwd)
{
  "project": "src/App.Infrastructure",
  "startupProject": "src/App.Api",
  "context": "AppDbContext"
}
```

```shell
# Now these are equivalent:
dotnet ef migrations add Init
dotnet ef migrations add Init --project src/App.Infrastructure --startup-project src/App.Api --context AppDbContext

# Create migration and apply in one step (EF11)
dotnet ef database update Init --add

# Remove without a live DB connection (EF11)
dotnet ef migrations remove --offline
```

---

## §8 Compiled Models

### §8.1 Generate and Enable

```xml
<!-- App.Infrastructure.csproj -->
<PropertyGroup>
  <EFOptimizeContext>true</EFOptimizeContext>
</PropertyGroup>
```

```shell
dotnet ef dbcontext optimize \
  --project src/App.Infrastructure \
  --startup-project src/App.Api \
  --output-dir CompiledModels
```

### §8.2 No .UseModel() Call Needed (EF9+)

```csharp
// ❌ old — explicit model registration
optionsBuilder.UseNpgsql(connectionString)
              .UseModel(AppDbContextModel.Instance);

// ✅ EF9+ — auto-detected, remove the UseModel() call entirely
optionsBuilder.UseNpgsql(connectionString);
```

### §8.3 NativeAOT / Pre-Compiled Queries

Compiled models are a prerequisite for NativeAOT. After generating:
- Ensure no `dynamic` or runtime-reflection patterns in your model config.
- Validate with `dotnet publish -r linux-x64 --self-contained` and inspect AOT warnings.

---

## §9 DbContext Configuration Reference

### §9.1 Standard Setup (EF10 + Npgsql)

```csharp
services.AddDbContextPool<AppDbContext>(o =>
    o.UseNpgsql(
        configuration.GetConnectionString("Default"),
        npgsql =>
        {
            npgsql.MigrationsAssembly("App.Infrastructure");
            npgsql.CommandTimeout(30);
            npgsql.UseVector();         // if using pgvector
        })
    .UseSnakeCaseNamingConvention()     // Npgsql convention — matches PostgreSQL idiom
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment()));
```

### §9.2 Optimistic Concurrency (PostgreSQL xmin)

```csharp
// Entity — map to PostgreSQL's built-in xmin system column
public class Order
{
    public int    Id      { get; set; }
    public uint   Version { get; set; }  // mapped to xmin
}

// Configuration
modelBuilder.Entity<Order>()
    .UseXminAsConcurrencyToken();
```

---

## §10 Npgsql-Specific Extensions

### §10.1 ILike — Case-Insensitive Pattern Matching

```csharp
// ❌ old — ToLower + Like is not index-aware
.Where(e => EF.Functions.Like(e.Name.ToLower(), "%searchterm%"))

// ✅ Translates to PostgreSQL ILIKE — index-aware with citext or functional index
.Where(e => EF.Functions.ILike(e.Name, $"%{term}%"))
```

Use `ILike` for all case-insensitive pattern matches. Pair with a `pg_trgm` GIN index
or a `citext` column type for index-supported lookups at scale.

### §10.2 JSON Path Queries (Npgsql)

```csharp
// Check key existence — translates to jsonb ? 'Key' or jsonb_typeof
.Where(p => EF.Functions.JsonTypeof(p.Metadata, "$.sku") != null)

// Value containment — translates to @> operator
.Where(p => EF.Functions.JsonContains(p.Metadata!, @"{""weight"": 10}"))

// Scalar extraction
.Select(p => EF.Functions.JsonValue(p.Metadata!, "$.sku"))
```

**Do not use `EF.Functions.JsonPathExists`** on PostgreSQL — that method targets
SQL Server's `JSON_PATH_EXISTS` (EF11). Use `JsonTypeof` or `JsonContains` instead,
or drop to `FromSql` for complex jsonpath predicates.

### §10.3 pg_trgm Trigram Similarity

```csharp
// OnModelCreating — register extension
modelBuilder.HasPostgresExtension("pg_trgm");

// Migrations — add GIN trigram index
migrationBuilder.Sql(
    "CREATE INDEX ix_products_name_trgm ON products USING gin (name gin_trgm_ops)");

// Query — fuzzy match
var results = await context.Products
    .Where(p => EF.Functions.TrigramsSimilarity(p.Name, term) > 0.3)
    .OrderByDescending(p => EF.Functions.TrigramsSimilarity(p.Name, term))
    .ToListAsync();
```

Default similarity threshold 0.3 is a good starting point; tune per dataset.
`TrigramsWordSimilarity` and `TrigramsStrictWordSimilarity` are also available.

### §10.4 ApplyConfigurationsFromAssembly — Non-Public Constructors (EF9+)

```csharp
// ✅ EF9+ — private nested configuration classes are reached automatically
public class Artist
{
    public int    Id   { get; set; }
    public string Name { get; set; } = "";

    private class Config : IEntityTypeConfiguration<Artist>
    {
        private Config() { }

        public void Configure(EntityTypeBuilder<Artist> b)
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.Name).HasMaxLength(200);
        }
    }
}

// In OnModelCreating — no explicit registration needed
modelBuilder.ApplyConfigurationsFromAssembly(typeof(Artist).Assembly);
```

Pre-EF9, `ApplyConfigurationsFromAssembly` only reached types with public parameterless
constructors. Private nested configurations were silently skipped, which could leave
entities unconfigured without any warning.