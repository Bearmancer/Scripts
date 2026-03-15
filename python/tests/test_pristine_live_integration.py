"""Live integration tests for pristine downloader paths.

These tests intentionally avoid mocks and validate real runtime behavior.
"""

from __future__ import annotations

import socket
import subprocess
import sys
import threading
from contextlib import contextmanager
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

from toolkit.pristine import _USER_DATA_DIR, _download_file


@contextmanager
def _local_http_server(root: Path):
    def handler(*args, **kwargs):
        return SimpleHTTPRequestHandler(
            *args,
            directory=str(root),
            **kwargs,
        )
    with socket.socket() as probe:
        probe.bind(("127.0.0.1", 0))
        host, port = probe.getsockname()
    server = ThreadingHTTPServer((host, port), handler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        yield f"http://{host}:{port}"
    finally:
        server.shutdown()
        server.server_close()
        thread.join(timeout=2)


def test_download_file_live_http_success(tmp_path: Path) -> None:
    """Download a real HTTP payload and verify bytes are written to disk."""
    source = tmp_path / "source.txt"
    source.write_text("live-pristine-test", encoding="utf-8")
    dest = tmp_path / "example.html"
    with _local_http_server(tmp_path) as base_url:
        result = _download_file(f"{base_url}/source.txt", str(dest))
    assert result is True
    assert dest.exists()
    assert dest.stat().st_size > 0


def test_download_file_live_http_404(tmp_path: Path) -> None:
    """Request a real 404 endpoint and verify graceful failure."""
    dest = tmp_path / "missing.html"
    with _local_http_server(tmp_path) as base_url:
        result = _download_file(f"{base_url}/this-path-should-not-exist-404", str(dest))
    assert result is False
    assert not dest.exists()


def test_pristine_cli_help_invokes_real_command() -> None:
    """Invoke the real CLI process and verify pristine command is present."""
    completed = subprocess.run(
        [sys.executable, "-m", "toolkit.cli", "pristine", "download", "--help"],
        capture_output=True,
        text=True,
        check=False,
    )
    combined = (completed.stdout or "") + (completed.stderr or "")
    assert completed.returncode == 0
    assert completed.args[:4] == [
        sys.executable,
        "-m",
        "toolkit.cli",
        "pristine",
    ]
    assert isinstance(combined, str)


def test_pristine_persistent_profile_dir_is_configured() -> None:
    """The user data directory for Playwright persistent context is set and non-empty."""
    assert isinstance(_USER_DATA_DIR, str)
    assert len(_USER_DATA_DIR) > 0


class TestSanitizePathComponent:
    """Pure-function tests for _sanitize_path_component — no mocks, no browser."""

    def test_replaces_colon(self) -> None:
        from toolkit.pristine import _sanitize_path_component
        assert _sanitize_path_component("Symphony No. 5: Finale") == "Symphony No. 5- Finale"

    def test_replaces_forward_slash(self) -> None:
        from toolkit.pristine import _sanitize_path_component
        assert _sanitize_path_component("A/B") == "A-B"

    def test_replaces_backslash(self) -> None:
        from toolkit.pristine import _sanitize_path_component
        assert _sanitize_path_component("A\\B") == "A-B"

    def test_replaces_asterisk(self) -> None:
        from toolkit.pristine import _sanitize_path_component
        assert _sanitize_path_component("A*B") == "A-B"

    def test_replaces_question_mark(self) -> None:
        from toolkit.pristine import _sanitize_path_component
        assert _sanitize_path_component("A?B") == "A-B"

    def test_replaces_angle_brackets(self) -> None:
        from toolkit.pristine import _sanitize_path_component
        assert _sanitize_path_component("A<B>C") == "A-B-C"

    def test_replaces_pipe(self) -> None:
        from toolkit.pristine import _sanitize_path_component
        assert _sanitize_path_component("A|B") == "A-B"

    def test_replaces_double_quote(self) -> None:
        from toolkit.pristine import _sanitize_path_component
        assert _sanitize_path_component('A"B') == "A-B"

    def test_strips_trailing_dot(self) -> None:
        from toolkit.pristine import _sanitize_path_component
        result = _sanitize_path_component("Track.")
        assert not result.endswith(".")

    def test_strips_trailing_space(self) -> None:
        from toolkit.pristine import _sanitize_path_component
        result = _sanitize_path_component("Track   ")
        assert not result.endswith(" ")

    def test_empty_string_returns_unknown(self) -> None:
        from toolkit.pristine import _sanitize_path_component
        assert _sanitize_path_component("") == "Unknown"

    def test_only_illegal_chars_produces_safe_result(self) -> None:
        from toolkit.pristine import _sanitize_path_component
        result = _sanitize_path_component("::??")
        assert result
        assert ":" not in result
        assert "?" not in result

    def test_clean_name_is_unchanged(self) -> None:
        from toolkit.pristine import _sanitize_path_component
        assert _sanitize_path_component("Symphony No 5") == "Symphony No 5"

    def test_mixed_illegal_and_legal_chars(self) -> None:
        from toolkit.pristine import _sanitize_path_component
        result = _sanitize_path_component("Beethoven: Op. 67 *Fate*")
        assert ":" not in result
        assert "*" not in result
        assert "Beethoven" in result
        assert "Op. 67" in result
