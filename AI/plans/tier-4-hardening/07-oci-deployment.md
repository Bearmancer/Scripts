# Tier 4 OCI Deployment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate the PostgreSQL 18 database to run in a Docker container on the remote OCI VM (`oci`), update local connections to target OCI, and configure a permanent WinSCP session profile for remote file/backup access.

**Architecture:** Connect to OCI via SSH/Tailscale. Deploy PostgreSQL 18 container on OCI. Configure the local connection string in `.env` to point to the remote host. Configure a permanent WinSCP session in the Windows Registry using the OpenSSH private key for remote management. All verified using TUnit reflection/execution tests.

**Tech Stack:** C# 14 / .NET 10 / EF Core 10 / Npgsql 10 / Docker Compose / WinSCP / PowerShell

---

## Pre-flight

- [ ] **Step 0: Pre-flight validation**

```powershell
Get-Command pwsh   -ErrorAction Stop
Get-Command dotnet -ErrorAction Stop
Get-Command git    -ErrorAction Stop
Get-Command ssh    -ErrorAction Stop

Write-Host ".NET: $(dotnet --version)"
Write-Host "git:  $(git --version)"
Write-Host "ssh:  $(ssh -V 2>&1)"

# Verify local build works
dotnet build 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' -ErrorAction Stop
```

---

## Task 1: OCI SSH Connection Gate

**Files:**
- Create: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Environment\OciDeploymentTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using TUnit;
using FluentAssertions;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using CSharpScripts.Data;

namespace Scripts.Tests.Environment;

internal sealed class OciDeploymentTests
{
    private static (int ExitCode, string StdOut, string StdErr) RunCommand(
        string fileName, string arguments)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = @"C:\Users\Lance\Dev\Scripts",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var proc = Process.Start(psi)!;
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        return (proc.ExitCode, stdout, stderr);
    }

    [Test]
    public void OciSshConnection_Succeeds_WithVerifiedFile()
    {
        var (exitCode, stdout, stderr) = RunCommand("ssh", "oci \"test -f /home/ubuntu/.oci_verified\"");
        exitCode.Should().Be(0, $"SSH verified file check failed: {stderr}\n{stdout}");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:
```powershell
dotnet test 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' --filter "OciDeploymentTests.OciSshConnection_Succeeds_WithVerifiedFile" 2>&1
```
Expected: FAIL with exit code `1` (file `/home/ubuntu/.oci_verified` does not exist).

- [ ] **Step 3: Write minimal implementation**

Execute a remote SSH command to create the verified file on OCI:
```powershell
ssh oci "touch /home/ubuntu/.oci_verified"
```

- [ ] **Step 4: Run test to verify it passes**

Run:
```powershell
dotnet test 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' --filter "OciDeploymentTests.OciSshConnection_Succeeds_WithVerifiedFile" 2>&1
```
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git -C 'C:\Users\Lance\Dev\Scripts' add csharp/tests/Scripts.Tests/Environment/OciDeploymentTests.cs
git -C 'C:\Users\Lance\Dev\Scripts' commit -m "feat(t4-07): add OCI SSH gate test and verified marker"
```

---

## Task 2: Deploy PostgreSQL container on OCI VM

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Environment\OciDeploymentTests.cs` (add container test)
- Create: `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\deploy_oci_postgres.ps1`

- [ ] **Step 1: Write the failing test**

Add to `OciDeploymentTests.cs`:
```csharp
    [Test]
    public void OciPostgresContainer_IsRunning()
    {
        var (exitCode, stdout, stderr) = RunCommand("ssh", "oci \"docker ps --filter name=postgres --format '{{.Status}}'\"");
        exitCode.Should().Be(0, $"Docker check failed: {stderr}");
        stdout.Trim().Should().StartWith("Up", "because PostgreSQL container must be running on OCI");
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run:
```powershell
dotnet test 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' --filter "OciDeploymentTests.OciPostgresContainer_IsRunning" 2>&1
```
Expected: FAIL (exit code `0` but stdout is empty).

- [ ] **Step 3: Write minimal implementation**

Create the deployment script `deploy_oci_postgres.ps1`:
```powershell
# C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\deploy_oci_postgres.ps1
$ErrorActionPreference = 'Stop'

# Create remote directory
ssh oci "mkdir -p /home/ubuntu/postgres"

# Write docker-compose.yml remotely
$composeContent = @"
services:
  postgres:
    image: postgres:18
    container_name: postgres
    environment:
      POSTGRES_DB: pg_db
      POSTGRES_USER: lance
      POSTGRES_PASSWORD: lance
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    restart: unless-stopped
volumes:
  postgres_data:
    driver: local
"@

$tempFile = [System.IO.Path]::GetTempFileName()
$composeContent | Out-File -FilePath $tempFile -Encoding utf8
scp $tempFile oci:/home/ubuntu/postgres/docker-compose.yml
Remove-Item $tempFile

# Run container stack
ssh oci "cd /home/ubuntu/postgres && docker compose up -d"
Write-Host "OCI PostgreSQL stack deployed successfully."
```

Execute the script:
```powershell
& 'C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\deploy_oci_postgres.ps1'
```

- [ ] **Step 4: Run test to verify it passes**

Run:
```powershell
dotnet test 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' --filter "OciDeploymentTests.OciPostgresContainer_IsRunning" 2>&1
```
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git -C 'C:\Users\Lance\Dev\Scripts' add csharp/tests/Scripts.Tests/Environment/OciDeploymentTests.cs
git -C 'C:\Users\Lance\Dev\Scripts' add powershell/ScriptsToolkit/deploy_oci_postgres.ps1
git -C 'C:\Users\Lance\Dev\Scripts' commit -m "feat(t4-07): add OCI postgres deployment script and test"
```

---

## Task 3: Local Environment Connection to OCI Database

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Environment\OciDeploymentTests.cs` (add connection test)
- Modify: `C:\Users\Lance\Dev\Scripts\.env`

- [ ] **Step 1: Write the failing test**

Add to `OciDeploymentTests.cs`:
```csharp
    [Test]
    public async Task OciDatabase_CanConnect_ViaConnectionString()
    {
        var connStr = System.Environment.GetEnvironmentVariable("PGCONNSTR");
        connStr.Should().NotBeNullOrWhiteSpace("PGCONNSTR must be loaded");
        connStr.Should().Contain("Host=oci", "because the application must connect to the remote OCI instance");

        var options = new DbContextOptionsBuilder<ScriptsDbContext>()
            .UseNpgsql(connStr)
            .Options;

        await using var context = new ScriptsDbContext(options);
        var canConnect = await context.Database.CanConnectAsync();
        canConnect.Should().BeTrue("because connection to OCI database must succeed");
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run:
```powershell
dotnet test 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' --filter "OciDeploymentTests.OciDatabase_CanConnect_ViaConnectionString" 2>&1
```
Expected: FAIL (host is `localhost` and doesn't match `Host=oci`).

- [ ] **Step 3: Write minimal implementation**

Modify `C:\Users\Lance\Dev\Scripts\.env`:
```env
# Docker Compose - PostgreSQL 18
POSTGRES_DB=pg_db
POSTGRES_USER=lance
POSTGRES_PASSWORD=lance

# EF Core Connection String (for dotnet ef commands and application)
PGCONNSTR=Host=oci;Database=pg_db;Username=lance;Password=lance

# Docker MCP Database Profile
DOCKER_MCP_DB_URL=postgresql://lance:lance@oci:5432/pg_db
```

Reload environment variables in terminal or run profile load script.

- [ ] **Step 4: Run test to verify it passes**

Run:
```powershell
dotnet test 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' --filter "OciDeploymentTests.OciDatabase_CanConnect_ViaConnectionString" 2>&1
```
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git -C 'C:\Users\Lance\Dev\Scripts' add csharp/tests/Scripts.Tests/Environment/OciDeploymentTests.cs
git -C 'C:\Users\Lance\Dev\Scripts' add .env
git -C 'C:\Users\Lance\Dev\Scripts' commit -m "feat(t4-07): redirect PGCONNSTR to OCI and verify connection"
```

---

## Task 4: Configure Permanent WinSCP Profile

**Files:**
- Modify: `C:\Users\Lance\Dev\Scripts\csharp\tests\Scripts.Tests\Environment\OciDeploymentTests.cs` (add WinSCP registry test)
- Create: `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\configure_winscp.ps1`

- [ ] **Step 1: Write the failing test**

Add to `OciDeploymentTests.cs`:
```csharp
    [Test]
    public void WinScpSession_OciProfile_Exists()
    {
        var regPath = @"HKEY_CURRENT_USER\Software\Martin Prikryl\WinSCP 2\Sessions\oci";
        
        var host = Microsoft.Win32.Registry.GetValue(regPath, "HostName", null);
        var user = Microsoft.Win32.Registry.GetValue(regPath, "UserName", null);
        var protocol = Microsoft.Win32.Registry.GetValue(regPath, "FSProtocol", null);
        var keyFile = Microsoft.Win32.Registry.GetValue(regPath, "PrivateKeyFile", null);

        host.Should().Be("oci", "WinSCP session HostName must be oci");
        user.Should().Be("ubuntu", "WinSCP session UserName must be ubuntu");
        protocol.Should().Be(5, "WinSCP protocol must be SFTP (5)");
        keyFile.Should().Be(@"C:\Users\Lance\.ssh\oci", "WinSCP session PrivateKeyFile path must be correct");
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run:
```powershell
dotnet test 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' --filter "OciDeploymentTests.WinScpSession_OciProfile_Exists" 2>&1
```
Expected: FAIL (registry path/properties not found).

- [ ] **Step 3: Write minimal implementation**

Create configuration script `C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\configure_winscp.ps1`:
```powershell
# C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\configure_winscp.ps1
$ErrorActionPreference = 'Stop'

$regPath = "HKCU:\Software\Martin Prikryl\WinSCP 2\Sessions\oci"
if (-not (Test-Path $regPath)) {
    New-Item -Path $regPath -Force | Out-Null
}

Set-ItemProperty -Path $regPath -Name "HostName" -Value "oci" -Force
Set-ItemProperty -Path $regPath -Name "UserName" -Value "ubuntu" -Force
Set-ItemProperty -Path $regPath -Name "FSProtocol" -Value 5 -Force
Set-ItemProperty -Path $regPath -Name "PrivateKeyFile" -Value "C:\Users\Lance\.ssh\oci" -Force

Write-Host "WinSCP permanent profile 'oci' configured successfully."
```

Execute the configuration script:
```powershell
& 'C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\configure_winscp.ps1'
```

- [ ] **Step 4: Run test to verify it passes**

Run:
```powershell
dotnet test 'C:\Users\Lance\Dev\Scripts\csharp\Scripts.slnx' --filter "OciDeploymentTests.WinScpSession_OciProfile_Exists" 2>&1
```
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git -C 'C:\Users\Lance\Dev\Scripts' add csharp/tests/Scripts.Tests/Environment/OciDeploymentTests.cs
git -C 'C:\Users\Lance\Dev\Scripts' add powershell/ScriptsToolkit/configure_winscp.ps1
git -C 'C:\Users\Lance\Dev\Scripts' commit -m "feat(t4-07): configure WinSCP session profile for OCI"
```
