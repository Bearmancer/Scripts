# Draft: Music/Work Schema & Integrity Policy

## Goal
Finalize the music/work schema design, implement the Work abstraction, establish a "Pure NULL" data integrity policy, and implement automated orphan purging.

## Current Status
- **Exploration Complete**: Entities mapped, sync flow analyzed, fixture state identified.
- **Key Finding**: Entities are currently non-nullable; `music.works` is missing; Tests lack isolation.

## Requirements & Decisions
### 1. Schema & Entities
- [x] **Pure NULL Policy**: Convert `ArtistId` and `AlbumId` to `int?` in `Track` and `Album`.
- [x] **Work Abstraction**: Create `music.works` table to group recordings across artists/albums.
- [x] **Classical Overlay**: Create `classical.movements` table for hierarchical work structures.
- [x] **Constraints**: Use `RESTRICT` on all FKs. No `CASCADE`.
- [x] **Integrity Mandate**: If a descriptive field is present (e.g., Artist Name), the corresponding ID MUST be present.

### 2. Data Integrity (Manual Purge)
- [x] **Manual Purge (L1-L3)**:
    - Trigger: Manual refresh/resync of scrobbles from a specific date.
    - Sequence: 
        1. Refresh scrobbles.
        2. L1: Purge Tracks with 0 references.
        3. L2: Purge Albums with 0 tracks.
        4. L3: Purge Artists with 0 albums/tracks.
    - Execution: Wrapped in a single transaction post-refresh.

### 3. Test Infrastructure
- [x] **Symmetry Isolation**: Move `PostgresFixture` to per-test schema isolation.

## Open Questions
- [ ] **Nullability Justification**: Need a detailed field-by-field assessment of nullable vs non-nullable.

