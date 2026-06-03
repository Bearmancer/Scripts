# Modern CLI/TUI Tools Reference

> A comprehensive guide to modern replacements for legacy Unix command-line utilities, organized by category.

## Table of Contents

1. [Introduction](#introduction)
2. [Category Index](#category-index)
3. [File Operations & Listing](#1-file-operations--listing)
4. [Text Processing & Search](#2-text-processing--search)
5. [Data Formats (JSON/YAML)](#3-data-formats-jsonyaml)
6. [System Monitoring & Diagnostics](#4-system-monitoring--diagnostics)
7. [Navigation & History](#5-navigation--history)
8. [Shell & Prompt](#6-shell--prompt)
9. [Terminal Multiplexing](#7-terminal-multiplexing)
10. [Development Tools](#8-development-tools)
11. [Networking & HTTP](#9-networking--http)
12. [Documentation & Rendering](#10-documentation--rendering)
13. [Language Server Protocol (LSP) Implementations](#11-language-server-protocol-lsp-implementations)
14. [Quick Install Reference](#quick-install-reference)

---

## Introduction

This document catalogs modern CLI and TUI tools — primarily written in Rust, Go, or other compiled languages — that provide superior alternatives to legacy Unix utilities. The guiding philosophy is:

- **Speed**: Rust/Go binaries start instantly vs Python/Node interpreters
- **Safety**: Memory safety, typed input/output, explicit error handling
- **UX**: Colorized output, previews, fuzzy search, interactive TUIs
- **Cross-platform**: Linux, macOS, Windows (often via WSL2 for Unix tools)

The [uutils/coreutils](https://github.com/uutils/coreutils) project deserves special mention: a Rust rewrite of ~100 GNU coreutils in a single dependency, covering most basic Unix commands (cp, mv, rm, ls, cat, etc.).

### When to Switch

| Scenario | Action |
|----------|--------|
| You pipe tool output to other tools daily | Replace with modern equivalent |
| You work with JSON/YAML configs | Add jq + yq |
| You manage Docker/Kubernetes | Add lazydocker + fzf |
| You read docs in terminal | Add bat + glow |
| You write scripts | Add shellharden + ripgrep |

---

## Category Index

| # | Category | Tool Count | Key Tools |
|---|----------|-----------|-----------|
| 1 | File Operations & Listing | 3 | eza, bat, fd |
| 2 | Text Processing & Search | 4 | ripgrep, sd, choose, delta |
| 3 | Data Formats (JSON/YAML) | 2 | jq, yq |
| 4 | System Monitoring & Diagnostics | 4 | dust, duf, procs, btop |
| 5 | Navigation & History | 4 | zoxide, broot, fzf, atuin |
| 6 | Shell & Prompt | 2 | starship, shellharden |
| 7 | Terminal Multiplexing | 1 | zellij |
| 8 | Development Tools | 4 | lazydocker, lazygit, difftastic, hyperfine |
| 9 | Networking & HTTP | 3 | httpie, xh, doggo |
| 10 | Documentation | 1 | glow |
| 11 | Language Server Protocol | 6 | gopls, golangci-lint, pyright, pylsp, omnisharp, PSES |

---

## 1. File Operations & Listing

### eza — Modern `ls`

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [eza-community/eza](https://github.com/eza-community/eza) — 22K stars |
| **Latest** | v0.23.4 (Oct 2025) — actively maintained |
| **Language** | Rust |
| **Replaces** | `ls`, `exa` (exa is unmaintained; eza is its active fork) |

#### Overview
eza is a modern replacement for `ls` with color-coded output, Git integration, file type icons, and extended metadata views. It preserves the `ls` interface while adding features that make directory listing genuinely useful for daily work.

#### Key Enhancements

| Feature | `ls` | eza |
|---------|------|-----|
| Color output | Basic (type-based) | Extended (permissions, size, Git status) |
| Git status | None | Built-in (`--git` flag) |
| File icons | None | Optional icon column |
| Tree view | `tree` command | `--tree` built-in |
| Extended metadata | `ls -la` only | fuse2, selinux, xattrs |
| Sorting | Name, time, size, ext | Name, time, size, ext, Git status |

#### Limitations
- Icon support requires a Nerd Font
- Slightly slower than `ls` on very large directories (millions of files)
- Git status requires a Git repository context

#### Installation

```bash
# Debian/Ubuntu
apt install eza

# macOS (Homebrew)
brew install eza

# Cargo (cross-platform)
cargo install eza

# Windows (winget)
winget install eza-community.eza
```

#### Usage Examples

```bash
# Default listing with icons
eza

# Long format with Git status
eza -l --git

# Tree view (3 levels)
eza --tree -L 3

# Show hidden files with extended metadata
eza -la --git --icons

# Sort by newest
eza -l --sort newest
```

#### Verdict
eza is a drop-in replacement for `ls` that adds immediate value — especially the Git status column and tree view. The icon support makes scanning directories faster. Recommended for anyone who uses `ls` more than a few times per day.

---

### bat — Modern `cat`

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [sharkdp/bat](https://github.com/sharkdp/bat) — 59K stars |
| **Latest** | v0.26.1 (Dec 2025) — actively maintained |
| **Language** | Rust |
| **Replaces** | `cat` |

#### Overview
bat is a `cat` clone with syntax highlighting, Git integration, and automatic paging. It detects file types and applies syntax highlighting, shows line numbers, and integrates with `less` for paging long files.

#### Key Enhancements

| Feature | `cat` | bat |
|---------|-------|-----|
| Syntax highlighting | None | 500+ languages |
| Line numbers | None | Enabled by default |
| Git modification marks | None | +/- marks in gutter |
| Paging | Manual `| less` | Automatic for long files |
| Non-printable chars | `cat -v` | `-A` for all |
| File concatenation | Default | `cat` mode via `-pp` |

#### Limitations
- Color output piped to non-terminals requires `--color=always`
- Syntax highlighting increases startup latency (~10ms)
- Cannot replace `cat` for binary file operations

#### Installation

```bash
# Debian/Ubuntu
apt install bat

# macOS (Homebrew)
brew install bat

# Cargo (cross-platform)
cargo install bat

# Windows (winget)
winget install sharkdp.bat
```

#### Usage Examples

```bash
# View file with syntax highlighting
bat file.py

# Show non-printable characters
bat -A file.bin

# Plain cat mode (no highlights)
bat -pp file.txt

# Read from stdin with highlighting
curl -s http://example.com/api | bat -l json

# Show changes in Git context
bat --diff file.py
```

#### Verdict
bat is the first tool to install after opening a terminal. The syntax highlighting alone improves readability, and the Git integration makes code reviews easier. The automatic paging eliminates accidental terminal flooding.

---

### fd — Modern `find`

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [sharkdp/fd](https://github.com/sharkdp/fd) — 43K stars |
| **Latest** | v10.4.2 (Mar 2026) — actively maintained |
| **Language** | Rust |
| **Replaces** | `find` |

#### Overview
fd is a fast, user-friendly replacement for `find`. It uses sensible defaults (recursive, ignore hidden/gitignored files, case-insensitive) and colorized output. The regex-based search is more intuitive than `find`'s expression syntax.

#### Key Enhancements

| Feature | `find` | fd |
|---------|--------|----|
| Defaults | Must specify path, recursion | Path (`.`), recursive, no-gitignored |
| Syntax | Expressions (`-name`, `-type f`) | Regex/glob patterns |
| Speed | Single-threaded | Parallel directory walk |
| Color output | None | Built-in |
| File types | `-type f/d` | `-t f`, `-t d` alias |
| Hidden files | `.` prefix filtering | Requires `-H` |
| Gitignore | Ignores completely | Respects `.gitignore` |
| Exec | `-exec` | `-x` or `--exec` |

#### Limitations
- Cannot do arbitrary-time predicates (e.g., "files not accessed in 30 days")
- No `-newer` comparison operator
- Less feature-rich for complex conditional searches

#### Installation

```bash
# Debian/Ubuntu
apt install fd-find

# macOS (Homebrew)
brew install fd

# Cargo (cross-platform)
cargo install fd-find

# Windows (winget)
winget install sharkdp.fd
```

#### Usage Examples

```bash
# Find by name pattern
fd "config.*json"

# Find by extension
fd -e py -e js

# Find directories only
fd -t d "src"

# Execute command on results
fd -e txt -x wc -l {}

# Case-sensitive exact match
fd --case-sensitive "README.md"
```

#### Verdict
fd replaces `find` for 90% of daily use cases with simpler syntax and faster results. The automatic `.gitignore` handling alone saves time browsing through `node_modules`, `.venv`, or `target/` directories.

---

## 2. Text Processing & Search

### ripgrep (rg) — Modern `grep`

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [BurntSushi/ripgrep](https://github.com/BurntSushi/ripgrep) — 64K stars |
| **Latest** | v15.1.0 (Oct 2025) — actively maintained |
| **Language** | Rust |
| **Replaces** | `grep`, `ag`, `ack`, `git grep` |

#### Overview
ripgrep (rg) is a line-oriented search tool that recursively searches directories for regex patterns. It respects `.gitignore`, uses SIMD-accelerated regex, and is dramatically faster than `grep` for directory-wide searches.

#### Key Enhancements

| Feature | `grep` | ripgrep |
|---------|--------|---------|
| Auto-ignore | None | Respects `.gitignore` |
| Recursive search | `-r` flag | Default |
| Speed | Sequential | Parallel (SIMD + multicore) |
| Hidden files | `.` prefix | Requires `--hidden` |
| Output format | Plain | Colorized with context |
| Encoding | ASCII | UTF-8, UTF-16, Latin-1 |
| Binary files | Shows gibberish | Skips by default |

#### Limitations
- No lookahead/lookbehind in POSIX mode (PCRE2 available with `-P` for lookarounds)
- No `-o` with multi-line mode
- Larger binary than grep

#### Installation

```bash
# Debian/Ubuntu
apt install ripgrep

# macOS (Homebrew)
brew install ripgrep

# Cargo (cross-platform)
cargo install ripgrep

# Windows (winget)
winget install BurntSushi.ripgrep
```

#### Usage Examples

```bash
# Search recursively (default)
rg "function main"

# Search specific file type
rg -t py "def test"

# Show context lines
rg -C 3 "TODO" src/

# Case-insensitive search
rg -i "error" --type rust

# Count matches
rg -c "import" src/

# Search hidden files
rg --hidden "api_key"
```

#### Verdict
ripgrep is the undisputed king of terminal search. Its combination of speed, `.gitignore` awareness, and sensible defaults makes grep feel obsolete. It's the only search tool you need for codebases.

---

### sd — Modern `sed`

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [so-fancy/diff-so-fancy](https://github.com/charmbracelet/gum) — 7K stars |
| **Latest** | v1.1.0 (Feb 2026) — actively maintained |
| **Language** | Rust |
| **Replaces** | `sed` for string replacement |

#### Overview
sd is an intuitive find-and-replace CLI tool. Unlike `sed`, which requires learning cryptic expressions (`s/foo/bar/g`), sd uses straightforward syntax: `sd "foo" "bar" file.txt`. It supports literal and regex modes.

#### Key Enhancements

| Feature | `sed` | sd |
|---------|-------|----|
| Syntax | `s/pattern/replacement/flags` | `sd pattern replacement` |
| Default behavior | Print output | In-place edit |
| Regex | Default | Requires `-r` |
| Literal strings | Must escape special chars | Default |
| File types | Text only | Text + limited binary |
| Multi-file | `-i` with find + xargs | Supports glob patterns |

#### Limitations
- No range-based operations (`sed '/start/,/end/command'`)
- No hold buffer or branching
- Simpler than sed for advanced text manipulation

#### Installation

```bash
# macOS (Homebrew)
brew install sd

# Cargo (cross-platform)
cargo install sd

# Arch Linux
pacman -S sd
```

#### Usage Examples

```bash
# Replace in file (in-place)
sd "old_text" "new_text" file.txt

# Replace with regex
sd -r "foo(\d+)" "bar$1" file.txt

# Replace in all .md files
sd "TODO" "DONE" *.md

# Preview changes (dry run with diff)
sd -s "foo" "bar" file.txt | diff file.txt -
```

#### Verdict
sd shines for the 80% of `sed` use cases that are simple string replacements. The intuitive syntax eliminates sed's learning curve. Keep `sed` for complex scripts; use `sd` for daily operations.

---

### choose — Modern `cut` / `awk`

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [theryangeary/choose](https://github.com/theryangeary/choose) — 2K stars |
| **Latest** | v1.3.7 (Aug 2025) — actively maintained |
| **Language** | Rust |
| **Replaces** | `cut`, `awk` for column extraction |

#### Overview
choose is a human-friendly alternative to `cut` and (for column extraction) `awk`. It uses 1-based indexing and inclusive ranges, eliminating `cut`'s confusing field numbering and `awk`'s verbose syntax for simple extraction.

#### Key Enhancements

| Feature | `cut` | choose |
|---------|-------|--------|
| Indexing | 1-based `-f 1` | 1-based `:0` |
| Ranges | `-f 3-5` | `:2:4` (inclusive) |
| Delimiter | `-d' '` | `-d' '` |
| Multi-char delimiter | Single char only | Multi-char supported |
| Output | Column only | Full line with highlighting |

#### Limitations
- Not a replacement for `awk`'s programming capabilities
- No built-in arithmetic or aggregation
- Smaller community, fewer features

#### Installation

```bash
# macOS (Homebrew)
brew install choose

# Cargo (cross-platform)
cargo install choose

# Arch Linux
pacman -S choose
```

#### Usage Examples

```bash
# Extract first two columns
ps aux | choose 0 1

# Column range (inclusive)
echo "a b c d" | choose :2

# Custom delimiter
cat file.csv | choose -d ',' 0 2

# Reverse selection
echo "a b c d" | choose !1
```

#### Verdict
choose is a small but valuable tool for column extraction. It doesn't replace `awk` entirely but handles the most common `cut`/`awk` task — "give me columns X to Y" — with a cleaner syntax.

---

### delta — Modern `diff` Pager

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [dandavison/delta](https://github.com/dandavison/delta) — 31K stars |
| **Latest** | v0.19.2 (Mar 2026) — actively maintained |
| **Language** | Rust |
| **Replaces** | `diff` output styling, `less` for git diffs |

#### Overview
delta is a syntax-highlighting pager for `git diff` and `diff` output. It's designed as a drop-in replacement for diff's output rendering, adding line numbers, side-by-side view, word-level diffing, and language-aware syntax highlighting.

#### Key Enhancements

| Feature | `diff` / git diff | delta |
|---------|--------------------|-------|
| Syntax highlighting | None | Language-aware |
| Line numbers | None | Side-by-side |
| Word-level diff | None | Within-line highlighting |
| Side-by-side | None | Built-in |
| Git integration | Native | Drop-in via `git config` |
| Theme support | None | 20+ built-in themes |

#### Limitations
- Pager for output — does not create diff files
- Can slow down on very large diffs (thousands of lines)
- Requires `less` for paging

#### Installation

```bash
# Debian/Ubuntu
apt install git-delta

# macOS (Homebrew)
brew install git-delta

# Cargo (cross-platform)
cargo install git-delta

# Windows (winget)
winget install dandavison.delta
```

#### Usage Examples

```bash
# Configure as git pager
git config --global core.pager "delta"

# View staged diff
git diff --cached

# Side-by-side comparison
git diff --delta-side-by-side

# Custom theme
git config --global delta.theme "Dracula"
```

#### Verdict
delta makes reading diffs genuinely pleasant. The syntax highlighting and word-level changes reveal changes at a glance. It's the first Git pager configuration recommended for any developer.

---

### difftastic — Structural `diff`

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [Wilfred/difftastic](https://github.com/Wilfred/difftastic) — 25K stars |
| **Latest** | v0.69.0 (Apr 2026) — actively maintained |
| **Language** | Rust |
| **Replaces** | `diff`, `git diff` |

#### Overview
difftastic is a structural diff tool that understands syntax trees, not just lines. Instead of showing which lines changed, it shows which expressions changed within those lines — making it dramatically better for code reviews.

#### Key Enhancements

| Feature | `diff` | difftastic |
|---------|--------|------------|
| Diff granularity | Line-level | Expression (AST) level |
| Syntax awareness | None | Language parser |
| Code format changes | Massive diff | Zero diff |
| Comment changes | Shows lines | Shows only comment |
| Line wrapping | Unreadable | Handles naturally |

#### Limitations
- Slower than line-based diff for large files
- Requires tree-sitter grammar installations for some languages
- Not suitable for non-code files

#### Installation

```bash
# macOS (Homebrew)
brew install difftastic

# Cargo (cross-platform)
cargo install difftastic

# Windows (scoop)
scoop install difftastic
```

#### Usage Examples

```bash
# Diff two files
difft file1.py file2.py

# Git integration (--diff-algorithm=histogram)
git diff --difftastic

# Customize display
difft --width 120 file1.js file2.js
```

#### Verdict
difftastic is revolutionary for code review. It solves the "reformatted code produced a massive diff" problem by showing only the semantic changes. Pair with delta for the ultimate diff experience.

---

## 3. Data Formats (JSON/YAML)

### jq — JSON Processor

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [jqlang/jq](https://github.com/jqlang/jq) — 35K stars |
| **Latest** | v1.8.1 (Jul 2025) — actively maintained |
| **Language** | C |
| **Replaces** | `python -c 'import json...'`, `grep`/`sed` on JSON |

#### Overview
jq is a lightweight command-line JSON processor with a domain-specific language for filtering, transforming, and manipulating JSON. Its filter expressions compose like Unix pipes, making it feel native to shell environments.

#### Key Enhancements

| Feature | Legacy Tools | jq |
|---------|--------------|-----|
| Query language | grep/sed (line-based) | Structure-aware expressions |
| Nested data | Manual iteration | Recursive descent (`..`) |
| Transformation | Python/Ruby scripts | Concise filter pipelines |
| Streaming | Loads entire file | `--stream` for large files |
| Output formats | Raw JSON | Colors, compact, pretty-print, raw |

#### Limitations
- Steep learning curve for filter syntax
- JSON only — no YAML/TOML/XML support
- Large files may still cause memory issues without streaming mode

#### Installation

```bash
# Debian/Ubuntu
apt install jq

# macOS (Homebrew)
brew install jq

# Windows (winget)
winget install jqlang.jq
```

#### Usage Examples

```bash
# Extract names from API response
curl -s api.example.com/users | jq '.[].name'

# Filter by condition
jq '.[] | select(.age > 25)' data.json

# Transform structure
jq '{users: [.[] | {name, email}]}' data.json

# Pretty-print
curl -s api | jq .
```

#### Verdict
jq is indispensable for anyone working with JSON in the terminal. Its expressive filter language and pipe composability make it the single most useful CLI utility for API work and configuration files.

---

### yq — YAML Processor

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [mikefarah/yq](https://github.com/mikefarah/yq) — 15K stars |
| **Latest** | v4.53.2 (Apr 2026) — actively maintained |
| **Language** | Go |
| **Replaces** | `sed`/`awk` on YAML, `python -c 'import yaml...'` |

#### Overview
yq extends beyond YAML to handle JSON, XML, CSV, and TOML with a consistent interface. It supports in-place editing, making it ideal for configuration file management across DevOps workflows.

#### Key Enhancements

| Feature | Legacy Tools | yq |
|---------|--------------|-----|
| Format support | YAML only | YAML, JSON, XML, CSV, TOML |
| In-place editing | sed (error-prone) | `eval -i` with safe syntax |
| Multi-document | Manual splitting | Native `---` stream handling |
| Anchors/aliases | Ignored | Full support |
| Path expressions | grep patterns | jq-inspired dot notation |

#### Limitations
- Complex nested expressions can become unwieldy
- XML handling less mature than dedicated tools
- Some YAML anchor edge cases

#### Installation

```bash
# Snap (Linux)
snap install yq

# macOS (Homebrew)
brew install yq

# Windows (winget)
winget install mikefarah.yq

# Go (cross-platform)
go install github.com/mikefarah/yq/v4@latest
```

#### Usage Examples

```bash
# Extract field
yq '.metadata.name' deployment.yaml

# In-place edit
yq eval -i '.spec.replicas = 3' deployment.yaml

# Convert YAML to JSON
yq -o json config.yaml

# Merge two files
yq eval-all 'select(fileIndex == 0) * select(fileIndex == 1)' base.yaml override.yaml
```

#### Verdict
yq is essential for Kubernetes, Docker Compose, and CI/CD pipeline workflows. Together with jq, they cover nearly all structured data needs in the terminal.

---

## 4. System Monitoring & Diagnostics

### dust — Modern `du`

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [bootandy/dust](https://github.com/bootandy/dust) — 12K stars |
| **Latest** | v1.2.4 (Jan 2026) — actively maintained |
| **Language** | Rust |
| **Replaces** | `du` |

#### Overview
dust (du + rust) is a more intuitive `du`. It shows disk usage as a horizontal bar chart, displaying the biggest directories first with visual proportions.

#### Key Enhancements

| Feature | `du` | dust |
|---------|------|------|
| Output format | Numbers only | Bar chart visualization |
| Sorting | Manual flags | Largest first (default) |
| Hidden files | Requires `-a` | Included by default |
| Depth limiting | `--max-depth=N` | Default sensible depth |

#### Limitations
- No CSV/JSON output
- Bar chart adds visual noise for scripting

#### Installation

```bash
# macOS (Homebrew)
brew install dust

# Cargo (cross-platform)
cargo install du-dust

# Windows (scoop)
scoop install dust
```

#### Usage Examples

```bash
# Show largest directories
dust

# Show specific directory
dust ~/Downloads

# Limit depth
dust -d 2

# Show files too
dust -b
```

---

### duf — Modern `df`

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [muesli/duf](https://github.com/muesli/duf) — 15K stars |
| **Latest** | v0.9.1 (Sep 2025) — actively maintained |
| **Language** | Go |
| **Replaces** | `df` |

#### Overview
duf is a colored, user-friendly `df` alternative. It groups mount points by type (local, network, fuse, special), shows usage percentage with a progress bar, and color-codes usage levels.

#### Key Enhancements

| Feature | `df` | duf |
|---------|------|-----|
| Output format | Table | Colored table with bars |
| Grouping | None | By filesystem type |
| Progress bars | None | Visual usage bars |
| File system types | `-T` flag | Auto-grouped |

#### Installation

```bash
# macOS (Homebrew)
brew install duf

# Cargo (cross-platform)
cargo install duf

# Windows (scoop)
scoop install duf
```

---

### procs — Modern `ps`

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [dalance/procs](https://github.com/dalance/procs) — 6K stars |
| **Latest** | v0.14.11 (Feb 2026) — actively maintained |
| **Language** | Rust |
| **Replaces** | `ps` |

#### Overview
procs is a modern replacement for `ps` with colored output, tree view, and keyword search. It colorizes entries by resource usage, shows Docker container names, and supports both keyword and regex search.

#### Key Enhancements

| Feature | `ps` | procs |
|---------|------|-------|
| Output color | None | Colored by resource intensity |
| Search | `ps aux | grep` | Built-in `--search` |
| Tree view | `pstree` | `--tree` |
| Docker context | None | Shows container names |
| Memory/CPU units | Dated | Human-readable |

#### Installation

```bash
# macOS (Homebrew)
brew install procs

# Cargo (cross-platform)
cargo install procs
```

---

### btop — Modern `top` / `htop`

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [aristocratos/btop](https://github.com/aristocratos/btop) — 33K stars |
| **Latest** | v1.4.7 (May 2026) — actively maintained |
| **Language** | C++ |
| **Replaces** | `top`, `htop`, `bashtop` |

#### Overview
btop is a resource monitor with GPU support, process management, and visual themes. It provides real-time CPU, memory, disk, network, and GPU monitoring with mouse support and detailed graphs.

#### Key Enhancements

| Feature | `top`/`htop` | btop |
|---------|---------------|------|
| GPU monitoring | None | NVIDIA/AMD/Intel |
| Disk I/O | None | Per-disk read/write |
| Network speed | None | Real-time graph |
| Themes | Limited | 50+ preset themes |
| Mouse support | htop (limited) | Full clickable UI |

#### Installation

```bash
# macOS (Homebrew)
brew install btop

# Debian/Ubuntu
apt install btop

# Cargo (cross-platform)
cargo install btop
```

---

### hyperfine — Modern `time`

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [sharkdp/hyperfine](https://github.com/sharkdp/hyperfine) — 28K stars |
| **Latest** | v1.20.0 (Nov 2025) — actively maintained |
| **Language** | Rust |
| **Replaces** | `time` |

#### Overview
hyperfine is a benchmarking tool for commands. It runs commands multiple times, calculates statistics (mean, min, max, std dev), and compares multiple commands against each other.

#### Key Enhancements

| Feature | `time` | hyperfine |
|---------|--------|-----------|
| Multiple runs | Manual | Automatic configurable |
| Statistics | Real time only | Mean, min, max, std dev |
| Comparative | Manual | Built-in command comparison |
| Output | Seconds only | Human + JSON/CSV |
| Warm-up | None | Configurable warm-up runs |

#### Installation

```bash
# macOS (Homebrew)
brew install hyperfine

# Cargo (cross-platform)
cargo install hyperfine
```

#### Usage Examples

```bash
# Benchmark a command
hyperfine "fd '\.py$'"

# Compare two commands
hyperfine "rg 'TODO'" "grep -rn 'TODO'"

# Warm-up and output JSON
hyperfine --warmup 5 --export-json results.json "npm run build"
```

---

## 5. Navigation & History

### zoxide — Modern `cd`

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [ajeetdsouza/zoxide](https://github.com/ajeetdsouza/zoxide) — 37K stars |
| **Latest** | v0.9.9 (Jan 2026) — actively maintained |
| **Language** | Rust |
| **Replaces** | `cd` |

#### Overview
zoxide is a smarter `cd` command that learns your directory preferences. After a few uses, `z <partial name>` jumps to the most relevant directory based on frequency and recency.

#### Key Enhancements

| Feature | `cd` | zoxide |
|---------|------|--------|
| Fuzzy matching | None | `z par` matches `/some/path` |
| Learning | None | Learns from `cd` and zoxide |
| Multi-query | None | `z a b` = directory containing `a` and `b` |
| Interactive | None | `zi` launches interactive menu |
| Exclusions | None | Configurable blacklist |

#### Installation

```bash
# macOS (Homebrew)
brew install zoxide

# Cargo (cross-platform)
cargo install zoxide

# Windows (winget)
winget install ajeetdsouza.zoxide

# Shell init
echo 'eval "$(zoxide init bash)"' >> ~/.bashrc
```

---

### broot — Modern `tree` / Navigation

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [Canop/broot](https://github.com/Canop/broot) — 13K stars |
| **Latest** | v1.57.0 (Jun 2026) — actively maintained |
| **Language** | Rust |
| **Replaces** | `tree`, `ls` for navigation |

#### Overview
broot is a tree explorer and file manager that combines directory tree display with fuzzy search and file operations. You type a pattern and broot instantly filters the tree, then you can navigate, preview, and open files.

#### Key Enhancements

| Feature | `tree` | broot |
|---------|--------|-------|
| Preview | None | File content preview |
| Fuzzy search | None | Instant tree filtering |
| File operations | None | Built-in operations |
| Git status | None | Color-coded status |
| Open files | Manual | Press Enter to open |

#### Installation

```bash
# macOS (Homebrew)
brew install broot

# Cargo (cross-platform)
cargo install broot
```

---

### fzf — Fuzzy Finder

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [junegunn/fzf](https://github.com/junegunn/fzf) — 81K stars |
| **Latest** | v0.73.1 (May 2026) — actively maintained |
| **Language** | Go |
| **Replaces** | Ctrl+R in bash, grep-based selection |

#### Overview
fzf is a general-purpose fuzzy finder. It takes a list on stdin and presents a fuzzy-search interface. The Ctrl+R history integration and Ctrl+T file search are the most common uses.

#### Key Enhancements

| Feature | Ctrl+R / grep | fzf |
|---------|---------------|-----|
| Search type | Exact/regex | Fuzzy matching |
| Interface | Text-only | TUI with preview pane |
| Integration | Shell-specific | Cross-shell keybindings |
| Preview | None | Configurable preview window |
| Extensibility | Limited | Plugin ecosystem |

#### Installation

```bash
# Debian/Ubuntu
apt install fzf

# macOS (Homebrew)
brew install fzf

# Cargo (cross-platform)
cargo install fzf

# Shell integration
/usr/share/fzf/install  # Enables Ctrl+R, Alt+C, Ctrl+T
```

---

### atuin — Shell History with Sync

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [ellie/atuin](https://github.com/ellie/atuin) — 30K stars |
| **Latest** | v18.16.1 (May 2026) — actively maintained |
| **Language** | Rust |
| **Replaces** | Bash/zsh history, Ctrl+R |

#### Overview
atuin replaces shell history with a SQLite-backed database with metadata (directory, exit code, session) and optional encrypted sync across machines.

#### Key Enhancements

| Feature | Legacy History | atuin |
|---------|----------------|-------|
| Storage | Flat text file | SQLite database |
| Search | Linear, exact | Fuzzy, multi-field |
| Cross-machine | None | Encrypted sync |
| Metadata | Timestamp only | Directory, exit code, session |
| Analytics | None | Usage statistics |

#### Installation

```bash
# Official installer
curl --proto '=https' --tlsv1.2 -sSf https://sh.atuin.io | bash
```

---

## 6. Shell & Prompt

### starship — Cross-Shell Prompt

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [starship/starship](https://github.com/starship/starship) — 58K stars |
| **Latest** | v1.25.1 (Apr 2026) — actively maintained |
| **Language** | Rust |
| **Replaces** | oh-my-zsh, Powerlevel10k, custom PS1 |

#### Overview
starship is a minimal, blazing-fast prompt that works across bash, zsh, fish, PowerShell, and more. It auto-detects language versions, Git status, and cloud context with sub-5ms latency.

#### Key Enhancements

| Feature | Legacy Tools | starship |
|---------|-------------|----------|
| Cross-shell support | Shell-specific | 6 shells |
| Prompt latency | 10-50ms+ | <5ms |
| Module count | Limited | 100+ modules |
| Configuration | Scattered dotfiles | Single TOML file |
| Language detection | Manual | Auto-detects |

#### Installation

```bash
# macOS/Linux
curl -sS https://starship.rs/install.sh | sh

# Shell init (bash example)
echo 'eval "$(starship init bash)"' >> ~/.bashrc
```

---

### shellharden — Bash Syntax Modernizer

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [koalaman/shellharden](https://github.com/koalaman/shellharden) — 5K stars |
| **Latest** | v4.3.1 (Mar 2024) — stable |
| **Language** | Rust |
| **Replaces** | Manual bash quoting fixes |

#### Overview
shellharden automatically modernizes bash syntax: quotes variables, replaces backticks with `$()`, and converts `[ ]` to `[[ ]]`. It can preview changes with `--diff` or apply them in-place.

#### Key Enhancements

| Feature | ShellCheck | shellharden |
|---------|------------|-------------|
| Mode | Lint-only | Auto-fix |
| Syntax upgrades | Warnings | Automatic |
| Diff preview | None | `--diff` flag |
| Safe mode | N/A | `--transform-unsafe` |

#### Installation

```bash
# macOS (Homebrew)
brew install shellharden

# Cargo (cross-platform)
cargo install shellharden
```

---

## 7. Terminal Multiplexing

### zellij — Modern `tmux`

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [zellij-org/zellij](https://github.com/zellij-org/zellij) — 33K stars |
| **Latest** | v0.44.3 (May 2026) — actively maintained |
| **Language** | Rust |
| **Replaces** | `tmux`, `screen` |

#### Overview
zellij is a terminal multiplexer with WASM plugins, floating panes, and a visible status bar. It ships with sensible defaults — no configuration required for basic use — and has built-in help with keybinding reference.

#### Key Enhancements

| Feature | `tmux`/`screen` | zellij |
|---------|------------------|--------|
| Default UI | Invisible status line | Status bar with keybindings |
| Plugin system | Shell scripts | WebAssembly (WASM) |
| Floating panes | Not native | Built-in |
| Session sharing | Manual SSH | Built-in |
| Layouts | Manual | Auto-layout presets |
| Help discovery | man / Google | In-app keybinding reference |

#### Installation

```bash
# Cargo (cross-platform)
cargo install zellij

# macOS (Homebrew)
brew install zellij

# Debian/Ubuntu
apt install zellij
```

#### Usage Examples

```bash
zellij                    # Start new session
zellij attach mysession   # Reattach
zellij run -- npm test    # Run command in new pane
zellij --layout compact   # Start with preset layout
```

---

## 8. Development Tools

### lazygit — Git TUI

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [jesseduffield/lazygit](https://github.com/jesseduffield/lazygit) — 79K stars |
| **Latest** | v0.62.1 (May 2026) — actively maintained |
| **Language** | Go |
| **Replaces** | `git` CLI for interactive use |

#### Overview
lazygit is a terminal UI for Git with staging, diff, branch, and merge operations all keyboard-navigable. It eliminates memorizing Git command flags for common operations.

#### Key Enhancements

| Feature | `git` CLI | lazygit |
|---------|-----------|---------|
| Staging | `git add -p` | Visual diff staging |
| Branching | `git checkout/branch` | Visual branch tree |
| Merging | `git merge` | Visual conflict resolution |
| History | `git log` | Visual commit graph |
| Rebase | `git rebase -i` | Interactive rebase TUI |

#### Installation

```bash
# macOS (Homebrew)
brew install lazygit

# Go (cross-platform)
go install github.com/jesseduffield/lazygit@latest

# Windows (winget)
winget install jesseduffield.lazygit
```

---

### lazydocker — Docker TUI

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [jesseduffield/lazydocker](https://github.com/jesseduffield/lazydocker) — 51K stars |
| **Latest** | v0.25.2 (Apr 2026) — actively maintained |
| **Language** | Go |
| **Replaces** | `docker` CLI for monitoring |

#### Overview
lazydocker provides a visual dashboard for containers, logs, resource stats, and docker-compose management — all keyboard-navigable.

#### Key Enhancements

| Feature | `docker` CLI | lazydocker |
|---------|--------------|------------|
| Container overview | `docker ps` (snapshot) | Live dashboard |
| Log viewing | `docker logs -f` | Integrated log panel |
| Resource stats | `docker stats` (separate) | Built-in |
| Rebuild workflow | Manual stop/remove/rebuild | One-key rebuild ('b') |
| Service management | Multiple commands | Navigate in TUI |

#### Installation

```bash
# macOS (Homebrew)
brew install lazydocker

# Go (cross-platform)
go install github.com/jesseduffield/lazydocker@latest
```

---

### hyperfine — Benchmarking

See [Section 4 - System Monitoring & Diagnostics](#hyperfine--modern-time)

---

## 9. Networking & HTTP

### httpie — Human-Friendly HTTP Client

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [httpie/cli](https://github.com/httpie/cli) — 38K stars |
| **Latest** | v3.2.4 (Nov 2024) — actively maintained |
| **Language** | Python |
| **Replaces** | `curl` for API testing |

#### Overview
httpie makes HTTP requests readable. It defaults to JSON, colorizes output, and converts curl's flag-heavy syntax to natural commands like `http POST api.example.com/users name=John`.

#### Key Enhancements

| Feature | `curl` | httpie |
|---------|--------|--------|
| JSON default | Manual headers | Automatic Content-Type |
| Syntax | Flag-heavy | Natural language |
| Output | Raw text | Colorized, formatted |
| Sessions | Manual | Built-in session persistence |
| HTTP/2 | Flag required | Default |

#### Installation

```bash
# pip (cross-platform)
pip install httpie

# macOS (Homebrew)
brew install httpie
```

---

### xh — Fast HTTP Client

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [ducaale/xh](https://github.com/ducaale/xh) — 8K stars |
| **Latest** | v0.25.3 (Dec 2025) — actively maintained |
| **Language** | Rust |
| **Replaces** | `curl`, `httpie` |

#### Overview
xh is a Rust reimplementation of httpie's syntax with near-instant startup time. Drop-in compatible with most httpie commands but compiled to a small, fast binary.

#### Key Enhancements

| Feature | `curl`/`httpie` | xh |
|---------|------------------|-----|
| Startup time | Python interpreter | Near-instant native |
| HTTP/2 | Optional | Default |
| Binary size | Large dependencies | Small, self-contained |
| Syntax | Compatible | httpie-compatible |

#### Installation

```bash
# Cargo (cross-platform)
cargo install xh

# macOS (Homebrew)
brew install xh
```

---

### doggo — Modern DNS Client

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [mr-karan/doggo](https://github.com/mr-karan/doggo) — 4K stars |
| **Latest** | v1.1.6 (May 2026) — actively maintained |
| **Language** | Go |
| **Replaces** | `dig`, `nslookup`, `host` |

#### Overview
doggo is a modern DNS client with colored output, DNS-over-HTTPS/TLS support, and JSON output. It makes DNS queries readable while supporting modern protocols.

#### Key Enhancements

| Feature | `dig`/`nslookup` | doggo |
|---------|-------------------|-------|
| Output format | Raw, unformatted | Colorized, structured |
| DoH/DoT | Not supported | Native |
| JSON output | Manual parsing | Built-in |
| Query syntax | Verbose flags | Intuitive arguments |

#### Installation

```bash
# macOS (Homebrew)
brew install doggo

# Go (cross-platform)
go install github.com/mr-karan/doggo/cmd/doggo@latest
```

---

## 10. Documentation & Rendering

### glow — Markdown Renderer

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [charmbracelet/glow](https://github.com/charmbracelet/glow) — 25K stars |
| **Latest** | v2.1.2 (Apr 2026) — actively maintained |
| **Language** | Go |
| **Replaces** | `less` for markdown, browser viewing |

#### Overview
glow renders markdown files beautifully in the terminal with typography, syntax highlighting, and layout. Supports local files, remote URLs, and has a pager mode.

#### Key Enhancements

| Feature | `less` / browser | glow |
|---------|------------------|------|
| Terminal rendering | Plain text | Rich styled output |
| Pager mode | Separate tool | Built-in pager |
| Remote URLs | Download first | Direct URL rendering |
| Theme system | N/A | Customizable themes |

#### Installation

```bash
# macOS (Homebrew)
brew install glow

# Debian/Ubuntu
apt install glow

# Windows (winget)
winget install charmbracelet.glow
```

#### Usage Examples

```bash
glow README.md                    # Render local file
glow -p README.md                 # Pager mode
glow https://example.com/docs     # Remote URL
```

---

## 11. Language Server Protocol (LSP) Implementations

### gopls — Go Language Server

| Attribute | Detail |
|-----------|--------|
| **Source** | [golang.org/x/tools/gopls](https://github.com/golang/tools/tree/master/gopls) |
| **Latest** | v0.22.0 (May 2026) — maintained by Go team |
| **Replaces** | godef, gogetdoc, goreturns, guru |

#### Key Enhancements

| Feature | Legacy Tools | gopls |
|---------|-------------|-------|
| Navigation | godef (file-level) | Project-wide go-to-definition |
| Diagnostics | vet, staticcheck separately | Integrated real-time |
| Formatting | gofmt, goreturns | Built-in with organize imports |
| Code Completion | Basic | Context-aware with generics |
| Refactoring | Manual | Rename, extract, organize imports |

#### Installation

```bash
go install golang.org/x/tools/gopls@latest
```

---

### golangci-lint — Go Linter Aggregator

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [golangci/golangci-lint](https://github.com/golangci/golangci-lint) — 17K stars |
| **Latest** | v2.12.2 (May 2026) — actively maintained |
| **Replaces** | Individual linters (golint, errcheck, staticcheck) |

#### Key Enhancements

| Feature | Individual Linters | golangci-lint |
|---------|-------------------|---------------|
| Execution | Sequential | Parallel, 10x faster |
| Configuration | Per-linter configs | Single YAML file |
| Auto-fix | Manual per tool | `--fix` flag |
| CI Integration | Custom scripts | GitHub Actions |

#### Installation

```bash
curl -sSfL https://raw.githubusercontent.com/golangci/golangci-lint/master/install.sh | sh -s -- -b $(go env GOPATH)/bin
```

---

### pyright — Python Type Checker

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [microsoft/pyright](https://github.com/microsoft/pyright) — 15K stars |
| **Latest** | v1.1.409 (Apr 2026) — maintained by Microsoft |
| **Replaces** | mypy (for speed), pyflakes |

#### Key Enhancements

| Feature | mypy / pyflakes | pyright |
|---------|----------------|---------|
| Speed | Slow (Python-based) | Fast (TypeScript-based, 10x+) |
| Type Inference | Conservative | Aggressive, project-aware |
| Configuration | Complex mypy.ini | Minimal config |
| IDE Integration | Plugin-dependent | Native Pylance |

#### Installation

```bash
npm install -g pyright
# or
pip install pyright
```

---

### pylsp — Python Language Server

| Attribute | Detail |
|-----------|--------|
| **Source** | [python-lsp-server](https://github.com/python-lsp/python-lsp-server) |
| **Latest** | v1.14.0 (Dec 2025) — maintained |
| **Replaces** | python-language-server (palantir), pyls |

#### Key Enhancements

| Feature | Legacy pyls | pylsp |
|---------|-------------|-------|
| Completiotopn | Jedi only | Jedi + rope refactoring |
| Linting | Separate plugins | Integrated flake8/pylint |
| Refactoring | Limited | Full rope support |
| Plugin System | Hardcoded | Load/unload at will |

#### Installation

```bash
pip install python-lsp-server[all]
```

---

### omnisharp — C# Language Server

| Attribute | Detail |
|-----------|--------|
| **GitHub** | [OmniSharp/omnisharp-roslyn](https://github.com/OmniSharp/omnisharp-roslyn) |
| **Latest** | v1.39.15 (Nov 2025) — maintained |
| **Replaces** | Manual editor IDE setup |

#### Key Enhancements

| Feature | Legacy Tooling | omnisharp |
|---------|----------------|-----------|
| Completion | Basic IntelliSense | Full Roslyn completion |
| Navigation | Limited go-to-def | Solution-wide references |
| Refactoring | IDE-specific | 20+ built-in refactorings |
| Project Support | .csproj only | Solution-level analysis |

#### Installation

```bash
dotnet tool install -g omnisharp
```

---

### PSES — PowerShell Extension for Editors

| Attribute | Detail |
|-----------|--------|
| **Source** | [PowerShell/PowerShellEditorServices](https://github.com/PowerShell/PowerShellEditorServices) |
| **Latest** | v4.4.0 (2026) — maintained by Microsoft |
| **Replaces** | ISE, manual debugging |

#### Key Enhancements

| Feature | ISE / Manual | PSES |
|---------|-------------|------|
| IntelliSense | Basic | Full parameter, module |
| Debugging | Basic breakpoints | Step-through, variable inspection |
| Script Analysis | Manual | Integrated real-time |
| Cross-Platform | Windows only | Windows, macOS, Linux |

#### Installation

```bash
# Installed via VS Code extension
code --install-extension ms-vscode.powershell
```

---

## Quick Install Reference

### Essential Tools (Install First)

```bash
# File operations
apt install bat fd-find ripgrep eza git-delta     # Debian/Ubuntu
brew install bat fd ripgrep eza git-delta          # macOS
cargo install bat fd-find ripgrep eza git-delta    # Cargo

# Navigation & data
apt install fzf jq yq
brew install fzf jq yq
cargo install fzf
```

### Development Tools

```bash
brew install lazygit lazydocker glow
cargo install zellij difftastic
go install github.com/jesseduffield/lazygit@latest
go install github.com/jesseduffield/lazydocker@latest
```

### LSPs

```bash
go install golang.org/x/tools/gopls@latest
go install github.com/golangci/golangci-lint/cmd/golangci-lint@latest
npm install -g pyright
pip install python-lsp-server[all]
dotnet tool install -g omnisharp
code --install-extension ms-vscode.powershell
```

### Monitoring

```bash
brew install btop duf dust hyperfine procs
cargo install du-dust btop hyperfine
```

### Interactive History

```bash
cargo install zoxide
curl --proto '=https' --tlsv1.2 -sSf https://sh.atuin.io | bash
```

---

*Generated: June 2026. Tool versions reflect latest releases at time of writing. Always check GitHub repositories for the most current versions.*
