# Entity Design & Refactoring — Consolidated Research

**Consolidated from:** 20260522-t1-02-entity-refactoring-research.md, 20260522-t1-04-entity-configs-research.md, angle-3-jsondocument.md

---

## 1. Entity Inventory & Status

| Entity       | File              | PK Type           | Config | DbSet | Status                       |
| ------------ | ----------------- | ----------------- | ------ | ----- | ---------------------------- |
| Artist       | `Artist.cs`       | `int` identity    | ✅      | ✅     | Has obsolete `Mbid` — DELETE |
| Album        | `Album.cs`        | `int` identity    | ✅      | ✅     | Has obsolete `Mbid` — DELETE |
| Track        | `Track.cs`        | `int` identity    | ✅      | ✅     | Has obsolete `Mbid` — DELETE |
| Scrobble     | `Scrobble.cs`     | `long` identity   | ✅      | ✅     | Clean                        |
| Video        | `Video.cs`        | `long` identity   | ✅      | ✅     | Diverges from plan spec      |
| ExecutionLog | `ExecutionLog.cs` | `int` serial      | ✅      | ✅     | Clean                        |
| FailedTask   | `FailedTask.cs`   | `int` serial      | ✅      | ✅     | Diverges from plan spec      |
| FiberyEntity | `FiberyEntity.cs` | `Guid` client-gen | ✅      | ✅     | Missing critical indexes     |
| SourceRecord | `SourceRecord.cs` | `Guid` client-gen | ❌      | ❌     | **UNMAPPED**                 |

---

## 2. Mbid Property Audit (Phase 02)

### 2.1 Properties Found

| Entity   | Line | Declaration                          | Usage Count |
| -------- | ---- | ------------------------------------ | ----------- |
| `Artist` | 8    | `public string? Mbid { get; init; }` | 0           |
| `Album`  | 10   | `public string? Mbid { get; init; }` | 0           |
| `Track`  | 11   | `public string? Mbid { get; init; }` | 0           |

### 2.2 Usage Search Results

**Zero external references found.** Full codebase grep for `.Mbid`, `"Mbid"`, and `nameof.*Mbid` across all `*.cs` files under `csharp/` returned only the 3 entity property declarations themselves. No service, orchestrator, CLI, configuration, or test code references these properties.

### 2.3 Recommendation

**REMOVE all three Mbid properties.** No migration needed (no migrations exist yet). No test files to update (tests directory does not exist at `csharp/tests/`).

### 2.4 Post-Removal Entity Shapes

**Artist.cs** (3 properties + 2 navs → 2 properties + 2 navs):
```csharp
internal sealed record Artist
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public JsonDocument? Metadata { get; init; }

    public ICollection<Album> Albums { get; } = [];
    public ICollection<Track> Tracks { get; } = [];
}
```

**Album.cs** (5 properties + 2 navs → 4 properties + 2 navs):
```csharp
internal sealed record Album
{
    public int Id { get; init; }
    public int ArtistId { get; init; }
    public string Title { get; init; } = null!;
    public DateOnly? ReleaseDate { get; init; }

    public Artist Artist { get; init; } = null!;
    public ICollection<Track> Tracks { get; } = [];
}
```

**Track.cs** (6 properties + 3 navs → 5 properties + 3 navs):
```csharp
internal sealed record Track
{
    public int Id { get; init; }
    public int ArtistId { get; init; }
    public int? AlbumId { get; init; }
    public string Title { get; init; } = null!;
    public int? Duration { get; init; }

    public Artist Artist { get; init; } = null!;
    public Album? Album { get; init; }
    public ICollection<Scrobble> Scrobbles { get; } = [];
}
```

---

## 3. Track Metadata Audit (Phase 02)

### 3.1 Current State

`Track.cs` has **no** `Metadata` property. The AGENTS.md schema lists `metadata JSONB` for `artists` only, not for tracks. The requirement "Remove `string? Metadata` from Track" is already satisfied — Track.cs has never had a Metadata property.

### 3.2 Recommendation

**No action needed.** Track.cs is already clean.

---

## 4. Legacy int ID Audit (Phase 02)

### 4.1 Current PK Types vs AGENTS.md Target Schema

| Entity         | Current PK                                    | AGENTS.md Target             | Match?           |
| -------------- | --------------------------------------------- | ---------------------------- | ---------------- |
| `Artist`       | `int Id`                                      | `id UUID PK`                 | **MISMATCH**     |
| `Album`        | `int Id` (FK: `int ArtistId`)                 | `id UUID PK`, `artist_id FK` | **MISMATCH**     |
| `Track`        | `int Id` (FK: `int ArtistId`, `int? AlbumId`) | `id UUID PK`                 | **MISMATCH**     |
| `Scrobble`     | `long Id` (FK: `int TrackId`)                 | `id BIGINT PK`               | OK (long=BIGINT) |
| `Video`        | `long Id`                                     | `id UUID PK`                 | **MISMATCH**     |
| `ExecutionLog` | `int Id`                                      | `id SERIAL` (auto-increment) | OK               |
| `FailedTask`   | `int Id`                                      | `id UUID`                    | **MISMATCH**     |

### 4.2 Recommendation

**Defer UUID migration to later phase.** The user's directive says "Remove legacy int IDs in favor of existing scheme." This is ambiguous given the current implementation. The int PKs are functional and the database doesn't exist yet (no migrations applied), so this is not a production concern. Focus Phase 02 on Mbid removal only.

---

## 5. Video Entity Discrepancy

### 5.1 Current Entity vs Plan Specification

| Property      | Current `Video.cs`            | Plan `01-entities.md` Task 5 |
| ------------- | ----------------------------- | ---------------------------- |
| `Id`          | `long`                        | `int`                        |
| `Url`         | `string` (unique index)       | —                            |
| `Title`       | `string`                      | `string`                     |
| `Description` | `string?`                     | —                            |
| `ChannelName` | `string` (indexed)            | —                            |
| `UploadDate`  | `DateOnly` (indexed)          | —                            |
| `SyncedAt`    | `DateTimeOffset`              | —                            |
| `Metadata`    | `Dict<string,string>` (jsonb) | —                            |
| `YoutubeId`   | —                             | `string`                     |
| `PlaylistId`  | —                             | `string`                     |
| `IsDeleted`   | —                             | `bool`                       |

The actual implementation diverges significantly from the plan. The plan's Video is YouTube-tracker focused (`YoutubeId`, `PlaylistId`, `IsDeleted` soft-delete), while the actual entity is more generic (`Url`, `ChannelName`, `UploadDate`, `SyncedAt`, `Metadata`).

### 5.2 Recommendation

**Flag for clarification.** T1-02 should not change Video unless directed. The current entity's `Dictionary<string, string> Metadata` is mapped as JSONB and is intentional (not obsolete).

---

## 6. FailedTask Entity Discrepancy

### 6.1 Current Entity vs Plan Specification

| Property       | Current `FailedTask.cs` | Plan `01-entities.md`        |
| -------------- | ----------------------- | ---------------------------- |
| `Id`           | `int`                   | `Guid`                       |
| `TaskName`     | `string`                | `Operation` (string)         |
| `ErrorMessage` | `string?`               | —                            |
| `Timestamp`    | `DateTimeOffset`        | `CreatedAt` (DateTimeOffset) |

The actual entity has more context (error message). Needs clarification.

---

## 7. JsonDocument Mapping (EF Core 10 + Npgsql 10)

### 7.1 The Issue

In EF Core 10 with Npgsql 10, a `System.NullReferenceException` is thrown inside `InMemoryTable` and `NpgsqlMigrator` when `GetKeyValueComparer()` is accessed for an entity property of type `System.Text.Json.JsonDocument` (e.g. `ExecutionLog.Payload`).

### 7.2 Root Cause

Npgsql 10 natively handles `JsonDocument` without manual configurations. However, if `mb.Ignore<System.Text.Json.JsonDocument>()` is declared in `OnModelCreating`, EF Core removes `JsonDocument` from the model metadata. But a property explicitly typed as `JsonDocument` still demands a `GetKeyValueComparer()` during context initialization. Because the type is globally ignored, EF Core throws a `NullReferenceException`.

### 7.3 Resolution

**Remove `mb.Ignore<System.Text.Json.JsonDocument>()`** from `OnModelCreating`. Allow EF Core and Npgsql to natively handle `JsonDocument` mapping.

### 7.4 Entities Using JsonDocument

| Entity         | Property   | Type                        | Column  |
| -------------- | ---------- | --------------------------- | ------- |
| `Artist`       | `Metadata` | `JsonDocument?`             | `jsonb` |
| `Video`        | `Metadata` | `Dictionary<string,string>` | `jsonb` |
| `ExecutionLog` | `Payload`  | `JsonDocument?`             | `jsonb` |
| `FiberyEntity` | `RawData`  | `JsonDocument?`             | `jsonb` |

---

## 8. Configuration Gaps (Phase 04)

### 8.1 Critical Gaps

1. **SourceRecordConfiguration missing** — Entity exists but no fluent config
2. **FiberyEntity missing composite unique index** — `(FiberyId, EntityType)` must be unique
3. **VideoConfiguration uses instance lambdas** — Should use `static` lambdas (style inconsistency)

### 8.2 High Priority Gaps

4. **Scrobble.Platform missing index** — Primary query filter
5. **ExecutionLog.SessionId missing index** — Primary grouping key
6. **FailedTask.TaskName missing index** — Primary query filter
7. **FiberyEntity.EntityType missing index** — Primary filter
8. **Video.UploadDate missing column type** — Should be `"date"`, not default timestamp

### 8.3 Medium Priority Gaps

9. **Album.ReleaseDate missing column type** — Should be `"date"`
10. **Video.SyncedAt missing column type** — Should be `"timestamptz"`
11. **Scrobble.ScrobbledAt missing standalone index** — Time-range queries
12. **Track missing composite unique index** — `(ArtistId, Title)` should be unique
13. **ExecutionLog.Timestamp missing index** — Time-range queries
14. **FailedTask.Timestamp missing index** — Time-range queries

### 8.4 Recommended SourceRecordConfiguration

```csharp
internal sealed class SourceRecordConfiguration : IEntityTypeConfiguration<SourceRecord>
{
    public void Configure(EntityTypeBuilder<SourceRecord> b)
    {
        b.ToTable(name: "source_records");
        b.HasKey(static e => e.Id);
        b.Property(static e => e.Id).HasDefaultValueSql(sql: "gen_random_uuid()");
        b.HasIndex(static e => e.SourceId).HasDatabaseName(name: "idx_source_records_source_id");
        b.HasIndex(static e => e.EntityType).HasDatabaseName(name: "idx_source_records_entity_type");
        b.HasIndex(static e => new { e.SourceId, e.EntityType })
            .IsUnique()
            .HasDatabaseName(name: "idx_source_records_source_entity_type");
        b.Property(static e => e.RawData).HasColumnType(typeName: "jsonb");
    }
}
```

Also add to DbContext: `public DbSet<SourceRecord> SourceRecords => Set<SourceRecord>();`

---

## 9. File Paths

```
Entities:
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Artist.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Album.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Track.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Scrobble.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\Video.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\ExecutionLog.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\FailedTask.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\FiberyEntity.cs
  C:\Users\Lance\Dev\Scripts\csharp\src\Data\Entities\SourceRecord.cs

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
