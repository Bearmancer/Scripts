# Implementation Plan

## Phase 1: EF Schema Robust Enhancements

- [x] Task 1.1: Fix translated properties casing. Update `EF_SCHEMA_AUTHORITY.md` and related drafts/documentation to use EF compliant PascalCase (`TranslatedTitle` and `TranslatedDescription`) instead of `TitleEn` and `DescriptionEn`. Ensure the `videos` schema explicitly reflects this change.
- [x] Task 1.2: Assess and robustly enhance the `ExternalId` schema for Music. Remove the `source_records` table from the `public` schema. Add `ExternalId` (or specific fields like `MusicBrainzId`, `DiscogsId`) and `SourceSystem` to relevant entities in the `music` schema (e.g., `artists`, `albums`, `tracks`, `release_progress`). This clearly separates MusicBrainz search lookup identities from Last.fm scrobble events. Ensure `EF_SCHEMA_AUTHORITY.md` is fully updated with these entity changes.
- [x] Task 1.3: Generate a proper comprehensive Mermaid ER Diagram visualizing all 4 updated schemas (`youtube`, `music`, `work`, `public`). Embed the diagram directly into `EF_SCHEMA_AUTHORITY.md` under the "ER Relationship Summary" section. Ensure it reflects the removed `source_records`, updated `ExternalId` columns, and corrected casing.
