# Xonsh ScriptsToolkit

> **Shell:** Xonsh
> **Version Target:** 0.14+
> **Philosophy:** Superset of Python 3. You can write valid Python, or process-heavy shell script, or mix both.

---

## Complete Implementation (`ScriptsToolkit.xsh`)

Save as `C:\Users\Lance\Dev\powershell\ScriptsToolkit.xsh`.

```python
#!/usr/bin/env xonsh
#
# ScriptsToolkit.xsh
# Full implementation of Scripts Toolkit
#

# ==============================================================================
# IMPORTS & CONSTANTS
# ==============================================================================

# Xonsh allows standard Python imports
import sys
from pathlib import Path
from typing import Optional, List

# Environment variables are accessed via $VAR or ${...}
# Python variables act as standard variables
REPO_ROOT = Path($HOME) / "Dev"
PYTHON_TOOLKIT = str(REPO_ROOT / "python/toolkit/cli.py")
CSHARP_ROOT = str(REPO_ROOT / "csharp")
LOG_DIR = str(REPO_ROOT / "logs")

# ==============================================================================
# HELPERS
# ==============================================================================

def log_info(msg: str) -> None:
    # xonsh rich printing (using print_color or ansi directly)
    print(f"\033[36m[INFO]\033[0m {msg}")

def log_success(msg: str) -> None:
    print(f"\033[32m[DONE]\033[0m {msg}")

def log_error(msg: str) -> None:
    # Print to stderr
    print(f"\033[31m[FAIL]\033[0m {msg}", file=sys.stderr)

def invoke_python(*args) -> None:
    # @(expr) evaluates python expression and puts it into subprocess command
    # ![cmd] executes cmd and captures pipeline object
    # checks returncode property
    
    # We flatten args explicitly for clean subprocess call
    cmd_args = list(args)
    
    # Subprocess execution in Xonsh
    # python @(PYTHON_TOOLKIT) @(cmd_args)
    # We inspect result object 'p' implicitly if not captured, 
    # but here we want to ensure success.
    p = ![python @(PYTHON_TOOLKIT) @(cmd_args)]
    
    if p.returncode != 0:
        log_error(f"Python toolkit failed: {p.errors}")
        # Raise generic exception to stop flow if strict
        raise Exception("Command failed")

# ==============================================================================
# UTILITIES
# ==============================================================================

def tkfn() -> None:
    print("\n\033[36mScriptsToolkit Functions (Xonsh)\033[0m")
    print("==================================")
    
    # List of tuples
    funcs = [
        ("Filesystem", "dirs", "List dirs"),
        ("Filesystem", "tree", "List tree"),
        ("Video", "remux", "Remux MKV"),
        ("Video", "compress", "Compress video"),
        ("Audio", "audio", "Convert audio"),
        ("Audio", "tomp3", "To MP3"),
        ("Transcription", "whisp", "Transcribe"),
        ("Sync", "syncall", "Sync All")
    ]
    
    current_cat = ""
    for cat, name, desc in funcs:
        if cat != current_cat:
            print(f"\n\033[33m{cat}\033[0m")
            current_cat = cat
        # Python string formatting
        print(f"  \033[32m{name:<12}\033[0m {desc}")
    print("")

# ==============================================================================
# COMMANDS
# ==============================================================================

# Type hinting is optional but recommended
def dirs(directory: str = ".", sort: str = "size") -> None:
    invoke_python("filesystem", "tree", "--directory", directory, "--sort", sort)

def tree(directory: str = ".") -> None:
    invoke_python("filesystem", "tree", "--directory", directory, "--include-files")

def torrent(directory: str = ".", recursive: bool = False) -> None:
    args = ["filesystem", "torrents", "--directory", directory]
    if recursive:
        args.append("--include-subdirectories")
    invoke_python(*args)

def remux(path: str = ".") -> None:
    invoke_python("video", "remux", "--path", path)

def compress(directory: str = ".") -> None:
    invoke_python("video", "compress", "--directory", directory)

def audio(directory: str = ".", fmt: str = "all") -> None:
    invoke_python("audio", "convert", "--directory", directory, "--mode", "convert", "--format", fmt)

def tomp3(path: str = ".") -> None:
    audio(path, "mp3")

# ==============================================================================
# TRANSCRIPTION
# ==============================================================================

def whisp(
    path: str,
    language: Optional[str] = None,
    model: str = "large-v3",
    translate: bool = False,
    force: bool = False,
    output: str = "."
) -> None:
    p = Path(path)
    srt_path = Path(output) / f"{p.stem}.srt"
    
    if srt_path.exists() and not force:
        print(f"\033[33mSkipped: {p.name} (SRT exists)\033[0m")
        return
    
    log_info(f"Transcribing: {p.name}")
    
    # Construct args list
    cmd = ["--model", model, "--output_format", "srt", "--output_dir", output]
    if language:
        cmd.extend(["--language", language])
    if translate:
        cmd.extend(["--task", "translate"])
    
    # Xonsh subprocess call
    # We pass the path as a string explicitly
    whisper-ctranslate2 @(cmd) @(str(p))
    
    log_success(f"Completed: {p.name}")

def wpf(directory: str = ".", language: Optional[str] = None) -> None:
    # Use pathlib globbing - standard Python
    # rglob for recursive, glob for flat
    exts = {"mp4", "mkv", "mp3", "flac"}
    count = 0
    # Iterate dir
    for f in Path(directory).glob("*"):
        if f.suffix[1:] in exts:
            whisp(str(f), language=language)
            count += 1
            
    if count == 0:
        log_error("No files found")

def wpj(path: str) -> None:
    whisp(path, language="ja")

# ==============================================================================
# SYNC
# ==============================================================================

def syncyt(force: bool = False) -> None:
    # 'pushd' is available as a command or alias in Xonsh
    # But context manager is more Pythonic
    with 0: # 0 means 'no redirection', just context block? No, typical python chdir
        # Actually xonsh has implicit cd.. but for script safety:
        # We can use os.chdir, or the special '![cd path]' which affects shell state?
        # NO. Subprocesses ( ![] ) do NOT affect current shell state.
        # We must use 'cd' builtin or 'os.chdir'.
        
        import os
        cwd = os.getcwd()
        os.chdir(CSHARP_ROOT)
        try:
            cmd = ["run", "--", "sync", "yt"]
            if force:
                cmd.append("--force")
            
            dotnet @(cmd)
        finally:
            os.chdir(cwd)

def synclf(since: Optional[str] = None) -> None:
    import os
    cwd = os.getcwd()
    os.chdir(CSHARP_ROOT)
    try:
        cmd = ["run", "--", "sync", "lastfm"]
        if since:
            cmd.extend(["--since", since])
        
        dotnet @(cmd)
    finally:
        os.chdir(cwd)

def syncall() -> None:
    print("\n\033[36m[YouTube Sync]\033[0m")
    syncyt()
    print("\n\033[36m[Last.fm Sync]\033[0m")
    synclf()
    log_success("All syncs complete!")

# ==============================================================================
# ALIAS REGISTRATION
# ==============================================================================

# Register functions as commands so they can be called like 'dirs .'
# aliases is a built-in dictionary
aliases['dirs'] = dirs
aliases['tree'] = tree
aliases['torrent'] = torrent
aliases['remux'] = remux
aliases['compress'] = compress
aliases['audio'] = audio
aliases['tomp3'] = tomp3
aliases['whisp'] = whisp
aliases['wpf'] = wpf
aliases['wpj'] = wpj
aliases['syncyt'] = syncyt
aliases['synclf'] = synclf
aliases['syncall'] = syncall
aliases['tkfn'] = tkfn

log_success("ScriptsToolkit (Xonsh) loaded.")
```

---

## Symbol & Feature Glossary (Xonsh)

| Feature          | Symbol / Syntax          | Detailed Explanation                                                                                                                                     |
| ---------------- | ------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Python Mode**  | `import os`              | Standard Python syntax is valid everywhere.                                                                                                              |
| **Subprocess**   | `cmd arg`                | Lines that look like shell commands are executed as subprocesses.                                                                                        |
| **Python Sub**   | `@(expr)`                | Forces evaluation of `expr` (variable or function) to insert into a subprocess command list.                                                             |
| **Capture Objs** | `![cmd]`                 | Runs `cmd` and returns a `CommandPipeline` object containing `.out`, `.err`, `.returncode`. Warning: this suppresses stdout unless inspected.            |
| **Uncaptured**   | `$[cmd]`                 | Runs `cmd` streaming output to console, returns `None`. (Default behavior for top-level commands).                                                       |
| **Aliases**      | `aliases['name'] = func` | Maps a shell command `name` to a Python function `func`. The function receives arguments as a list of strings if not typed, or mapped to types if typed. |
| **Path Lib**     | `Path(path)`             | Xonsh heavily integrates `pathlib`. `/` operator joins paths.                                                                                            |
| **Environment**  | `$VAR`                   | Access environment variables. Can also assign: `$PATH.append(...)`.                                                                                      |
| **Tilde**        | `~/Dev`                  | Paths with `~` are expanded automatically in subprocess mode, but require `.expanduser()` in Python mode.                                                |
| **Bool Flag**    | `flag: bool`             | In alias functions, a `bool` argument usually maps to a flag (e.g. `--flag`). Xonsh handles parsing automatically if registered properly.                |

### Formatting
- **Indent:** 4 spaces (Python standard).
- **Control Flow:** `if`, `for`, `while`, `try/except` are all standard Python.
- **Mix:** Writing `echo @("hello")` is valid. Mixing modes is the core power.
