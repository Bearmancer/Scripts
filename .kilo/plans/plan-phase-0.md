# Phase 0: Environment Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Downgrade entire solution from .NET 11 Preview to .NET 10 stable, pin EF Core 10 / Npgsql 10 NuGet packages, create docker-compose.yml for PostgreSQL 18 (credentials in `.env`).

**Architecture:** PowerShell scripts in `.kilo/tests` assert SDK presence, global.json pinning, TFM retargeting, NuGet version pinning, and baseline restore/build/test. Docker Compose for PostgreSQL 18.

**Tech Stack:** .NET 10, EF Core 10, Npgsql 10, PostgreSQL 18, Docker Compose, PowerShell

---

### Task 0.0: Create docker-compose.yml for PostgreSQL 18

**Files:**
- Create: `docker-compose.yml`

- [ ] **Step 1: Write docker-compose.yml**

Create `docker-compose.yml`:
```yaml
services:
  postgres:
    image: postgres:18
    container_name: postgres
    env_file:
      - .env
    environment:
      POSTGRES_DB: ${POSTGRES_DB:-pg_db}
      POSTGRES_USER: ${POSTGRES_USER:-lance}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-lance}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./.kilo/references/init_schema.sql:/docker-entrypoint-initdb.d/init.sql
    restart: unless-stopped
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER:-lance} -d ${POSTGRES_DB:-pg_db}"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  postgres_data:
    driver: local
```

- [ ] **Step 1.5: Create .env file with credentials**

```powershell
@"
POSTGRES_DB=pg_db
POSTGRES_USER=lance
POSTGRES_PASSWORD=lance
PGCONNSTR=Host=localhost;Database=pg_db;Username=lance;Password=lance
DOCKER_MCP_DB_URL=postgresql://lance:lance@host.docker.internal:5432/pg_db
"@ | Set-Content ".env" -Encoding UTF8
```

- [ ] **Step 2: Verify files exist**

Run: `Test-Path "C:\Users\Lance\Dev\Scripts\docker-compose.yml" -and (Test-Path "C:\Users\Lance\Dev\Scripts\.env")`
Expected: `True`

- [ ] **Step 3: Commit**

```bash
git add docker-compose.yml
git commit -m "infra: add docker-compose.yml for PostgreSQL 18 (credentials in .env)"
```

---

### Task 0.1: Verify .NET 10 SDK availability

**Files:**
- Create: `.kilo/tests/Assert-DotnetSdk.ps1`

- [ ] **Step 1: Write the PowerShell script**

Create `.kilo/tests/Assert-DotnetSdk.ps1`:
```powershell
$ErrorActionPreference = 'Stop'
$out = dotnet --list-sdks 2>&1
if (-not ($out -match '10\.\d+\.\d+')) {
    throw "FAIL: No .NET 10 SDK found. Install from https://dotnet.microsoft.com/download/dotnet/10.0`n$out"
}
Write-Host "PASS: .NET 10 SDK detected."
```

- [ ] **Step 2: Read-back verification**

Run: `Get-Content C:\Users\Lance\Dev\Scripts\.kilo\tests\Assert-DotnetSdk.ps1 | Select-String '10\.'`
Expected: Match found.

- [ ] **Step 3: Run the script**

Run: `pwsh -File C:\Users\Lance\Dev\Scripts\.kilo\tests\Assert-DotnetSdk.ps1`
Expected: `PASS: .NET 10 SDK detected.`

- [ ] **Step 4: Commit**

```bash
git add .kilo/tests/Assert-DotnetSdk.ps1
git commit -m "chore: verify .NET 10 SDK availability"
```

---

### Task 0.2: Pin global.json to .NET 10

**Files:**
- Modify: `global.json`

- [ ] **Step 1: Write failing test**

Create `.kilo/tests/Assert-GlobalJson.ps1`:
```powershell
$ErrorActionPreference = 'Stop'
$path = 'C:\Users\Lance\Dev\Scripts\global.json'
if (-not (Test-Path $path)) { throw "FAIL: global.json absent." }
$j = Get-Content $path -Raw | ConvertFrom-Json
if (-not $j.sdk.version.StartsWith('10.')) { throw "FAIL: sdk.version is '$($j.sdk.version)', expected 10.x" }
Write-Host "PASS: global.json pins SDK to $($j.sdk.version)."
```

- [ ] **Step 2: Run test — expect FAIL (currently 11.x)**

Run: `pwsh -File C:\Users\Lance\Dev\Scripts\.kilo\tests\Assert-GlobalJson.ps1`
Expected: FAIL

- [ ] **Step 3: Write global.json with .NET 10 SDK version**

```powershell
$ErrorActionPreference = 'Stop'
$sdk = (dotnet --list-sdks 2>&1 | Select-String '10\.\d+\.\d+' | Select-Object -Last 1).Matches[0].Value
$json = @{ sdk = @{ version = $sdk; rollForward = "latestPatch" }; test = @{ runner = "Microsoft.Testing.Platform" } } | ConvertTo-Json
Set-Content -Path 'C:\Users\Lance\Dev\Scripts\global.json' -Value $json -Encoding UTF8
```

- [ ] **Step 4: Re-run test — expect PASS**

Run: `pwsh -File C:\Users\Lance\Dev\Scripts\.kilo\tests\Assert-GlobalJson.ps1`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add global.json .kilo/tests/Assert-GlobalJson.ps1
git commit -m "chore: pin global.json to .NET 10, add database config"
```

---

### Task 0.3: Retarget Directory.Build.props — net11.0 → net10.0

**Files:**
- Modify: `csharp/Directory.Build.props`

- [ ] **Step 1: Write failing test**

Create `.kilo/tests/Assert-BuildProps-Tfm.ps1`:
```powershell
$ErrorActionPreference = 'Stop'
$content = Get-Content 'C:\Users\Lance\Dev\Scripts\csharp\Directory.Build.props' -Raw
if ($content -notmatch '<TargetFramework>net10\.0</TargetFramework>') {
    throw "FAIL: Directory.Build.props does not target net10.0"
}
if ($content -match 'SuppressNETCoreSdkPreviewMessage') {
    throw "FAIL: SuppressNETCoreSdkPreviewMessage still present"
}
Write-Host "PASS: Directory.Build.props targets net10.0"
```

- [ ] **Step 2: Run test — expect FAIL**

Run: `pwsh -File C:\Users\Lance\Dev\Scripts\.kilo\tests\Assert-BuildProps-Tfm.ps1`
Expected: FAIL

- [ ] **Step 3: Fix Directory.Build.props**

```powershell
$path = 'C:\Users\Lance\Dev\Scripts\csharp\Directory.Build.props'
(Get-Content $path -Raw -Encoding UTF8) `
    -replace '<TargetFramework>net11\.0</TargetFramework>','<TargetFramework>net10.0</TargetFramework>' `
    -replace '\s*<SuppressNETCoreSdkPreviewMessage>true</SuppressNETCoreSdkPreviewMessage>\s*', '' |
    Set-Content $path -Encoding UTF8 -NoNewline
```

- [ ] **Step 4: Re-run test — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add csharp/Directory.Build.props .kilo/tests/Assert-BuildProps-Tfm.ps1
git commit -m "chore: retarget Directory.Build.props to net10.0"
```

---

### Task 0.4: Pin EF Core packages to 10.x

**Files:**
- Modify: `csharp/CSharpScripts.csproj`

- [ ] **Step 1: Write failing test**

Create `.kilo/tests/Assert-EfCore-Version.ps1`:
```powershell
$ErrorActionPreference = 'Stop'
$content = Get-Content 'C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj' -Raw
if ($content -notmatch 'Microsoft\.EntityFrameworkCore" Version="10\.') {
    throw "FAIL: EF Core 10.x version not found in csproj."
}
Write-Host "PASS: EF Core 10.x pinned."
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Pin EF Core packages**

```powershell
$path = 'C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj'
(Get-Content $path -Raw -Encoding UTF8) `
    -replace '"Microsoft\.EntityFrameworkCore" Version="[^"]*"','"Microsoft.EntityFrameworkCore" Version="10.0.0"' `
    -replace '"Microsoft\.EntityFrameworkCore\.Design" Version="[^"]*"','"Microsoft.EntityFrameworkCore.Design" Version="10.0.0"' `
    -replace '"Microsoft\.EntityFrameworkCore\.Tools" Version="[^"]*"','"Microsoft.EntityFrameworkCore.Tools" Version="10.0.0"' |
    Set-Content $path -Encoding UTF8 -NoNewline
```

- [ ] **Step 4: Re-run test — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add csharp/CSharpScripts.csproj .kilo/tests/Assert-EfCore-Version.ps1
git commit -m "chore: pin EF Core packages to 10.0.0"
```

---

### Task 0.5: Pin Npgsql packages to 10.x

**Files:**
- Modify: `csharp/CSharpScripts.csproj`

- [ ] **Step 1: Write failing test**

Create `.kilo/tests/Assert-Npgsql-Version.ps1`:
```powershell
$ErrorActionPreference = 'Stop'
$content = Get-Content 'C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj' -Raw
if ($content -notmatch 'Npgsql\.EntityFrameworkCore\.PostgreSQL" Version="10\.') {
    throw "FAIL: Npgsql EF Core 10.x version not found in csproj."
}
if ($content -notmatch '"Npgsql" Version="10\.') {
    throw "FAIL: Npgsql 10.x version not found in csproj."
}
Write-Host "PASS: Npgsql 10.x pinned."
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Pin Npgsql packages**

```powershell
$path = 'C:\Users\Lance\Dev\Scripts\csharp\CSharpScripts.csproj'
(Get-Content $path -Raw -Encoding UTF8) `
    -replace '"Npgsql\.EntityFrameworkCore\.PostgreSQL" Version="[^"]*"','"Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0"' `
    -replace '"Npgsql" Version="[^"]*"','"Npgsql" Version="10.0.0"' |
    Set-Content $path -Encoding UTF8 -NoNewline
```

- [ ] **Step 4: Re-run test — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add csharp/CSharpScripts.csproj .kilo/tests/Assert-Npgsql-Version.ps1
git commit -m "chore: pin Npgsql packages to 10.0.0"
```

---

### Task 0.6: `dotnet restore` baseline

**Files:**
- Create: `.kilo/tests/Verify-Restore.ps1`

- [ ] **Step 1: Run restore**

```powershell
$ErrorActionPreference = 'Stop'
Set-Location "C:\Users\Lance\Dev\Scripts"
$out = dotnet restore 2>&1
if ($out -match 'error') { throw "Restore errors: $out" }
if ($out -match 'net11\.0') { throw "net11.0 references still present" }
Write-Host "RESTORE_PASS"
```

- [ ] **Step 2: Commit**

```bash
git add .kilo/tests/Verify-Restore.ps1
git commit -m "chore: baseline dotnet restore on .NET 10"
```

---

### Task 0.7: `dotnet build` baseline

**Files:**
- Create: `.kilo/tests/Verify-Build.ps1`

- [ ] **Step 1: Run build**

```powershell
$ErrorActionPreference = 'Stop'
Set-Location "C:\Users\Lance\Dev\Scripts"
$out = dotnet build --no-restore 2>&1
if ($out -match 'Error\(s\)' -and $out -notmatch '0 Error') { throw $out }
Write-Host "BUILD_PASS"
```

- [ ] **Step 2: Commit**

```bash
git add .kilo/tests/Verify-Build.ps1
git commit -m "chore: baseline dotnet build on .NET 10"
```

---

### Task 0.8: `dotnet test` baseline

**Files:**
- Create: `.kilo/tests/Verify-Test.ps1`

- [ ] **Step 1: Run tests (skip if test project absent)**

```powershell
$ErrorActionPreference = 'Stop'
Set-Location "C:\Users\Lance\Dev\Scripts"
$testProj = 'csharp/src/Tests/CSharpScripts.Tests.csproj'
if (-not (Test-Path $testProj)) {
    Write-Host "SKIP: Test project does not exist yet (Phase 1 will create it)"
    exit 0
}
$out = dotnet test $testProj --no-build 2>&1
if ($out -match 'Failed') { throw $out }
Write-Host "TEST_PASS"
```

- [ ] **Step 2: Commit**

```bash
git add .kilo/tests/Verify-Test.ps1
git commit -m "chore: baseline dotnet test on .NET 10"
```
