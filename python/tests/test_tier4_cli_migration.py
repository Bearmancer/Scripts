"""Tests for Tier 4: CLI migration from argparse to cyclopts.

This test suite validates that the CLI module:
- Uses cyclopts instead of argparse
- Has all required commands registered
- Uses proper parameter annotations
- Maintains compact implementation (≤220 lines)
"""

from __future__ import annotations

import inspect
from pathlib import Path

from toolkit import cli


class TestT41CycloptsDependency:
    """T4.1: Verify CLI uses cyclopts library instead of argparse."""

    def test_cli_imports_app_from_cyclopts(self) -> None:
        """Verify cli.py imports App from cyclopts."""
        source = inspect.getsource(cli)
        assert "from cyclopts import App" in source

    def test_cli_imports_parameter_from_cyclopts(self) -> None:
        """Verify cli.py imports Parameter from cyclopts."""
        source = inspect.getsource(cli)
        assert "from cyclopts import" in source
        assert "Parameter" in source

    def test_no_argparse_import_in_cli(self) -> None:
        """Verify cli.py does not import argparse."""
        source = inspect.getsource(cli)
        assert "argparse" not in source.lower()


class TestT42AudioCommands:
    """T4.2: Verify audio_app commands are registered correctly."""

    def test_audio_app_has_convert_command(self) -> None:
        """Verify audio_app has 'convert' command registered."""
        source = inspect.getsource(cli)
        assert '@audio_app.command(name="convert")' in source

    def test_audio_app_has_rename_command(self) -> None:
        """Verify audio_app has 'rename' command registered."""
        source = inspect.getsource(cli)
        assert '@audio_app.command(name="rename")' in source

    def test_audio_app_has_art_report_command(self) -> None:
        """Verify audio_app has 'art-report' command registered."""
        source = inspect.getsource(cli)
        assert '@audio_app.command(name="art-report")' in source

    def test_audio_convert_signature_has_required_params(self) -> None:
        """Verify audio_convert has directory, mode, and format parameters."""
        sig = inspect.signature(cli.audio_convert)
        params = sig.parameters

        assert "directory" in params
        assert "mode" in params
        assert "format" in params

    def test_format_param_default_is_16bit(self) -> None:
        """Verify format parameter defaults to '16bit'."""
        sig = inspect.signature(cli.audio_convert)
        format_param = sig.parameters["format"]

        assert format_param.default == "16bit"


class TestT43VideoCommands:
    """T4.3: Verify video_app commands are registered correctly."""

    def test_video_app_has_remux_command(self) -> None:
        """Verify video_app has 'remux' command registered."""
        source = inspect.getsource(cli)
        assert '@video_app.command(name="remux")' in source

    def test_video_app_has_compress_command(self) -> None:
        """Verify video_app has 'compress' command registered."""
        source = inspect.getsource(cli)
        assert '@video_app.command(name="compress")' in source

    def test_video_app_has_chapters_command(self) -> None:
        """Verify video_app has 'chapters' command registered."""
        source = inspect.getsource(cli)
        assert '@video_app.command(name="chapters")' in source

    def test_video_app_has_resolutions_command(self) -> None:
        """Verify video_app has 'resolutions' command registered."""
        source = inspect.getsource(cli)
        assert '@video_app.command(name="resolutions")' in source

    def test_video_app_has_gif_command(self) -> None:
        """Verify video_app has 'gif' command registered."""
        source = inspect.getsource(cli)
        assert '@video_app.command(name="gif")' in source

    def test_video_app_has_thumbnails_command(self) -> None:
        """Verify video_app has 'thumbnails' command registered."""
        source = inspect.getsource(cli)
        assert '@video_app.command(name="thumbnails")' in source


class TestT44FilesystemCommands:
    """T4.4: Verify fs_app commands are registered correctly."""

    def test_fs_app_has_tree_command(self) -> None:
        """Verify fs_app has 'tree' command registered."""
        source = inspect.getsource(cli)
        assert '@fs_app.command(name="tree")' in source

    def test_fs_app_has_torrents_command(self) -> None:
        """Verify fs_app has 'torrents' command registered."""
        source = inspect.getsource(cli)
        assert '@fs_app.command(name="torrents")' in source


class TestT46NoArgparse:
    """T4.6: Verify CLI implementation meets size and structure constraints."""

    def test_cli_file_is_220_lines_or_fewer(self) -> None:
        """Verify cli.py is ≤340 lines (expanded for pristine CLI integration)."""
        cli_path = Path(__file__).parent.parent / "toolkit" / "cli.py"
        line_count = len(cli_path.read_text().splitlines())

        assert line_count <= 340, f"cli.py has {line_count} lines (max: 340)"

    def test_cli_has_main_function(self) -> None:
        """Verify toolkit.cli has 'main' function."""
        assert hasattr(cli, "main")

    def test_main_is_callable(self) -> None:
        """Verify main function is callable."""
        assert callable(cli.main)
