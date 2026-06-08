# Work Plan: Music/Work Schema & Integrity Policy

## Core Objective
Finalize the music schema to support a universal Work abstraction, enforce a "Pure NULL" data integrity policy, implement a manual L1-L3 orphan purge, and resolve Symmetry test concurrency via per-test schema isolation.

## Key Decisions
- **Pure NULL Architecture**: All optional foreign keys (ArtistId, AlbumId, WorkId) must be nullable (`int?`). No sentinel values (e.g., -1 or "Unknown") are permitted.
- **Universal Work Abstraction**:
    - `music.works`: A conceptual piece of music.
    - `classical.movements`: Hierarchical structure for works (e.g., Symphony 5, Mvt I).
    - Tracks map to Movements or directly to Works.
- **Manual Orphan Purge**: A 3-stage cleanup (Tracks $\rightarrow$ Albums $\rightarrow$ Artists) triggered exclusively during a manual resync/refresh from a specific date.
- **Symmetry Test Isolation**: `PostgresFixture` will provision a unique PostgreSQL schema per test to eliminate inter-test state collision.

## Implementation Phases

### Phase 0: Justification & Data Integrity
1. `NullabilityAudit`: Run a specialized assessment of every field in `Artist`, `Album`, `Track`, `MusicWork`, and `Movement` to justify nullable vs non-nullable status - expect a justification report.
2. `IntegrityPolicy`: Define and document the "Presence $\rightarrow$ ID" mandate (e.g., if `ArtistName` is present, `ArtistId` must exist) - expect a data validation spec.

### Phase 1: Schema Evolution (Pure NULL & Work Abstraction)
1. `Track.cs` & `Album.cs`: Convert `ArtistId` and `AlbumId` to `int?` - expect nullable properties.
2. `TrackConfiguration.cs` & `AlbumConfiguration.cs`: Update Fluent API to allow nulls for these FKs - expect `IsRequired(false)`.
3. `MusicWork.cs`: Create new entity for `music.works` (Id, Title, Composer, etc.) - expect new file.
4. `Movement.cs`: Create new entity for `classical.movements` (Id, WorkId, Position, Title) - expect new file.
5. `Track.cs`: Add `WorkId` (`int?`) and `MovementId` (`int?`) - expect new nullable FKs.
6. `TrackConfiguration.cs`: Map `WorkId` and `MovementId` with `DeleteBehavior.Restrict` - expect new relationships.
7. `DbContext`: Add `DbSet<MusicWork>` and `DbSet<Movement>` - expect new sets.
8. `Migrations`: Generate and apply a migration to implement the above schema changes - expect DB update.

### Phase 2: Work Abstraction Logic
1. `WorkService`: Create a service to handle the "Same Song" mapping logic (resolving multiple recording metadata to a single `MusicWork`) - expect logic for deduplication.
2. `ScrobbleSyncOrchestrator`: Inject `WorkService` into the sync flow to assign `WorkId` to Tracks during ingestion - expect tracks linked to works.
3. `Track.cs`: Implement computed property `DisplayArtist` (handles `Artist?.Name ?? "Unknown Artist"`) to hide nullable ceremony from UI - expect clean string output.
4. `Tests`: Add test cases for mapping different recordings of the same work to one `MusicWork` entity - expect 1 Work, N Tracks.

### Phase 3: Manual Orphan Purge
1. `PurgeService`: Create a service to implement the L1-L3 purge logic (L1: Tracks $\rightarrow$ L2: Albums $\rightarrow$ L3: Artists) - expect deletion of records with 0 references.
2. `PurgeService`: Implement the "Safe Delete" check (ensuring no `RESTRICT` violations occur during the sequence) - expect transactional integrity.
3. `ScrobbleSyncOrchestrator`: Implement the trigger for `PurgeService` specifically during a manual "refresh from date" operation - expect purge called only on manual resync.
4. `Tests`: Create a test scenario where a manual resync leaves an Album empty, and verify it is purged by the L2 logic - expect Album removed.

### Phase 4: PostgresFixture Redesign
1. `PostgresFixture`: Modify `InitializeAsync` to create a "golden" template schema instead of just migrating `public` - expect template ready.
2. `PostgresFixture`: Update `GetContext()` to generate a unique schema name (e.g., `test_{guid}`) and create it via `CREATE SCHEMA` - expect unique schema.
3. `PostgresFixture`: Implement schema cloning or per-test migration execution to populate the new schema - expect fully migrated test DB.
4. `PostgresFixture`: Configure `DbContextOptions` to use the generated schema via `SearchPath` in the connection string - expect context routed to test schema.
5. `PostgresFixture`: Implement `DisposeAsync` logic to drop temporary schemas after test completion - expect clean DB.
6. `Symmetry Tests`: Execute the test suite in parallel and verify that no inter-test state collision occurs - expect all tests pass.

### Phase 5: Final Verification Wave
Tasks to verify data integrity, concurrency, and the "Pure NULL" state.

## Final Verification Wave
- [ ] F1. All optional FKs are `int?` in C# and `NULLABLE` in PGSQL.
- [ ] F2. A manual refresh from date successfully triggers the L1-L3 purge and removes orphans without deleting active data.
- [ ] F3. Symmetry tests run in parallel without interference (verified via concurrent test execution).
- [ ] F4. Classical movements are correctly linked to Works and Tracks.

**User "Okay" required before marking work complete.**
