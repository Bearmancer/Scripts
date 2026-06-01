# DbContext & Configuration — Consolidated Research

**Consolidated from:** 20260522-t1-03-dbcontext-config-research.md, 20260522-t1-04-entity-configs-research.md, angle-2-compiled-model.md, angle-4-pendingmodelchanges.md

---

## 1. DbContext Status (Phase 03)

### 1.1 Current ScriptsDbContext.cs

**File:** `csharp/src/Data/ScriptsDbContext.cs` (24 lines)

```csharp
internal sealed class ScriptsDbContext : DbContext
{
    public ScriptsDbContext(DbContextOptions<ScriptsDbContext> options)
        : base(options: options) => ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<Track> Tracks => Set<Track>();
    public DbSet<EntityScrobble> Scrobbles => Set<EntityScrobble>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<ExecutionLog> ExecutionLogs => Set<ExecutionLog>();
    public DbSet<FiberyEntity> FiberyEntities => Set<FiberyEntity>();
    public DbSet<FailedTask> FailedTasks => Set<FailedTask>();

    protected override void OnModelCreating(ModelBuilder mb) =>
        mb.ApplyConfigurationsFromAssembly(assembly: typeof(ScriptsDbContext).Assembly);
}
```

### 1.2 Feature Checklist

| Feature                           | Status   | Details                                                                                                  |
| --------------------------------- | -------- | -------------------------------------------------------------------------------------------------------- |
| NoTracking as default             | **DONE** | Set in constructor via `ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking`          |
| `ApplyConfigurationsFromAssembly` | **DONE** | Called in `OnModelCreating` with `typeof(ScriptsDbContext).Assembly`                                     |
| `OnConfiguring` override          | NONE     | Not present. Connection string is passed via `DbContextOptions` (DI) or `DbContextFactory` (design-time) |
| DbSet for each mapped entity      | **8/9**  | Missing: `DbSet<SourceRecord>`                                                                           |

### 1.3 Gap Analysis

#### Unmapped Entity: `SourceRecord`

- **File:** `csharp/src/Data/Entities/SourceRecord.cs`
- **Missing:**
  - No `DbSet<SourceRecord>` in `ScriptsDbContext`
  - No `SourceRecordConfiguration.cs` in `Configuration/` folder
  - No table mapping defined
- **Impact:** Entity exists in code but is invisible to EF Core — cannot be queried, migrated, or seeded.

#### Missing PostgreSQL Extensions

- **No `HasPostgresExtension("unaccent")`** — required for accent-insensitive functional indexes
- **No `HasPostgresExtension("pg_trgm")`** — required for trigram similarity functional indexes

### 1.4 Required Changes

| Priority | Task                                                                    |
| -------- | ----------------------------------------------------------------------- |
| **P0**   | Add `DbSet<SourceRecord>` for the unmapped `SourceRecord` entity        |
| **P0**   | Create `SourceRecordConfiguration.cs` mapping to `source_records` table |
| **P1**   | Add `HasPostgresExtension("unaccent")` in `OnModelCreating`             |
| **P1**   | Add `HasPostgresExtension("pg_trgm")` in `OnModelCreating`              |

---

## 2. Entity Configuration Files (8 files, all present)

All configurations implement `IEntityTypeConfiguration<T>` and live in `csharp/src/Data/Configuration/`:

| File                           | Entity         | Table             | Key/Identity                     | Notable                                                         |
| ------------------------------ | -------------- | ----------------- | -------------------------------- | --------------------------------------------------------------- |
| `ArtistConfiguration.cs`       | `Artist`       | `artists`         | `UseIdentityAlwaysColumn`        | Unique index on `Name`, JSONB `Metadata`                        |
| `AlbumConfiguration.cs`        | `Album`        | `albums`          | `UseIdentityAlwaysColumn`        | FK to Artist, unique index on `(ArtistId, Title)`               |
| `TrackConfiguration.cs`        | `Track`        | `tracks`          | `UseIdentityAlwaysColumn`        | FK to Artist + Album, index on Title                            |
| `ScrobbleConfiguration.cs`     | `Scrobble`     | `scrobbles`       | `UseIdentityAlwaysColumn`        | FK to Track, unique on `(TrackId, ScrobbledAt)`, `timestamptz`  |
| `VideoConfiguration.cs`        | `Video`        | `videos`          | `UseIdentityAlwaysColumn`        | Unique index on `Url`, JSONB `Metadata`                         |
| `ExecutionLogConfiguration.cs` | `ExecutionLog` | `execution_logs`  | `HasKey` + `ValueGeneratedOnAdd` | `timestamptz` with `CURRENT_TIMESTAMP` default, JSONB `Payload` |
| `FiberyEntityConfiguration.cs` | `FiberyEntity` | `fibery_entities` | `HasKey` (no identity)           | JSONB `RawData`                                                 |
| `FailedTaskConfiguration.cs`   | `FailedTask`   | `failed_tasks`    | `HasKey` + `ValueGeneratedOnAdd` | `timestamptz` with `CURRENT_TIMESTAMP` default                  |

---

## 3. Configuration Gaps (Phase 04)

### 3.1 Critical Gaps

1. **SourceRecordConfiguration missing** — Entity exists but no fluent config
2. **FiberyEntity missing composite unique index** — `(FiberyId, EntityType)` must be unique
3. **VideoConfiguration uses non-`static` lambdas** — All other configs use `static` lambdas (style inconsistency)

### 3.2 High Priority Gaps

4. **Scrobble.Platform missing index** — Primary query filter
5. **ExecutionLog.SessionId missing index** — Primary grouping key
6. **FailedTask.TaskName missing index** — Primary query filter
7. **FiberyEntity.EntityType missing index** — Primary filter
8. **Video.UploadDate missing column type** — Should be `"date"`, not default timestamp

### 3.3 Medium Priority Gaps

9. **Album.ReleaseDate missing column type** — Should be `"date"`
10. **Video.SyncedAt missing column type** — Should be `"timestamptz"`
11. **Scrobble.ScrobbledAt missing standalone index** — Time-range queries
12. **Track missing composite unique index** — `(ArtistId, Title)` should be unique
13. **ExecutionLog.Timestamp missing index** — Time-range queries
14. **FailedTask.Timestamp missing index** — Time-range queries

### 3.4 Low Priority Gaps

15. **Unnamed indexes** — Video, FK, management entities should have database names
16. **PK strategy inconsistency** — Music entities use `UseIdentityAlwaysColumn`, management use `ValueGeneratedOnAdd`
17. **ExecutionLog.ExitCode missing index** — Filter by success/failure

---

## 4. Compiled Models & PendingModelChangesWarning

### 4.1 The Compiled Model Lock

**Purpose:** Compiled models bypass the overhead of `OnModelCreating` reflection. EF Core uses pre-generated source code to instantly load the entity metadata at startup.

**The Lock (By-Design Limitation):**
- According to Microsoft Docs: "The model must be manually synchronized by regenerating it any time the model definition or configuration change."
- When you call `UseModel(MyDbContextModel.Instance)`, EF Core completely ignores `OnModelCreating`.
- Any runtime modifications to the model will not be evaluated if the context is initialized with `UseModel`.

### 4.2 PendingModelChangesWarning (EF Core 9+)

**Behavior Change:** Starting in EF Core 9, if the runtime model (the output of `OnModelCreating`) has pending changes compared to the last migration snapshot, EF Core throws an exception by default when `MigrateAsync` is called.

**Why:** "Forgetting to add a new migration after making model changes is a common mistake that can be hard to diagnose... The new exception ensures that the app's model matches the database."

**Resolution:** Whenever `OnModelCreating` is updated—even just to remove an `Ignore` statement for an internal type mapping—the migration snapshot must be updated via `dotnet ef migrations add <Name>` and the compiled model must be regenerated via `dotnet ef dbcontext optimize`.

### 4.3 Workflow for Configuration Changes

1. **Modify `OnModelCreating`** (e.g., remove `mb.Ignore<JsonDocument>()`)
2. **Add a migration:** `dotnet ef migrations add <DescriptiveName>`
3. **Regenerate compiled model:** `dotnet ef dbcontext optimize --project src/Data/Scripts.Data.csproj --output-dir CompiledModels`
4. **Build and test:** `dotnet build` and `dotnet test`

---

## 5. DbContext Registration

### 5.1 Current DbContextRegistration.cs

**File:** `csharp/src/Data/DbContextRegistration.cs:5-13`

```csharp
internal static class DbContextRegistration
{
    public static IServiceCollection AddScriptsDbContext(this IServiceCollection services)
    {
        var connStr = GetEnvironmentVariable("PGCONNSTR") ?? throw ...;
        return services.AddDbContext<ScriptsDbContext>(opts => opts.UseNpgsql(connStr));
    }
}
```

### 5.2 Missing: EnableRetryOnFailure

**Gap:** No EF Core retry strategy is configured. Both entry points for DbContext creation lack `EnableRetryOnFailure`.

**Required Change:**

```csharp
services.AddDbContext<ScriptsDbContext>(opts => opts.UseNpgsql(connectionString: connStr,
    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
        maxRetryCount: 3,
        maxRetryDelay: TimeSpan.FromSeconds(30),
        errorCodesToAdd: null
    )));
```

---

## 6. Design-Time Factory

### 6.1 Current ScriptsDbContextFactory.cs

**File:** `csharp/src/Data/ScriptsDbContextFactory.cs:14`

```csharp
optionsBuilder.UseNpgsql(connectionString: connStr);
```

**Missing:** `EnableRetryOnFailure` (needed for `dotnet ef` commands too)

---

## 7. Summary of Required Changes

### Critical (blocking)
1. Create `SourceRecordConfiguration.cs`
2. Add `DbSet<SourceRecord>` to `ScriptsDbContext`
3. Add unique composite index on `FiberyEntity.(FiberyId, EntityType)`
4. Add `HasPostgresExtension("unaccent")` and `HasPostgresExtension("pg_trgm")` to `OnModelCreating`

### High Priority
5. Add `Platform` index + column type on `ScrobbleConfiguration`
6. Add `SessionId` index on `ExecutionLogConfiguration`
7. Add `TaskName` index on `FailedTaskConfiguration`
8. Add `FiberyId` and `EntityType` indexes on `FiberyEntityConfiguration`
9. Add `UploadDate` column type `"date"` on `VideoConfiguration`
10. Add `EnableRetryOnFailure` to `DbContextRegistration` and `ScriptsDbContextFactory`

### Medium Priority
11. Add `Mbid` indexes on Artist, Album, Track (but Mbid properties being removed in Phase 02)
12. Add `ReleaseDate` column type `"date"` on `AlbumConfiguration`
13. Add `SyncedAt` column type `"timestamptz"` on `VideoConfiguration`
14. Add standalone `ScrobbledAt` index on `ScrobbleConfiguration`
15. Add composite unique `(ArtistId, Title)` on `TrackConfiguration`
16. Add `Timestamp` indexes on management entities

### Low Priority
17. Fix lambda style in `VideoConfiguration` (`v =>` → `static v =>`)
18. Name unnamed indexes (Video, FK, management entities)
19. Standardize PK strategy (`ValueGeneratedOnAdd` → `UseIdentityAlwaysColumn` for int PKs)

---

## 8. File Paths

```
DbContext:
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContext.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\DbContextRegistration.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\ScriptsDbContextFactory.cs

Configurations:
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\ArtistConfiguration.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\AlbumConfiguration.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\TrackConfiguration.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\ScrobbleConfiguration.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\VideoConfiguration.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\ExecutionLogConfiguration.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\FailedTaskConfiguration.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Configuration\FiberyEntityConfiguration.cs
```
