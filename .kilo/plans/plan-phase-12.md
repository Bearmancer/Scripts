# Phase 12: Domain Naming Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolve namespace and type naming collisions by suffixing database entities with `Entity`, suffixing DTOs with descriptive suffixes, and removing global `Models` usings.

**Architecture:** Rename entity classes and configuration classes, rename model records, remove global using, and add explicit using declarations in affected source files.

**Tech Stack:** C#, dotnet CLI

---

### Task 12.1: Rename `Artist` → `ArtistEntity`

**Files:**
- Modify: `csharp/src/Data/Entities/Artist.cs`
- Modify: `csharp/src/Data/ScriptsDbContext.cs`
- Modify: `csharp/src/Data/Configuration/ArtistConfiguration.cs`

- [ ] **Step 1: Rename the Artist class to ArtistEntity in the entity file**

**Pre-modification code chunk for `csharp/src/Data/Entities/Artist.cs`:**
```csharp
namespace CSharpScripts.Data.Entities;

internal sealed record Artist
{
	public int Id { get; init; }
	public string Name { get; init; } = null!;
```

**Post-modification code chunk for `csharp/src/Data/Entities/Artist.cs`:**
```csharp
namespace CSharpScripts.Data.Entities;

internal sealed record ArtistEntity
{
	public int Id { get; init; }
	public string Name { get; init; } = null!;
```

- [ ] **Step 2: Update DbSet in ScriptsDbContext**

**Pre-modification code chunk for `csharp/src/Data/ScriptsDbContext.cs`:**
```csharp
internal sealed class ScriptsDbContext : DbContext
{
	public ScriptsDbContext(DbContextOptions<ScriptsDbContext> options) : base(options) => ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

	public DbSet<Artist> Artists => Set<Artist>();
	public DbSet<Album> Albums => Set<Album>();
```

**Post-modification code chunk for `csharp/src/Data/ScriptsDbContext.cs`:**
```csharp
internal sealed class ScriptsDbContext : DbContext
{
	public ScriptsDbContext(DbContextOptions<ScriptsDbContext> options) : base(options) => ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

	public DbSet<ArtistEntity> Artists => Set<ArtistEntity>();
	public DbSet<Album> Albums => Set<Album>();
```

- [ ] **Step 3: Update configuration declaration**

**Pre-modification code chunk for `csharp/src/Data/Configuration/ArtistConfiguration.cs`:**
```csharp
namespace CSharpScripts.Data.Configuration;

internal sealed class ArtistConfiguration : IEntityTypeConfiguration<Artist>
{
	public void Configure(EntityTypeBuilder<Artist> b)
```

**Post-modification code chunk for `csharp/src/Data/Configuration/ArtistConfiguration.cs`:**
```csharp
namespace CSharpScripts.Data.Configuration;

internal sealed class ArtistConfiguration : IEntityTypeConfiguration<ArtistEntity>
{
	public void Configure(EntityTypeBuilder<ArtistEntity> b)
```

- [ ] **Step 4: Commit**

```bash
git add csharp/src/Data/Entities/Artist.cs csharp/src/Data/ScriptsDbContext.cs csharp/src/Data/Configuration/ArtistConfiguration.cs
git commit -m "refactor: rename Artist entity to ArtistEntity"
```

---

### Task 12.2: Rename `Album` → `AlbumEntity`

**Files:**
- Modify: `csharp/src/Data/Entities/Album.cs`
- Modify: `csharp/src/Data/ScriptsDbContext.cs`
- Modify: `csharp/src/Data/Configuration/AlbumConfiguration.cs`

- [ ] **Step 1: Rename the Album class to AlbumEntity**

**Pre-modification code chunk for `csharp/src/Data/Entities/Album.cs`:**
```csharp
namespace CSharpScripts.Data.Entities;

internal sealed record Album
{
	public int Id { get; init; }
	public int ArtistId { get; init; }
	public string Title { get; init; } = null!;
```

**Post-modification code chunk for `csharp/src/Data/Entities/Album.cs`:**
```csharp
namespace CSharpScripts.Data.Entities;

internal sealed record AlbumEntity
{
	public int Id { get; init; }
	public int ArtistId { get; init; }
	public string Title { get; init; } = null!;
```

- [ ] **Step 2: Update DbSet and relationships in entities**

Update `ScriptsDbContext.cs` and references in other entities (like `ArtistEntity.cs` navigation collection of `AlbumEntity`).

- [ ] **Step 3: Commit**

```bash
git commit -a -m "refactor: rename Album entity to AlbumEntity"
```

---

### Task 12.3: Rename `Track` → `TrackEntity`

**Files:**
- Modify: `csharp/src/Data/Entities/Track.cs`
- Modify: `csharp/src/Data/ScriptsDbContext.cs`
- Modify: `csharp/src/Data/Configuration/TrackConfiguration.cs`

- [ ] **Step 1: Rename class and update configurations**

Rename `Track` -> `TrackEntity` in entities and DbSet/configurations.

- [ ] **Step 2: Commit**

```bash
git commit -a -m "refactor: rename Track entity to TrackEntity"
```

---

### Task 12.4: Rename `Scrobble` → `ScrobbleEntity`

**Files:**
- Modify: `csharp/src/Data/Entities/Scrobble.cs`
- Modify: `csharp/src/Data/ScriptsDbContext.cs`
- Modify: `csharp/src/Data/Configuration/ScrobbleConfiguration.cs`

- [ ] **Step 1: Rename class and update configurations**

Rename `Scrobble` -> `ScrobbleEntity` in entities, configurations, and `ScriptsDbContext`.

- [ ] **Step 2: Commit**

```bash
git commit -a -m "refactor: rename Scrobble entity to ScrobbleEntity"
```

---

### Task 12.5: Rename `Video` → `VideoEntity`

**Files:**
- Modify: `csharp/src/Data/Entities/Video.cs`
- Modify: `csharp/src/Data/ScriptsDbContext.cs`
- Modify: `csharp/src/Data/Configuration/VideoConfiguration.cs`

- [ ] **Step 1: Rename class and update configurations**

Rename `Video` -> `VideoEntity` in entities, configurations, and `ScriptsDbContext`.

- [ ] **Step 2: Commit**

```bash
git commit -a -m "refactor: rename Video entity to VideoEntity"
```

---

### Task 12.6: Rename DTO models in `LastFm.cs`

**Files:**
- Modify: `csharp/src/Models/LastFm.cs`

- [ ] **Step 1: Rename `Scrobble` -> `LastFmScrobbleDto` and `FetchState` -> `LastFmFetchState`**

**Pre-modification code chunk for `csharp/src/Models/LastFm.cs`:**
```csharp
namespace CSharpScripts.Models;

internal sealed record Scrobble(
	string TrackName,
	string ArtistName,
	string AlbumName,
	DateTime? PlayedAt
)
{
	public string FormattedDate =>
		PlayedAt?.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "";
}

internal sealed record FetchState
{
	public int LastPage { get; init; }
```

**Post-modification code chunk for `csharp/src/Models/LastFm.cs`:**
```csharp
namespace CSharpScripts.Models;

internal sealed record LastFmScrobbleDto(
	string TrackName,
	string ArtistName,
	string AlbumName,
	DateTime? PlayedAt
)
{
	public string FormattedDate =>
		PlayedAt?.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "";
}

internal sealed record LastFmFetchState
{
	public int LastPage { get; init; }
```

- [ ] **Step 2: Commit**

```bash
git add csharp/src/Models/LastFm.cs
git commit -m "refactor: rename Last.fm model records to LastFmScrobbleDto and LastFmFetchState"
```

---

### Task 12.7: Rename `SearchResult` DTO in `Music.cs`

**Files:**
- Modify: `csharp/src/Models/Music.cs`

- [ ] **Step 1: Rename `SearchResult` -> `MusicSearchResult`**

**Pre-modification code chunk for `csharp/src/Models/Music.cs`:**
```csharp
internal sealed record SearchResult(
	MusicSource Source,
	string Id,
	string Title,
	string? Artist,
	int? Year,
	string? Format,
	string? Label,
	string? ReleaseType,
	int? Score = null,
	string? Country = null,
	string? CatalogNumber = null,
	string? Status = null,
	string? Disambiguation = null,
	List<string>? Genres = null,
	List<string>? Styles = null
);
```

**Post-modification code chunk for `csharp/src/Models/Music.cs`:**
```csharp
internal sealed record MusicSearchResult(
	MusicSource Source,
	string Id,
	string Title,
	string? Artist,
	int? Year,
	string? Format,
	string? Label,
	string? ReleaseType,
	int? Score = null,
	string? Country = null,
	string? CatalogNumber = null,
	string? Status = null,
	string? Disambiguation = null,
	List<string>? Genres = null,
	List<string>? Styles = null
);
```

- [ ] **Step 2: Commit**

```bash
git add csharp/src/Models/Music.cs
git commit -m "refactor: rename SearchResult DTO to MusicSearchResult"
```

---

### Task 12.8: Rename `ServiceUsage` DTO in `Cloud.cs`

**Files:**
- Modify: `csharp/src/Models/Cloud.cs`

- [ ] **Step 1: Rename `ServiceUsage` -> `AzureServiceUsage`**

**Pre-modification code chunk for `csharp/src/Models/Cloud.cs`:**
```csharp
internal record AzureUsageReport(
	string SubscriptionId,
	IReadOnlyList<ServiceUsage> Services,
	decimal TotalCost,
	string BillingPeriod
);

internal record ServiceUsage(string ServiceName, string Meter, decimal Cost, string Currency);
```

**Post-modification code chunk for `csharp/src/Models/Cloud.cs`:**
```csharp
internal record AzureUsageReport(
	string SubscriptionId,
	IReadOnlyList<AzureServiceUsage> Services,
	decimal TotalCost,
	string BillingPeriod
);

internal record AzureServiceUsage(string ServiceName, string Meter, decimal Cost, string Currency);
```

- [ ] **Step 2: Commit**

```bash
git add csharp/src/Models/Cloud.cs
git commit -m "refactor: rename ServiceUsage DTO to AzureServiceUsage"
```

---

### Task 12.9: Remove global using for Models namespace

**Files:**
- Modify: `csharp/src/GlobalUsings.cs`

- [ ] **Step 1: Remove the global using statement**

**Pre-modification code chunk for `csharp/src/GlobalUsings.cs`:**
```csharp
global using CSharpScripts.Core.Auth;
global using CSharpScripts.Data;
global using Microsoft.EntityFrameworkCore;
global using CSharpScripts.Models;
global using CSharpScripts.Services.Language;
global using CsvHelper;
```

**Post-modification code chunk for `csharp/src/GlobalUsings.cs`:**
```csharp
global using CSharpScripts.Core.Auth;
global using CSharpScripts.Data;
global using Microsoft.EntityFrameworkCore;
global using CSharpScripts.Services.Language;
global using CsvHelper;
```

- [ ] **Step 2: Fix any compilation errors by adding explicit `using CSharpScripts.Models;` to files using DTOs**

- [ ] **Step 3: Commit**

```bash
git commit -a -m "refactor: remove global using of CSharpScripts.Models"
```
