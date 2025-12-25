# Python Tasks - Comprehensive List

*Generated: December 25, 2025*  
*Source Files: To-Do.md (#1, #31, #46-50), migration_signatures.md*

---

## Overview

**Total Tasks:** 8  
**Complexity Distribution:**
- ⭐ Low: 3 tasks
- ⭐⭐ Medium: 3 tasks
- ⭐⭐⭐ High: 2 tasks

**Current Python Structure:**
```
python/
├── pyproject.toml (basedpyright config)
├── requirements.txt
└── toolkit/
    ├── __init__.py
    ├── audio.py (17 functions)
    ├── cli.py (Typer-based, 12 commands)
    ├── cuesheet.py (5 functions)
    ├── filesystem.py (6 functions)
    ├── lastfm.py (4 functions)
    ├── logging_config.py
    └── video.py (17 functions)
```

**Migration Strategy:**
- ✅ **KEEP:** Last.fm scrobble as standalone Python (legacy, working)
- ✅ **KEEP:** Typer-based CLI structure
- ❌ **DO NOT:** Migrate to C# (unless specific need arises)
- ❌ **DO NOT:** Duplicate functionality in PowerShell

---

## Category 1: Type Checking & Configuration

### PY-001: Verify basedpyright Configuration
**Complexity:** ⭐ Low  
**Source:** To-Do.md #1, #31, #47  
**Priority:** HIGH (validate current state)

**Current Config (`pyproject.toml`):**
```toml
[tool.basedpyright]
typeCheckingMode = "strict"
exclude = ["last.fm Scrobble Updater"]
reportMissingTypeStubs = false
reportUnknownMemberType = false
reportUnknownArgumentType = false
reportUnknownVariableType = false
reportUnknownParameterType = false
reportUnknownLambdaType = false
reportAny = false
```

**Verification Steps:**
```bash
cd python
basedpyright .
```

**Expected Output:**
- Minimal errors from toolkit code
- All third-party library warnings suppressed
- No errors from excluded folders

**Files:**
- `python/pyproject.toml` (already configured)
- `python/toolkit/*.py` (verify no errors)

**Implementation Steps:**
1. Run basedpyright on entire python/ directory
2. Review output for legitimate errors
3. Fix any actual code issues (not library issues)
4. If new library warnings appear, add to suppressions
5. Document expected clean output

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 15 minutes

---

### PY-002: Document pyproject.toml Purpose
**Complexity:** ⭐ Low (Documentation)  
**Source:** To-Do.md #47  
**Priority:** LOW (educational)

**Content:** Already documented in `markdown/explanations/vscode_configuration_explained.md`

**Additional Documentation:**
Create section explaining why pyproject.toml exists specifically in this repo:

**Purpose:**
1. **basedpyright Type Checking:** Suppress warnings from untyped third-party libraries
2. **Project Metadata:** Define Python version, dependencies
3. **Tool Configuration:** Centralized config for Black, pytest (if added later)

**Why Not Use Multiple Config Files:**
- Old way: `setup.py`, `setup.cfg`, `.flake8`, `pytest.ini`, `mypy.ini` (fragmented)
- New way: `pyproject.toml` (PEP 518/621 standard, single source)

**Files:**
- `markdown/explanations/python_configuration.md` (new file)

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 20 minutes

---

### PY-003: Run basedpyright and Fix Errors
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #48, master_task_list.md  
**Priority:** MEDIUM (code quality)

**Process:**
```bash
cd python
basedpyright . > basedpyright_errors.txt
cat basedpyright_errors.txt
```

**Error Categories:**
1. **Third-party library issues:** Add to suppressions in pyproject.toml
2. **Actual code issues:** Fix in toolkit code
3. **Missing type hints:** Add type annotations

**Example Fixes:**
```python
# Before:
def process_tracks(tracks, cue_file, volume_adjustment):
    pass

# After:
from typing import List, Optional

def process_tracks(
    tracks: List[dict[str, Any]], 
    cue_file: str, 
    volume_adjustment: Optional[float] = None
) -> None:
    pass
```

**Files:**
- All `python/toolkit/*.py` files with errors

**Implementation Steps:**
1. Run basedpyright and capture output
2. Categorize errors
3. Fix actual code issues (add type hints, fix bugs)
4. Add third-party suppressions to pyproject.toml
5. Re-run until clean (or acceptable error level)

**Dependencies:** PY-001 (baseline verification)  
**Blocks:** None  
**Estimated Time:** 2-3 hours

---

## Category 2: CLI Enhancement

### PY-004: Generate Python Signature List
**Complexity:** ⭐ Low  
**Source:** To-Do.md #50  
**Priority:** LOW (documentation)

**Task:** Use pydoc to list all Python function signatures

**Implementation:**
```bash
cd python/toolkit
python -m pydoc audio > ../../markdown/reference/python_audio_signatures.txt
python -m pydoc video > ../../markdown/reference/python_video_signatures.txt
python -m pydoc filesystem > ../../markdown/reference/python_filesystem_signatures.txt
python -m pydoc cuesheet > ../../markdown/reference/python_cuesheet_signatures.txt
python -m pydoc lastfm > ../../markdown/reference/python_lastfm_signatures.txt
```

**Alternative (better formatting):**
```python
import inspect
from toolkit import audio, video, filesystem, cuesheet, lastfm

for module in [audio, video, filesystem, cuesheet, lastfm]:
    print(f"### Module: {module.__name__}")
    for name, obj in inspect.getmembers(module, inspect.isfunction):
        sig = inspect.signature(obj)
        print(f"- {name}{sig}")
```

**Output Example:**
```
### Module: audio
- prepare_directory(directory: str) -> None
- create_output_directory(directory: str, suffix: str) -> str
- rename_file_red(path: str) -> None
- calculate_image_size(path: str) -> tuple[int, int]
...
```

**Files:**
- `markdown/reference/python_signatures.md` (new file)
- `python/toolkit/*.py` (read signatures)

**Dependencies:** None  
**Blocks:** PY-005  
**Estimated Time:** 30 minutes

---

### PY-005: Integrate Last.fm into Typer CLI
**Complexity:** ⭐⭐⭐ High  
**Source:** To-Do.md #46  
**Priority:** MEDIUM (cleanup)

**Current State:**
```
python/
├── toolkit/
│   ├── cli.py (Typer, has audio/video/filesystem commands)
│   └── lastfm.py (module with functions)
└── [legacy]/
    └── last.fm scrobble updater/ (old standalone script)
```

**Target State:**
```
python/toolkit/
├── cli.py (Typer, NOW includes lastfm command)
├── lastfm.py (refactored as module)
└── ... (other modules)

# DELETED:
# python/last.fm scrobble updater/
```

**Implementation:**
```python
# In cli.py
import typer
from toolkit import lastfm

app = typer.Typer()

# ... existing commands (audio, video, etc.)

@app.command(name="lastfm-sync")
def lastfm_update(
    sheet_id: str = typer.Option(..., "--sheet", "-s", help="Google Sheets ID"),
    credentials: str = typer.Option("credentials.json", "--creds", "-c")
):
    """Sync Last.fm scrobbles to Google Sheets"""
    typer.echo("Authenticating with Google Sheets...")
    lastfm.update_scrobbles(sheet_id, credentials)
    typer.secho("✓ Sync complete", fg=typer.colors.GREEN)
```

**Files:**
- `python/toolkit/cli.py` (add lastfm command)
- `python/toolkit/lastfm.py` (refactor to be callable from CLI)
- `python/last.fm scrobble updater/` (DELETE after verification)

**Implementation Steps:**
1. Review `last.fm scrobble updater` folder
2. Extract core logic into `toolkit/lastfm.py`
3. Add Typer command in `cli.py`
4. Test: `python toolkit/cli.py lastfm-sync --sheet <id>`
5. Verify identical functionality to old script
6. Update PowerShell/C# to NOT reference old script
7. Delete legacy folder

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 2-3 hours

---

### PY-006: Document Typer CLI Usage
**Complexity:** ⭐ Low (Documentation)  
**Source:** Derived from PY-005  
**Priority:** LOW (user docs)

**Create:** `markdown/reference/python_cli_usage.md`

**Content:**
```markdown
# Python Toolkit CLI Usage

## Installation
```bash
cd python
pip install -r requirements.txt
```

## Commands

### Audio Conversion
```bash
python toolkit/cli.py audio convert -d /path/to/dir -m flac
python toolkit/cli.py audio rename -d /path/to/dir
```

### Video Processing
```bash
python toolkit/cli.py video remux /path/to/disc
python toolkit/cli.py video compress -d /path/to/dir
```

### Last.fm Sync
```bash
python toolkit/cli.py lastfm-sync --sheet <SHEET_ID>
```

## Help
```bash
python toolkit/cli.py --help
python toolkit/cli.py audio --help
```
```

**Files:**
- `markdown/reference/python_cli_usage.md` (new)

**Dependencies:** PY-005  
**Blocks:** None  
**Estimated Time:** 30 minutes

---

## Category 3: Migration Analysis (For Future C# Migration)

### PY-007: Create Function Migration Plan (Python → C#)
**Complexity:** ⭐⭐⭐ High  
**Source:** To-Do.md #51, #59, master_task_list.md  
**Priority:** LOW (future work, not immediate)

**Scope:**  
Analyze each Python function for potential C# migration using .NET design philosophy

**Approach:**
1. **DO NOT** create 1:1 Python-to-C# ports
2. **DO** identify functionality gaps in C# toolkit
3. **DO** design .NET-idiomatic implementations
4. **PREFER** functional style where appropriate (avoid unnecessary classes)

**Analysis Template:**
```markdown
### Function: process_sacd_directory(directory, fmt)

**Python Implementation:**
- Uses subprocess to call sacd_extract, dff2dsf, etc.
- Imperative style with global state

**C# Design:**
- Use CliWrap for process management
- Functional approach: pure functions, no side effects
- Or: FluentAPI pattern for builder-style calls

**Libraries Needed:**
- CliWrap (subprocess replacement)
- FFMpegCore (audio/video processing)
- ImageSharp (image manipulation)

**Priority:** LOW (SACD processing is rare)
```

**Modules to Analyze:**
1. **audio.py** (17 functions)
   - SACD extraction: Niche, low priority
   - FLAC conversion: Already covered by C#/PowerShell?
   - Gain calculation: Could use FFMpegCore
   
2. **video.py** (17 functions)
   - Remuxing: Check if PowerShell/C# already have this
   - Compression: FFMpegCore in C#
   - GIF creation: FFMpegCore + ImageSharp
   
3. **cuesheet.py** (5 functions)
   - CUE parsing: Specialized, consider keeping in Python
   
4. **filesystem.py** (6 functions)
   - Already duplicated in PowerShell (tree, torrents)
   - Low priority for C# migration

**Files:**
- `markdown/implementation/python_to_csharp_migration.md` (new, detailed analysis)
- `python/toolkit/*.py` (read function signatures)
- `csharp/src/**/*.cs` (check for existing functionality)
- `powershell/ScriptsToolkit/ScriptsToolkit.psm1` (check for overlaps)

**Implementation Steps:**
1. Run PY-004 to generate signature list
2. For each function:
   - Check if C# equivalent exists
   - Check if PowerShell equivalent exists
   - Assess necessity for migration
   - Design C# implementation (if needed)
3. Prioritize functions by usage frequency
4. Create migration roadmap

**Dependencies:** PY-004 (signature list)  
**Blocks:** None  
**Estimated Time:** 4-6 hours (analysis only, not implementation)

---

### PY-008: Assess Functional vs OOP Design for C# Migration
**Complexity:** ⭐⭐ Medium  
**Source:** To-Do.md #51  
**Priority:** LOW (architectural guidance)

**Question:** When migrating Python functions to C#, when to use:
1. Static methods in static classes (functional style)
2. Extension methods
3. Instance methods in services (OOP style)

**Guidelines:**

| Pattern | When to Use | Example |
|---------|-------------|---------|
| **Static Class** | Pure functions, no state | `AudioConverter.CalculateGain(file)` |
| **Extension Method** | Fluent API, enhance existing types | `file.CalculateGain()` |
| **Service Class** | Needs dependencies (HttpClient, config) | `new DiscogsService(client).Search()` |
| **Record/Struct** | Data transfer objects | `record ReleaseInfo(string Label, int Year)` |

**Example Analysis:**

```python
# Python: Functional
def calculate_gain(dff_file: str, target_headroom_db: float) -> float:
    # Pure function, no state
    pass
```

**C# Options:**

```csharp
// Option 1: Static method (BEST for pure functions)
public static class AudioAnalyzer
{
    public static double CalculateGain(string dffFile, double targetHeadroomDb) { }
}
// Usage: var gain = AudioAnalyzer.CalculateGain(file, -0.5);

// Option 2: Extension method (good for fluent API)
public static class AudioFileExtensions
{
    public static double CalculateGain(this FileInfo dffFile, double targetHeadroomDb) { }
}
// Usage: var gain = new FileInfo(path).CalculateGain(-0.5);

// Option 3: Service (AVOID - no dependencies needed)
public class AudioAnalyzerService
{
    public double CalculateGain(string dffFile, double targetHeadroomDb) { }
}
// Usage: var service = new AudioAnalyzerService(); service.CalculateGain(...);
```

**Recommendation for This Codebase:**
- **Prefer:** Static classes for pure functions (immutable, testable)
- **Use:** Services only when dependencies required (HttpClient, ILogger)
- **Avoid:** Unnecessary class instantiation

**Files:**
- `markdown/explanations/csharp_design_patterns.md` (new)

**Dependencies:** None  
**Blocks:** None  
**Estimated Time:** 1 hour (create guidelines document)

---

## Summary

### Task Priorities

| Priority | Tasks | Time Estimate |
|----------|-------|---------------|
| HIGH | 1 (PY-001) | 15 min |
| MEDIUM | 3 (PY-003, PY-005, PY-008) | 5-8 hours |
| LOW | 4 (PY-002, PY-004, PY-006, PY-007) | 6-8 hours |

### Immediate Actions (Next 1-2 Hours)

1. **PY-001:** Verify basedpyright works correctly (15 min)
2. **PY-004:** Generate signature list (30 min)
3. **PY-002:** Document pyproject.toml (20 min)

### Near-Term (Next Week)

4. **PY-003:** Fix basedpyright errors (2-3 hours)
5. **PY-005:** Integrate Last.fm into CLI (2-3 hours)
6. **PY-006:** Document CLI usage (30 min)

### Long-Term (Future)

7. **PY-007:** Migration analysis (4-6 hours, low priority)
8. **PY-008:** Design pattern guidelines (1 hour)

### Critical Questions

**Q1:** Should Python toolkit be migrated to C# at all?
**A1:** **NO, not immediately.** Python toolkit works, is maintained, and some functionality (audio/video processing) is easier in Python. Keep as-is, migrate only if:
- Performance becomes an issue
- Desire to consolidate to single language
- Specific feature requires C# integration

**Q2:** What to do with Last.fm scrobble updater?
**A2:** **Integrate into Typer CLI** (PY-005), then delete legacy folder. Keep in Python (already works with Google Sheets API).

**Q3:** Should we standardize on one CLI (Python vs C# vs PowerShell)?
**A3:** **No.** Each serves a purpose:
- **C#:** Music metadata, sync orchestration (performance-critical)
- **Python:** Audio/video processing (FFmpeg integration, rich ecosystem)
- **PowerShell:** System automation, scheduled tasks, module management

---

## Module-Specific Notes

### audio.py (17 functions)
- SACD processing: Very specialized, keep in Python
- FLAC conversion: Check for overlap with existing tools
- Metadata extraction: Likely duplicated elsewhere

### video.py (17 functions)
- Remuxing: PowerShell might already have this
- Compression: Useful, keep in Python (FFmpeg complexity)
- Thumbnail generation: Good candidate for C# migration (ImageSharp)

### cuesheet.py (5 functions)
- CUE parsing: Specialized format, keep in Python
- Track extraction: Niche use case

### filesystem.py (6 functions)
- Directory tree: **Duplicated in PowerShell** (Get-Directories)
- Torrent creation: Unique, keep in Python
- File renaming: Check for overlap

### lastfm.py (4 functions)
- **Action:** Integrate into Typer CLI (PY-005)
- **Do NOT** migrate to C# or PowerShell (working Google Sheets integration)

---

## Files to Create

### Documentation
1. `markdown/reference/python_signatures.md` (PY-004)
2. `markdown/reference/python_cli_usage.md` (PY-006)
3. `markdown/explanations/python_configuration.md` (PY-002)
4. `markdown/explanations/csharp_design_patterns.md` (PY-008)

### Implementation Plans
5. `markdown/implementation/python_to_csharp_migration.md` (PY-007)

### Reports
6. `python/basedpyright_errors.txt` (PY-003, temporary)

---

## Dependency Graph

```
PY-001 (Verify) ──► PY-003 (Fix Errors)
                 └► PY-004 (Signatures) ──► PY-007 (Migration Plan)
                                          └► PY-008 (Design Patterns)

PY-005 (Integrate Last.fm) ──► PY-006 (Document CLI)

PY-002 (Document pyproject.toml) [standalone]
```

### Quick Wins
- PY-001: Verify basedpyright (15 min)
- PY-002: Document pyproject.toml (20 min)
- PY-004: Generate signatures (30 min)

### High-Impact
- PY-003: Fix type errors (improves code quality)
- PY-005: Integrate Last.fm (cleanup legacy code)
