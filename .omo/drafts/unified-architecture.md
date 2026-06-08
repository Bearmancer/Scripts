# Unified Architecture Specification (Draft)

**Status**: Draft / Synthesis Phase
**Goal**: Merge architecture sprawl into a single, decision-complete reference.

## 1. Core Database Principles
- **Infrastructure**: 1 PostgreSQL Database --> 4 Logical Schemas.
- **Deletion Policy**: 
    - **No Hard Deletes**: Normal operations cannot delete records.
    - **RESTRICT/NO ACTION**: All Foreign Keys use these to prevent accidental cascading loss. (A "Safety Lock" that blocks parents from being deleted if children exist).
    - **No Soft-Delete**: No `IsDeleted` flags.
    - **Cleanup**: Date-based bulk deletes during resync only.
- **Timezones**: Internally `timestamptz` (UTC). External presentation converted to IST.
- **Indexing**: Convention `idx_{table}_{columns}`.
- **Concepts Guide**: Refer to the "Database Basics" section for intuitive explanations of FKs, Slugs, and Enums.

## 2. Technical Implementation "Gold Standards"

### 🛠️ EF Core 10 Schema Management
- **Pattern**: Use `ToTable("TableName", schema: "SchemaName")` in Fluent API for explicit domain mapping.
- **Migration Strategy**: Use a **Single Migration Context** for schema updates and separate **Domain Contexts** for application logic to avoid "Referenced table created twice" errors.
- **FK Warning**: Be aware that `Down()` methods in migrations may drop the wrong FK if entities share the same name across different schemas.

### 🧪 High-Performance Testing (TUnit/xUnit)
- **Pattern**: **Per-Test Schema Isolation**. 
- **Logic**: Share one PostgreSQL container, but generate a unique schema (e.g., `test_guid`) per test.
- **Lifecycle**: `Symmetry` (Migrate in Setup --> `DROP SCHEMA ... CASCADE` in Cleanup). This allows full parallelization without state leakage.

### 📦 Metadata Storage (`jsonb` vs Structured)
- **The Tipping Point**: Move from `jsonb` to structured columns IF:
    1. You need to `ORDER BY` or `GROUP BY` the field.
    2. The field is a Foreign Key.
    3. The field is used in $>70\%$ of `WHERE` clauses.
    4. The table exceeds $1\text{M}$ rows (to avoid GIN write amplification).
- **Implementation**: Use **Complex Types** with `.ToJson()` in EF Core 10.

### 🚀 Compiled Models & CI/CD
- **Resolution**: Treat Compiled Models as **Build Artifacts**.
- **Pipeline**: `dotnet ef dbcontext optimize` --> Commit `.g.cs` files to Git --> `options.UseModel(CompiledModel.Instance)`.
- **Verification**: CI must fail if committed compiled models drift from entity definitions.

## 2. Schema Definitions

### 📺 `youtube` Schema
*Purpose: Scraped YouTube playlist metadata.*
- **Table: `videos`**
    - Column Order: Title, Translated Title, Description, Translated Description, Channel Name, Upload Date, Duration, URL.
    - Naming: `TranslatedTitle`, `TranslatedDescription` (Standardized).
    - Constraints: Unique URL, Trigram index on titles.

### 🎵 `music` Schema
*Purpose: Last.fm scrobble data.*
- **Table: `artists`**, **`albums`**, **`tracks`**, **`scrobbles`**.
- **Refactor**: Classical music ranking moved to a separate `classical` schema to avoid "Schema Pollution" (excessive nulls).
- **Cleanup**: Remove `albums.ReleaseDate`.
- **Validation**: Verify if `scrobbles.Platform` is provided by API; delete if not.
- **Mapping**: Use a cross-domain mapping table (`music.scrobble_to_classical_map`) to link Last.fm tracks to Classical recordings.

### 🎻 `classical` Schema (NEW)
*Purpose: Classical music ranking and recording tracking.*
- **Table: `works`**: Abstract compositions (Composer, Work Number, Key, Catalogue No).
- **Table: `movements`**: Parts of a work (Position, Name).
- **Table: `recordings`**: Specific captured instances (RecordingDate, Medium).
- **Table: `performers`**: People/Ensembles (Name, Type).
- **Table: `recording_performers`**: Many-to-many mapping (Role).
- **Table: `venues`**: Physical location of live recordings.
- **Column Rename**: `CreatedAt` --> `RecordingDate`.

### 🛠️ `work` Schema
*Purpose: Native work tracker (Linear-esque).*
- **Table: `projects`**, **`issues`**.
- **Simplification**: Remove `Priority`, `PrioritySort`, `Estimate`.
- **Structure**: Project --> Issue (1:N), Issue --> Issue (Self-referencing).

### ⚙️ `public` Schema
*Purpose: Technical infrastructure and logging.*
- **Table: `execution_logs`**
    - Columns: Id, Timestamp, CorrelationId (Groups related executions), TaskName, Payload, Status (Succeeded/Failed/Partial), ErrorMessage, DurationMs, ExitCode.
- **Refactor**: Merged `failed_tasks` into `execution_logs` to remove redundancy.
- **ID Mapping**: Move `source_records` functionality directly into target entities as `ExternalId` and `SourceSystem` columns to eliminate bottlenecks and joins.

## 3. Visualizations & Documentation (Planned)
- **Master Diagram**: A single comprehensive Mermaid diagram showing all 4 schemas and their inter-relations.
- **EF Reference Doc**: An exhaustive guide explaining every type, field, purpose, and relationship for every entity.

## 4. Open Questions / Unresolved
- [To be filled from research results]
