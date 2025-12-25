# Completion Tools - Verified Documentation

*Generated: After systematic verification of each tool's actual commands and documentation*

---

## Summary of Corrections from Earlier Claims

| Claim | Reality |
|-------|---------|
| "carapace eagerly loads 500+ completers" | ❌ **WRONG** - Carapace is lazy-loaded by design. `carapace _carapace powershell` registers one completer function that dynamically routes to 669 completers on demand |
| "argc has ~0 built-in completers" | ❌ **WRONG** - argc-completions repo has **1000+** completers |
| "PSCompletions is just for prettier terminal" | ❌ **WRONG** - PSCompletions integrates with argc/carapace as a unified menu frontend via `enable_menu_enhance = 1` |

---

## 1. Carapace (VERIFIED ✅)

### Stats
- **Total Completers:** 669 (verified via `(carapace --list).Count`)
- **Lazy Loading:** Yes, by design - single completer function handles all 669 commands
- **Dynamic Macros:** 200+ macros for dynamic completion (verified via `carapace --macro`)

### Key Commands
```powershell
# Setup (single line loads ALL completers lazily)
carapace _carapace powershell | Out-String | Invoke-Expression

# List all completers
carapace --list

# Show available macros (for dynamic completion)
carapace --macro

# Bridge detection for unknown commands
carapace --detect <command>

# Help/documentation
carapace --help
```

### Configuration Paths
- Cache: `C:\Users\Lance\AppData\Local\carapace`
- Config: `C:\Users\Lance\AppData\Roaming\carapace`
- Custom Specs: `C:\Users\Lance\AppData\Roaming\carapace\specs`

### Bridge Support
Environment variable enables bridging completion from other shells:
```powershell
$env:CARAPACE_BRIDGES = 'zsh,fish,bash,inshellisense'
```

### Creating Custom Completions (Spec Files)
Spec files go in `C:\Users\Lance\AppData\Roaming\carapace\specs\`:

```yaml
# whisper-ctranslate2.yaml
name: whisper-ctranslate2
description: Whisper transcription using CTranslate2
flags:
  --model: Choose model size
  --task: transcribe or translate
  --language: Source language
  --output_format: Output format
completion:
  flag:
    model: ["tiny", "tiny.en", "base", "base.en", "small", "small.en", "medium", "medium.en", "large-v1", "large-v2", "large-v3", "large-v3-turbo", "turbo"]
    task: ["transcribe", "translate"]
    output_format: ["txt", "vtt", "srt", "tsv", "json", "all"]
    device: ["auto", "cpu", "cuda"]
    compute_type: ["default", "auto", "int8", "int8_float16", "float16", "float32"]
  positional:
    - ["$files"]  # audio files
```

### Key Macros for Dynamic Completion
| Macro | Purpose |
|-------|---------|
| `$files` | Complete files |
| `$directories` | Complete directories |
| `tools.docker.Containers` | Docker containers |
| `tools.git.LocalBranches` | Git branches |
| `tools.ffmpeg.Formats` | FFmpeg formats |
| `env.Names` | Environment variables |

---

## 2. argc / argc-completions (VERIFIED ✅)

### What is argc?
argc is a CLI framework AND completion engine. The `argc` binary generates shell completions.

### argc-completions Repository
- **Total Completers:** 1000+ (per GitHub docs: "Autocomplete for 1000+ commands")
- **Key Feature:** Auto-generates completions from `--help` text
- **Location:** Must be cloned from GitHub: `git clone https://github.com/sigoden/argc-completions.git`

### Key Commands
```powershell
# Generate shell completion script for commands
argc --argc-completions powershell whisper-ctranslate2

# Generate completion candidates at runtime
argc --argc-compgen powershell "" whisper-ctranslate2 --

# Full help
argc --argc-help
```

### Auto-Generate New Completions
In the argc-completions repo:
```bash
./scripts/generate.sh whisper-ctranslate2
```

This parses `whisper-ctranslate2 --help` and generates a completion script.

### How argc Completions Work
1. Parse help text with `parse-table.awk`
2. Generate argc script with `parse-script.awk`
3. Optional: Add `_patch_*` functions for customization
4. Optional: Add `_choice_*` functions for dynamic data

### Integration with PSCompletions
argc generates a `Register-ArgumentCompleter` scriptblock that PSCompletions can intercept and enhance with its menu.

---

## 3. PSCompletions (VERIFIED ✅)

### What It Is
A PowerShell completion manager that provides a rich menu system for ALL completion sources (not just psc add completions).

### Current Settings (Your System)
```
enable_menu = 1          (Rich menu enabled)
enable_menu_enhance = 1  (Intercepts ALL tab completion, including argc/carapace)
enable_tip = 1           (Shows description pane / help docs)
```

### Built-in Completers (~60+)
```powershell
psc search ""  # List all available
```
Includes: 7z, bun, cargo, choco, conda, deno, docker, gh, git, kubectl, mise, ngrok, npm, pip, pnpm, python, scoop, uv, volta, winget, yarn, and more.

### Key Commands
```powershell
# Add completion
psc add git scoop winget

# Search available
psc search git

# List added completions
psc list

# Find completion storage
psc which git

# Menu configuration
psc menu config

# Individual settings
psc menu config enable_menu 1
psc menu config enable_menu_enhance 1
psc menu config enable_tip 1
```

### Menu Configuration Options (23+)
| Option | Purpose |
|--------|---------|
| `enable_menu` | Enable/disable PSCompletions menu |
| `enable_menu_enhance` | Intercept ALL tab completions (argc, carapace, native) |
| `enable_tip` | Show description/help pane |
| `enable_tip_when_enhance` | Show tips for enhanced (non-psc) completions |
| `enable_list_follow_cursor` | Menu follows cursor position |
| `enable_enter_when_single` | Auto-apply single match |
| `trigger_key` | Key to open menu (default: Tab) |
| `filter_symbol` | Symbol for filter input |
| `completion_suffix` | Suffix after completion value |

### How enable_menu_enhance Works
When `enable_menu_enhance = 1`:
1. PSCompletions overrides PowerShell's TabExpansion2
2. Gets completions from ANY source (argc, carapace, native cmdlets)
3. Displays them in PSCompletions' rich menu with tips/colors

This is why PSCompletions integrates with argc/carapace - it's the presentation layer.

---

## 4. Creating whisper-ctranslate2 Completions

### Option A: Carapace Spec File (Recommended)
Create `C:\Users\Lance\AppData\Roaming\carapace\specs\whisper-ctranslate2.yaml`:

```yaml
name: whisper-ctranslate2
description: Whisper transcription using CTranslate2
flags:
  --model: "Model to use for transcription"
  --model_directory: "Path to model directory"
  --model_dir: "Alias for model_directory"
  --output_dir: "Directory for output files"
  --output_format: "Output format"
  --device: "Device to use (auto/cpu/cuda)"
  --compute_type: "Compute type"
  --task: "Task to perform"
  --language: "Source language"
  --temperature: "Sampling temperature"
  --beam_size: "Beam size for decoding"
  --word_timestamps: "Enable word-level timestamps"
  --verbose: "Enable verbose output"
  -h, --help: "Show help"
completion:
  flag:
    model:
      - "tiny\tSmallest model"
      - "tiny.en\tEnglish-only tiny"
      - "base\tBase model"
      - "base.en\tEnglish-only base"
      - "small\tSmall model"
      - "small.en\tEnglish-only small"
      - "medium\tMedium model"
      - "medium.en\tEnglish-only medium"
      - "large-v1\tLarge v1"
      - "large-v2\tLarge v2"
      - "large-v3\tLarge v3"
      - "large-v3-turbo\tTurbo variant"
      - "turbo\tOptimized turbo"
    output_format:
      - "txt\tPlain text"
      - "vtt\tWebVTT subtitles"
      - "srt\tSRT subtitles"
      - "tsv\tTab-separated values"
      - "json\tJSON format"
      - "all\tAll formats"
    device: ["auto", "cpu", "cuda"]
    compute_type:
      - "default\tDefault compute type"
      - "auto\tAuto-detect"
      - "int8\t8-bit integer"
      - "int8_float16\tInt8 + Float16"
      - "float16\t16-bit float"
      - "float32\t32-bit float"
    task: ["transcribe", "translate"]
    language:
      - "en\tEnglish"
      - "es\tSpanish"
      - "fr\tFrench"
      - "de\tGerman"
      - "it\tItalian"
      - "pt\tPortuguese"
      - "ja\tJapanese"
      - "zh\tChinese"
      - "ko\tKorean"
      - "ru\tRussian"
  positional:
    - ["$files"]
```

### Option B: argc-completions Generate
```bash
cd argc-completions
./scripts/generate.sh whisper-ctranslate2
# Creates completions/whisper-ctranslate2.sh
```

### Option C: PSCompletions Custom
PSCompletions doesn't support custom completions easily - use carapace or argc instead.

---

## 5. Recommended Setup for Your Profile

### Minimal Profile Setup (Fast Load)
```powershell
# ONE LINE - loads argc and carapace completers lazily
argc --argc-completions powershell argc | Out-String | Invoke-Expression
carapace _carapace powershell | Out-String | Invoke-Expression

# PSCompletions auto-loads if installed as module
# ensure menu enhance is on:
# psc menu config enable_menu_enhance 1
# psc menu config enable_tip 1
```

### What Happens When You Press Tab
1. PowerShell calls TabExpansion2
2. PSCompletions intercepts (if `enable_menu_enhance = 1`)
3. Gets completions from registered completers (argc/carapace/native)
4. Displays rich menu with descriptions/tips
5. Applies selected completion

### Why This Is Fast
- argc: Generates completer that delegates to `argc --argc-compgen` only when Tab pressed
- carapace: Single completer function routes to 669 completers on demand
- PSCompletions: Just intercepts and renders - doesn't preload completion data

---

## 6. Summary

| Tool | Completers | Lazy? | Description Pane | Custom Easy? |
|------|------------|-------|------------------|--------------|
| carapace | 669 | ✅ Yes | Via PSCompletions | ✅ YAML specs |
| argc-completions | 1000+ | ✅ Yes | Via PSCompletions | ✅ ./generate.sh |
| PSCompletions | ~60 | ✅ Yes | ✅ Native (`enable_tip`) | ❌ Harder |

### Best Practice
1. Use **carapace** as primary completer (669 commands, YAML specs for custom)
2. Use **argc-completions** for commands carapace doesn't have
3. Use **PSCompletions** as the menu/presentation layer with `enable_menu_enhance = 1`
4. For **whisper-ctranslate2**: Create a carapace YAML spec file
