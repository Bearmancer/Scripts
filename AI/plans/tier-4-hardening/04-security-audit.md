# Security Audit Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Verify no secrets appear anywhere in working directory or C# source files, confirm `.env` is git-ignored, update Python dependencies, and run `uv sync` + `safety check`.

**Architecture:** Gitleaks scans the working tree (not git history) for credential patterns. TUnit reflection tests scan C# source for hardcoded connection string fragments. Python tooling audit verifies `pyproject.toml` dependencies are up-to-date and have no known CVEs. All findings are remediated before the commit.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / PostgreSQL 18 / TUnit / FluentAssertions / Gitleaks / Python uv / safety

---

## Pre-flight

- [ ] **Step 0: Pre-flight validation**

```powershell
Get-Command pwsh   -ErrorAction Stop
Get-Command dotnet -ErrorAction Stop
Get-Command git    -ErrorAction Stop

# Gitleaks must be installed
Get-Command gitleaks -ErrorAction Stop
Write-Host "gitleaks: $(gitleaks version)"

# uv must be installed
Get-Command uv -ErrorAction Stop
Write-Host "uv: $(uv --version)"

dotnet restore /home/lance/Scripts/csharp/Scripts.slnx -ErrorAction Stop
```

---

## Task 1: Write security TUnit tests

**Files:**
- Create: `csharp/tests/Scripts.Tests/SecurityTests/SecretScanTests.cs`

- [ ] **Step 1: Write failing secret-scan tests**

```csharp
// csharp/tests/Scripts.Tests/SecurityTests/SecretScanTests.cs
using System.Text.RegularExpressions;
using FluentAssertions;
using TUnit;

namespace Scripts.Tests.SecurityTests;

public class SecretScanTests
{
    private static readonly string CsharpSrcRoot =
        @"/home/lance/Scripts/csharp/src";

    private static IEnumerable<string> GetSourceFiles() =>
        Directory.GetFiles(CsharpSrcRoot, "*.cs", SearchOption.AllDirectories)
                 .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"));

    [Test]
    public void NoHardcodedConnectionStrings_InCSharpSource()
    {
        var violations = GetSourceFiles()
            .Where(f => File.ReadAllText(f).Contains("Host=localhost;Database="))
            .ToList();

        violations.Should().BeEmpty(
            $"these files contain hardcoded connection strings: {string.Join(", ", violations.Select(Path.GetFileName))}");
    }

    [Test]
    public void NoPasswordPatterns_InCSharpSource()
    {
        var passwordPattern = new Regex(@"Password=[A-Za-z0-9!@#\$%\^&\*\(\)_\+\-=\[\]\{\}\|;:,\.<>\?]+",
            RegexOptions.Compiled);

        var violations = GetSourceFiles()
            .Where(f => passwordPattern.IsMatch(File.ReadAllText(f)))
            .Select(Path.GetFileName)
            .ToList();

        violations.Should().BeEmpty(
            $"these files contain hardcoded passwords: {string.Join(", ", violations)}");
    }

    [Test]
    public void EnvFile_IsInGitignore()
    {
        var gitignorePath = @"/home/lance/Scripts/.gitignore";
        File.Exists(gitignorePath).Should().BeTrue(".gitignore must exist at repo root");

        var gitignore = File.ReadAllText(gitignorePath);
        gitignore.Should().Contain(".env",
            ".gitignore must exclude .env to prevent secret leakage");
    }

    [Test]
    public void DotEnvFile_DoesNotExist_InGitTrackedFiles()
    {
        // .env must not be tracked by git
        var result = RunGit("ls-files .env");
        result.Trim().Should().BeEmpty(".env must not be tracked by git (should be in .gitignore)");
    }

    [Test]
    public void NoPgConnStr_HardcodedInAnyFile()
    {
        // Must not appear as a literal string (real value contains Host=, Password=, etc.)
        var violations = GetSourceFiles()
            .Where(f =>
            {
                var content = File.ReadAllText(f);
                return content.Contains("Host=") && content.Contains("Password=");
            })
            .Select(Path.GetFileName)
            .ToList();

        violations.Should().BeEmpty(
            $"files with potential full connection strings: {string.Join(", ", violations)}");
    }

    private static string RunGit(string arguments)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = @"/home/lance/Scripts",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        using var proc = System.Diagnostics.Process.Start(psi)!;
        var output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();
        return output;
    }
}
```

- [ ] **Step 2: Read-back**

```powershell
$file = '/home/lance/Scripts/csharp/tests\Scripts.Tests\SecurityTests\SecretScanTests.cs'
Test-Path $file | Should -Be $true
Write-Host "Read-back OK"
```

- [ ] **Step 3: Run — confirm all PASS (or identify violations)**

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "SecretScanTests" `
    --logger "console;verbosity=detailed" 2>&1
```

If any test fails: the failure message names the offending file. Remediate before proceeding.

- [ ] **Step 3.5: State assessment**

If violations found:
1. Open the file.
2. Replace the hardcoded value with `Environment.GetEnvironmentVariable("PGCONNSTR")` or similar.
3. Re-run the test until it passes.

---

## Task 2: Run Gitleaks working-directory scan

- [ ] **Step 1: Run Gitleaks scan**

```powershell
$result = gitleaks detect `
    --no-git `
    --source '/home/lance/Scripts' `
    --config '/home/lance/Scripts/.gitleaks.toml' `
    2>&1

Write-Host $result
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    throw "Gitleaks found secrets (exit code $exitCode). Review output above and redact before continuing."
}
Write-Host "Gitleaks: CLEAN"
```

> **Note:** If `.gitleaks.toml` does not exist, run without `--config`:
> ```powershell
> gitleaks detect --no-git --source '/home/lance/Scripts' 2>&1
> ```

- [ ] **Step 2: If Gitleaks reports findings — redact them**

For each finding:
1. Note the file path and line.
2. Replace the secret with an environment variable reference or a placeholder.
3. Verify the file no longer triggers Gitleaks.
4. Re-run the full scan until exit code is `0`.

- [ ] **Step 3: Re-run TUnit security tests**

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "SecretScanTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: all 5 tests PASS.

---

## Task 3: Python dependency audit

- [ ] **Step 1: Sync Python dependencies**

```powershell
$pythonDir = '/home/lance/Scripts/python'
Test-Path $pythonDir | Should -Be $true

uv sync --project $pythonDir 2>&1 | Tee-Object -Variable syncOut
$syncOut | Write-Host
if ($LASTEXITCODE -ne 0) { throw "uv sync failed" }
Write-Host "uv sync: OK"
```

- [ ] **Step 2: Run safety check for CVEs**

```powershell
uv run --project '/home/lance/Scripts/python' safety check 2>&1 | Tee-Object -Variable safetyOut
$safetyOut | Write-Host
# safety exits 0 if no CVEs, non-zero if vulnerabilities found
if ($LASTEXITCODE -ne 0) {
    Write-Warning "safety check found vulnerabilities — review above output"
    # For each finding: update the affected package in pyproject.toml via uv add <package>@latest
}
```

- [ ] **Step 3: Update any flagged packages**

For each CVE found:
```powershell
# Example — update a specific package
uv add --project '/home/lance/Scripts/python' requests --upgrade
```

Re-run `uv run safety check` after each update until exit code is `0`.

- [ ] **Step 4: Write Python audit test (file-content check)**

```csharp
// Add to SecretScanTests.cs
[Test]
public void PythonFiles_ContainNoHardcodedApiKeys()
{
    var pythonRoot = @"/home/lance/Scripts/python";
    var apiKeyPattern = new Regex(@"[A-Za-z0-9_-]{32,}", RegexOptions.Compiled);
    var knownSafePatterns = new[] { "hash", "digest", "checksum", "uuid", "placeholder" };

    var violations = Directory.GetFiles(pythonRoot, "*.py", SearchOption.AllDirectories)
        .Where(f => !f.Contains("__pycache__"))
        .Where(f =>
        {
            var content = File.ReadAllText(f);
            // Look for assignment of a long string that looks like an API key
            return Regex.IsMatch(content, @"(?:api_key|token|secret)\s*=\s*['""][A-Za-z0-9_\-]{20,}['""]",
                RegexOptions.IgnoreCase);
        })
        .Select(Path.GetFileName)
        .ToList();

    violations.Should().BeEmpty(
        $"Python files with potential hardcoded API keys: {string.Join(", ", violations)}");
}
```

- [ ] **Step 5: Run the updated security tests**

```powershell
dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --filter "SecretScanTests" `
    --logger "console;verbosity=detailed" 2>&1
```

Expected: all tests PASS.

---

## Task 4: Final audit commit

- [ ] **Step 1: Final Gitleaks scan**

```powershell
gitleaks detect --no-git --source '/home/lance/Scripts' 2>&1
if ($LASTEXITCODE -ne 0) { throw "Gitleaks: secrets still present" }
Write-Host "Gitleaks final scan: CLEAN"
```

- [ ] **Step 2: Full build + test run**

```powershell
dotnet build /home/lance/Scripts/csharp/Scripts.slnx 2>&1 | Tee-Object -Variable b
$b | Where-Object { $_ -match ' error ' } | Should -BeNullOrEmpty

dotnet test /home/lance/Scripts/csharp/Scripts.slnx `
    --logger "console;verbosity=normal" 2>&1
```

- [ ] **Step 3: Commit**

```powershell
git -C /home/lance/Scripts add `
    csharp/tests/Scripts.Tests/SecurityTests/ `
    python/
git -C /home/lance/Scripts commit -m "feat(t4-04): security audit — gitleaks clean, no hardcoded secrets, Python deps updated"
```

---

## Acceptance Criteria

- [ ] `gitleaks detect --no-git --source /home/lance/Scripts` exits with code `0`
- [ ] All 6 `SecretScanTests` pass
- [ ] `.env` is listed in `.gitignore` and not tracked by git
- [ ] No `Host=localhost;Database=` or `Password=` appears in any `.cs` file
- [ ] `uv sync` completes with exit code `0`
- [ ] `uv run safety check` exits with code `0` (or all CVEs are documented with mitigations)
