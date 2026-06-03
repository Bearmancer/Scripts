#!/usr/bin/env bash
# =============================================================================
# install_env.sh — Auto-check + auto-install all missing deps
# Idempotent: safe to re-run anytime. Detects what's missing, installs it.
# =============================================================================
set -euo pipefail

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'
ok()   { echo -e "  ${GREEN}✓${NC} $1"; }
info() { echo -e "  ${CYAN}→${NC} $1"; }
warn() { echo -e "  ${YELLOW}⚠${NC} $1"; }
fail() { echo -e "  ${RED}✗${NC} $1"; }
header() { echo -e "\n${CYAN}═══ $1 ═══${NC}"; }

STEPS_DONE=0; STEPS_TOTAL=0
COUNT_CHECK()  { STEPS_TOTAL=$((STEPS_TOTAL + 1)); }
COUNT_PASS()   { STEPS_DONE=$((STEPS_DONE + 1)); ok "$1"; }
COUNT_FAIL()   { fail "$1"; }
CHECK_CMD()    { COUNT_CHECK; if command -v "$1" &>/dev/null; then COUNT_PASS "$2"; else COUNT_FAIL "$2"; return 1; fi; }
add_bashrc_source() {
  local marker="$1" line="$2"
  grep -qF "$marker" "$HOME/.bashrc" 2>/dev/null && return
  echo "$line" >> "$HOME/.bashrc"
  info "Added to .bashrc: $marker"
}

trap 'echo -e "\n${RED}Aborted at step ${STEPS_DONE}/${STEPS_TOTAL}${NC}"; exit 1' INT

echo -e "${CYAN}══════════════════════════════════════════════════════════════${NC}"
echo -e "${CYAN}  Auto-Env Bootstrap : WSL2/Ubuntu Dev Environment Installer${NC}"
echo -e "${CYAN}══════════════════════════════════════════════════════════════${NC}"

# ============================== PACKAGE MANAGERS =============================

header "1. System Package Managers"
# Check if we have passwordless sudo or interactive terminal
if sudo -n true 2>/dev/null; then
  sudo apt-get update -qq && sudo apt-get install -y -qq \
    build-essential curl wget git unzip pkg-config libssl-dev \
    python3 python3-pip python3-venv \
    >/dev/null 2>&1 && ok "apt packages (build-essential, git, python3, etc)"
elif [ -t 0 ]; then
  sudo apt-get update -qq && sudo apt-get install -y -qq \
    build-essential curl wget git unzip pkg-config libssl-dev \
    python3 python3-pip python3-venv \
    >/dev/null 2>&1 && ok "apt packages (build-essential, git, python3, etc)"
else
  warn "Non-interactive shell — skipping sudo apt. Run manually: sudo apt-get install -y build-essential curl wget git unzip pkg-config libssl-dev python3 python3-pip python3-venv"
fi

CHECK_CMD cargo "cargo (Rust) already installed" || {
  info "Installing Rust via rustup..."
  curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y --no-modify-path >/dev/null 2>&1
  . "$HOME/.cargo/env"
  CHECK_CMD cargo "cargo installed successfully" || true
}

CHECK_CMD go "go (Golang) already installed" || {
  info "Installing Go 1.24..."
  GOFILE=$(curl -sL https://go.dev/dl/ | grep -oP 'go1\.24\.[0-9]+\.linux-amd64\.tar\.gz' | head -1)
  [ -z "$GOFILE" ] && GOFILE="go1.24.4.linux-amd64.tar.gz"
  curl -fsSL "https://go.dev/dl/$GOFILE" -o /tmp/go.tar.gz
  if sudo -n true 2>/dev/null || [ -t 0 ]; then
    sudo rm -rf /usr/local/go && sudo tar -C /usr/local -xzf /tmp/go.tar.gz
  else
    warn "No sudo — extracting Go to $HOME/.local/go instead of /usr/local/go"
    mkdir -p "$HOME/.local"
    rm -rf "$HOME/.local/go"
    tar -C "$HOME/.local" -xzf /tmp/go.tar.gz
    export PATH="$HOME/.local/go/bin:$PATH"
    add_bashrc_source "# --- auto-env: go" 'export PATH="$HOME/.local/go/bin:$PATH"'
  fi
  rm /tmp/go.tar.gz
  add_bashrc_source "# --- auto-env: go path" 'export PATH=$PATH:/usr/local/go/bin'
  export PATH=$PATH:/usr/local/go/bin
  CHECK_CMD go "go installed successfully" || true
}

CHECK_CMD pwsh "pwsh (PowerShell 7) already installed" || {
  info "Installing PowerShell 7..."
  PWVER="7.5.0"
  curl -fsSL "https://github.com/PowerShell/PowerShell/releases/download/v${PWVER}/powershell-${PWVER}-linux-x64.tar.gz" -o /tmp/pwsh.tar.gz
  mkdir -p "$HOME/.local/pwsh"
  tar -xzf /tmp/pwsh.tar.gz -C "$HOME/.local/pwsh"
  rm /tmp/pwsh.tar.gz
  chmod +x "$HOME/.local/pwsh/pwsh"
  add_bashrc_source "# --- auto-env: pwsh" 'export PATH="$HOME/.local/pwsh:$PATH"'
  export PATH="$HOME/.local/pwsh:$PATH"
  CHECK_CMD pwsh "pwsh installed successfully (no sudo)" || true
}

CHECK_CMD uv "uv (Python package manager) already installed" || {
  info "Installing uv..."
  curl -fsSL https://astral.sh/uv/install.sh | sh >/dev/null 2>&1
  export PATH="$HOME/.local/bin:$PATH"
  CHECK_CMD uv "uv installed successfully" || true
}

CHECK_CMD node "node already installed" || {
  if sudo -n true 2>/dev/null || [ -t 0 ]; then
    info "Installing Node.js 22 LTS via NodeSource..."
    curl -fsSL https://deb.nodesource.com/setup_22.x | sudo -E bash - >/dev/null 2>&1
    sudo apt-get install -y -qq nodejs >/dev/null 2>&1
  else
    info "Installing Node.js 22 via tarball (no sudo)..."
    curl -fsSL https://nodejs.org/dist/v22.14.0/node-v22.14.0-linux-x64.tar.xz -o /tmp/node.tar.xz
    tar -xf /tmp/node.tar.xz -C /tmp/
    mkdir -p "$HOME/.local/node"
    cp -r /tmp/node-v22.14.0-linux-x64/* "$HOME/.local/node/"
    rm -rf /tmp/node-v22.14.0-linux-x64 /tmp/node.tar.xz
    add_bashrc_source "# --- auto-env: node" 'export PATH="$HOME/.local/node/bin:$PATH"'
    export PATH="$HOME/.local/node/bin:$PATH"
  fi
  CHECK_CMD node "node installed successfully" || true
}

# ============================= CLI TOOLS — CARGO =============================

CARGO_TOOLS=(
  "zellij:zellij"
  "lazygit:lazygit"
  "dusage:dusage"
  "xh:xh"
  "dog:dog"
  "yq:yq"
  "bat:bat"
  "ripgrep:rg"
  "du-dust:dust"
  "lsd:lsd"
  "fd-find:fd"
  "procs:procs"
  "bottom:btm"
  "mcfly:mcfly"
  "starship:starship"
  "tealdeer:tldr"
  "zoxide:zoxide"
)

header "2. Rust/Cargo CLI Tools"
. "$HOME/.cargo/env" 2>/dev/null || true
for entry in "${CARGO_TOOLS[@]}"; do
  PKG="${entry%%:*}"
  CMD="${entry##*:}"
  CHECK_CMD "$CMD" "$CMD already installed" || {
    info "cargo install $PKG ..."
    cargo install "$PKG" --quiet >/dev/null 2>&1 && COUNT_PASS "$CMD installed" || COUNT_FAIL "cargo install $PKG failed"
  }
done

# Tools using cargo-binstall if available (saves compile time)
for pkg in "cargo-binstall:cargo-binstall" "cargo-update:cargo-install-update" "cargo-edit:cargo-edit"; do
  PKG="${pkg%%:*}"
  CMD="${pkg##*:}"
  CHECK_CMD "$CMD" "$CMD already installed" || {
    cargo install "$PKG" --quiet >/dev/null 2>&1 && ok "$CMD installed" || true
  }
done

CHECK_CMD just "just already installed" || {
  cargo install just --quiet >/dev/null 2>&1 && ok "just installed" || true
}

# fzf — go binary, not cargo
CHECK_CMD fzf "fzf already installed" || {
  info "Installing fzf via git..."
  git clone --depth 1 https://github.com/junegunn/fzf.git ~/.fzf >/dev/null 2>&1
  ~/.fzf/install --all --no-bash --no-fish --no-zsh >/dev/null 2>&1
  CHECK_CMD fzf "fzf installed" || true
}

# ============================= CLI TOOLS — GO ================================

header "3. Go-based CLI Tools"
export PATH=$PATH:/usr/local/go/bin:$HOME/go/bin:$HOME/.local/go/bin
CHECK_CMD lazydocker "lazydocker already installed" || {
  go install github.com/jesseduffield/lazydocker@latest >/dev/null 2>&1 && ok "lazydocker installed" || true
}

# ============================= CLI TOOLS — PIP ===============================

header "4. Python CLI Tools"
CHECK_CMD httpie "httpie already installed" || {
  pip3 install --user --quiet httpie >/dev/null 2>&1 && ok "httpie installed" || true
}

CHECK_CMD ruff "ruff already installed" || {
  pip3 install --user --quiet ruff >/dev/null 2>&1 && ok "ruff installed" || true
}

CHECK_CMD shellharden "shellharden already installed" || {
  info "Installing shellharden via cargo..."
  cargo install shellharden --quiet >/dev/null 2>&1 && ok "shellharden installed" || true
}

# ============================= LSPs ==========================================

header "5. Language Servers (LSPs)"
CHECK_CMD gopls "gopls already installed" || {
  go install golang.org/x/tools/gopls@latest >/dev/null 2>&1 && ok "gopls installed" || true
}

CHECK_CMD golangci-lint "golangci-lint already installed" || {
  curl -fsSL https://raw.githubusercontent.com/golangci/golangci-lint/master/install.sh | sh -s -- -b "$HOME/go/bin" >/dev/null 2>&1 && ok "golangci-lint installed" || true
}

CHECK_CMD pyright "pyright already installed" || {
  npm install -g pyright >/dev/null 2>&1 && ok "pyright installed" || true
}

export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
CHECK_CMD dotnet "dotnet SDK already installed" || {
  info "Installing .NET SDK 9.0..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 9.0 >/dev/null 2>&1
  add_bashrc_source "# --- auto-env: dotnet" 'export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"'
  export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
  CHECK_CMD dotnet "dotnet SDK ready" || true
}
CHECK_CMD "omnisharp" "omnisharp already installed" || {
  dotnet tool install --global omnisharp >/dev/null 2>&1 && ok "omnisharp installed" || warn "omnisharp install failed (try: dotnet tool install --global omnisharp)"
}

CHECK_CMD "pylsp" "python-lsp-server already installed" || {
  pip3 install --user --quiet python-lsp-server >/dev/null 2>&1 && ok "pylsp installed" || true
}

CHECK_CMD "PowerShellEditorServices" "PSES already installed" || {
  pwsh -NoProfile -Command "Install-Module -Name PowerShellEditorServices -Force -SkipPublisherCheck -Scope CurrentUser" >/dev/null 2>&1 && ok "PSES installed" || warn "PSES install failed (pwsh may not be in PATH yet)"
}

# ============================= PLAYWRIGHT ====================================

header "6. Playwright (Python venv)"
PW_VENV="$HOME/playwright-venv"
if [ -f "$PW_VENV/bin/python3" ] && "$PW_VENV/bin/python3" -c "import playwright" 2>/dev/null; then
  ok "Playwright venv ready"
else
  info "Setting up Playwright in $PW_VENV ..."
  python3 -m venv "$PW_VENV"
  "$PW_VENV/bin/pip3" install --quiet playwright >/dev/null 2>&1
  "$PW_VENV/bin/python3" -m playwright install chromium >/dev/null 2>&1
  ok "Playwright venv created + chromium installed"
fi
CHECK_CMD agy "agy already in ~/.local/bin" || {
  warn "agy binary not found (expected ELF at ~/.local/bin/agy) — check if it's from a distro package"
}

# ============================= TMUX PLUGINS ==================================

header "7. Tmux Plugins (tpm)"
TPM_DIR="$HOME/.tmux/plugins/tpm"
if [ -d "$TPM_DIR" ]; then
  ok "tpm already installed"
  if [ -d "$TPM_DIR/.git" ]; then
    info "Updating tpm ..."
    git -C "$TPM_DIR" pull --ff-only --quiet >/dev/null 2>&1 || true
  fi
else
  info "Installing tpm ..."
  git clone --depth 1 https://github.com/tmux-plugins/tpm "$TPM_DIR" >/dev/null 2>&1
  ok "tpm installed"
fi
# Install tpm plugins via tpm's install script
if [ -f "$TPM_DIR/bin/install_plugins" ]; then
  info "Installing tmux plugins (tpm) ..."
  TMUX_PLUGIN_MANAGER_PATH="$HOME/.tmux/plugins" bash "$TPM_DIR/bin/install_plugins" >/dev/null 2>&1 || true
  ok "tmux plugins installed via tpm"
fi

# ============================= SHELL CONFIG ==================================

header "8. Shell Config (.bashrc)"
add_bashrc_source "# --- auto-env: PATH ---" 'export PATH="$HOME/.local/bin:$HOME/go/bin:$PATH"'
add_bashrc_source "# --- auto-env: cargo"   '. "$HOME/.cargo/env"'
add_bashrc_source "# --- auto-env: starship" 'eval "$(starship init bash)"'
add_bashrc_source "# --- auto-env: zoxide"   'eval "$(zoxide init bash)"'
add_bashrc_source "# --- auto-env: atuin"    'eval "$(atuin init bash)"' 2>/dev/null || true
ok "Shell config sources verified"

# ============================= NPM GLOBAL PACKAGES ===========================

header "9. NPM Global Packages"
for pkg in "@kilocode/cli" "@openai/codex" "cline" "@opencode-ai/plugin"; do
  if npm list -g --depth=0 "$pkg" 2>/dev/null | grep -q "$pkg"; then
    ok "$pkg already installed"
  else
    info "npm install -g $pkg ..."
    npm install -g "$pkg" --quiet >/dev/null 2>&1 && ok "$pkg installed" || warn "$pkg install failed"
  fi
done

# ============================= GOOSE SKILLS SYNC =============================

header "10. Goose Skills Sync (canonical → goose)"
GOOSE_SKILLS_DIR="$HOME/.config/goose/skills"
CANONICAL_SKILLS_DIR="$HOME/.agents/skills"
mkdir -p "$GOOSE_SKILLS_DIR"
SYNCED=0; MISSING_SKILLS=0
declare -a GOOSE_SKILL_NAMES=(
  a11y-audit archive ask autopilot brainstorming cancel caveman caveman-commit caveman-compress
  caveman-help caveman-review caveman-stats code-review compliance-check configure-notifications
  context-optimize cost customize-opencode debug debug-workflow deep-interview delegation
  design-dialogue design-system dispatching-parallel-agents doctor execute executing-plans
  execution find-skills finishing-a-development-branch handoff help hud-setup implementation-planning
  learn orchestrate plan prd ralplan receiving-code-review requesting-code-review resume-session
  review review-code session-management sessions status subagent-driven-development
  systematic-debugging team test-driven-development using-superpowers validation
  verification-before-completion verify wait writing-plans writing-skills
)
for skill in "${GOOSE_SKILL_NAMES[@]}"; do
  CANON="$CANONICAL_SKILLS_DIR/$skill"
  TARGET="$GOOSE_SKILLS_DIR/$skill"
  if [ -L "$TARGET" ] && [ "$(readlink -f "$TARGET")" = "$(readlink -f "$CANON")" ]; then
    : # already synced
  elif [ -d "$CANON" ]; then
    ln -sfn "$CANON" "$TARGET" 2>/dev/null
    SYNCED=$((SYNCED + 1))
  else
    MISSING_SKILLS=$((MISSING_SKILLS + 1))
  fi
done
[ "$SYNCED" -gt 0 ] && info "Synced $SYNCED skills from canonical to goose" || true
[ "$MISSING_SKILLS" -gt 0 ] && warn "$MISSING_SKILLS skills referenced but missing from canonical repo" || true
ok "Goose skills check complete"

# ============================= FINAL REPORT ==================================

header "FINAL STATUS — Installed Commands"
declare -a FINAL_CHECK_LIST=(
  "cargo:rustup/rustc"
  "go:go"
  "pwsh:pwsh"
  "uv:uv"
  "node:node"
  "zellij:zellij"
  "lazygit:lazygit"
  "dusage:dusage"
  "xh:xh"
  "bat:bat"
  "rg:rg"
  "dust:dust"
  "lsd:lsd"
  "fd:fd"
  "procs:procs"
  "btm:btm"
  "mcfly:mcfly"
  "starship:starship"
  "zoxide:zoxide"
  "tldr:tldr"
  "fzf:fzf"
  "just:just"
  "lazydocker:lazydocker"
  "gopls:gopls"
  "golangci-lint:golangci-lint"
  "pyright:pyright"
  "pylsp:pylsp"
  "httpie:httpie"
  "ruff:ruff"
  "shellharden:shellharden"
  "dog:dogdoggo binary"
  "yq:yq"
  "atuin:atuin"
)
INSTALLED=0; MISSING=0
for entry in "${FINAL_CHECK_LIST[@]}"; do
  CMD="${entry%%:*}"
  LABEL="${entry##*:}"
  if command -v "$CMD" &>/dev/null; then
    echo -e "  ${GREEN}✓${NC} $LABEL"
    INSTALLED=$((INSTALLED + 1))
  else
    WS="         "; SHORT="${LABEL:0:25}"
    echo -e "  ${RED}✗${NC} $SHORT → not in PATH"
    MISSING=$((MISSING + 1))
  fi
done

echo -e "\n${CYAN}══════════════════════════════════════════════════════════════${NC}"
echo -e "  ${GREEN}Installed: $INSTALLED  ${RED}Missing: $MISSING${NC}"
echo -e "  ${YELLOW}Next: open a new terminal, or run: source ~/.bashrc${NC}"
echo -e "${CYAN}══════════════════════════════════════════════════════════════${NC}"
