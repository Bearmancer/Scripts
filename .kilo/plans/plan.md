# CPM — Consolidated Maximalist TDD Plan (SRP Granularity)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Stack:** C# 15 / .NET 11 (Preview) / EF Core 11 / Npgsql 11 / PostgreSQL 16+ Docker
>
> **SRP RULE:** Each task touches exactly ONE property / ONE file / ONE concept. No en-masse edits.
>
> **ABSOLUTE ZERO PRESUMPTION RULESET:**
> 1. **No Tooling Presumptions:** Verify `pwsh`, `dotnet`, and `git` exist before starting.
> 2. **No Success Presumptions:** NEVER use `-ErrorAction SilentlyContinue`. All commands use `-ErrorAction Stop`.
> 3. **No I/O Presumptions:** Every file create/delete/move MUST be followed by a `Test-Path` assertion.
> 4. **No Encoding Presumptions:** Explicitly declare `-Encoding UTF8`.
> 5. **No Exit Code Presumptions:** Capture `2>&1` output and run Regex assertions.
> 6. **No Path Presumptions:** Use absolute paths (`C:\Users\Lance\Dev\Scripts\...`).
> 7. **No State Presumptions:** Log Current State, Reason, What, Expected Outcome before each mutation.
> 8. **No NuGet Presumptions:** Always `dotnet restore` before build/test.
> 9. **No Deletion Presumptions:** Create `.bak.YYYYMMDD_HHmmss` before any deletion.
> 10. **Strict TDD Granularity:** ONE miniscule addition per task.

---

## Plan Tiers (Phases)

Below is the directory of active implementation plans split by tier/phase. Each file contains self-contained tasks with pre- and post-code contexts.

* **Phase 0: Commit Squash & Dedup** (Completed - history rewritten to 143 commits)
* **[Phase 1: Test Infrastructure](file:///C:/Users/Lance/Dev/Scripts/.kilo/plans/plan-phase-1.md)** - Establish and verify the baseline C# test environment.
* **[Phase 2: Repo Cleanup](file:///C:/Users/Lance/Dev/Scripts/.kilo/plans/plan-phase-2.md)** - Assert IDE directories (.vscode and .idea) are clean and absent.
* **[Phase 3: Google Deprecation](file:///C:/Users/Lance/Dev/Scripts/.kilo/plans/plan-phase-3.md)** - Verify sheets removal and clear orchestrators of Google dependencies.
* **[Phase 4: Shared Infrastructure](file:///C:/Users/Lance/Dev/Scripts/.kilo/plans/plan-phase-4.md)** - Verify diacritic stripping, case normalization, and db registration rules.
* **[Phase 5: Entity Refactoring](file:///C:/Users/Lance/Dev/Scripts/.kilo/plans/plan-phase-5.md)** - Remove obsolete metadata and ID properties from domain records.
* **[Phase 6: DbContext Configuration](file:///C:/Users/Lance/Dev/Scripts/.kilo/plans/plan-phase-6.md)** - Verify NoTracking, configurations assembly loading, and Videos DbSet.
* **[Phase 7: Entity Configurations](file:///C:/Users/Lance/Dev/Scripts/.kilo/plans/plan-phase-7.md)** - Verify index, key, and identity column generation configurations on model builder.
* **[Phase 8: Database Migrations](file:///C:/Users/Lance/Dev/Scripts/.kilo/plans/plan-phase-8.md)** - Generate migration, add unaccent/trigram extensions, unique functional indexes, and apply to database.
* **[Phase 9: Sync Service Updates](file:///C:/Users/Lance/Dev/Scripts/.kilo/plans/plan-phase-9.md)** - Normalize lookups using ILike, and execute updates/deletes in PostgresService.
* **[Phase 10: EF11 Query Upgrades](file:///C:/Users/Lance/Dev/Scripts/.kilo/plans/plan-phase-10.md)** - Implement MaxByAsync, JsonTypeof searches, and fuzzy artist query upgrades.
* **[Phase 11: Optimization — Compiled Model](file:///C:/Users/Lance/Dev/Scripts/.kilo/plans/plan-phase-11.md)** - Enable EFOptimizeContext and generate compiled model to speed up startup.
* **[Phase 12: Domain Naming Refactor](file:///C:/Users/Lance/Dev/Scripts/.kilo/plans/plan-phase-12.md)** - Rename entities to Entity suffix, rename DTOs, and clean global models import.
* **[Phase 13: Final Verification](file:///C:/Users/Lance/Dev/Scripts/.kilo/plans/plan-phase-13.md)** - Run final restore, compile, test suite check, and force-push main.
