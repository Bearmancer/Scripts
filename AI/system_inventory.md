# System Inventory — Full Analysis

> Comprehensive audit of SDKs, CLI tools, AI tools, MCP servers, skills, plugins, custom applications, and state.

---

## 1. SDKs & Runtimes

| SDK | Version | Path | Status |
|-----|---------|------|--------|
| **Rust (rustc)** | 1.96.0 (2026-05-25) | `~/.cargo/bin/rustc` | ✅ |
| **Cargo** | 1.96.0 | `~/.cargo/bin/cargo` | ✅ |
| **.NET SDK** | 10.0.108 | `/usr/lib/dotnet/sdk` | ✅ |
| **ASP.NET Core Runtime** | 10.0.8 | shared | ✅ |
| **.NET Runtime** | 10.0.8 | shared | ✅ |
| **Node.js / npm** | 26.2.0 / 11.16.0 | nvm | ✅ |
| **Python** | 3.12.3 | system | ✅ |
| **Go** | — | not found | ❌ |
| **PowerShell (pwsh)** | — | not found | ❌ |

### Rustup Components & Plugins

| Component | Status | Targets |
|-----------|--------|---------|
| **rustc** (stable x86_64) | ✅ installed | native |
| **cargo** | ✅ installed | — |
| **clippy** | ✅ installed | — |
| **rustfmt** | ✅ installed | — |
| **rust-analyzer** | ✅ available | — |
| **rust-docs** | ✅ installed | — |
| **rust-std** | ✅ installed | x86_64-linux-gnu |
| **rust-std (cross-compile)** | ⬜ available | 90+ targets (aarch64, wasm32, arm, etc.) |
| **llvm-tools** | ⬜ available | — |

### .NET Components & Workloads

| Component | Version |
|-----------|---------|
| .NET SDK | 10.0.108 |
| ASP.NET Core Runtime | 10.0.8 |
| .NET Runtime | 10.0.8 |

### Node.js & npm

| Tool | Version |
|------|---------|
| Node.js | 26.2.0 (via nvm) |
| npm | 11.16.0 |

---

## 2. AI CLI Tools

| Tool | Version | Install Method | Description | Plugin/Skill System |
|------|---------|---------------|-------------|---------------------|
| **opencode** | 1.15.13 | `~/.opencode/bin/opencode` (145MB) | Primary AI coding assistant | 60 skills from `~/.agents/skills/` + MCP plugin system |
| **@openai/codex** | 0.136.0 | npm global | OpenAI Codex CLI agent | MCP plugin system, memory DB, personality config |
| **@kilocode/cli** | 7.3.21 | npm global | KiloCode AI CLI | Plugins via `@kilocode/plugin` (v7.3.16) + SDK |
| **cline** | 3.0.15 | npm global | Cline autonomous coding agent | — |
| **kiro-cli** | — | `~/.local/bin/kiro-cli` | Kiro AI terminal tools | — |
| **kiro-cli-chat** | — | `~/.local/bin/kiro-cli-chat` | Kiro AI chat | — |
| **kiro-cli-term** | — | `~/.local/bin/kiro-cli-term` | Kiro AI terminal mode | — |

### Cross-Agent Plugin/Skill Architecture

All AI agents share a **unified skill/plugin infrastructure**:

```
~/.agents/skills/          ← Canonical skill repository (60 skills, ~140 files)
├── brainstorming/         ← 4 files (SKILL.md, reviewer, visual, scripts)
├── caveman/               ← 2 files (SKILL.md, README)
├── systematic-debugging/  ← 11 files (SKILL.md, CREATION-LOG, etc.)
└── ... (60 total)

Agent Configs:
├── ~/.config/opencode/    ← opencode config + MCP servers
├── ~/.config/kilo/        ← KiloCode config (shares MCP config with opencode)
├── ~/.config/goose/       ← Goose config + MCP server + symlinked skills
├── ~/.codex/              ← Codex config (own MCP servers, memory DB, goals DB)

Skill Symlinks:
└── ~/.config/goose/skills/* → ../../../.agents/skills/*  (47 symlinks)
```

Key insight: **Goose** (`~/.config/goose/skills/`) uses symlinks to share the same skill set as opencode, but only has ~47 of 60 skills linked. KiloCode and Codex use their own configs with the same MCP servers.

### Agent Config & MCP Sharing Matrix

| Agent | Config Location | MCP Servers | Skill System | DB/State |
|-------|----------------|-------------|--------------|----------|
| **opencode** | `~/.config/opencode/opencode.jsonc` | 4 (MCP_DOCKER, agentql, crawl4ai, playwright) | `~/.agents/skills/` (60) | `~/.local/share/opencode/` |
| **Codex** | `~/.codex/config.toml` | **Same 4** MCP servers | None | `goals_1.sqlite`, `logs_2.sqlite` |
| **KiloCode** | `~/.config/kilo/kilo.jsonc` | **Same 4** MCP servers | `@kilocode/plugin` SDK `(v7.3.16)` | `~/.local/share/kilo/kilo.db` |
| **Goose** | `~/.config/goose/config.yaml` | **Same** MCP_DOCKER only | **47 symlinks** → `~/.agents/skills/` | — |
| **Cline** | `~/.config/cline/` (empty) | — | — | — |

### Agent Plugin SDK Versions (npm)

| Plugin Package | Version | Hosts | Purpose |
|----------------|---------|-------|---------|
| **@opencode-ai/plugin** | 1.15.13 | opencode | Plugin API (shell, tool, example modules) |
| **@kilocode/plugin** | 7.3.16 | opencode + KiloCode | Plugin API (distributed to both) |
| **@kilocode/sdk** | 7.3.16 | KiloCode | SDK for building plugins |

### Codex Agent Config Detail

| Setting | Value |
|---------|-------|
| Model | `gpt-5.4-mini` |
| Reasoning effort | `xhigh` |
| Personality | `pragmatic` |
| MCP servers | 4 (MCP_DOCKER, agentql, crawl4ai, playwright) |
| Features | memories, external_migration, network_proxy, prevent_idle_sleep |
| State DBs | `goals_1.sqlite` (goals), `logs_2.sqlite` (logs) |
| History | `history.jsonl` |
| Cache | `cache/` (apps, tools, server info) |

### Goose Agent Config Detail

| Setting | Value |
|---------|-------|
| MCP servers | 1 (MCP_DOCKER only) |
| Skills | 47 symlinks to `~/.agents/skills/` |
| Config | `~/.config/goose/config.yaml` |

### Goose Skills — Shared via Symlinks (47 of 60)

| Skill | Symlinked? | Notes |
|-------|-----------|-------|
| a11y-audit ✅ | aegisops-ai ✅ | archive ✅ | ask ✅ | autopilot ✅ |
| cancel ✅ | cavecrew ✅ | caveman ✅ | caveman-commit ✅ | caveman-compress ✅ |
| caveman-help ✅ | caveman-review ✅ | caveman-stats ✅ | code-review ✅ | compliance-check ✅ |
| configure-notifications ✅ | context-optimize ✅ | cost ✅ | debug ✅ | debug-workflow ✅ |
| deep-interview ✅ | delegation ✅ | design-dialogue ✅ | design-system ✅ | doctor ✅ |
| execute ✅ | execution ✅ | handoff ✅ | help ✅ | hud-setup ✅ |
| implementation-planning ✅ | learn ✅ | orchestrate ✅ | plan ✅ | prd ✅ |
| ralplan ✅ | resume-session ✅ | review ✅ | review-code ✅ | session-management ✅ |
| sessions ✅ | status ✅ | team ✅ | validation ✅ | verify ✅ | wait ✅ |

**Missing from Goose (13):** brainstorming, dispatching-parallel-agents, executing-plans, finishing-a-development-branch, find-skills, receiving-code-review, requesting-code-review, subagent-driven-development, systematic-debugging, test-driven-development, using-superpowers, verification-before-completion, writing-plans, writing-skills

### opencode Skills — Full Inventory (60)

Each skill contains a `SKILL.md` with instructions plus optional supporting files.

| Skill | Files | Sub-Type | Description |
|-------|-------|----------|-------------|
| **a11y-audit** | 1 | SKILL.md | WCAG accessibility audit |
| **aegisops-ai** | 1 | SKILL.md | DevSecOps & FinOps guardrails |
| **archive** | 1 | Session | Archive active session |
| **ask** | 1 | Session | Run Gemini advisor prompt |
| **autopilot** | 1 | Execution | Autonomous feature driving |
| **brainstorming** | 4 | Design | SKILL.md, spec-reviewer, visual-companion, scripts |
| **cancel** | 1 | Session | Safely stop work |
| **cavecrew** | 2 | Caveman | SKILL.md, README — caveman subagent delegation |
| **caveman** | 2 | Caveman | SKILL.md, README — ultra-compressed mode |
| **caveman-commit** | 2 | Caveman | SKILL.md, README — compressed commits |
| **caveman-compress** | 4 | Caveman | SKILL.md, README, SECURITY — compress memory files |
| **caveman-help** | 2 | Caveman | SKILL.md, README — quick reference card |
| **caveman-review** | 2 | Caveman | SKILL.md, README — compressed code review |
| **caveman-stats** | 2 | Caveman | SKILL.md, README — token usage stats |
| **code-review** | 1 | Review | Standalone code review |
| **compliance-check** | 1 | Review | GDPR/CCPA compliance |
| **configure-notifications** | 1 | Config | Slack/Discord/Telegram setup |
| **context-optimize** | 1 | Optimization | Signal-to-noise analysis |
| **cost** | 1 | Session | Token usage metrics |
| **debug** | 1 | Debug | Debug workflow |
| **debug-workflow** | 1 | Debug | Maestro debugging workflow |
| **deep-interview** | 1 | Design | Socratic interview |
| **delegation** | 1 | Execution | Agent delegation best practices |
| **design-dialogue** | 1 | Design | Structured design conversations |
| **design-system** | 1 | Design | Design system token extraction |
| **dispatching-parallel-agents** | 1 | Execution | Parallel agent coordination |
| **doctor** | 1 | Debug | Setup & health inspection |
| **execute** | 1 | Execution | Plan execution |
| **executing-plans** | 1 | Execution | External session execution |
| **execution** | 1 | Execution | Phase execution methodology |
| **find-skills** | 1 | Discovery | Skill discovery & installation |
| **finishing-a-development-branch** | 1 | Workflow | Branch completion |
| **handoff** | 1 | Session | Structured handoff documents |
| **help** | 1 | Help | Commands/skills explanation |
| **hud-setup** | 1 | Config | HUD surface configuration |
| **implementation-planning** | 1 | Design | Implementation plan generation |
| **learn** | 1 | Meta | Extract reusable lessons |
| **orchestrate** | 1 | Execution | Full Maestro workflow |
| **plan** | 1 | Planning | Phased execution plans |
| **prd** | 1 | Planning | PRD generation |
| **ralplan** | 1 | Planning | Iterative planning with consensus |
| **receiving-code-review** | 1 | Review | Receiving review feedback |
| **requesting-code-review** | 2 | Review | SKILL.md, code-reviewer prompt |
| **resume-session** | 1 | Session | Interrupted session resumption |
| **review** | 1 | Review | Structured code review |
| **review-code** | 1 | Review | Maestro-style code review |
| **session-management** | 1 | Session | Session state tracking |
| **sessions** | 1 | Session | Session history inspection |
| **status** | 1 | Session | Session status summary |
| **subagent-driven-development** | 4 | Execution | SKILL.md, implementer prompt, code-quality-reviewer |
| **systematic-debugging** | 11 | Debug | SKILL.md, CREATION-LOG, condition-based-waiting, refs |
| **team** | 1 | Execution | Parallel tmux workers |
| **test-driven-development** | 2 | TDD | SKILL.md, anti-patterns doc |
| **using-superpowers** | 2 | Onboarding | SKILL.md, skill invocation guide |
| **validation** | 1 | Meta | Phase output validation |
| **verification-before-completion** | 1 | TDD | Pre-completion verification |
| **verify** | 1 | TDD | Acceptance criteria verification |
| **wait** | 1 | Session | Rate-limit state manager |
| **writing-plans** | 2 | Planning | SKILL.md, plan-document-reviewer |
| **writing-skills** | 7 | Meta | SKILL.md, anthropic-best-practices, persuasion-principles, refs |

---

## 3. MCP Servers Configuration

Defined in `~/.config/opencode/opencode.jsonc`:

| Server | Type | Command / URL | Status |
|--------|------|---------------|--------|
| **MCP_DOCKER** | local | `docker mcp gateway run --profile default` | ✅ |
| **agentql** | local | `npx -y agentql-mcp` (key-based) | ✅ |
| **crawl4ai** | remote | `http://localhost:11235/mcp/sse` | ✅ |
| **playwright** | local | `npx -y @playwright/mcp@latest` | ✅ |

### MCP_DOCKER Gateway — Sub-Servers (50+)

The Docker MCP gateway exposes Azure and general-purpose MCP servers:

| Domain | Servers |
|--------|---------|
| **Azure Compute** | compute, aks, containerapps, functionapp, appservice |
| **Azure Data** | cosmos, postgres, mysql, sql, kusto, redis |
| **Azure Storage** | storage, fileshares, storagesync |
| **Azure Networking** | signalr, eventgrid, eventhubs, servicebus |
| **Azure Security** | keyvault, role, policy, quota |
| **Azure AI** | search, speech, foundry, foundryextensions |
| **Azure DevOps** | deploy, azd, arm, bicepschema, azureterraform, azureterraformbestpractices |
| **Azure Monitor** | monitor, advisor, applicationinsights, grafana, workbooks, resourcehealth, appliance, applens |
| **Azure Mgmt** | acr, group_list, pricing, marketplace, subscription_list |
| **Azure Migration** | azuremigrate, cloudarchitect, wellarchitectedframework |
| **Azure Other** | virtualdesktop, communication, confidentialledger, deviceregistry, loadtesting, managedlustre, azurebackup, datadog |
| **Browser** | browser_navigate, browser_snapshot, browser_click, browser_type, browser_evaluate, browser_screenshot, browser_tabs, browser_network_*, browser_drag, browser_drop, browser_fill_form, browser_hover, browser_press_key, browser_select_option, browser_wait_for, browser_console_messages, browser_handle_dialog, browser_resize, browser_file_upload, browser_run_code_unsafe |
| **Search/Scrape** | firecrawl_search, firecrawl_scrape, firecrawl_crawl, firecrawl_extract, firecrawl_map, firecrawl_agent, firecrawl_agent_status, firecrawl_monitor_*, firecrawl_interact, firecrawl_parse, supadata_* |
| **C#/.NET Dev** | NamespacesExplorer, NamespaceTypes, ReferencedAssembliesExplorer, NuGetPackageSearch, NuGetPackageVersions |
| **Microsoft Docs** | microsoft_docs_search, microsoft_docs_fetch, microsoft_code_sample_search, documentation, get_azure_bestpractices |
| **Library Docs** | get-library-docs, resolve-library-id |
| **Other** | sequentialthinking, code-mode, mcp-find/add/remove/config-set, read_graph/search_nodes/create_entities/create_relations/open_nodes, cost |

---

## 4. Cargo-Installed CLI Tools (6)

| Tool | Version | Crate | Replaces | Category | Plugin System |
|------|---------|-------|----------|----------|---------------|
| **dust** | 1.2.4 | du-dust | `du` | System Monitor | None |
| **lsd** | 1.2.0 | lsd | `ls` | File Ops | Themes, icons (Nerd Font) |
| **tree** | 1.3.0 | rust_tree | `tree` | File Ops | None |
| **fresh** | 0.3.10 | fresh-editor | nano/vim | Editor | Config files, themes, locale |
| **lite-pg** | 0.1.1 | lite-pg | psql | Database | None |
| **pg_cli** | 0.1.0 | pg_cli | psql | Database | None |

### Available Cargo Plugins (not installed)

| Plugin | Purpose |
|--------|---------|
| **cargo-edit** | Edit Cargo.toml dependencies (superseded by cargo add 1.62+) |
| **cargo-watch** | Watch source for changes |
| **cargo-audit** | Audit dependencies for vulnerabilities |
| **cargo-expand** | Expand macros |
| **cargo-outdated** | Check for outdated deps |
| **cargo-tarpaulin** | Code coverage |
| **cargo-llvm-cov** | LLVM-based coverage |

---

## 5. System CLI Tools

| Tool | Version | Method | Replaces | Category |
|------|---------|--------|----------|----------|
| **jq** | 1.7 | apt | JSON parsing | Data |
| **fd-find** | 9.0.0 | apt | `find` | File Ops |
| **tmux** | 3.4 | apt | Terminal multiplexer | TUI |
| **helix (hx)** | — | apt | `vim`/`nano` | Editor |
| **nnn** | — | apt | File manager | TUI |
| **tealdeer** | — | apt | `man` (tldr client) | Docs |
| **gh** | — | apt | GitHub CLI | Dev |
| **calibre** | — | apt | E-book management | Media |
| **Azure CLI (az)** | 2.87.0 | apt | Azure mgmt | Cloud |
| **git** | — | system | VCS | Dev |
| **docker** | — | system | Containers | Dev |
| **docker compose** | v5.1.4 | system | Multi-container | Dev |
| **AWS CLI** | — | local bin | AWS mgmt | Cloud |

### Azure CLI Extensions (0 installed)

`az extension list` returned empty. No custom Azure CLI extensions installed (e.g., aks-preview, account, etc.).

### Docker Plugins (0 installed)

`docker plugin ls` returned empty. No Docker volume/network/authorization plugins installed.

### Git Plugins

Only built-in git-core commands (git-add, git-am, git-branch, etc.). No custom git aliases or subcommands.

---

## 6. npm Global Packages (5)

| Package | Version | Description | Plugin System |
|---------|---------|-------------|---------------|
| **@kilocode/cli** | 7.3.21 | KiloCode AI CLI | Platform bins (linux-x64, win-x64, darwin-arm64, etc.) |
| **@kilocode/cli-linux-x64** | 7.3.16 | Platform-specific binary | — |
| **@openai/codex** | 0.136.0 | OpenAI Codex agent | Platform bins (linux-x64, darwin-arm64, etc.) |
| **cline** | 3.0.15 | Autonomous coding agent | — |
| **npm** | 11.16.0 | Package manager | Package.json scripts, lifecycle hooks |

---

## 7. Custom Applications

### Python Toolkit (`/home/lance/Scripts/python/toolkit/`) — 11 Modules

| Module | Dependencies | Purpose | Plugin Points |
|--------|-------------|---------|---------------|
| **cli.py** | cyclopts, rich | Main CLI entrypoint (cyclopts-based) | Cyclopts subcommands auto-discover |
| **audio.py** | ffmpeg-python | Audio conversion, SACD ISO extraction | FFmpeg codec plugins |
| **video.py** | — | Video processing | — |
| **filesystem.py** | py3createtorrent | Torrent creation, filesystem ops | — |
| **lastfm.py** | pylast, gspread, google-auth | Last.fm → Google Sheets sync | gspread auth extensions |
| **cuesheet.py** | deflacue | Cue sheet parsing | — |
| **pristine.py** | requests | Pristine Classical streaming download | — |
| **utils.py** | unidecode, chardet | General utilities | — |
| **types.py** | — | Type definitions | — |
| **exceptions.py** | — | Custom exceptions | — |
| **logging_config.py** | — | Logging config | — |

### Toolkit Dependencies (16)

| Package | Purpose | Plugin System |
|---------|---------|---------------|
| **cyclopts** | CLI framework | Decorator-based command discovery |
| **rich** | Terminal formatting | Renderable protocol, themes |
| **ffmpeg-python** | FFmpeg bindings | FFmpeg codecs (extensible) |
| **pylast** | Last.fm API | — |
| **gspread** | Google Sheets | Auth plugins |
| **requests** | HTTP | Adapters, auth plugins |
| **Pillow** | Images | Plugins for formats |
| **playwright** | Browser | Browser-specific drivers |
| **deflacue** | Cue sheets | — |
| **py3createtorrent** | Torrents | — |
| **tqdm** | Progress bars | Callback hooks |
| **pyperclip** | Clipboard | Platform backends |
| **pathvalidate** | Paths | — |
| **chardet** | Encoding | Detection plugins |
| **unidecode** | Transliteration | — |

### C# / .NET Project — Scripts.slnx

| Component | Type | Plugin/Extension Points |
|-----------|------|------------------------|
| **DbContext** | EF Core | Entity configs, interceptors, conventions |
| **Compiled Models** | EF Core | Precompiled query evaluation |
| **YouTube models** | C# records | YouTubeVideo, YouTubePlaylist, PlaylistSnapshot |
| **Fibery models** | C# classes | FiberyEntity, ExecutionLog, FailedTask |
| **Last.fm models** | C# classes | Scrobble, Album, Artist, Track, SourceRecord |
| **Configurations** | EF Core Fluent API | IEntityTypeConfiguration<T> |
| **Services** | C# classes | YouTube sync, translation, Last.fm sync |

### PowerShell Scripts (4)

| Script | Purpose | Plugin Points |
|--------|---------|---------------|
| **Microsoft.PowerShell_profile.ps1** | Shell profile | Module auto-loading |
| **ScriptsToolkit/AzureQuickSetup.ps1** | Azure provisioning | Azure PowerShell modules |
| **ScriptsToolkit/Remove-CodeComments.ps1** | Code cleanup | — |
| **ScriptsToolkit/ScriptsToolkit.Data.ps1** | Data utilities | — |

---

## 7.b. User-Installed Apt Packages (34 total)

| Package | Category | Notes |
|---------|----------|-------|
| **azure-cli** | Cloud | Azure management CLI |
| **calibre** | Media | E-book library management |
| **dotnet-sdk-10.0** | SDK | .NET 10 development |
| **fd-find** | Dev | Modern `find` |
| **gh** | Dev | GitHub CLI |
| **gnupg** | Security | Encryption |
| **helix (hx)** | Editor | Rust-based modern editor |
| **jq** | Data | JSON processor |
| **nnn** | TUI | Terminal file manager |
| **tealdeer** | Docs | Fast `tldr` client |
| **tmux** | TUI | Terminal multiplexer |
| base-files, bash, coreutils, curl, git, grep, etc. | System | Base system utilities |
| ubuntu-minimal, ubuntu-wsl | System | WSL2 base |

## 8. uv/uvx & Playwright Browsers

### uv/uvx — Python Package Manager

| Attribute | Detail |
|-----------|--------|
| **Version** | uv 0.11.17 (x86_64) |
| **Path** | `~/.local/bin/uv` |
| **uv tools installed** | 0 (none) |
| **uvx** | Same version, runs CLI from any Python package |
| **Available Python versions** | 3.11-3.15 (all downloadable, 3.12.3 is system) |

### Playwright State

| Component | Status | Details |
|-----------|--------|---------|
| **Python playwright pkg** | ⚠️ Missing | Not installed via pip (system Python has no pip) |
| **Playwright MCP** | ✅ v1.60.0 | Available via npx |
| **Browsers installed** | ✅ 2 | `chromium_headless_shell-1223`, `daemon` |
| **Browser cache** | ✅ | `~/.cache/ms-playwright/` |
| **Python dependency** | ❌ Not usable | System Python 3.12.3 has no pip; toolkit deps in pyproject.toml require Python ≥3.14 |

**Issue:** The Python toolkit (`pyproject.toml`) lists `playwright` and requires `python >=3.14`, but system Python is 3.12.3 with no pip. Playwright browsers are installed (chromium headless shell 1223 + daemon), but the Python package can't use them without a venv or pip.

## 9a. Modern CLI Reference — Skills & Plugin Analysis

### Tool Plugin System Comparison

| Tool | Plugin/Extension System | Theme/Config | Notes |
|------|------------------------|--------------|-------|
| **eza** | None | Icons via Nerd Font, colors, Git | Config via `~/.config/eza/` |
| **bat** | Syntax definitions | Theme via `--theme`, `bat --list-themes` | Custom language defs possible |
| **fd** | None | Config via `.fdignore`, `.gitignore` | — |
| **ripgrep** | None | Config via `.ripgreprc`, `.gitignore` | — |
| **sd** | None | None | Simple find-replace |
| **choose** | None | None | — |
| **delta** | None | Config via `~/.gitconfig`, 20+ themes | Git config sections |
| **difftastic** | Tree-sitter grammars | Config via env vars | Language parsers auto-download |
| **fzf** | Vim/tmux integration | Config via `~/.fzfrc` | Key bindings, preview |
| **atuin** | Shell integration | Config via `~/.config/atuin/config.toml` | Encrypted sync |
| **starship** | 100+ built-in modules | Config via `~/.config/starship.toml` | TOML-based modules |
| **shellharden** | None | None | CLI flags only |
| **zoxide** | Shell integration | Config via env vars | Learning database |
| **broot** | None | Config via `~/.config/broot/config.toml` | Launch params |
| **dust** | None | None | — |
| **duf** | None | None | — |
| **procs** | None | Config via `~/.config/procs/config.toml` | — |
| **btop** | Themes | Config via `~/.config/btop/btop.conf` | 50+ themes |
| **hyperfine** | None | None | — |
| **glow** | Themes | Config via `~/.config/glow/glow.yml` | Charm Cloud |
| **zellij** | WASM plugins | Config via `~/.config/zellij/config.kdl` | Layouts, themes, plugins |
| **httpie** | Plugin ecosystem | Config via `~/.config/httpie/config.json` | Auth, formatters |
| **xh** | None | Config via env vars | httpie-compatible |
| **doggo** | None | Config via `~/.config/doggo/config.toml` | DNS resolvers |
| **lazygit** | User config | Config via `~/.config/lazygit/config.yml` | Custom commands |
| **lazydocker** | None | Config via `~/.config/lazydocker/config.yml` | — |
| **jq** | None | None | — |
| **yq** | None | None | — |
| **fresh** | None | Themes, locale | Config files |
| **lsd** | Icons (Nerd Font) | Config via `~/.config/lsd/config.yaml` | Themes |
| **tmux** | Plugin system (tpm) | Config via `~/.tmux.conf` | **No plugins installed** |

### Plugin System Capabilities by Tool

| System | Plugin Count | Plugin Type | Runtime |
|--------|-------------|-------------|---------|
| **zellij** | ✅ WASM | WebAssembly | Loaded on demand |
| **httpie** | pip packages | Python | Loaded at startup |
| **bat** | Syntax defs | Sublime syntax | Compiled in |
| **difftastic** | Tree-sitter | Native grammars | Downloaded |
| **starship** | Built-in modules | TOML config | Compiled in |
| **btop** | Themes | Config | Loaded at startup |
| **lazygit** | User config | YAML | Compiled in |
| **tmux** | Shell scripts | bash | **0 installed** |
| **fzf** | Vim/tmux | VimL/Shell | Key binding config |

---

## 9b. State & Data Files

| Data Dir | Contents | Format |
|----------|----------|--------|
| `state/lastfm/scrobbles.json` | Scrobble data | JSON |
| `state/lastfm/sync.json` | Sync metadata | JSON |
| `state/youtube/sync.json` | YouTube sync state | JSON |
| `state/pristine/auth.json` | Pristine auth | JSON |
| `state/postgres/` | PostgreSQL state | — |
| `fibery/Knowledge/` | Fibery knowledge base | — |
| `fibery/Repos/` | Fibery repository data | — |

---

## 10. Summary Statistics

| Category | Count | Details |
|----------|-------|---------|
| **SDKs/Runtimes** | 5 | Rust, .NET, Node, Python (Go + pwsh missing) |
| **Rustup components** | 3 installed + 90+ available | clippy, rustfmt, docs |
| **AI CLI agents** | 7 | opencode, codex, kilocode, cline, kiro ×3 |
| **AI agent configs with MCP** | 4 | opencode, codex, kilocode, goose (all share MCP servers) |
| **Unified agent skills** | 60 | `~/.agents/skills/` with ~140 support files |
| **Goose symlinked skills** | 47/60 | Missing 13 newer skills |
| **Plugin npm packages** | 3 | @opencode-ai/plugin v1.15.13, @kilocode/plugin v7.3.16, @kilocode/sdk v7.3.16 |
| **MCP servers (primary)** | 4 | MCP_DOCKER, agentql, crawl4ai, playwright |
| **MCP servers (sub)** | 50+ | Azure, browser, firecrawl, C# dev, docs |
| **Cargo tools (installed)** | 6 | dust, lsd, tree, fresh, lite-pg, pg_cli |
| **Cargo plugins (available)** | 7+ | edit, watch, audit, expand, outdated, tarpaulin, llvm-cov |
| **System CLI tools (user installed)** | 34 apt packages | Notable: azure-cli, calibre, dotnet-10, fd, gh, helix, jq, nnn, tealdeer, tmux |
| **Azure CLI extensions** | 0 | None installed |
| **Docker plugins** | 0 | None installed |
| **Git plugins/aliases** | 0 | None installed |
| **npm global packages** | 5 | kilocode, codex, cline, npm |
| **tmux plugins** | 0 | None installed |
| **Shell frameworks** | 0 | No oh-my-zsh, oh-my-bash, or similar |
| **Python toolkit modules** | 11 | audio, video, filesystem, lastfm, etc. |
| **Python dependencies** | 16 | cyclopts, rich, ffmpeg-python, pylast, etc. |
| **Playwright browsers** | 2 | chromium_headless_shell-1223, daemon |
| **C# .NET project** | 1 | EF Core, 3 DB schemas, compiled models |
| **PowerShell scripts** | 4 | profile, Azure quickstart, code tools, data utils |
| **Modern CLI reference tools** | 30 documented | **6 installed, 24 missing** (20% coverage) |
| **Tool plugin systems** | 8 tools have plugin support | zellij (WASM), httpie (pip), starship (modules), etc. |
| **JetBrains Rider** | 2026.2 | C# IDE installed (remote dev mode) |
| **VS Code** | Latest | No AI extensions installed |

---

## 11. Tool Plugin Capability Matrix

| Tool | Has Plugin System? | Active Plugins | Max Plugins |
|------|--------------------|----------------|-------------|
| **tmux** | ✅ tpm | 0 | Unlimited |
| **zellij** | ✅ WASM | 0 (default only) | Unlimited |
| **httpie** | ✅ pip | 0 | ~50 |
| **lazygit** | ✅ user config | N/A | N/A |
| **starship** | ✅ built-in modules | 100+ | 100+ |
| **btop** | ✅ themes | 0 | 50+ |
| **bat** | ✅ syntax defs | 0 | 500+ |
| **difftastic** | ✅ tree-sitter | 0 | ~100 |
| **fresh** | ✅ themes/locale | 0 | configurable |
| **lsd** | ✅ icons/themes | 0 | configurable |

---

*Generated: June 2026. Source: live system audit of `/home/lance/Scripts/`, `~/.opencode/`, `~/.agents/`, `~/.config/`, and SDK tooling.*
