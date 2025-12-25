# VS Code Configuration Explained

*Last Updated: December 25, 2025*

## Why .vscode Matters in This Repository

### The Problem
Without workspace-specific VS Code configuration, developers face:
- Manual launch of C# CLI for testing
- No integrated debugging experience
- Inconsistent formatting across team members
- Manual invocation of linters/analyzers
- Search results polluted by build artifacts

### Global Settings vs Workspace Settings

#### Global Settings (`~\AppData\Roaming\Code\User\settings.json`)
**Scope:** All VS Code windows/workspaces  
**Use Case:** Personal preferences (theme, font size, keybindings)  
**Limitation:** Cannot reference repo-specific paths

#### Workspace Settings (`.vscode/settings.json`)
**Scope:** Only this repository  
**Use Case:** Project-specific tool configurations  
**Advantage:** Can use `${workspaceFolder}` for relative paths

---

## File Formats Used

### TOML (`.toml`)
**Example:** `pyproject.toml`  
**Purpose:** Python project configuration  
**Features:**
- Minimal, human-readable
- Used by Black, basedpyright, pytest, Poetry
- Automatically discovered by Python tools

### JSON (`.json`)
**Example:** `.vscode/settings.json`, `package.json`  
**Purpose:** Configuration files, data exchange  
**Features:**
- Strict syntax (quoted keys, no trailing commas)
- Native JavaScript object format
- Used by VS Code, npm, many modern tools

### YAML (`.yml`/`.yaml`)
**Example:** GitHub Actions workflows  
**Purpose:** Config files, CI/CD definitions  
**Features:**
- Whitespace-sensitive (indentation matters!)
- Supports comments
- More human-friendly than JSON

### XML (`.xml`)
**Example:** `.csproj`, NuGet packages  
**Purpose:** .NET project files, legacy configs  
**Features:**
- Verbose tag-based syntax
- Schema validation
- MSBuild uses this

### XAML (`.xaml`)
**Example:** WPF/UWP application UIs  
**Purpose:** UI markup for Windows desktop apps  
**Not Used:** Not applicable to this CLI-focused repository

---

## Tool Architecture

### C# Tooling

#### Roslyn
- **What:** Microsoft's official C# compiler and code analysis platform
- **Where:** Built into .NET SDK
- **Purpose:** Compiles C#, provides IntelliSense, refactorings

#### OmniSharp
- **What:** Language server that wraps Roslyn for editors
- **Where:** Bundled with VS Code C# extension
- **Purpose:** Exposes Roslyn to VS Code, Vim, Emacs via Language Server Protocol (LSP)

#### CSharpier
- **What:** Opinionated C# code formatter
- **Where:** Installed as .NET tool: `dotnet tool install csharpier`
- **Configuration:** Respects `.editorconfig` (you have this!)
- **Invocation:** 
  - CLI: `dotnet csharpier .`
  - VS Code: Install CSharpier extension, format on save

**You Already Have:**
- `.editorconfig` in `csharp/` directory
- CSharpier respects EditorConfig rules automatically

---

### Python Tooling

#### basedpyright
- **What:** Fast, strict Python type checker (fork of Pyright)
- **Where:** Installed via pip: `pip install basedpyright`
- **Configuration:** `pyproject.toml` → `[tool.basedpyright]` section
- **VS Code Integration:**
  1. Install **Pylance** extension (Microsoft) OR **Pyright** extension
  2. VS Code automatically reads `pyproject.toml`
  3. No `.vscode/settings.json` config needed!

**You Already Have:**
- `pyproject.toml` with basedpyright config
- Type checking mode: strict
- Excluded: `last.fm Scrobble Updater` (legacy folder)
- Suppressed warnings: `reportMissingTypeStubs`, `reportUnknownMemberType`, etc.

#### Black
- **What:** Uncompromising Python code formatter
- **Where:** Installed via pip: `pip install black`
- **Configuration:** `pyproject.toml` → `[tool.black]` section (optional)
- **Invocation:**
  - CLI: `black .`
  - VS Code: Install Black extension, format on save

---

### PowerShell Tooling

#### PSScriptAnalyzer
- **What:** PowerShell linter (finds code smells, best practice violations)
- **Type:** PowerShell MODULE (not a standalone exe)
- **Installation:** `Install-Module -Name PSScriptAnalyzer`
- **Configuration:** `PSScriptAnalyzerSettings.psd1` (you have this in `powershell\ScriptsToolkit\`)
- **VS Code Integration:**
  1. Install **PowerShell** extension (Microsoft)
  2. Extension auto-discovers and runs PSScriptAnalyzer
  3. Reads your `.psd1` settings file
  4. Shows warnings inline in editor

**Your Settings File (`PSScriptAnalyzerSettings.psd1`):**
```powershell
@{
    IncludeRules = @(
        'PSAvoidLongLines',
        'PSAvoidUsingInvokeExpression',  # Your profile violates this! (carapace/argc)
        'PSAvoidUsingPlainTextForPassword',
        'PSAvoidUsingPositionalParameters',
        'PSUseBOMForUnicodeEncodedFile'
    )
    ExcludeRules = @()
}
```

**Difference from CSharpier/Black:**
- CSharpier/Black: **Formatters** (auto-fix code style)
- PSScriptAnalyzer: **Linter** (reports issues, some auto-fixable)

---

## VS Code Configuration Files

### `.vscode/settings.json`
**Purpose:** Workspace-specific editor settings

**What It Should Configure:**
```json
{
  "omnisharp.defaultLaunchSolution": "${workspaceFolder}/csharp/CSharpScripts.sln",
  "python.defaultInterpreterPath": "${workspaceFolder}/python/.venv/Scripts/python.exe",
  "powershell.scriptAnalysis.settingsPath": "${workspaceFolder}/powershell/ScriptsToolkit/PSScriptAnalyzerSettings.psd1",
  
  "search.exclude": {
    "**/bin": true,
    "**/obj": true,
    "**/__pycache__": true,
    "**/node_modules": true
  },
  
  "files.associations": {
    "*.nu": "nushell",
    "*.psm1": "powershell",
    "*.psd1": "powershell"
  },
  
  "editor.formatOnSave": true,
  "[csharp]": {
    "editor.defaultFormatter": "csharpier.csharpier-vscode"
  },
  "[python]": {
    "editor.defaultFormatter": "ms-python.black-formatter"
  },
  
  "terminal.integrated.defaultProfile.windows": "PowerShell",
  "terminal.integrated.profiles.windows": {
    "PowerShell": {
      "source": "PowerShell",
      "args": ["-NoLogo"]
    }
  }
}
```

**Why This Matters:**
1. **OmniSharp solution path** - Tells C# extension which .sln to use
2. **Python interpreter** - Ensures VS Code uses correct Python version
3. **Search exclude** - Faster searches (Ctrl+Shift+F), no build artifact noise
4. **File associations** - Proper syntax highlighting for `.nu` (Nushell), `.psm1`, `.psd1`
5. **Format on save** - Automatic formatting with CSharpier/Black
6. **Terminal default** - Opens PowerShell 7, not legacy PowerShell 5.1

---

### `.vscode/launch.json`
**Purpose:** Debug configurations (F5 to debug)

**Example Configurations:**
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Debug: C# Music Fill",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/csharp/bin/Debug/net10.0/CSharpScripts.dll",
      "args": ["music", "fill", "-i", "${workspaceFolder}/exports/missing_fields.tsv"],
      "cwd": "${workspaceFolder}/csharp",
      "stopAtEntry": false,
      "console": "integratedTerminal"
    },
    {
      "name": "Debug: C# Music Search",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/csharp/bin/Debug/net10.0/CSharpScripts.dll",
      "args": ["music", "search", "-t", "Symphony No. 5", "-a", "Beethoven"],
      "cwd": "${workspaceFolder}/csharp",
      "console": "integratedTerminal"
    },
    {
      "name": "Debug: Python Toolkit",
      "type": "python",
      "request": "launch",
      "program": "${workspaceFolder}/python/toolkit/cli.py",
      "args": ["audio", "convert", "-d", "test_directory", "-m", "flac"],
      "console": "integratedTerminal"
    },
    {
      "name": "Attach to PowerShell",
      "type": "PowerShell",
      "request": "attach",
      "processId": "${command:PickPSHostProcess}"
    }
  ]
}
```

**What This Enables:**
1. **Press F5** → Automatically builds C# project → Launches with pre-filled arguments → Attach debugger
2. **Set breakpoints** in C# code → Step through line-by-line → Inspect variables
3. **Debug Python scripts** with arguments
4. **Attach to running PowerShell process** for module debugging

**Without launch.json:**
- Manual `dotnet build` → Manual `dotnet run music fill -i ...` → No debugging

---

### `.vscode/tasks.json`
**Purpose:** Runnable tasks (Ctrl+Shift+B or Command Palette)

**Example Tasks:**
```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "Build C# Project",
      "command": "dotnet",
      "type": "shell",
      "args": ["build", "${workspaceFolder}/csharp/CSharpScripts.sln"],
      "group": {
        "kind": "build",
        "isDefault": true
      },
      "problemMatcher": "$msCompile"
    },
    {
      "label": "Format C# with CSharpier",
      "command": "dotnet",
      "type": "shell",
      "args": ["csharpier", "${workspaceFolder}/csharp"],
      "problemMatcher": []
    },
    {
      "label": "Format Python with Black",
      "command": "black",
      "type": "shell",
      "args": ["${workspaceFolder}/python"],
      "problemMatcher": []
    },
    {
      "label": "Analyze PowerShell Module",
      "command": "pwsh",
      "type": "shell",
      "args": [
        "-Command",
        "Invoke-ScriptAnalyzer -Path '${workspaceFolder}/powershell/ScriptsToolkit' -Settings '${workspaceFolder}/powershell/ScriptsToolkit/PSScriptAnalyzerSettings.psd1'"
      ],
      "problemMatcher": []
    },
    {
      "label": "Run basedpyright",
      "command": "basedpyright",
      "type": "shell",
      "args": ["${workspaceFolder}/python"],
      "problemMatcher": []
    }
  ]
}
```

**What This Enables:**
1. **Ctrl+Shift+B** → Builds C# project instantly
2. **Command Palette → "Run Task" → "Format C# with CSharpier"** → Formats entire codebase
3. **Run analyzers** without leaving IDE
4. **Integrated with build** → Errors show in Problems panel

**Without tasks.json:**
- Switch to terminal → Type commands manually → Switch back to editor

---

## Why Global Config Is Insufficient

### Scenario 1: Multiple Repositories
You have:
- `C:\Users\Lance\Dev\Scripts` (this repo)
- `C:\Users\Lance\Dev\OtherProject` (different C# project)

**Global setting:**
```json
{
  "omnisharp.defaultLaunchSolution": "C:\\Users\\Lance\\Dev\\Scripts\\csharp\\CSharpScripts.sln"
}
```

**Problem:** OmniSharp tries to load `Scripts\csharp\CSharpScripts.sln` even when editing `OtherProject` → Error!

**Workspace setting (in Scripts/.vscode/settings.json):**
```json
{
  "omnisharp.defaultLaunchSolution": "${workspaceFolder}/csharp/CSharpScripts.sln"
}
```

**Benefit:** Each repo has own .sln path

---

### Scenario 2: Tool Versions
**Global:** Python 3.12 by default  
**This Repo:** Requires Python 3.12  
**Other Repo:** Requires Python 3.10

**Solution:** Workspace settings specify Python interpreter per-repo

---

### Scenario 3: Analyzer Rules
**This Repo:** Allow `Invoke-Expression` (for argc/carapace)  
**Production Repo:** Strict - no `Invoke-Expression`

**Solution:** Workspace settings point to different `PSScriptAnalyzerSettings.psd1` files

---

## Summary

### What You Already Have ✓
- ✅ `.editorconfig` (C# formatting rules)
- ✅ `pyproject.toml` (Python basedpyright config)
- ✅ `PSScriptAnalyzerSettings.psd1` (PowerShell analyzer rules)

### What's Missing (Should Create)
- ❌ `.vscode/settings.json` (workspace-specific IDE config)
- ❌ `.vscode/launch.json` (debug configurations)
- ❌ `.vscode/tasks.json` (build/format/analyze tasks)

### Benefits of Adding .vscode
1. **One-click debugging** (F5) with pre-filled arguments
2. **Faster searches** (excluded build artifacts)
3. **Format on save** (automatic CSharpier/Black)
4. **Integrated tasks** (Ctrl+Shift+B to build)
5. **Consistent environment** across all devs (if team project)

### Tool Installation Checklist
```powershell
# C# Formatting
dotnet tool install -g csharpier

# Python Type Checking & Formatting
pip install basedpyright black

# PowerShell Analysis
Install-Module -Name PSScriptAnalyzer -Scope CurrentUser

# VS Code Extensions
# - C# (ms-dotnettools.csharp)
# - CSharpier (csharpier.csharpier-vscode)
# - Python (ms-python.python)
# - Pylance (ms-python.vscode-pylance)
# - Black Formatter (ms-python.black-formatter)
# - PowerShell (ms-vscode.powershell)
```
