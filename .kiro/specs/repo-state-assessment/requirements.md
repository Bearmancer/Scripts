# Requirements Document

## Introduction

This document specifies requirements for a comprehensive repository assessment and integration task covering build error resolution, documentation consolidation, Fibery data migration, and git history analysis for the Scripts repository. The repository is a multi-language automation toolkit (C#/.NET 10, Python, PowerShell) currently undergoing an EF Core 10 migration (Tier 1, approximately 70% complete). The system must resolve blocking build errors, establish a single source of truth for documentation, create a PostgreSQL ingestion pipeline for Fibery static exports, and analyze git commit history for meaningful squash groupings.

## Glossary

- **Build_System**: The .NET 10 SDK and MSBuild toolchain used to compile C# projects
- **Lingua_Library**: SearchPioneer.Lingua v1.0.5 language detection library with PascalCase enum values
- **Documentation_System**: The collection of markdown files in AI/plans/ and root AGENTS.md that describe repository state
- **Fibery_Export**: Static markdown and JSON files exported from Fibery workspace located in fibery/ directory
- **PostgreSQL_Database**: Local PostgreSQL 18 database managed via Docker Compose, connection string in $env:PGCONNSTR
- **Git_History**: Commit log containing 50+ recent commits across multiple feature branches and merge operations
- **EF_Core_Migration**: Tier 1 migration work converting CSV/JSON persistence to EF Core 10 with Npgsql 10
- **Ingestion_Pipeline**: ETL process to parse Fibery exports and insert structured data into PostgreSQL
- **Commit_Squash_Group**: Logically related commits that should be combined into a single atomic commit
- **Single_Source_Document**: Unified markdown file consolidating AGENTS.md, CURRENT_STATUS.md, and INDEX.md

## Requirements

### Requirement 1: Build Error Resolution

**User Story:** As a developer, I want the C# build to succeed without errors, so that I can continue development work on the EF Core migration.

#### Acceptance Criteria

1. WHEN THE Build_System compiles LanguageIdentifier.cs, THE Build_System SHALL use PascalCase enum references (Language.English, Language.French, Language.German, Language.Spanish, Language.Portuguese, Language.Italian, Language.Dutch, Language.Russian, Language.Chinese, Language.Japanese, Language.Korean, Language.Arabic, Language.Hindi, Language.Bengali, Language.Catalan, Language.Czech, Language.Danish, Language.Finnish, Language.Greek, Language.Hungarian, Language.Norwegian, Language.Polish, Language.Romanian, Language.Slovak, Language.Swedish, Language.Turkish, Language.Ukrainian, Language.Vietnamese, Language.Thai)
2. WHEN THE Build_System compiles Scripts.slnx, THE Build_System SHALL produce zero compilation errors
3. WHEN THE Build_System compiles Scripts.slnx, THE Build_System SHALL produce zero warnings with TreatWarningsAsErrors enabled
4. THE Build_System SHALL replace all SCREAMING_SNAKE_CASE Lingua_Library enum references with PascalCase equivalents
5. WHEN THE Build_System encounters Language enum null comparison, THE Build_System SHALL use nullable Language? type or remove null comparison
6. WHEN dotnet build Scripts.slnx completes, THE Build_System SHALL return exit code 0

### Requirement 2: Documentation Consolidation

**User Story:** As a repository maintainer, I want a single authoritative documentation file, so that agents and developers have consistent, non-conflicting information about repository state.

#### Acceptance Criteria

1. THE Documentation_System SHALL create a unified document at AI/plans/REPOSITORY_STATUS.md
2. THE Documentation_System SHALL verify that AGENTS.md is the single source of truth (CURRENT_STATUS.md and INDEX.md have been consolidated into AGENTS.md and deleted)
3. WHEN conflicting information exists between source documents, THE Documentation_System SHALL use the most recent timestamp as authority
4. THE Documentation_System SHALL preserve all critical sections: Project Overview, Key Technologies, Environment Setup, Building & Running, C# Project Structure, Database Schema, EF Core 10 Version Notes, Development Conventions, Absolute Zero Presumption Ruleset, Plan Navigation, Current Status, Tier Overview, Critical Path CPM, TDD Execution Loop, Plan File Inventory
5. THE Documentation_System SHALL include test status (170 passing, 0 failing, 100% pass rate)
6. THE Documentation_System SHALL document Tier 1 progress (phases T1-00 through T1-11, T1-14, T1-15 complete; T1-12, T1-13 pending; T1-16 blocked)
7. THE Documentation_System SHALL document blocking relationships (T2 blocked by T1 sign-off, T3 blocked by T2 sign-off, T4 blocked by T3 sign-off)
8. THE Documentation_System SHALL include EF Core 10 vs EF11 API restrictions with code examples
9. THE Documentation_System SHALL document current blockers (T1-12 logging relocation, T1-13 Lingua migration)
10. WHEN THE Documentation_System creates REPOSITORY_STATUS.md, THE Documentation_System SHALL add deprecation notices to AGENTS.md, CURRENT_STATUS.md, and INDEX.md pointing to the new single source

### Requirement 3: Fibery Data Migration State Documentation

**User Story:** As a data engineer, I want comprehensive documentation of the Fibery export structure and migration requirements, so that I can design an appropriate PostgreSQL ingestion pipeline.

#### Acceptance Criteria

1. THE Documentation_System SHALL create AI/plans/FIBERY_MIGRATION.md
2. THE Documentation_System SHALL document the directory structure of fibery/ export (Knowledge/, Repos/, subdirectories)
3. THE Documentation_System SHALL identify all entity types in the Fibery_Export (Guide, Execution Logs, Issue, Project)
4. THE Documentation_System SHALL document file formats present in Fibery_Export (markdown, JSON, other)
5. THE Documentation_System SHALL specify target PostgreSQL_Database schema for fibery_entities table (Id UUID PK, FiberyId VARCHAR(255), EntityType VARCHAR(100), RawData JSONB, ImportedAt TIMESTAMPTZ, SourcePath TEXT)
6. THE Documentation_System SHALL document parsing requirements for each entity type
7. THE Documentation_System SHALL specify idempotency requirements (re-running import should not create duplicates)
8. THE Documentation_System SHALL document error handling requirements (malformed files, missing fields, encoding issues)
9. THE Documentation_System SHALL specify logging requirements for import operations
10. WHEN THE Documentation_System documents entity types, THE Documentation_System SHALL include sample file paths and content structure examples

### Requirement 4: Fibery Ingestion Pipeline

**User Story:** As a data engineer, I want an automated pipeline to ingest Fibery exports into PostgreSQL, so that Fibery data is queryable alongside other repository data.

#### Acceptance Criteria

1. THE Ingestion_Pipeline SHALL parse all markdown files in fibery/ directory recursively
2. THE Ingestion_Pipeline SHALL parse all JSON files in fibery/ directory recursively
3. WHEN THE Ingestion_Pipeline encounters a Fibery entity file, THE Ingestion_Pipeline SHALL extract FiberyId from file metadata or content
4. WHEN THE Ingestion_Pipeline encounters a Fibery entity file, THE Ingestion_Pipeline SHALL determine EntityType from directory structure
5. THE Ingestion_Pipeline SHALL store complete file content in RawData JSONB column
6. THE Ingestion_Pipeline SHALL record SourcePath relative to fibery/ directory
7. THE Ingestion_Pipeline SHALL record ImportedAt timestamp in UTC
8. WHEN THE Ingestion_Pipeline encounters a duplicate FiberyId, THE Ingestion_Pipeline SHALL update existing record rather than insert duplicate
9. WHEN THE Ingestion_Pipeline encounters a parsing error, THE Ingestion_Pipeline SHALL log error details and continue processing remaining files
10. THE Ingestion_Pipeline SHALL use EF Core 10 ExecuteUpdateAsync for upsert operations
11. THE Ingestion_Pipeline SHALL use IDbContextFactory for database access
12. THE Ingestion_Pipeline SHALL implement retry logic via Polly v8 resilience policies
13. WHEN THE Ingestion_Pipeline completes, THE Ingestion_Pipeline SHALL report total files processed, records inserted, records updated, and errors encountered
14. THE Ingestion_Pipeline SHALL validate PostgreSQL_Database connection before processing files
15. THE Ingestion_Pipeline SHALL use UTF-8 encoding for all file read operations

### Requirement 5: Git History Analysis

**User Story:** As a repository maintainer, I want analysis of recent git commits grouped by logical feature work, so that I can create meaningful squash commits that preserve development history intent.

#### Acceptance Criteria

1. THE Git_History SHALL analyze the most recent 50 commits from git log
2. THE Git_History SHALL identify commit groups by common prefixes (feat(t1-XX), fix(t1-XX), chore, docs, refactor)
3. THE Git_History SHALL identify merge commits and exclude them from squash groups
4. THE Git_History SHALL group consecutive commits with identical tier/phase prefixes
5. WHEN THE Git_History identifies a logical group, THE Git_History SHALL include commit SHAs, messages, and author timestamps
6. THE Git_History SHALL create AI/plans/GIT_SQUASH_ANALYSIS.md with proposed squash groups
7. THE Git_History SHALL propose squash commit messages that summarize grouped work
8. THE Git_History SHALL identify commits that should remain standalone (major milestones, sign-offs, merges)
9. THE Git_History SHALL calculate total commits per group
10. THE Git_History SHALL preserve chronological order within groups
11. WHEN THE Git_History encounters commits without conventional commit prefixes, THE Git_History SHALL group by date proximity and content similarity
12. THE Git_History SHALL identify the current HEAD commit and working branch
13. THE Git_History SHALL document any uncommitted changes in working directory
14. THE Git_History SHALL provide git rebase -i command templates for each proposed squash group

### Requirement 6: Assessment Report Generation

**User Story:** As a project manager, I want a comprehensive assessment report of all completed work, so that I can understand repository state and next actions.

#### Acceptance Criteria

1. THE Documentation_System SHALL create AI/plans/ASSESSMENT_REPORT.md
2. THE Documentation_System SHALL document build status (errors resolved, warnings count, exit code)
3. THE Documentation_System SHALL document documentation consolidation status (files created, deprecated, conflicts resolved)
4. THE Documentation_System SHALL document Fibery migration status (entities identified, schema defined, pipeline implemented)
5. THE Documentation_System SHALL document git history analysis status (commits analyzed, groups identified, squash proposals generated)
6. THE Documentation_System SHALL include test execution results (total tests, passing, failing, pass rate)
7. THE Documentation_System SHALL identify remaining blockers for Tier 1 sign-off
8. THE Documentation_System SHALL provide recommended next actions in priority order
9. THE Documentation_System SHALL include timestamps for all assessment activities
10. THE Documentation_System SHALL document any assumptions made during assessment
11. THE Documentation_System SHALL document any risks or concerns identified
12. WHEN THE Documentation_System generates the report, THE Documentation_System SHALL include executive summary section with key findings

### Requirement 7: Validation and Verification

**User Story:** As a quality engineer, I want automated validation of all assessment work, so that I can confirm requirements are met before proceeding to next phase.

#### Acceptance Criteria

1. THE Build_System SHALL execute dotnet build Scripts.slnx and verify exit code 0
2. THE Build_System SHALL execute dotnet test Scripts.slnx and verify all tests pass
3. THE Documentation_System SHALL verify REPOSITORY_STATUS.md exists and contains all required sections
4. THE Documentation_System SHALL verify FIBERY_MIGRATION.md exists and documents all entity types
5. THE Documentation_System SHALL verify GIT_SQUASH_ANALYSIS.md exists and contains squash proposals
6. THE Documentation_System SHALL verify ASSESSMENT_REPORT.md exists and contains all required sections
7. THE Ingestion_Pipeline SHALL verify fibery_entities table exists in PostgreSQL_Database
8. THE Ingestion_Pipeline SHALL verify at least one record inserted into fibery_entities table
9. THE Git_History SHALL verify git log command executes successfully
10. THE Documentation_System SHALL verify all deprecated documentation files contain deprecation notices
11. WHEN validation fails for any requirement, THE Documentation_System SHALL document failure reason in ASSESSMENT_REPORT.md
12. THE Documentation_System SHALL generate validation checklist with pass/fail status for each requirement

### Requirement 8: Integration with Existing Workflow

**User Story:** As a developer, I want assessment work to integrate seamlessly with existing Tier 1 EF Core migration, so that no conflicts or regressions are introduced.

#### Acceptance Criteria

1. THE Build_System SHALL not modify any files in csharp/src/Data/ except LanguageIdentifier.cs
2. THE Build_System SHALL not modify any EF Core entity files
3. THE Build_System SHALL not modify any EF Core migration files
4. THE Build_System SHALL not modify any test files except to add new Fibery ingestion tests
5. THE Ingestion_Pipeline SHALL use existing ScriptsDbContext for database access
6. THE Ingestion_Pipeline SHALL follow existing repository patterns (IDbContextFactory, Polly resilience, Serilog logging)
7. THE Ingestion_Pipeline SHALL not interfere with existing EF Core migrations
8. THE Documentation_System SHALL preserve existing plan files in AI/plans/tier-1-ef-migration/
9. THE Documentation_System SHALL preserve existing plan files in AI/plans/tier-2-cpm-split/
10. THE Documentation_System SHALL preserve existing plan files in AI/plans/tier-3-domain/
11. THE Documentation_System SHALL preserve existing plan files in AI/plans/tier-4-hardening/
12. WHEN THE Build_System resolves Lingua errors, THE Build_System SHALL maintain compatibility with existing LanguageIdentifier tests
13. THE Git_History SHALL not modify git history or perform any git operations beyond read-only analysis
14. THE Documentation_System SHALL follow Absolute Zero Presumption Ruleset for all file operations
