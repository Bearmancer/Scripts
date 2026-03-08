#!/usr/bin/env bash
set -euo pipefail

# ── System packages ───────────────────────────────────────────────────────────
sudo apt update
sudo apt install -y build-essential cmake git libsndfile1-dev ffmpeg sox python3-pip python3-venv

# ── Build sacd_extract from source ────────────────────────────────────────────
SACD_REPO=/tmp/sacd-ripper
if [ ! -f /usr/local/bin/sacd_extract ]; then
    git clone https://github.com/sacd-ripper/sacd-ripper.git "$SACD_REPO"
    cmake -S "$SACD_REPO" -B "$SACD_REPO/build"
    cmake --build "$SACD_REPO/build" -j "$(nproc)"
    sudo install -m 755 "$SACD_REPO/build/tools/sacd_extract/sacd_extract" /usr/local/bin/
    rm -rf "$SACD_REPO"
fi

# ── Python dependencies ──────────────────────────────────────────────────────
python3 -m pip install --user ffmpeg-python chardet deflacue pathvalidate

# ── Verify ────────────────────────────────────────────────────────────────────
echo "=== Verification ==="
sacd_extract --help | head -1
ffmpeg -version | head -1
python3 -c "import ffmpeg; print('ffmpeg-python OK')"
python3 -c "import chardet; print('chardet OK')"
python3 -c "import deflacue; print('deflacue OK')"
python3 -c "import pathvalidate; print('pathvalidate OK')"
echo "=== Setup complete ==="
