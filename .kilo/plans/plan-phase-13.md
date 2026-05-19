# Phase 13: Security, Secrets & Python Upgrades Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade Python dependencies to latest secure versions, redact secrets defaults, and run Gitleaks audit to clean any accidentally-committed credentials.

**Architecture:** Use `uv` for Python dependency management. PowerShell scripts for redaction and Gitleaks execution.

**Tech Stack:** Python 3.12, `uv`, PowerShell, Gitleaks

---

### Task 13.1: Upgrade Python dependencies

**Files:**
- Modify: `python/pyproject.toml`

- [ ] **Step 1: Upgrade pyasn1, requests, urllib3**

```powershell
Set-Location "C:\Users\Lance\Dev\Scripts\python"
uv lock --upgrade-package pyasn1 --upgrade-package requests --upgrade-package urllib3
```

- [ ] **Step 2: Upgrade cryptography, Pygments, pillow**

```powershell
uv lock --upgrade-package cryptography --upgrade-package pygments --upgrade-package pillow
```

- [ ] **Step 3: Upgrade pytest**

```powershell
uv lock --upgrade-package pytest
```

- [ ] **Step 4: Verify lock file**

Run: `Test-Path "C:\Users\Lance\Dev\Scripts\python\uv.lock"`
Expected: `True`

- [ ] **Step 5: Commit**

```bash
git add python/uv.lock
git commit -m "security: upgrade Python dependencies to latest versions"
```

---

### Task 13.2: Redact secrets defaults

**Files:**
- Run: `powershell/ScriptsToolkit/Redact-LocalSecrets.ps1`

- [ ] **Step 1: Execute redaction script**

```powershell
pwsh -File "C:\Users\Lance\Dev\Scripts\powershell\ScriptsToolkit\Redact-LocalSecrets.ps1"
```

- [ ] **Step 2: Verify no plaintext secrets remain**

Search for common secret patterns in source:
```powershell
Get-ChildItem -Recurse -Include *.cs,*.ps1,*.py,*.json,*.yml -Exclude *.lock,uv.lock | Select-String -Pattern 'password\s*=\s*"[^$]|api[_-]?key\s*=\s*"[^$]|secret\s*=\s*"[^$]' -CaseSensitive:$false
```
Expected: No matches (or only placeholder/documentation values).

- [ ] **Step 3: Commit**

```bash
git commit --allow-empty -m "security: verify no plaintext secrets in source"
```

---

### Task 13.3: Gitleaks audit

**Files:**
- Run: `gitleaks` (if installed)

- [ ] **Step 1: Run Gitleaks detect**

```powershell
gitleaks detect --source "C:\Users\Lance\Dev\Scripts" --verbose 2>&1
```

If Gitleaks is not installed:
```powershell
winget install --id Gitleaks.Gitleaks
gitleaks detect --source "C:\Users\Lance\Dev\Scripts" --verbose 2>&1
```

- [ ] **Step 2: Fix any findings**

Review each finding and determine if it's a real secret (revoke/rotate) or a false positive (add to `.gitleaks.toml` allowlist).

- [ ] **Step 3: Commit**

```bash
git commit --allow-empty -m "security: Gitleaks audit complete"
```
