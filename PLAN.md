# Implementation Plan

## Phase 1: EF Schema Robust Enhancements

- [x] Task 1.1: Fix translated properties casing. Update `EF_SCHEMA_AUTHORITY.md` and related drafts/documentation to use EF compliant PascalCase (`TranslatedTitle` and `TranslatedDescription`) instead of `TitleEn` and `DescriptionEn`. Ensure the `videos` schema explicitly reflects this change.
- [x] Task 1.2: Assess and robustly enhance the `ExternalId` schema for Music. Remove the `source_records` table from the `public` schema. Add `ExternalId` (or specific fields like `MusicBrainzId`, `DiscogsId`) and `SourceSystem` to relevant entities in the `music` schema (e.g., `artists`, `albums`, `tracks`, `release_progress`). This clearly separates MusicBrainz search lookup identities from Last.fm scrobble events. Ensure `EF_SCHEMA_AUTHORITY.md` is fully updated with these entity changes.
- [x] Task 1.3: Generate a proper comprehensive Mermaid ER Diagram visualizing all 4 updated schemas (`youtube`, `music`, `work`, `public`). Embed the diagram directly into `EF_SCHEMA_AUTHORITY.md` under the "ER Relationship Summary" section. Ensure it reflects the removed `source_records`, updated `ExternalId` columns, and corrected casing.


## Phase 2: Outstanding Team Findings (TDD Gates)

```powershell
# Step A1.1
# FAIL CONDITION
(Test-Path .omo/evidence/A1.1-diagnostic.txt) -eq $false

# PASS CONDITION
(Test-Path .omo/evidence/A1.1-diagnostic.txt) -and ((rg 'ERROR_NOACCESS' .omo/evidence/A1.1-diagnostic.txt | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step A1.2
# FAIL CONDITION
(Test-Path .omo/evidence/A1.2-cli-check.txt) -eq $false

# PASS CONDITION
(Test-Path .omo/evidence/A1.2-cli-check.txt) -and ((rg 'upgrade' .omo/evidence/A1.2-cli-check.txt | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step A1.3
# FAIL CONDITION
(Test-Path .omo/evidence/backups) -eq $false

# PASS CONDITION
(gci .omo/evidence/backups/ -File | Measure-Object).Count -ge 4
```
```powershell
# Step A1.4
# FAIL CONDITION
(Test-Path .omo/evidence/A1.4-smoke-test.txt) -eq $false

# PASS CONDITION
(Test-Path .omo/evidence/A1.4-smoke-test.txt) -and ((rg '200 OK' .omo/evidence/A1.4-smoke-test.txt | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step A1.6
# FAIL CONDITION
$task = schtasks /query /tn OpenCode-Serve /xml 2>$null; [string]::IsNullOrEmpty($task)

# PASS CONDITION
$xml = schtasks /query /tn OpenCode-Serve /xml 2>$null; ($xml -match 'RestartCount') -and ($xml -match 'RestartInterval')
```
```powershell
# Step A2.1
# FAIL CONDITION
(Test-Path .omo/evidence/A2.1-tailscale-check.txt) -eq $false

# PASS CONDITION
(Test-Path .omo/evidence/A2.1-tailscale-check.txt) -and ((rg 'tail2e6179' .omo/evidence/A2.1-tailscale-check.txt | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step A2.2
# FAIL CONDITION
(Test-Path .omo/evidence/A2.2-ssh-baseline.txt) -eq $false

# PASS CONDITION
(Test-Path .omo/evidence/A2.2-ssh-baseline.txt) -and ((rg 'Connected|Authenticated' .omo/evidence/A2.2-ssh-baseline.txt | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step A2.3
# FAIL CONDITION
$config = 'C:\ProgramData\ssh\sshd_config'; (Test-Path $config) -eq $false

# PASS CONDITION
$config = 'C:\ProgramData\ssh\sshd_config'; (rg 'ListenAddress.*tail2e6179' $config | Measure-Object -Line).Lines -ge 1 -and (rg 'AllowUsers.*100\.64' $config | Measure-Object -Line).Lines -ge 1
```
```powershell
# Step A2.4
# FAIL CONDITION
(Test-Path .omo/evidence/A2.4-ssh-verify.txt) -eq $false

# PASS CONDITION
(Test-Path .omo/evidence/A2.4-ssh-verify.txt) -and ((rg 'tailscale.*success|ipv4.*blocked' .omo/evidence/A2.4-ssh-verify.txt | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step A2.5
# FAIL CONDITION
(Test-Path .omo/evidence/SSH-ROLLBACK.txt) -eq $false

# PASS CONDITION
(Test-Path .omo/evidence/SSH-ROLLBACK.txt) -and ((rg 'rollback|undo|revert' .omo/evidence/SSH-ROLLBACK.txt | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step A3.2
# FAIL CONDITION
(Test-Path .omo/evidence/A3.2-oci-output.txt) -eq $false

# PASS CONDITION
(Test-Path .omo/evidence/A3.2-oci-output.txt) -and ((rg 'instance|reboot' .omo/evidence/A3.2-oci-output.txt | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step A3.3
# FAIL CONDITION
(Test-Path .omo/evidence/task-14-user-summary.md) -eq $false

# PASS CONDITION
(Test-Path .omo/evidence/task-14-user-summary.md) -eq $true
```
```powershell
# Step A4.2
# FAIL CONDITION
(Test-Path .omo/evidence/A4.2-oci-backup-output.txt) -eq $false

# PASS CONDITION
(Test-Path .omo/evidence/A4.2-oci-backup-output.txt) -and ((rg 'backup|volume' .omo/evidence/A4.2-oci-backup-output.txt | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step A4.3
# FAIL CONDITION
(Test-Path .omo/evidence/task-17-user-summary-backup.md) -eq $false

# PASS CONDITION
(Test-Path .omo/evidence/task-17-user-summary-backup.md) -eq $true
```
```powershell
# Step B1.5
# FAIL CONDITION
(Test-Path csharp/src/Data/Entities/MusicWork.cs) -eq $false

# PASS CONDITION
$f = 'csharp/src/Data/Entities/MusicWork.cs'; (Test-Path $f) -and (rg 'public.*Id' $f) -and (rg 'public.*Name' $f) -and (rg 'public.*Composer' $f) -and (rg 'public.*CatalogueNumber' $f) -and (rg 'public.*KeySignature' $f) -and (rg 'public.*ExternalId' $f) -and (rg 'public.*SourceSystem' $f) -and (rg 'public.*Metadata' $f) -and (rg 'public.*CreatedAt' $f) -and (rg 'public.*UpdatedAt' $f)
```
```powershell
# Step B1.6
# FAIL CONDITION
(Test-Path csharp/src/Data/Entities/Movement.cs) -eq $false

# PASS CONDITION
$f = 'csharp/src/Data/Entities/Movement.cs'; (Test-Path $f) -and (rg 'public.*WorkId' $f) -and (rg 'public.*Position' $f) -and (rg 'public.*Name' $f) -and (rg 'public.*CreatedAt' $f) -and (rg 'public.*UpdatedAt' $f)
```
```powershell
# Step B1.7
# FAIL CONDITION
(rg 'public int\? WorkId' csharp/src/Data/Entities/Track.cs | Measure-Object -Line).Lines -eq 0

# PASS CONDITION
((rg 'public int\? WorkId' csharp/src/Data/Entities/Track.cs | Measure-Object -Line).Lines -ge 1) -and ((rg 'public int\? MovementId' csharp/src/Data/Entities/Track.cs | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step B1.8
# FAIL CONDITION
(rg 'WorkId|MovementId' csharp/src/Data/Configuration/TrackConfiguration.cs | Measure-Object -Line).Lines -eq 0

# PASS CONDITION
((rg 'WorkId' csharp/src/Data/Configuration/TrackConfiguration.cs | Measure-Object -Line).Lines -ge 1) -and ((rg 'MovementId' csharp/src/Data/Configuration/TrackConfiguration.cs | Measure-Object -Line).Lines -ge 1) -and ((rg 'DeleteBehavior\.Restrict' csharp/src/Data/Configuration/TrackConfiguration.cs | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step B1.9
# FAIL CONDITION
(rg 'DbSet<MusicWork>' csharp/src/Data/ScriptsDbContext.cs | Measure-Object -Line).Lines -eq 0

# PASS CONDITION
((rg 'DbSet<MusicWork>' csharp/src/Data/ScriptsDbContext.cs | Measure-Object -Line).Lines -ge 1) -and ((rg 'DbSet<Movement>' csharp/src/Data/ScriptsDbContext.cs | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step B1.10
# FAIL CONDITION
(Test-Path csharp/src/Data/Entities/Recording.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Data/Entities/Recording.cs) -and (Test-Path csharp/src/Data/Entities/Performer.cs) -and (Test-Path csharp/src/Data/Entities/RecordingPerformer.cs) -and (Test-Path csharp/src/Data/Entities/Venue.cs)
```
```powershell
# Step B1.11
# FAIL CONDITION
(Test-Path csharp/src/Data/Entities/ScrobbleClassicalMap.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Data/Entities/ScrobbleClassicalMap.cs) -and (Test-Path csharp/src/Data/Configuration/ScrobbleClassicalMapConfiguration.cs)
```
```powershell
# Step B1.13
# FAIL CONDITION
Test-Path csharp/src/Data/Entities/Issue.cs

# PASS CONDITION
(Test-Path csharp/src/Data/Entities/Issue.cs) -eq $false
```
```powershell
# Step B1.15
# FAIL CONDITION
(Test-Path csharp/Migrations/*_WaveB1_SchemaEvolution.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/Migrations/*_WaveB1_SchemaEvolution.cs) -and ((dotnet build csharp/Scripts.slnx 2>$null; $LASTEXITCODE) -eq 0)
```
```powershell
# Step B2.1
# FAIL CONDITION
(Test-Path csharp/src/Services/Music/WorkService.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Services/Music/WorkService.cs) -and ((rg 'GetOrCreateWorkAsync' csharp/src/Services/Music/WorkService.cs | Measure-Object -Line).Lines -ge 1) -and (Test-Path csharp/tests/Scripts.Tests/Services/Music/WorkServiceTests.cs)
```
```powershell
# Step B2.2
# FAIL CONDITION
(rg 'WorkService' csharp/src/Orchestrators/ScrobbleSyncOrchestrator.cs | Measure-Object -Line).Lines -ge 1

# PASS CONDITION
(rg 'WorkService' csharp/src/Orchestrators/ScrobbleSyncOrchestrator.cs | Measure-Object -Line).Lines -eq 0
```
```powershell
# Step B2.3
# FAIL CONDITION
(rg 'DisplayArtist' csharp/src/Data/Entities/Track.cs | Measure-Object -Line).Lines -eq 0

# PASS CONDITION
(rg 'DisplayArtist.*Artist\?\.Name' csharp/src/Data/Entities/Track.cs | Measure-Object -Line).Lines -ge 1
```
```powershell
# Step B2.4
# FAIL CONDITION
(Test-Path csharp/tests/Scripts.Tests/Services/Music/WorkServiceActivationTests.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/tests/Scripts.Tests/Services/Music/WorkServiceActivationTests.cs) -and ((rg '\[Test\]|\[Fact\]' csharp/tests/Scripts.Tests/Services/Music/WorkServiceActivationTests.cs | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step B3.1
# FAIL CONDITION
(Test-Path csharp/src/Services/Music/PurgeService.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Services/Music/PurgeService.cs) -and ((rg 'PurgeOrphansAsync' csharp/src/Services/Music/PurgeService.cs | Measure-Object -Line).Lines -ge 1) -and (Test-Path csharp/tests/Scripts.Tests/Services/Music/PurgeServiceTests.cs)
```
```powershell
# Step B3.2
# FAIL CONDITION
(rg 'BeginTransaction' csharp/src/Services/Music/PurgeService.cs | Measure-Object -Line).Lines -eq 0

# PASS CONDITION
((rg 'BeginTransaction' csharp/src/Services/Music/PurgeService.cs | Measure-Object -Line).Lines -ge 1) -and ((rg 'Tracks' csharp/src/Services/Music/PurgeService.cs | Measure-Object -Line).Lines -ge 1) -and ((rg 'Albums' csharp/src/Services/Music/PurgeService.cs | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step B3.3
# FAIL CONDITION
(rg 'PurgeOrphansAsync' csharp/src/Orchestrators/ScrobbleSyncOrchestrator.cs | Measure-Object -Line).Lines -eq 0

# PASS CONDITION
((rg 'PurgeOrphansAsync' csharp/src/Orchestrators/ScrobbleSyncOrchestrator.cs | Measure-Object -Line).Lines -ge 1) -and ((rg 'ExecuteForceResyncAsync' csharp/src/Orchestrators/ScrobbleSyncOrchestrator.cs | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step B3.4
# FAIL CONDITION
(Test-Path csharp/tests/Scripts.Tests/Orchestrators/ScrobbleSyncOrchestratorTests.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/tests/Scripts.Tests/Orchestrators/ScrobbleSyncOrchestratorTests.cs) -and ((rg 'ForceResync|Purge' csharp/tests/Scripts.Tests/Orchestrators/ScrobbleSyncOrchestratorTests.cs | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step B4.1
# FAIL CONDITION
(rg 'MigrateAsync' csharp/tests/Scripts.Tests/DbContext/PostgresFixture.cs | Measure-Object -Line).Lines -eq 0

# PASS CONDITION
((rg 'template_schema' csharp/tests/Scripts.Tests/DbContext/PostgresFixture.cs | Measure-Object -Line).Lines -ge 1) -and ((rg 'MigrateAsync' csharp/tests/Scripts.Tests/DbContext/PostgresFixture.cs | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step B4.2
# FAIL CONDITION
(rg 'Guid\.NewGuid' csharp/tests/Scripts.Tests/DbContext/PostgresFixture.cs | Measure-Object -Line).Lines -eq 0

# PASS CONDITION
(rg 'test_.*Guid\.NewGuid.*:N' csharp/tests/Scripts.Tests/DbContext/PostgresFixture.cs | Measure-Object -Line).Lines -ge 1
```
```powershell
# Step B4.3
# FAIL CONDITION
(rg 'Database\.Migrate\(\)' csharp/tests/Scripts.Tests/DbContext/PostgresFixture.cs | Measure-Object -Line).Lines -eq 0

# PASS CONDITION
(rg 'Database\.Migrate\(\)' csharp/tests/Scripts.Tests/DbContext/PostgresFixture.cs | Measure-Object -Line).Lines -ge 1
```
```powershell
# Step B4.4
# FAIL CONDITION
(rg 'NpgsqlConnectionStringBuilder' csharp/tests/Scripts.Tests/DbContext/PostgresFixture.cs | Measure-Object -Line).Lines -eq 0

# PASS CONDITION
((rg 'SearchPath' csharp/tests/Scripts.Tests/DbContext/PostgresFixture.cs | Measure-Object -Line).Lines -ge 1) -and ((rg 'NpgsqlConnectionStringBuilder' csharp/tests/Scripts.Tests/DbContext/PostgresFixture.cs | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step B4.5
# FAIL CONDITION
(rg 'DROP SCHEMA' csharp/tests/Scripts.Tests/DbContext/PostgresFixture.cs | Measure-Object -Line).Lines -eq 0

# PASS CONDITION
(rg 'DROP SCHEMA.*CASCADE' csharp/tests/Scripts.Tests/DbContext/PostgresFixture.cs | Measure-Object -Line).Lines -ge 1
```
```powershell
# Step B4.6
# FAIL CONDITION
$false

# PASS CONDITION
dotnet test csharp/tests/Scripts.Tests/Scripts.Tests.csproj --parallel 2>$null; $LASTEXITCODE -eq 0
```
```powershell
# Step C1.1
# FAIL CONDITION
(Test-Path csharp/tests/Scripts.Tests/Mcp/SchemaListToolTests.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Mcp/Tools/SchemaListTool.cs) -and ((rg 'list_schemas' csharp/src/Mcp/Tools/SchemaListTool.cs | Measure-Object -Line).Lines -ge 1) -and (Test-Path csharp/tests/Scripts.Tests/Mcp/SchemaListToolTests.cs)
```
```powershell
# Step C1.2
# FAIL CONDITION
(Test-Path csharp/tests/Scripts.Tests/Mcp/TableListToolTests.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Mcp/Tools/TableListTool.cs) -and ((rg 'list_tables' csharp/src/Mcp/Tools/TableListTool.cs | Measure-Object -Line).Lines -ge 1) -and (Test-Path csharp/tests/Scripts.Tests/Mcp/TableListToolTests.cs)
```
```powershell
# Step C1.3
# FAIL CONDITION
(Test-Path csharp/tests/Scripts.Tests/Mcp/DescribeTableToolTests.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Mcp/Tools/DescribeTableTool.cs) -and ((rg 'describe_table' csharp/src/Mcp/Tools/DescribeTableTool.cs | Measure-Object -Line).Lines -ge 1) -and (Test-Path csharp/tests/Scripts.Tests/Mcp/DescribeTableToolTests.cs)
```
```powershell
# Step C1.4
# FAIL CONDITION
(Test-Path csharp/tests/Scripts.Tests/Mcp/EntityDefinitionToolTests.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Mcp/Tools/EntityDefinitionTool.cs) -and ((rg 'get_entity_definition' csharp/src/Mcp/Tools/EntityDefinitionTool.cs | Measure-Object -Line).Lines -ge 1) -and (Test-Path csharp/tests/Scripts.Tests/Mcp/EntityDefinitionToolTests.cs)
```
```powershell
# Step C2.1
# FAIL CONDITION
(Test-Path csharp/tests/Scripts.Tests/Mcp/EntitySearchToolTests.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Mcp/Tools/EntitySearchTool.cs) -and ((rg 'search_entities' csharp/src/Mcp/Tools/EntitySearchTool.cs | Measure-Object -Line).Lines -ge 1) -and (Test-Path csharp/tests/Scripts.Tests/Mcp/EntitySearchToolTests.cs)
```
```powershell
# Step C2.2
# FAIL CONDITION
(Test-Path csharp/tests/Scripts.Tests/Mcp/RowCountToolTests.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Mcp/Tools/RowCountTool.cs) -and ((rg 'get_row_count' csharp/src/Mcp/Tools/RowCountTool.cs | Measure-Object -Line).Lines -ge 1) -and (Test-Path csharp/tests/Scripts.Tests/Mcp/RowCountToolTests.cs)
```
```powershell
# Step C2.3
# FAIL CONDITION
(Test-Path csharp/tests/Scripts.Tests/Mcp/EntityGetToolTests.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Mcp/Tools/EntityGetTool.cs) -and ((rg 'get_entity_by_id' csharp/src/Mcp/Tools/EntityGetTool.cs | Measure-Object -Line).Lines -ge 1) -and (Test-Path csharp/tests/Scripts.Tests/Mcp/EntityGetToolTests.cs)
```
```powershell
# Step C2.4
# FAIL CONDITION
(Test-Path csharp/tests/Scripts.Tests/Mcp/DatabaseResourcesTests.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Mcp/Resources/DatabaseResources.cs) -and ((rg 'pg://database/migrations' csharp/src/Mcp/Resources/DatabaseResources.cs | Measure-Object -Line).Lines -ge 1) -and ((rg 'pg://database/stats' csharp/src/Mcp/Resources/DatabaseResources.cs | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step C3.1
# FAIL CONDITION
(Test-Path csharp/tests/Scripts.Tests/Mcp/HealthCheckToolTests.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Mcp/Tools/HealthCheckTool.cs) -and ((rg 'check_health' csharp/src/Mcp/Tools/HealthCheckTool.cs | Measure-Object -Line).Lines -ge 1) -and (Test-Path csharp/tests/Scripts.Tests/Mcp/HealthCheckToolTests.cs)
```
```powershell
# Step C3.2
# FAIL CONDITION
(Test-Path csharp/tests/Scripts.Tests/Mcp/ScrobbleSyncToolTests.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Mcp/Tools/ScrobbleSyncTool.cs) -and ((rg 'trigger_scrobble_sync' csharp/src/Mcp/Tools/ScrobbleSyncTool.cs | Measure-Object -Line).Lines -ge 1) -and (Test-Path csharp/tests/Scripts.Tests/Mcp/ScrobbleSyncToolTests.cs)
```
```powershell
# Step C3.3
# FAIL CONDITION
(Test-Path csharp/tests/Scripts.Tests/Mcp/SyncStatusToolTests.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Mcp/Tools/SyncStatusTool.cs) -and ((rg 'get_sync_status' csharp/src/Mcp/Tools/SyncStatusTool.cs | Measure-Object -Line).Lines -ge 1) -and (Test-Path csharp/tests/Scripts.Tests/Mcp/SyncStatusToolTests.cs)
```
```powershell
# Step C3.4
# FAIL CONDITION
(Test-Path csharp/tests/Scripts.Tests/Mcp/DatabaseStatsToolTests.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Mcp/Tools/DatabaseStatsTool.cs) -and ((rg 'get_database_stats' csharp/src/Mcp/Tools/DatabaseStatsTool.cs | Measure-Object -Line).Lines -ge 1) -and (Test-Path csharp/tests/Scripts.Tests/Mcp/DatabaseStatsToolTests.cs)
```
```powershell
# Step C3.5
# FAIL CONDITION
(Test-Path csharp/tests/Scripts.Tests/Mcp/SyncResourcesTests.cs) -eq $false

# PASS CONDITION
(Test-Path csharp/src/Mcp/Resources/SyncResources.cs) -and ((rg 'pg://sync/status' csharp/src/Mcp/Resources/SyncResources.cs | Measure-Object -Line).Lines -ge 1)
```
```powershell
# Step C3.6
# FAIL CONDITION
(rg 'pg://database/info' csharp/src/Mcp/Resources/DatabaseResources.cs | Measure-Object -Line).Lines -eq 0

# PASS CONDITION
(rg 'pg://database/info' csharp/src/Mcp/Resources/DatabaseResources.cs | Measure-Object -Line).Lines -ge 1
```
```powershell
# Step C3.7
# FAIL CONDITION
(rg 'pg://database/extensions' csharp/src/Mcp/Resources/DatabaseResources.cs | Measure-Object -Line).Lines -eq 0

# PASS CONDITION
(rg 'pg://database/extensions' csharp/src/Mcp/Resources/DatabaseResources.cs | Measure-Object -Line).Lines -ge 1
```
```powershell
# Step C4.1
# FAIL CONDITION
(gci csharp/tests/Scripts.Tests/Mcp/*MockTests.cs -ErrorAction SilentlyContinue | Measure-Object).Count -eq 0

# PASS CONDITION
(gci csharp/tests/Scripts.Tests/Mcp/*MockTests.cs -ErrorAction SilentlyContinue | Measure-Object).Count -ge 1
```
```powershell
# Step C4.2
# FAIL CONDITION
(rg '\[Test\]' csharp/tests/Scripts.Tests/Mcp/Integration/ | Measure-Object -Line).Lines -lt 61

# PASS CONDITION
(rg '\[Test\]' csharp/tests/Scripts.Tests/Mcp/Integration/ | Measure-Object -Line).Lines -ge 61
```
```powershell
# Step C4.3
# FAIL CONDITION
(rg '\[Test\]' csharp/tests/Scripts.Tests/Mcp/E2E/ | Measure-Object -Line).Lines -lt 26

# PASS CONDITION
(rg '\[Test\]' csharp/tests/Scripts.Tests/Mcp/E2E/ | Measure-Object -Line).Lines -ge 26
```
```powershell
# Step C4.4
# FAIL CONDITION
(Test-Path csharp/tests/Scripts.Tests/Mcp/Performance) -eq $false

# PASS CONDITION
(Test-Path csharp/tests/Scripts.Tests/Mcp/Performance) -and ((gci csharp/tests/Scripts.Tests/Mcp/Performance/*.cs -ErrorAction SilentlyContinue | Measure-Object).Count -ge 1)
```
```powershell
# Step D3
# FAIL CONDITION
dotnet build csharp/Scripts.slnx 2>$null; $LASTEXITCODE -ne 0

# PASS CONDITION
dotnet build csharp/Scripts.slnx 2>$null; $LASTEXITCODE -eq 0
```
```powershell
# Step D5
# FAIL CONDITION
(rg 'public.*Bio|public.*ImageUrl|public.*DateTime.*CreatedAt|public.*DateTime.*UpdatedAt' csharp/src/Data/Entities/Artist.cs | Measure-Object -Line).Lines -lt 4

# PASS CONDITION
((rg 'public.*string.*Name' csharp/src/Data/Entities/Artist.cs | Measure-Object -Line).Lines -ge 1) -and ((rg 'public.*Bio' csharp/src/Data/Entities/Artist.cs | Measure-Object -Line).Lines -ge 1) -and ((rg 'public.*ImageUrl' csharp/src/Data/Entities/Artist.cs | Measure-Object -Line).Lines -ge 1) -and ((rg 'public.*DateTime.*CreatedAt' csharp/src/Data/Entities/Artist.cs | Measure-Object -Line).Lines -ge 1) -and ((rg 'public.*DateTime.*UpdatedAt' csharp/src/Data/Entities/Artist.cs | Measure-Object -Line).Lines -ge 1) -and ((rg 'FK_albums_artists_ArtistId' csharp/Migrations/*_WaveB1_SchemaEvolution.cs | Measure-Object -Line).Lines -eq 0)
```
```powershell
# Step E1.1
# FAIL CONDITION
(rg 'InterruptResume|ReorderOnly|SaveLoadOrder' csharp/tests/Scripts.Tests/Services/Sync/YouTube/YouTubeServiceTests.cs 2>$null | Measure-Object -Line).Lines -eq 0

# PASS CONDITION
(rg 'InterruptResume|ReorderOnly|SaveLoadOrder' csharp/tests/Scripts.Tests/Services/Sync/YouTube/YouTubeServiceTests.cs 2>$null | Measure-Object -Line).Lines -ge 3
```
```powershell
# Step E2.1
# FAIL CONDITION
(Test-Path csharp/src/Orchestrators/YouTubePlaylistOrchestrator.cs) -eq $true

# PASS CONDITION
((Test-Path csharp/src/Orchestrators/YouTubePlaylistOrchestrator.cs) -eq $false) -and ((rg 'YouTubePlaylistOrchestrator' csharp/src/ -l 2>$null | Measure-Object).Count -eq 0)
```
```powershell
# Step E2.2
# FAIL CONDITION
(rg 'SortPlaylistAsync' csharp/src/Services/Sync/YouTube/YouTubeService.cs 2>$null | Measure-Object -Line).Lines -eq 0

# PASS CONDITION
(rg 'SortPlaylistAsync' csharp/src/Services/Sync/YouTube/YouTubeService.cs 2>$null | Measure-Object -Line).Lines -ge 1
```
```powershell
# Step E3.1
# FAIL CONDITION
(rg 'TextNormalizer\.Normalize' csharp/src/Services/Sync/YouTube/YouTubeService.cs 2>$null | Measure-Object -Line).Lines -eq 0

# PASS CONDITION
(rg 'TextNormalizer\.Normalize' csharp/src/Services/Sync/YouTube/YouTubeService.cs 2>$null | Measure-Object -Line).Lines -ge 1
```






