

param(
    [switch]$SkipWinget,
    [switch]$SkipVSCode,
    [switch]$SkipWSL
)

$ErrorActionPreference = "Stop"
$Host.UI.RawUI.WindowTitle = "Installing Dev Environment..."

function Write-Step($msg)  { Write-Host "`n━━ $msg ━━━" -ForegroundColor Cyan }
function Write-Ok($msg)   { Write-Host "  ✓ $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "  ⚠ $msg" -ForegroundColor Yellow }
function Write-Do($msg)   { Write-Host "  → $msg" -ForegroundColor Blue }


Write-Step "Checking prerequisites"


if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    Write-Warn "winget not found. Install App Installer from Microsoft Store first."
    Write-Warn "https://www.microsoft.com/p/app-installer/9nblggh4nns1"
    $SkipWinget = $true
}


$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator
)
if (-not $isAdmin) {
    Write-Warn "Not running as admin. Some winget installs may fail."
    Write-Warn "Re-run as Administrator for best results."
}


if (-not $SkipWinget) {
    Write-Step "Step 1: Installing Windows packages via winget"

    $packages = @(
        @{ Id = "Microsoft.PowerShell";       Name = "PowerShell 7" }
        @{ Id = "GoLang.Go";                  Name = "Go" }
        @{ Id = "Git.Git";                    Name = "Git" }
        @{ Id = "Docker.DockerDesktop";       Name = "Docker Desktop" }
        @{ Id = "Microsoft.VisualStudioCode"; Name = "VS Code" }
        @{ Id = "9P9W93JBQ3G7";               Name = "Windows Terminal" }
        @{ Id = "Charmbracelet.Glow";         Name = "glow (markdown)" }
        @{ Id = "charmbracelet.glow";         Name = "glow (alt)" }
    )

    foreach ($pkg in $packages) {
        $installed = winget list --id $pkg.Id --accept-source-agreements 2>$null | Select-String $pkg.Id
        if (-not $installed) {
            Write-Do "Installing $($pkg.Name)..."
            winget install --id $pkg.Id --silent --accept-package-agreements --accept-source-agreements 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Ok "$($pkg.Name) installed"
            } else {
                Write-Warn "$($pkg.Name) install may have failed (code: $LASTEXITCODE)"
            }
        } else {
            Write-Ok "$($pkg.Name) already installed"
        }
    }
} else {
    Write-Step "Step 1: Skipping winget packages"
}


if (-not $SkipVSCode) {
    Write-Step "Step 2: VS Code extensions"

    $extensions = @(
        
        @{ Id = "openai.codex";                  Name = "OpenAI Codex" }
        @{ Id = "saoudrizwan.claude-dev";        Name = "Claude Dev" }
        @{ Id = "github.copilot";                Name = "GitHub Copilot" }

        
        @{ Id = "ms-dotnettools.csharp";         Name = "C# (OmniSharp)" }
        @{ Id = "ms-python.python";              Name = "Python" }
        @{ Id = "ms-python.vscode-pylance";      Name = "Pylance" }
        @{ Id = "golang.go";                     Name = "Go" }
        @{ Id = "ms-vscode.powershell";          Name = "PowerShell (PSES)" }

        
        @{ Id = "eamodio.gitlens";               Name = "GitLens" }
        @{ Id = "ms-azuretools.vscode-docker";   Name = "Docker" }
        @{ Id = "mhutchie.git-graph";            Name = "Git Graph" }
        @{ Id = "vscode-icons-team.vscode-icons";Name = "VSCode Icons" }
    )

    
    $codePaths = @(
        "${env:ProgramFiles}\Microsoft VS Code\bin\code.cmd",
        "${env:LocalAppData}\Programs\Microsoft VS Code\bin\code.cmd",
        "${env:USERPROFILE}\AppData\Local\Programs\Microsoft VS Code\bin\code.cmd"
    )

    $codePath = $null
    foreach ($p in $codePaths) { if (Test-Path $p) { $codePath = $p; break } }

    if (-not $codePath) {
        Write-Warn "VS Code not found. Skipping extensions."
    } else {
        foreach ($ext in $extensions) {
            $installed = & $codePath --list-extensions 2>$null | Select-String $ext.Id
            if (-not $installed) {
                Write-Do "Installing $($ext.Name)..."
                & $codePath --install-extension $ext.Id --force 2>$null
                if ($LASTEXITCODE -eq 0) {
                    Write-Ok "$($ext.Name) installed"
                } else {
                    Write-Warn "$($ext.Name) install failed"
                }
            } else {
                Write-Ok "$($ext.Name) already installed"
            }
        }
    }
} else {
    Write-Step "Step 2: Skipping VS Code extensions"
}


if (-not $SkipWSL) {
    Write-Step "Step 3: WSL2 config copy"

    $wslHome = "\\wsl.localhost\Ubuntu\home\lance"
    $wslDesktop = "\\wsl.localhost\Ubuntu\mnt\c\Users\Lance\Desktop"

    
    $wslRunning = wsl -l -q 2>$null | Select-String "Ubuntu"
    if (-not $wslRunning) {
        Write-Warn "WSL2 Ubuntu not detected. Skipping WSL config copy."
        Write-Warn "Run 'wsl --install -d Ubuntu' first."
    } else {
        $targetDir = "$env:USERPROFILE\Desktop\Fibery-Migration-Schema"
        if (-not (Test-Path $targetDir)) {
            New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
        }

        
        $srcFiles = @(
            "AI/schema_mapping.md",
            "AI/schema_visual.mmd",
            "AI/system_inventory.md",
            "install_env.sh",
            "powershell/Install-Env.ps1"
        )

        foreach ($file in $srcFiles) {
            $wslPath = "$wslHome/Scripts/$file"
            $destName = Split-Path $file -Leaf
            $destPath = "$targetDir\$destName"
            if (Test-Path $wslPath) {
                Copy-Item $wslPath $destPath -Force
                Write-Ok "Copied $destName"
            } else {
                Write-Warn "Not found in WSL: $file"
            }
        }
    }
} else {
    Write-Step "Step 3: Skipping WSL integration"
}


Write-Step "Installation complete!"

Write-Host @"

Summary:
  Winget:         $(if (-not $SkipWinget) { 'done' } else { 'skipped' })
  VS Code Ext:    $(if (-not $SkipVSCode) { 'done' } else { 'skipped' })
  WSL Copy:       $(if (-not $SkipWSL) { 'done' } else { 'skipped' })

Next steps:
  1. Open WSL2 and run: cd ~/Scripts && bash install_env.sh
  2. Restart terminal for PATH changes
  3. In VS Code, sign in to AI extensions (Codex, Copilot)
  4. For Docker: start Docker Desktop, enable WSL2 backend
"@
