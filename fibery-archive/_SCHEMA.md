# Fibery Archive PostgreSQL Schema

A relational representation of the 47 markdown files in `fibery-archive/`. Each file follows the same 5-field Fibery template (Description, Plan, Prompt, Research, Validation) and can vary from a fully-fleshed-out issue to a stub with empty sections. The schema preserves that variability, supports topic clustering, duplicate detection, and present-state tracking of what currently exists.

Source directory: `C:\Users\Lance\Dev\Scripts\AI\references\fibery-archive\`
Source count: 47 markdown files
Date range observed: 2026-01-14 to 2026-05-06

---

## Mermaid ERD

*ER diagram removed - will be regenerated via mermaid skill in diagrams directory*

---

## Table Reference

### `issues`

One row per markdown file. Captures identity, current status, and the metadata that wraps the 5-field body.

| Column | Type | Description |
| --- | --- | --- |
| `id` | `SERIAL PRIMARY KEY` | Surrogate key. |
| `source_filename` | `TEXT NOT NULL UNIQUE` | Exact filename in the archive. |
| `original_title` | `TEXT` | Title from the first `#` heading after the Description marker. |
| `slug` | `TEXT NOT NULL` | Lowercased, hyphenated title. |
| `content_hash` | `TEXT NOT NULL` | SHA-256 of raw file contents. Used for duplicate detection. |
| `line_count` | `INTEGER NOT NULL` | Total lines in the source file. |
| `byte_count` | `INTEGER NOT NULL` | File size in bytes. |
| `fibery_issue_id` | `TEXT` | Original Fibery UUID if known. Null for files reconstructed from local Markdown. |
| `fibery_issue_number` | `INTEGER` | Original Fibery human-readable number when referenced inside the file. |
| `state` | `TEXT NOT NULL DEFAULT 'unknown'` | One of `research`, `plan`, `prompt`, `execution`, `validation`, `ticked`, `stub`, `unknown`. Derived from the Validation field. |
| `ticked` | `BOOLEAN` | Mirrors Fibery's `Ticked` checkbox. Null when the field never existed for the issue. |
| `ticked_state` | `TEXT` | Raw state: `true`, `false`, `null`, `unknown`. Captures the audit note that some issues have `Ticked=null` because they predate the field. |
| `earliest_date` | `DATE` | Earliest ISO date found in the body. |
| `latest_date` | `DATE` | Latest ISO date found in the body. |
| `summary` | `TEXT` | One-line human summary. Empty for stub files. |
| `created_from` | `TEXT` | Origin marker: `linear`, `fibery`, `agent-session`, `local-markdown`. |
| `ingested_at` | `TIMESTAMPTZ NOT NULL DEFAULT NOW()` | Import time. |

Constraints:

* `state` is free-form `TEXT` rather than an enum so the loader can preserve unusual markers (e.g. `backlog`).
* `ticked_state` accepts the four values seen in the source set.

---

### `issue_sections`

The 5-field Fibery template produces exactly 5 rows per issue. Captures both content and the stub-vs-real distinction.

| Column | Type | Description |
| --- | --- | --- |
| `id` | `SERIAL PRIMARY KEY` | Surrogate key. |
| `issue_id` | `INTEGER NOT NULL REFERENCES issues(id) ON DELETE CASCADE` | Parent issue. |
| `section_name` | `TEXT NOT NULL` | One of `description`, `plan`, `prompt`, `research`, `validation`. Stored lowercase. |
| `section_order` | `SMALLINT NOT NULL` | 1=description, 2=plan, 3=prompt, 4=research, 5=validation. |
| `content` | `TEXT NOT NULL` | Raw Markdown body with the `---` separator and `#` heading stripped. |
| `content_length` | `INTEGER NOT NULL` | Character count of `content`. |
| `word_count` | `INTEGER NOT NULL` | Word count of `content`. |
| `is_stub` | `BOOLEAN NOT NULL` | True when the section is just the header (`# Plan` followed by `---`) with no body. |
| `is_real` | `BOOLEAN NOT NULL GENERATED ALWAYS AS (NOT is_stub) STORED` | Convenience inverse of `is_stub`. |
| `status_marker` | `TEXT` | Explicit marker in the body: `pass`, `fail`, `ticked`, `pending`, `blocked`, `validated`, `null`. |

Constraints:

* `UNIQUE (issue_id, section_name)` guarantees exactly one row per field per issue.

---

### `topics`

Normalised lookup for topic clustering. Topics derive from filename prefixes and recurring project names.

| Column | Type | Description |
| --- | --- | --- |
| `id` | `SERIAL PRIMARY KEY` | Surrogate key. |
| `name` | `TEXT NOT NULL UNIQUE` | Display name (e.g. `Cline Hook`, `OCI`, `Parsec`, `Docker`, `Fibery`, `AI Agents`, `SSH`, `VS Code`, `Windows`, `Java`, `Network`, `Forensic`). |
| `slug` | `TEXT NOT NULL UNIQUE` | URL-safe form. |
| `category` | `TEXT` | Higher-level bucket: `agent-runtime`, `infrastructure`, `media-stack`, `language`, `process`, `tools`. |
| `description` | `TEXT` | One-line description of the topic. |

---

### `issue_topics`

Many-to-many join. A file can belong to multiple topics (e.g. `Eliminate duplicate.md` is both `Docker` and `VPN`).

| Column | Type | Description |
| --- | --- | --- |
| `issue_id` | `INTEGER NOT NULL REFERENCES issues(id) ON DELETE CASCADE` | |
| `topic_id` | `INTEGER NOT NULL REFERENCES topics(id) ON DELETE CASCADE` | |
| `confidence` | `REAL NOT NULL DEFAULT 1.0` | 1.0 for primary, 0.5 for secondary. |
| `source` | `TEXT` | How the tag was assigned: `filename-prefix`, `body-mention`, `manual`. |

Constraints:

* `PRIMARY KEY (issue_id, topic_id)`.

---

### `related_issues`

Self-referencing M2M. Captures duplicates and lateral references between active issues.

| Column | Type | Description |
| --- | --- | --- |
| `id` | `SERIAL PRIMARY KEY` | Surrogate key. |
| `source_issue_id` | `INTEGER NOT NULL REFERENCES issues(id) ON DELETE CASCADE` | The issue doing the referencing. |
| `target_issue_id` | `INTEGER NOT NULL REFERENCES issues(id) ON DELETE CASCADE` | The issue being referenced. |
| `relation_type` | `TEXT NOT NULL` | One of `duplicate`, `related`, `blocks`, `blocked-by`, `see-also`, `sub-issue`, `parent-issue`. |
| `notes` | `TEXT` | Free text explaining the link. |

Constraints:

* `CHECK (source_issue_id <> target_issue_id)` prevents self-loops.

---

### `issue_links`

External or internal links discovered in the body (Fibery guides, GitHub repos, file paths).

| Column | Type | Description |
| --- | --- | --- |
| `id` | `SERIAL PRIMARY KEY` | Surrogate key. |
| `source_issue_id` | `INTEGER NOT NULL REFERENCES issues(id) ON DELETE CASCADE` | |
| `link_type` | `TEXT NOT NULL` | `fibery-guide`, `github`, `local-file`, `documentation`, `external`. |
| `url` | `TEXT NOT NULL` | The URL or path. |
| `label` | `TEXT` | Anchor text or surrounding context. |

---

### `execution_log_entries`

When a file documents a stream of agent commands (e.g. `AI Agents — Executio.md`), each entry loads as a row.

| Column | Type | Description |
| --- | --- | --- |
| `id` | `SERIAL PRIMARY KEY` | Surrogate key. |
| `issue_id` | `INTEGER NOT NULL REFERENCES issues(id) ON DELETE CASCADE` | |
| `command_class` | `TEXT` | `skip`, `pipeline`, `action` per the Fibery execution log schema. |
| `command_text` | `TEXT` | Full text. Fibery Name field truncates to 80 chars. |
| `status` | `TEXT` | `success`, `failed`, `transient`, `logic-error`, `unknown`. |
| `reasoning_class` | `TEXT` | `Z` (transient), `L` (logic/perm), `U` (unknown). |
| `error_excerpt` | `TEXT` | First 500 chars of stderr. |
| `executed_at` | `TIMESTAMPTZ` | UTC timestamp from the log line. |

---

### `validation_results`

One or more rows per issue. The source set contains explicit `PASS` / `FAIL` / `PASSED` markers in the Validation field, plus issue numbers (e.g. `#185`, `#192`, `#195`, `#219`) referenced in bodies.

| Column | Type | Description |
| --- | --- | --- |
| `id` | `SERIAL PRIMARY KEY` | Surrogate key. |
| `issue_id` | `INTEGER NOT NULL REFERENCES issues(id) ON DELETE CASCADE` | |
| `result` | `TEXT NOT NULL` | `pass`, `fail`, `partial`, `blocked`, `pending`. |
| `verdict` | `TEXT` | Verbatim verdict line: `PASS`, `FAILED`, `VALIDATION FAILED - 4 issue(s) must be fixed`. |
| `summary` | `TEXT` | Multi-line summary block. |
| `validated_on` | `DATE` | Date parsed from the verdict line. |

---

### `section_checklist_items`

Checklist items (`- [ ]` and `- [x]`) extracted from any section.

| Column | Type | Description |
| --- | --- | --- |
| `id` | `SERIAL PRIMARY KEY` | Surrogate key. |
| `section_id` | `INTEGER NOT NULL REFERENCES issue_sections(id) ON DELETE CASCADE` | |
| `item_order` | `SMALLINT NOT NULL` | Position within the section. |
| `item_text` | `TEXT NOT NULL` | Checklist text with the `- [ ]` prefix stripped. |
| `completed` | `BOOLEAN NOT NULL` | True for `- [x]`. |

---

## Indexes

```sql
CREATE INDEX idx_issues_state ON issues(state);
CREATE INDEX idx_issues_ticked ON issues(ticked);
CREATE INDEX idx_issues_ticked_state ON issues(ticked_state);
CREATE INDEX idx_issues_fibery_number ON issues(fibery_issue_number);
CREATE INDEX idx_issues_state_ticked ON issues(state, ticked);
CREATE INDEX idx_issues_latest_date ON issues(latest_date DESC);
CREATE INDEX idx_issues_content_hash ON issues(content_hash);

CREATE INDEX idx_issue_sections_issue_id ON issue_sections(issue_id);
CREATE INDEX idx_issue_sections_stub ON issue_sections(is_stub) WHERE is_stub;
CREATE INDEX idx_issue_sections_real ON issue_sections(is_real) WHERE is_real;
CREATE INDEX idx_issue_sections_name ON issue_sections(section_name);

CREATE INDEX idx_issue_topics_topic_id ON issue_topics(topic_id);
CREATE INDEX idx_issue_topics_confidence ON issue_topics(confidence DESC);

CREATE INDEX idx_related_issues_source ON related_issues(source_issue_id);
CREATE INDEX idx_related_issues_target ON related_issues(target_issue_id);
CREATE INDEX idx_related_issues_type ON related_issues(relation_type);
CREATE INDEX idx_related_issues_duplicates
  ON related_issues(source_issue_id, target_issue_id)
  WHERE relation_type = 'duplicate';

CREATE INDEX idx_issue_links_source ON issue_links(source_issue_id);
CREATE INDEX idx_issue_links_type ON issue_links(link_type);

CREATE INDEX idx_execution_log_issue_id ON execution_log_entries(issue_id);
CREATE INDEX idx_execution_log_status ON execution_log_entries(status);
CREATE INDEX idx_execution_log_class ON execution_log_entries(command_class);

CREATE INDEX idx_validation_results_issue_id ON validation_results(issue_id);
CREATE INDEX idx_validation_results_result ON validation_results(result);

CREATE INDEX idx_section_checklist_section_id ON section_checklist_items(section_id);
CREATE INDEX idx_section_checklist_completed ON section_checklist_items(completed);
```

Trigram index for title and summary search:

```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX idx_issues_title_trgm ON issues USING gin (original_title gin_trgm_ops);
CREATE INDEX idx_issues_summary_trgm ON issues USING gin (summary gin_trgm_ops);
CREATE INDEX idx_issue_sections_content_trgm
  ON issue_sections USING gin (content gin_trgm_ops);
```

---

## CREATE TABLE Statements

```sql
CREATE TABLE issues (
    id                  SERIAL PRIMARY KEY,
    source_filename     TEXT NOT NULL UNIQUE,
    original_title      TEXT,
    slug                TEXT NOT NULL,
    content_hash        TEXT NOT NULL,
    line_count          INTEGER NOT NULL,
    byte_count          INTEGER NOT NULL,
    fibery_issue_id     TEXT,
    fibery_issue_number INTEGER,
    state               TEXT NOT NULL DEFAULT 'unknown',
    ticked              BOOLEAN,
    ticked_state        TEXT,
    earliest_date       DATE,
    latest_date         DATE,
    summary             TEXT,
    created_from        TEXT,
    ingested_at         TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE issue_sections (
    id              SERIAL PRIMARY KEY,
    issue_id        INTEGER NOT NULL REFERENCES issues(id) ON DELETE CASCADE,
    section_name    TEXT NOT NULL CHECK (section_name IN
                       ('description','plan','prompt','research','validation')),
    section_order   SMALLINT NOT NULL,
    content         TEXT NOT NULL,
    content_length  INTEGER NOT NULL,
    word_count      INTEGER NOT NULL,
    is_stub         BOOLEAN NOT NULL,
    is_real         BOOLEAN GENERATED ALWAYS AS (NOT is_stub) STORED,
    status_marker   TEXT,
    UNIQUE (issue_id, section_name)
);

CREATE TABLE topics (
    id          SERIAL PRIMARY KEY,
    name        TEXT NOT NULL UNIQUE,
    slug        TEXT NOT NULL UNIQUE,
    category    TEXT,
    description TEXT
);

CREATE TABLE issue_topics (
    issue_id   INTEGER NOT NULL REFERENCES issues(id) ON DELETE CASCADE,
    topic_id   INTEGER NOT NULL REFERENCES topics(id) ON DELETE CASCADE,
    confidence REAL NOT NULL DEFAULT 1.0,
    source     TEXT,
    PRIMARY KEY (issue_id, topic_id)
);

CREATE TABLE related_issues (
    id               SERIAL PRIMARY KEY,
    source_issue_id  INTEGER NOT NULL REFERENCES issues(id) ON DELETE CASCADE,
    target_issue_id  INTEGER NOT NULL REFERENCES issues(id) ON DELETE CASCADE,
    relation_type    TEXT NOT NULL,
    notes            TEXT,
    CHECK (source_issue_id <> target_issue_id)
);

CREATE TABLE issue_links (
    id              SERIAL PRIMARY KEY,
    source_issue_id INTEGER NOT NULL REFERENCES issues(id) ON DELETE CASCADE,
    link_type       TEXT NOT NULL,
    url             TEXT NOT NULL,
    label           TEXT
);

CREATE TABLE execution_log_entries (
    id              SERIAL PRIMARY KEY,
    issue_id        INTEGER NOT NULL REFERENCES issues(id) ON DELETE CASCADE,
    command_class   TEXT,
    command_text    TEXT,
    status          TEXT,
    reasoning_class TEXT,
    error_excerpt   TEXT,
    executed_at     TIMESTAMPTZ
);

CREATE TABLE validation_results (
    id           SERIAL PRIMARY KEY,
    issue_id     INTEGER NOT NULL REFERENCES issues(id) ON DELETE CASCADE,
    result       TEXT NOT NULL,
    verdict      TEXT,
    summary      TEXT,
    validated_on DATE
);

CREATE TABLE section_checklist_items (
    id         SERIAL PRIMARY KEY,
    section_id INTEGER NOT NULL REFERENCES issue_sections(id) ON DELETE CASCADE,
    item_order SMALLINT NOT NULL,
    item_text  TEXT NOT NULL,
    completed  BOOLEAN NOT NULL
);
```

---

## Seed Data

```sql
INSERT INTO topics (name, slug, category) VALUES
    ('Cline Hook',  'cline-hook',  'agent-runtime'),
    ('OCI',         'oci',         'infrastructure'),
    ('Parsec',      'parsec',      'media-stack'),
    ('Docker',      'docker',      'infrastructure'),
    ('Fibery',      'fibery',      'process'),
    ('AI Agents',   'ai-agents',   'agent-runtime'),
    ('SSH',         'ssh',         'infrastructure'),
    ('VS Code',     'vscode',      'tools'),
    ('Windows',     'windows',     'tools'),
    ('Java',        'java',        'language'),
    ('Network',     'network',     'infrastructure'),
    ('Forensic',    'forensic',    'process'),
    ('VPN',         'vpn',         'infrastructure'),
    ('Scripts',     'scripts',     'process'),
    ('Audit',       'audit',       'process'),
    ('Cline',       'cline',       'agent-runtime');
```

---

## Source File Inventory (47 files)

| Filename | Primary topic |
| --- | --- |
| Agent Config Refacto.md | Cline Hook |
| AI Agents — Executio.md | AI Agents |
| Analyze 24h OCI chan.md | OCI |
| Assess and optimize.md | Scripts |
| Assess Local SQL DB.md | Scripts |
| Bitwarden Windows SS.md | Windows |
| Build Hook Validator.md | Cline Hook |
| Cline → Copilot Hook.md | Cline Hook |
| Cline Hook Failure C.md | Cline Hook |
| Cline Hook System —.md | Cline Hook |
| Cline Model Collatio.md | Cline |
| Data Volume Recovery.md | Docker |
| Deploy researchers o.md | AI Agents |
| Diagnose and FIX- Po.md | Cline Hook |
| Diagnose CLine Freez.md | Cline |
| Docker Modular Archi.md | Docker |
| Eliminate duplicate.md | Docker |
| Fibery Migration Meg.md | Fibery |
| Fibery Workspace Aud.md | Fibery |
| Fix Cline HookRuntim.md | Cline Hook |
| Fix Kilo agent doom.md | AI Agents |
| Forensic Analysis- A.md | Forensic |
| Forensic Analysis- N.md | Forensic |
| JDK Modernization Pa.md | Java |
| Link IDrive to rclon.md | Scripts |
| Network Architecture.md | Network |
| OCI Instance Provisi.md | OCI |
| OCI Media Stack Setu.md | OCI |
| OCI SSH.md | SSH |
| Parsec -6023--11002.md | Parsec |
| Parsec Network Diagn.md | Parsec |
| Parsec Network Topol.md | Parsec |
| Parsec Performance D.md | Parsec |
| Phase 2- Fibery Work.md | Fibery |
| PowerShell Cline Hoo.md | Cline Hook |
| Research- Google AI.md | AI Agents |
| Resolve Prowlarr Clo.md | Docker |
| Setup SSH OCI (140.2.md | SSH |
| Sonarr- qBittorrent.md | Docker |
| Tighten Docker port.md | Docker |
| Ubuntu Host Hardenin.md | OCI |
| Unify DNS configurat.md | Network |
| Update Windows SSH G.md | SSH |
| VPN Namespace Wiring.md | VPN |
| VS Code Insiders NUL.md | VS Code |
| Windows Update Failu.md | Windows |

---

## Example INSERT (Present-State Tracking)

One representative file with all five sections, topic assignments, a duplicate link, and a validation result. Shows the current state of an active issue.

```sql
INSERT INTO issues (
    source_filename, original_title, slug, content_hash,
    line_count, byte_count, state, ticked, ticked_state,
    earliest_date, latest_date, summary,
    created_from, fibery_issue_number
) VALUES (
    'Parsec -6023--11002.md',
    'Parsec Connectivity Failure',
    'parsec-connectivity-failure',
    'sha256:placeholder-parsec-6023-11002-hash',
    220, 6777,
    'validation', FALSE, 'false',
    DATE '2026-01-14', DATE '2026-05-04',
    'Double NAT plus CGNAT prevents Parsec P2P hole punching. UPnP workaround rejected by Parsec release 18.',
    'fibery', NULL
)
RETURNING id;

INSERT INTO issue_sections (issue_id, section_name, section_order, content, content_length, word_count, is_stub, status_marker) VALUES
    (1, 'description', 1, 'Parsec connectivity failure with errors -6023 / -11002 since May 4 2026. Affected host: DESKTOP-MJ3FF9U. Last successful connection May 4 10:04 via BUD to 49.47.249.7:30843.', 800, 110, FALSE, NULL),
    (1, 'plan',        2, 'Five resolution approaches: router bridge mode, ISP public IP, disable UPnP, ZeroTier P2P VPN, LAN connection via 192.168.0.2.', 700, 95, FALSE, NULL),
    (1, 'prompt',      3, '', 0, 0, TRUE, NULL),
    (1, 'research',    4, 'Network diagnostic battery. Confirmed double NAT at 192.168.1.1 -> 192.168.0.1, ISP CGNAT at 10.100.120.34, UPnP rejected, public IP 103.207.57.31. Error codes: -6023/-11002 (2), Error 6 (3), -6105 (~30), -15101 (~500), -710049 (~20), -710022 (~25).', 1200, 175, FALSE, NULL),
    (1, 'validation',  5, 'Root cause: double NAT plus CGNAT on both peers. May 4 fix attempt (disable UPnP) failed: Parsec 18 does not recognise the config key.', 800, 115, FALSE, 'partial');

INSERT INTO issue_topics (issue_id, topic_id, confidence, source) VALUES
    (1, (SELECT id FROM topics WHERE slug = 'parsec'),  1.0, 'filename-prefix'),
    (1, (SELECT id FROM topics WHERE slug = 'network'), 1.0, 'body-mention');

INSERT INTO related_issues (source_issue_id, target_issue_id, relation_type, notes) VALUES
    (
        (SELECT id FROM issues WHERE source_filename = 'Parsec -6023--11002.md'),
        (SELECT id FROM issues WHERE source_filename = 'Parsec Network Diagn.md'),
        'duplicate',
        'Same Parsec session and root cause. Canonical record is Parsec -6023--11002.md.'
    );

INSERT INTO validation_results (issue_id, result, verdict, summary, validated_on) VALUES
    (
        (SELECT id FROM issues WHERE source_filename = 'Parsec -6023--11002.md'),
        'partial', 'VALIDATION FAILED - 1 root cause confirmed, 3 secondary issues open',
        'Double NAT plus CGNAT confirmed. AMD encoder -15101, Wi-Fi 2.4 GHz contention, packet loss remain open.',
        DATE '2026-05-04'
    );
```

---

## Example Queries

### Q1: Active issues (ticked=false)

What is currently open and needs work, newest first.

```sql
SELECT
    i.source_filename,
    i.original_title,
    i.state,
    i.summary,
    i.latest_date
FROM issues i
WHERE i.ticked = FALSE
ORDER BY i.latest_date DESC NULLS LAST;
```

### Q2: Stubs to populate

Files with at least one stub section, with the missing fields named.

```sql
SELECT
    i.source_filename,
    COUNT(*) FILTER (WHERE s.is_stub) AS stub_section_count,
    COUNT(*) FILTER (WHERE s.is_real) AS real_section_count,
    STRING_AGG(
        CASE WHEN s.is_stub THEN s.section_name END,
        ', ' ORDER BY s.section_order
    ) AS stub_sections
FROM issues i
JOIN issue_sections s ON s.issue_id = i.id
GROUP BY i.id, i.source_filename
HAVING COUNT(*) FILTER (WHERE s.is_stub) > 0
ORDER BY stub_section_count DESC, i.source_filename;
```

### Q3: Duplicates to consolidate

```sql
SELECT
    src.source_filename     AS source_file,
    src.fibery_issue_number AS source_number,
    tgt.source_filename     AS duplicate_of,
    tgt.fibery_issue_number AS target_number,
    r.notes
FROM related_issues r
JOIN issues src ON src.id = r.source_issue_id
JOIN issues tgt ON tgt.id = r.target_issue_id
WHERE r.relation_type = 'duplicate'
ORDER BY src.source_filename;
```

### Q4: Checklist progress per active issue

How many `- [ ]` items are still open per section, restricted to issues that are not ticked.

```sql
SELECT
    i.source_filename,
    s.section_name,
    COUNT(*) AS total_items,
    COUNT(*) FILTER (WHERE c.completed) AS done,
    COUNT(*) FILTER (WHERE NOT c.completed) AS open
FROM issues i
JOIN issue_sections s        ON s.issue_id = i.id
LEFT JOIN section_checklist_items c ON c.section_id = s.id
WHERE i.ticked = FALSE
GROUP BY i.id, i.source_filename, s.section_name
HAVING COUNT(*) > 0
ORDER BY open DESC, i.source_filename;
```

---

## Notes on Field Semantics

* `state` is intentionally a free-text column because the source files mix Fibery pipeline states (Research, Plan, Prompt, Execution, Validation, Ticked) with informal markers (Backlog, Stub). Encoding these as an enum would lose information.
* `ticked` is `BOOLEAN` with three-valued semantics: `true`, `false`, `NULL`. `NULL` represents the pre-field schema gap and is preserved distinctly in `ticked_state`.
* `is_stub` is determined by the loader: a section is stub if its body (after stripping the header and the `---` separator) is empty or contains only whitespace.
* Topic assignment combines filename-prefix matching (high confidence) with body mention of canonical topic names (lower confidence). The `confidence` column lets queries filter accordingly.
* `content_hash` lets duplicate detection survive file renames. Combined with `related_issues.relation_type = 'duplicate'`, both textual and structural duplicates can be tracked against the current canonical record.
