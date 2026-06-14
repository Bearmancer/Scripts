# Implementation Plan

> **Governance**: This is the **only** plan file per AGENTS.md. Done work lives in `changelog.md`. Unfinished work lives here. No other plan files exist.

## Universal Serilog Tracing Refactor
- [x] **L1** Add `MethodTracker` struct and `Track` method to `src/Core/Log.cs`
- [x] **L2** Write TUnit tests for `Log.Track` in `tests/Scripts.Tests/Core/LogTests.cs`
- [x] **L3** Refactor `src/Services/Language/AzureTranslationService.cs` (Apply 1-liner)
- [x] **L4** Refactor `src/Services/Language/AzureVisionService.cs` (Apply 1-liner)
- [x] **L5** Refactor `src/Services/Language/AzureOpenAIService.cs` (Apply 1-liner)
- [x] **L6** Refactor `src/Services/Language/AzureDocumentIntelligenceService.cs` (Apply 1-liner)
- [x] **L7** Refactor `src/Core/Auth/AzureAuth.cs` (Apply 1-liner)
- [x] **L8** Refactor `src/Core/Auth/GoogleAuth.cs` and `TcpCodeReceiver.cs` (Remove `Console.WriteLine`, apply 1-liner)
- [x] **BUILD** `dotnet build csharp/Scripts.slnx` and run tests

## Phase 0.2: Remaining Azure Services Integration (UNFINISHED)
- [x] **O2.0** Upgrade `AzureTranslationService.cs` to Translator API version `2026-06-06` *(Note: Verified June 2026 - v3.0 is NOT officially retiring, but the new GA version enables LLM capabilities)* — Done 2026-06-14: switched to `TranslateInputItem(text, TranslationTarget("en"), language)` schema; verified build green
- [x] **O2.1** `AzureOpenAIService` (Whisper + GPT-4o-mini) — Done 2026-06-14: 131 lines, 2 methods + DEBUG delegate hooks; added `AzureOpenAIWhisperDeploymentName` env var
- [x] **O2.2** `AzureVisionService` (image OCR + caption + tags) — Done 2026-06-14: 165 lines, 3 methods (ExtractTextAsync, CaptionAsync, TagAsync) + 3 DEBUG delegate hooks
- [ ] **O2.3a** `AzureDocumentIntelligenceService` (structured extraction)
- [ ] **O2.3b** DELETE `Reader/Ocr/AzureDocumentIntelligenceOcrProvider.cs`
- [ ] **O2.3c** REFACTOR 3 Local extractors (`LocalPdfExtractor`, `LocalImageExtractor`, `LocalEpubExtractor`) to call O2.3a
- [ ] **O2.12** `SubtitleCommand`
- [x] **O2.8** Tests for `AzureOpenAIServiceTests.cs` — Done 2026-06-14: 237 lines, 15 tests; cannot run due to pre-existing `TestPaths.cs` sentinel issue (see changelog)
- [x] **O2.9** Tests for `AzureVisionServiceTests.cs` — Done 2026-06-14: 253 lines, 19 tests; same TestPaths issue blocks execution
- [ ] **O2.10** Tests for `AzureDocumentIntelligenceServiceTests.cs`
- [ ] **BUILD** `dotnet build csharp/Scripts.slnx` — 0 errors, 0 warnings
- [ ] **CLEANUP** Flip checkboxes to `[x]`, update `changelog.md`

## Foundry Resource Setup
- [ ] **O2.X** Add Bicep template at `infra/foundry.bicep` for the single Foundry resource
- [ ] Update `.env` to set `FOUNDRY_PROJECT_NAME=scripts-prod` (default)

## YouTube Pipeline Rebuild (UNFINISHED)
- [ ] Task 2: Split raw DTOs from derived DTOs
- [ ] Task 3: Build separate translation files for each playlist
- [ ] Task 4: Add manifest / index and clean filename scheme
- [ ] Task 5: Demote `sync.json` to cursor-only and add run history
- [ ] Task 6: Implement PostgreSQL current tables and history tables
- [ ] Task 7: Wire rename, delete, and change-detection behavior
- [ ] Task 8: Add PostgreSQL backup, WAL, and restore workflow
- [ ] Task 9: Prove offline rebuild from local files with zero YouTube calls
- [ ] F1: Plan compliance audit
- [ ] F2: Code quality review
- [ ] F3: Real end-to-end QA
- [ ] F4: Scope fidelity and backup integrity

## Fibery Expungement (UNFINISHED, deferred per user)
- [ ] Delete `FiberyEntity.cs`, `FiberyEntityConfiguration.cs`, related compiled models, tests
- [ ] Purge all migrations and regenerate fresh `InitialCreate` (no fibery)
- [ ] Extract 18 valuable files from `fibery-archive/` to `AI/references/`
- [ ] Delete remaining 26 files in `fibery-archive/` + directory
- [ ] Remove `fibery` schema and `fibery_entities` table from PGSQL
- [ ] Update `AGENTS.md` (remove line 29)

## Phase 2: Outstanding Team Findings (TDD Gates) — UNFINISHED, LOW PRIORITY
- [ ] A1.1-E3.1 TDD gates for infrastructure, SSH, OCI, EF entities, etc.
