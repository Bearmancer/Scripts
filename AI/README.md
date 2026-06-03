# Scripts — System Setup Reference

> Quick-reference for installing and configuring the full development stack across WSL2 (Ubuntu) and Windows.

## What's Here

| File | Purpose |
|------|---------|
| `system_inventory.md` | Full audit of installed SDKs, AI agents, MCP servers, CLI tools, plugins, and custom apps |
| `schema_mapping.md` | PostgreSQL database schema mapping for YouTube, Fibery, Last.fm |
| `schema_visual.mmd` | Mermaid ERD for all 3 databases |
| `cli_tools_reference.md` | 21 modern CLI/TUI tools with essays, install commands, comparison tables |

## Setup Scripts

| Script | Platform | What It Installs |
|--------|----------|------------------|
| `../install_env.sh` | Linux/WSL2 (bash) | SDKs (Go, pwsh), 20+ missing CLI tools, cargo plugins, tmux plugins, shell config |
| `../powershell/Install-Env.ps1` | Windows (pwsh) | Windows-native tools via winget/choco, VS Code extensions, symlinks |

## Install Order

```
1. install_env.sh       → Linux: SDKs, CLI tools, shell config
2. Install-Env.ps1      → Windows: winget packages, VS Code extensions
```

---

## SDKs

| SDK | Status | Install |
|-----|--------|---------|
| Rust 1.96 + cargo | ✅ | `curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs \| sh` |
| .NET 10.0 SDK | ✅ | `apt install dotnet-sdk-10.0` |
| **Go** | ❌ | `wget https://go.dev/dl/go1.24.linux-amd64.tar.gz && rm -rf /usr/local/go && tar -C /usr/local -xzf go1.24.linux-amd64.tar.gz` |
| **PowerShell (pwsh)** | ❌ | `wget https://github.com/PowerShell/PowerShell/releases/download/v7.5.0/powershell_7.5.0-1.deb_amd64.deb && dpkg -i powershell_7.5.0-1.deb_amd64.deb` |
| Node 26.2 + npm | ✅ | nvm |
| Python 3.12 | ✅ | system |

---

## CLI Tools — Status Overview

### Installed (referenced, 12)
dust, lsd, tree, fresh, jq, fd-find, tmux, az, helix, nnn, tealdeer, gh

### Missing (24)
bat, eza, ripgrep, fzf, yq, delta, difftastic, lazygit, lazydocker, zoxide, broot, duf, procs, btop, hyperfine, glow, zellij, httpie, xh, doggo, atuin, starship, shellharden, sd

---

## AI Agents & MCP

```
opencode v1.15.13   ← primary
├── 60 skills from ~/.agents/skills/
├── MCP_DOCKER, agentql, crawl4ai, playwright
└── plugins: @opencode-ai/plugin v1.15.13, @kilocode/plugin v7.3.16

codex v0.136.0      ← OpenAI agent
├── same 4 MCP servers
├── GPT-5.4-mini, personality: pragmatic
└── goals DB, logs DB, history

kilocode v7.3.21    ← KiloCode agent
├── same 4 MCP servers, plugin SDK
└── ~/.local/share/kilo/kilo.db

goose (installed)   ← Block/Goose agent
├── 47 shared skills (symlinks to ~/.agents/skills/)
└── 1 MCP server (MCP_DOCKER only)

cline v3.0.15       ← Cline agent (no local config)
kiro-cli            ← Kiro AI terminal (3 binaries)
```

---

## System Paths

```
~/.cargo/bin/           ← Rust/cargo tools (dust, lsd, etc.)
~/.local/bin/           ← Local scripts (uv, aws, kiro, agy)
~/.opencode/bin/        ← opencode binary (145MB)
~/.nvm/versions/node/   ← Node.js via nvm
/usr/lib/dotnet/        ← .NET SDK 10.0
/usr/bin/               ← System tools (jq, fd, tmux, helix, etc.)
~/.config/opencode/     ← opencode config + MCP servers
~/.config/kilo/         ← KiloCode config (mirrors opencode MCP)
~/.codex/               ← Codex config + DBs
~/.config/goose/        ← Goose config + skills symlinks
~/.agents/skills/       ← Canonical skill repo (60 shared skills)
~/.cache/ms-playwright/ ← Playwright browsers (chromium, daemon)
~/.local/share/opencode/← opencode sessions, snapshots, repos
~/.local/share/kilo/    ← KiloCode sessions, snapshots, DB
```

---

## Quick Install One-Liners

```bash
# Cargo (fastest for Rust tools)
cargo install bat eza ripgrep fd-find fzf git-delta difftastic \
  du-dust duf procs btop hyperfine glow zellij zoxide broot \
  shellharden sd choose xh doggo

# Go (requires Go SDK)
go install github.com/jesseduffield/lazygit@latest
go install github.com/jesseduffield/lazydocker@latest
go install github.com/mikefarah/yq/v4@latest

# Pip
pip install httpie

# Curl
curl --proto '=https' --tlsv1.2 -sSf https://sh.atuin.io | sh
curl -sS https://starship.rs/install.sh | sh

# Apt
apt install bat fd-find ripgrep fzf jq yq git-delta tmux zellij glow zoxide

# Cargo plugins
cargo install cargo-watch cargo-audit cargo-expand cargo-outdated
cargo install cargo-tarpaulin cargo-llvm-cov
```
