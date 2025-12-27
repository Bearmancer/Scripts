# Python Implementation Plan - Toolkit & CLI

*Last Updated: December 25, 2025*
*Based on Typer documentation, Stack Overflow research, and best practices*

---

## Executive Summary

| Metric | Current | Target |
|--------|---------|--------|
| **basedpyright errors** | 0 | 0 ✅ |
| **Functions documented** | 61 | 61 ✅ |
| **CLI framework** | argparse | Typer |
| **Scrobble integration** | Isolated | Integrated |

---

## Research Sources

### 1. Typer Official Documentation
**Source:** https://typer.tiangolo.com/

**Key Features:**
- Built on Click, adds type hints
- Automatic CLI generation from function signatures
- Built-in shell completion support (including PowerShell)
- "FastAPI of CLIs" - same design patterns

**Basic Usage:**
```python
import typer

app = typer.Typer()

@app.command()
def process(
    input_file: str = typer.Argument(..., help="Input file path"),
    output: str = typer.Option("output.txt", "--output", "-o", help="Output path"),
    verbose: bool = typer.Option(False, "--verbose", "-v", help="Enable verbose output"),
):
    """Process a file with optional verbose output."""
    if verbose:
        typer.echo(f"Processing {input_file}...")
    # Process logic here

if __name__ == "__main__":
    app()
```

---

### 2. Typer vs Click Comparison
**Source:** https://typer.tiangolo.com/alternatives/

| Feature | Click | Typer |
|---------|-------|-------|
| Type hints | Decorators only | Native Python types |
| IDE support | Limited | Full autocomplete |
| Learning curve | Moderate | Low (if know FastAPI) |
| Shell completion | Via plugin | Built-in |
| Code verbosity | High | Low |

**Typer Advantages:**
- Uses Python type annotations directly
- Better IDE autocomplete support
- Less code duplication (no decorator params matching function params)
- Modern Python 3.6+ design

---

### 3. Stack Overflow Findings
**Source:** https://stackoverflow.com/questions/tagged/typer

**Common Patterns:**

**Subcommands in single file:**
```python
app = typer.Typer()
audio_app = typer.Typer()
video_app = typer.Typer()

app.add_typer(audio_app, name="audio")
app.add_typer(video_app, name="video")
```

**Progress bars:**
```python
import typer
from rich.progress import track

@app.command()
def process(files: list[str]):
    for file in track(files, description="Processing..."):
        # Process each file
        pass
```

**Dynamic defaults:**
```python
from typing import Optional

@app.command()
def cmd(
    output: Optional[str] = typer.Option(None, "--output", "-o"),
    input_file: str = typer.Argument(...),
):
    if output is None:
        output = input_file.replace(".txt", "_processed.txt")
```

---

## Current Python Toolkit Structure

### Module Overview (61 functions)

| Module | Functions | Description |
|--------|-----------|-------------|
| audio.py | 17 | Audio processing, transcription |
| video.py | 17 | Video manipulation, ffmpeg wrappers |
| cli.py | 12 | Current CLI entry points |
| filesystem.py | 6 | File operations, organization |
| cuesheet.py | 5 | CUE sheet parsing/generation |
| lastfm.py | 4 | Last.fm API integration |
| logging_config.py | 0 | Logging configuration |

### Key Functions by Module

**audio.py:**
- `extract_audio()` - Extract audio from video
- `convert_to_wav()` - Convert audio formats
- `get_duration()` - Get audio duration
- `normalize_audio()` - Normalize audio levels

**video.py:**
- `extract_frames()` - Extract frames from video
- `create_gif()` - Create GIF from video
- `add_subtitles()` - Burn subtitles into video
- `resize_video()` - Resize video dimensions

**lastfm.py:**
- `get_recent_tracks()` - Fetch recent scrobbles
- `update_now_playing()` - Update now playing
- `scrobble_track()` - Scrobble a track
- `get_user_info()` - Get user profile info

---

## High-Priority Tasks

### PY-004: Typer-Based CLI Overhaul

**Current cli.py structure:**
```python
# argparse-based, verbose
parser = argparse.ArgumentParser()
subparsers = parser.add_subparsers()
# ... many lines of setup
```

**Target Typer structure:**
```python
import typer
from typing import Optional, List
from pathlib import Path

app = typer.Typer(help="Scripts Toolkit CLI")

# Audio commands
audio_app = typer.Typer(help="Audio processing commands")
app.add_typer(audio_app, name="audio")

@audio_app.command()
def extract(
    input_file: Path = typer.Argument(..., help="Input video file"),
    output: Optional[Path] = typer.Option(None, "--output", "-o"),
    format: str = typer.Option("mp3", "--format", "-f"),
):
    """Extract audio from video file."""
    from .audio import extract_audio
    result = extract_audio(input_file, output, format)
    typer.echo(f"Extracted to: {result}")

# Video commands
video_app = typer.Typer(help="Video processing commands")
app.add_typer(video_app, name="video")

@video_app.command()
def gif(
    input_file: Path = typer.Argument(...),
    start: float = typer.Option(0.0, "--start", "-s"),
    duration: float = typer.Option(5.0, "--duration", "-d"),
    fps: int = typer.Option(15, "--fps"),
):
    """Create GIF from video segment."""
    from .video import create_gif
    result = create_gif(input_file, start, duration, fps)
    typer.echo(f"Created: {result}")

# LastFM commands
lastfm_app = typer.Typer(help="Last.fm scrobbling commands")
app.add_typer(lastfm_app, name="lastfm")

@lastfm_app.command()
def recent(
    limit: int = typer.Option(10, "--limit", "-n"),
    user: Optional[str] = typer.Option(None, "--user", "-u"),
):
    """Show recent scrobbles."""
    from .lastfm import get_recent_tracks
    tracks = get_recent_tracks(limit, user)
    for track in tracks:
        typer.echo(f"{track.artist} - {track.name}")

if __name__ == "__main__":
    app()
```

---

### PY-006: Integrate Scrobble Module

**Current State:** Isolated in `last.fm Scrobble Updater/` folder

**Target:** Integrated into `toolkit/lastfm.py`

**Migration Steps:**
1. Review scrobble updater code
2. Extract core functions to `lastfm.py`
3. Add Typer CLI commands
4. Delete isolated folder (PY-005)

---

## pyproject.toml Configuration

**Current (working):**
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

**After Typer migration:**
```toml
[project]
name = "scripts-toolkit"
version = "1.0.0"
requires-python = ">=3.10"
dependencies = [
    "typer[all]>=0.21.0",
    "rich>=13.0.0",
]

[project.scripts]
toolkit = "toolkit.cli:app"

[tool.basedpyright]
typeCheckingMode = "strict"
reportMissingTypeStubs = false
reportUnknownMemberType = false
```

---

## Implementation Priority

### Immediate (This Week)
1. **PY-007:** Assess all Python files individually
2. Install Typer: `pip install "typer[all]"`
3. Create basic Typer app structure

### Short-term (This Month)
4. **PY-004:** Migrate cli.py to Typer
5. Add shell completion: `toolkit --install-completion`
6. **PY-006:** Integrate scrobble module

### Long-term (Next Quarter)
7. **PY-005:** Delete isolated scrobble folder
8. Full test coverage
9. Package for distribution

---

## Shell Completion

**Typer PowerShell completion:**
```powershell
# Install completion
toolkit --install-completion powershell

# Or manual registration
Register-ArgumentCompleter -Native -CommandName toolkit -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)
    toolkit --show-completion powershell | Invoke-Expression
}
```

---

## References

1. **Typer Docs:** https://typer.tiangolo.com/
2. **Typer GitHub:** https://github.com/fastapi/typer
3. **Rich (for progress/tables):** https://rich.readthedocs.io/
4. **Click (underlying library):** https://click.palletsprojects.com/
5. **basedpyright:** https://docs.basedpyright.com/

---

*Document generated from verified research and documentation review.*
