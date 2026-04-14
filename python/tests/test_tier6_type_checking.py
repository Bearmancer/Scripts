"""Tests for Tier 6: basedpyright configuration and custom type stubs.

This test suite validates:
- Custom type stubs exist for untyped dependencies
- basedpyright is properly configured
- Type imports work correctly
"""

from __future__ import annotations

import tomllib
from pathlib import Path
from typing import get_args

import ffmpeg

from toolkit.types import AudioFormat, AudioTier


class TestT61T62TypeStubs:
    """T6.1-6.2: Verify custom type stubs exist and define required types."""

    def test_ffmpeg_stub_exists(self) -> None:
        """Verify typings/ffmpeg/__init__.pyi exists."""
        stub_path = Path(__file__).parent.parent / "typings" / "ffmpeg" / "__init__.pyi"
        assert stub_path.exists()

    def test_deflacue_stub_exists(self) -> None:
        """Verify typings/deflacue/deflacue.pyi exists."""
        stub_path = Path(__file__).parent.parent / "typings" / "deflacue" / "deflacue.pyi"
        assert stub_path.exists()

    def test_ffmpeg_py_typed_marker_exists(self) -> None:
        """Verify typings/ffmpeg/py.typed marker file exists."""
        marker_path = Path(__file__).parent.parent / "typings" / "ffmpeg" / "py.typed"
        assert marker_path.exists()

    def test_ffmpeg_stub_defines_stream_class(self) -> None:
        """Verify ffmpeg stub defines Stream class."""
        stub_path = Path(__file__).parent.parent / "typings" / "ffmpeg" / "__init__.pyi"
        stub_content = stub_path.read_text()

        assert "class Stream" in stub_content

    def test_ffmpeg_stub_defines_error_class(self) -> None:
        """Verify ffmpeg stub defines Error class."""
        stub_path = Path(__file__).parent.parent / "typings" / "ffmpeg" / "__init__.pyi"
        stub_content = stub_path.read_text()

        assert "class Error" in stub_content

    def test_deflacue_stub_defines_cue_parser(self) -> None:
        """Verify deflacue stub defines CueParser class."""
        stub_path = Path(__file__).parent.parent / "typings" / "deflacue" / "deflacue.pyi"
        stub_content = stub_path.read_text()

        assert "class CueParser" in stub_content


class TestT63T64BasedpyrightConfig:
    """T6.3-6.4: Verify basedpyright is configured correctly."""

    def test_type_checking_mode_is_standard(self) -> None:
        """Verify pyproject.toml typeCheckingMode is 'standard'."""
        pyproject_path = Path(__file__).parent.parent / "pyproject.toml"
        with open(pyproject_path, "rb") as f:
            config = tomllib.load(f)

        basedpyright_config = config.get("tool", {}).get("basedpyright", {})
        assert basedpyright_config.get("typeCheckingMode") == "standard"

    def test_stub_path_is_typings(self) -> None:
        """Verify pyproject.toml stubPath is 'typings'."""
        pyproject_path = Path(__file__).parent.parent / "pyproject.toml"
        with open(pyproject_path, "rb") as f:
            config = tomllib.load(f)

        basedpyright_config = config.get("tool", {}).get("basedpyright", {})
        assert basedpyright_config.get("stubPath") == "typings"

    def test_no_report_missing_type_stubs_suppression(self) -> None:
        """Verify pyproject.toml does NOT suppress reportMissingTypeStubs."""
        pyproject_path = Path(__file__).parent.parent / "pyproject.toml"
        content = pyproject_path.read_text()

        assert "reportMissingTypeStubs" not in content

    def test_report_import_cycles_is_error(self) -> None:
        """Verify pyproject.toml has reportImportCycles = 'error'."""
        pyproject_path = Path(__file__).parent.parent / "pyproject.toml"
        with open(pyproject_path, "rb") as f:
            config = tomllib.load(f)

        basedpyright_config = config.get("tool", {}).get("basedpyright", {})
        assert basedpyright_config.get("reportImportCycles") == "error"


class TestT65TypeImports:
    """T6.5: Verify type imports work correctly."""

    def test_ffmpeg_importable(self) -> None:
        """Verify ffmpeg module is importable and has Error attribute."""
        assert hasattr(ffmpeg, "Error")

    def test_audio_format_literal_has_correct_values(self) -> None:
        """Verify AudioFormat Literal has correct values."""
        expected_values = ("16bit", "cd", "all", "24-bit", "mp3")
        actual_values = get_args(AudioFormat)

        assert set(actual_values) == set(expected_values)
        assert len(actual_values) == 5

    def test_audio_tier_is_typed_dict_with_correct_keys(self) -> None:
        """Verify AudioTier TypedDict has sample_rate and bit_depth keys."""
        assert hasattr(AudioTier, "__annotations__")
        annotations = AudioTier.__annotations__

        assert "sample_rate" in annotations
        assert "bit_depth" in annotations
        assert annotations["sample_rate"] == int
        assert annotations["bit_depth"] == int
