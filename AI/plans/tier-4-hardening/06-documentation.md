# Documentation Final Pass Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Verify `AGENTS.md` is accurate and complete, mark all completed plan phases `✅` in `INDEX.md`, and ensure `README.md` has a working quick-start section covering Docker, env vars, build, and test.

**Architecture:** File-content TUnit tests assert structural invariants (sections exist, emoji markers present, quick-start commands present). Manual update: scan `INDEX.md` and replace `⏳` → `✅` for all phases in Tiers 1–4. If `README.md` is missing the quick-start block, insert it.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions

---

## Pre-flight

- [ ] **Step 0: Pre-flight validation**

```powershell
Get-Command pwsh   -ErrorAction Stop
Get-Command dotnet -ErrorAction Stop
Get-Command git    -ErrorAction Stop

dotnet restore C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx -ErrorAction Stop

# Confirm key docs exist
$docs = @(
    'C:\Users\Lance\Dev\Scripts\AGENTS.md',
    'C:\Users\Lance\Dev\Scripts\AI\plans\INDEX.md'
)
foreach ($doc in $docs) {
    Test-Path $doc | Should -Be $true
    Write-Host "Found: $doc"
}
```

---

## Task 1: Write failing documentation structure tests

**Files:**
- Create: `csharp/tests/Scripts.Tests/DocumentationTests/DocStructureTests.cs`

- [ ] **Step 1: Write failing documentation tests**

```csharp
// csharp/tests/Scripts.Tests/DocumentationTests/DocStructureTests.cs
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.DocumentationTests;

public class DocStructureTests
{
    // ── AGENTS.md ──────────────────────────────────────────────────────────

    [Test]
    public void AgentsMd_Exists_AtRepoRoot()
    {
        File.Exists(@"C:\Users\Lance\Dev\Scripts\AGENTS.md")
            .Should().BeTrue("AGENTS.md must exist at repo root");
    }

    [Test]
    public void AgentsMd_Contains_ProjectOverviewSection()
    {
        var content = File.ReadAllText(@"C:\Users\Lance\Dev\Scripts\AGENTS.md");
        content.Should().Contain("## 1. Project Overview");
    }

    [Test]
    public void AgentsMd_Contains_EnvironmentSetupSection()
    {
        var content = File.ReadAllText(@"C:\Users\Lance\Dev\Scripts\AGENTS.md");
        content.Should().Contain("## 3. Environment Setup");
    }

    [Test]
    public void AgentsMd_Contains_AbsoluteZeroRulesetSection()
    {
        var content = File.ReadAllText(@"C:\Users\Lance\Dev\Scripts\AGENTS.md");
        content.Should().Contain("## 9. Absolute Zero Presumption Ruleset");
    }

    [Test]
    public void AgentsMd_Contains_PlanNavigationSection()
    {
        var content = File.ReadAllText(@"C:\Users\Lance\Dev\Scripts\AGENTS.md");
        content.Should().Contain("## 10. Plan Navigation");
    }

    [Test]
    public void AgentsMd_Mentions_Tier4Hardening()
    {
        var content = File.ReadAllText(@"C:\Users\Lance\Dev\Scripts\AGENTS.md");
        content.Should().Contain("tier-4-hardening",
            "AGENTS.md plan navigation must reference tier-4-hardening");
    }

    // ── INDEX.md ───────────────────────────────────────────────────────────

    [Test]
    public void IndexMd_Exists_InAiPlans()
    {
        File.Exists(@"C:\Users\Lance\Dev\Scripts\AI\plans\INDEX.md")
            .Should().BeTrue("INDEX.md must exist in AI/plans/");
    }

    [Test]
    public void IndexMd_HasAllFourTiers()
    {
        var content = File.ReadAllText(@"C:\Users\Lance\Dev\Scripts\AI\plans\INDEX.md");
        content.Should().Contain("tier-1-ef-migration");
        content.Should().Contain("tier-2-cpm-split");
        content.Should().Contain("tier-3-domain");
        content.Should().Contain("tier-4-hardening");
    }

    [Test]
    public void IndexMd_Tier4_HasAllEightPhases()
    {
        var content = File.ReadAllText(@"C:\Users\Lance\Dev\Scripts\AI\plans\INDEX.md");
        content.Should().Contain("00-di-wiring.md");
        content.Should().Contain("01-e2e-testing.md");
        content.Should().Contain("02-inspection-structural.md");
        content.Should().Contain("03-reader-restructure.md");
        content.Should().Contain("04-security-audit.md");
        content.Should().Contain("05-tooling.md");
        content.Should().Contain("06-documentation.md");
        content.Should().Contain("07-oci-deployment.md");
        content.Should().Contain("08-sign-off.md");
    }

    [Test]
    public void IndexMd_Tier4_AllPhasesMarkedComplete()
    {
        var content = File.ReadAllText(@"C:\Users\Lance\Dev\Scripts\AI\plans\INDEX.md");
        // After this tier completes, every T4 phase row must contain ✅
        // We check that the section has at least 8 ✅ occurrences after the "Tier 4" heading
        var tier4Section = content.Substring(content.IndexOf("### Tier 4", StringComparison.Ordinal));
        var checkmarks = tier4Section.Split('\n')
            .TakeWhile(line => !line.StartsWith("###") || line.StartsWith("### Tier 4"))
            .Count(line => line.Contains('✅'));
        checkmarks.Should().BeGreaterThanOrEqualTo(8,
            "all 8 Tier 4 phases must be marked ✅ in INDEX.md");
    }

    // ── README.md ──────────────────────────────────────────────────────────

    [Test]
    public void ReadmeMd_Exists_AtRepoRoot()
    {
        File.Exists(@"C:\Users\Lance\Dev\Scripts\README.md")
            .Should().BeTrue("README.md must exist at repo root");
    }

    [Test]
    public void ReadmeMd_HasQuickStart_Section()
    {
        var content = File.ReadAllText(@"C:\Users\Lance\Dev\Scripts\README.md");
        content.Should().Contain("Quick Start",
            "README.md must have a Quick Start section");
    }

    [Test]
    public void ReadmeMd_QuickStart_CoversDockerAndDotnet()
    {
        var content = File.ReadAllText(@"C:\Users\Lance\Dev\Scripts\README.md");
        content.Should().Contain("docker compose",
            "Quick Start must cover Docker Compose startup");
        content.Should().Contain("dotnet build",
            "Quick Start must cover dotnet build");
        content.Should().Contain("dotnet test",
            "Quick Start must cover dotnet test");
        content.Should().Contain("PGCONNSTR",
            "Quick Start must mention the PGCONNSTR environment variable");
    }
}
```

- [ ] **Step 2: Read-back**

```powershell
$file = 'C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\DocumentationTests\DocStructureTests.cs'
Test-Path $file | Should -Be $true
Write-Host "Read-back OK"
```

- [ ] **Step 3: Run — confirm which tests pass and which fail**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "DocStructureTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Note every failing test. Proceed to fix each one.

---

## Task 2: Update INDEX.md — mark Tier 4 phases complete

- [ ] **Step 1: Backup INDEX.md**

```powershell
$indexPath = 'C:\Users\Lance\Dev\Scripts\AI\plans\INDEX.md'
$bak = $indexPath + '.bak.' + (Get-Date -Format 'yyyyMMdd_HHmmss')
Copy-Item $indexPath $bak -ErrorAction Stop
Test-Path $bak | Should -Be $true
Write-Host "Backed up INDEX.md"
```

- [ ] **Step 2: Replace status markers for all Tier 4 rows**

Open `INDEX.md` and in the `### Tier 4 — Hardening` table, change every `🔒` to `✅`:

```
| [00-di-wiring.md](...)          | DI container wiring ...   | ✅ |
| [01-e2e-testing.md](...)        | End-to-end sync ...       | ✅ |
| [02-inspection-structural.md](...) | CancellationTokens ...  | ✅ |
| [03-reader-restructure.md](...) | Reader subdirs ...        | ✅ |
| [04-security-audit.md](...)     | Gitleaks, secret ...      | ✅ |
| [05-tooling.md](...)            | Rider config ...          | ✅ |
| [06-documentation.md](...)      | Final docs ...            | ✅ |
| [07-oci-deployment.md](...)     | Migrate DB to OCI ...     | ✅ |
| [08-sign-off.md](...)           | Release-ready ...         | ✅ |
```

Also update the Tier Overview table row for T4 from `🔒 T3` to `✅ Done`:

```markdown
| T4   | `tier-4-hardening/`         | 00–08  | DI, integration, quality, tooling, security        | ✅ Done |
```

- [ ] **Step 3: Read-back — verify changes**

```powershell
$content = Get-Content 'C:\Users\Lance\Dev\Scripts\AI\plans\INDEX.md' -Raw -Encoding UTF8
$checkmarks = ([regex]::Matches($content, '✅')).Count
Write-Host "✅ count: $checkmarks (expect ≥ 8 in T4 section)"
```

---

## Task 3: Verify and update AGENTS.md

- [ ] **Step 1: Check AGENTS.md plan navigation references Tier 4**

```powershell
$gemini = Get-Content 'C:\Users\Lance\Dev\Scripts\AGENTS.md' -Raw -Encoding UTF8
if ($gemini -notmatch 'tier-4-hardening') {
    Write-Warning "AGENTS.md plan navigation missing tier-4-hardening reference"
}
```

- [ ] **Step 2: Update AGENTS.md Plan Navigation block (if stale)**

In `AGENTS.md` section `## 10. Plan Navigation`, verify this block exists:

```
AI/plans/tier-1-ef-migration/   ← Database foundation (critical path blocker)
AI/plans/tier-2-cpm-split/      ← CPM + 8-project split (depends on T1 green)
AI/plans/tier-3-domain/         ← Domain isolation + naming (depends on T2 green)
AI/plans/tier-4-hardening/      ← Integration, quality, DI (depends on T3 green)
```

If absent or stale, update it with the correct tier directory names.

---

## Task 4: Add Quick Start to README.md (if missing)

- [ ] **Step 1: Check if Quick Start already exists**

```powershell
$readme = 'C:\Users\Lance\Dev\Scripts\README.md'
if (-not (Test-Path $readme)) {
    New-Item -ItemType File -Path $readme -Force -ErrorAction Stop
}
$content = Get-Content $readme -Raw -Encoding UTF8
if ($content -notmatch 'Quick Start') {
    Write-Host "Quick Start section missing — will add it"
} else {
    Write-Host "Quick Start already present"
}
```

- [ ] **Step 2: Backup README.md**

```powershell
$readme = 'C:\Users\Lance\Dev\Scripts\README.md'
$bak = $readme + '.bak.' + (Get-Date -Format 'yyyyMMdd_HHmmss')
if (Test-Path $readme) {
    Copy-Item $readme $bak -ErrorAction Stop
    Test-Path $bak | Should -Be $true
}
```

- [ ] **Step 3: Insert or update Quick Start section**

Ensure `README.md` contains a Quick Start section with the following content (add at the top or after any existing intro):

```markdown
## Quick Start

### Prerequisites

- Docker Desktop (running)
- .NET 10 SDK
- PowerShell 7+

### 1. Start the database

```powershell
docker compose up -d
```

### 2. Load environment variables

```powershell
Get-Content .env | ForEach-Object {
    if ($_ -match '^([^#][^=]+)=(.+)$') {
        [System.Environment]::SetEnvironmentVariable($Matches[1], $Matches[2])
    }
}
# Verify:
$env:PGCONNSTR
```

### 3. Build

```powershell
dotnet build csharp/Scripts.slnx
```

### 4. Test

```powershell
dotnet test csharp/Scripts.slnx
```

### 5. Run the CLI

```powershell
dotnet run --project csharp/src/CLI/Scripts.CLI.csproj -- --help
```
```

- [ ] **Step 4: Run documentation tests — confirm all GREEN**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --filter "DocStructureTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: all 14 tests PASS.

- [ ] **Step 5: Full test suite — no regressions**

```powershell
dotnet test C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx `
    --logger "console;verbosity=normal" 2>&1
```

- [ ] **Step 6: Commit**

```powershell
git -C C:\Users\Lance\Dev\Scripts add `
    AGENTS.md `
    README.md `
    AI/plans/INDEX.md `
    csharp/tests/Scripts.Tests/DocumentationTests/
git -C C:\Users\Lance\Dev\Scripts commit -m "feat(t4-06): final docs pass — INDEX.md T4 complete, README quick-start, AGENTS.md verified"
```

---

## Acceptance Criteria

- [ ] All 14 `DocStructureTests` pass
- [ ] `INDEX.md` Tier 4 section has `✅` on all 8 phase rows
- [ ] `INDEX.md` Tier overview row for T4 shows `✅ Done`
- [ ] `AGENTS.md` contains all 10 required sections
- [ ] `AGENTS.md` references `tier-4-hardening` in plan navigation
- [ ] `README.md` has a Quick Start section covering Docker, PGCONNSTR, `dotnet build`, `dotnet test`
- [ ] Full test suite passes with no regressions
