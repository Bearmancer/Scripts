# EF Core 10 Migration — Consolidated Debugging Plan

**Date**: 2026-05-25  
**Status**: Consolidating fixes for Testcontainers Lifecycle, Compiled Model Lock, EF Core 10 JsonDocument NullReferenceException, and PendingModelChangesWarning.

---

## Executive Summary

The EF Core 10 migration tests have been blocked by a chain of infrastructure and model configuration issues:
1. **Testcontainers Lifecycle**: Fixtures were not properly cleaned up or awaited, exhausting resources.
2. **Compiled Model Lock**: Using `UseModel(ScriptsDbContextModel.Instance)` locked the EF model, preventing test-specific configurations from taking effect.
3. **NullReferenceException during Model Building**: Once the compiled model lock was bypassed for tests, `OnModelCreating` was evaluated. The presence of `mb.Ignore<System.Text.Json.JsonDocument>();` crashed EF Core 10 and Npgsql 10 when initializing `KeyValueComparer` for JSON columns (e.g., `ExecutionLog.Payload`).
4. **PendingModelChangesWarning**: Removing `mb.Ignore<JsonDocument>()` fixed the `NullReferenceException`, but caused the runtime model to differ from the existing migration snapshot, which fails the `MigrateAsync` operation with a `PendingModelChangesWarning`.

---

## Implementation Plan

### Phase 1: Clean Up Previous Test Infrastructure Fixes
Ensure `DatabaseTestFixture` uses an increased timeout and properly cleans up test containers, and that tests inherit from `DatabaseTestBase` instead of creating multiple unmanaged containers. (This phase has largely been implemented but must be verified).

### Phase 2: Fix EF Core 10 JSON Configuration
**File**: `csharp/src/Data/ScriptsDbContext.cs`
- Remove `mb.Ignore<System.Text.Json.JsonDocument>();` from `OnModelCreating`. Npgsql 10 has native support for `JsonDocument` and ignoring it corrupts the model metadata for JSON properties.

### Phase 3: Regenerate Migration Snapshot and Compiled Models
Because the runtime model definition changed in Phase 2, the snapshot and compiled model are now outdated, causing `PendingModelChangesWarning` during `MigrateAsync`.

1. **Add an empty migration to update snapshot**:
   `dotnet ef migrations add FixJsonDocumentModel -p csharp/src/Data/Scripts.Data.csproj -s csharp/src/CLI/Scripts.CLI.csproj`
2. **Optimize DbContext to regenerate compiled models**:
   `dotnet ef dbcontext optimize --output-dir CompiledModels --namespace CSharpScripts.Data.CompiledModels -p csharp/src/Data/Scripts.Data.csproj -s csharp/src/CLI/Scripts.CLI.csproj`

### Phase 4: Validation
Run the test suite to confirm that 130+ tests now pass successfully:
`dotnet test csharp/Scripts.slnx --no-build`
