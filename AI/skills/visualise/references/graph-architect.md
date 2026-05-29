# Graph Architect

Structural knowledge mapping for books (EPUB) and Java codebases. Focuses on node-link relationship diagrams with chapter/module gating.

## Rendering Modes

| Mode    | Purpose                  | Faction   | Edges                     |
| ------- | ------------------------ | --------- | ------------------------- |
| Stalin  | Relationship network     | By group  | Particles/arrows/dashed   |
| Teacher | Educational overlay      | By domain | All solid, concept badges |
| Bulow   | Genealogy/hierarchy tree | By level  | Ancestry coloring         |

## Data Model

**CanonicalEnvelope** (backend, superset):
- `metadata`, `chapterGate`, `nodes[]`, `links[]`
- Backend-only fields: `introducedChapter`, `fate`

**FrontendPayload** (projected):
- `projectPayload(envelope, mode)`: Gate → Project → Legend
- Strips `introducedChapter`; strips `fate` in Teacher mode

**API**: `GET /api/graph?chapter={N}&mode={stalin|teacher|bulow}`

## Character Presence Rule

Hide node if: `(lastActiveChapter + 2 < confirmedChapter) AND (reappearsAfterChapter undefined OR > confirmedChapter)`

## Book Pipeline

`EpubProgressTracker`: parses EPUB via `zipfile` + `bs4`.
`StoryGraphAgent`: `advance_to_chapter(N)` → `CanonicalEnvelope`.

| Graph Field         | Book Source                 |
| ------------------- | --------------------------- |
| `id`                | Character name slug         |
| `faction`           | Allegiance/group            |
| `centrality`        | Narrative importance (1–20) |
| `introducedChapter` | First chapter appearance    |

## Java Pipeline

Static analysis via `javalang` / `tree-sitter-java`.

| Graph Field         | Java Source                      |
| ------------------- | -------------------------------- |
| `id`                | Fully-qualified class name slug  |
| `faction`           | Package group                    |
| `centrality`        | Inbound deps + inheritance depth |
| `introducedChapter` | Learning module number           |

Edge types: `extends`, `implements`, `uses`, `creates`, `calls`, `annotated_by`

## Schema Reference

```ts
interface CanonicalNode {
  id: string; label: string; faction: Faction; centrality: number;
  bio: string; fate: Fate; introducedChapter: number;
  lastActiveChapter?: number; reappearsAfterChapter?: number;
  educationalNote?: string; conceptTags?: string[];
}
interface CanonicalEdge {
  source: string; target: string; type: EdgeType; weight: number;
  introducedChapter: number; educationalContext?: string;
}
type Fate = 'survived' | 'purged_shot' | 'suicide' | 'natural_death' | 'assassinated' | 'died_in_captivity';
type ViewMode = 'stalin' | 'teacher' | 'bulow';
```

## Stack

| Layer         | Technology                     |
| ------------- | ------------------------------ |
| Visualization | `react-force-graph-2d`         |
| Forces        | `d3-force`                     |
| Frontend      | Vite + React + TypeScript      |
| EPUB parsing  | Python 3.14 + `beautifulsoup4` |
