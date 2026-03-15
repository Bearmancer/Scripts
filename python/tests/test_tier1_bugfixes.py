"""Regression tests for Tier 1 bug fixes in the Python toolkit.

This test suite validates that 5 critical bug fixes remain fixed:
- T1.1: CUE parameters are parameterized (not hardcoded)
- T1.2: No duplicate rename_file_red() function
- T1.3: downsample_flac() has no temp file collision risk
- T1.4: sanitize_name() returns correct path
- T1.5: get_flac_tiers() handles all AudioFormat values
"""

import inspect
from pathlib import Path

import pytest

from toolkit import audio, cuesheet, filesystem
from toolkit.types import AudioFormat, AudioTier


class TestT11CueParametersParameterized:
    """T1.1: Verify CUE sheet processing functions accept parameterized audio settings."""

    def test_process_tracks_has_sample_fmt_parameter(self) -> None:
        """Verify process_tracks accepts sample_fmt parameter instead of hardcoding it."""
        sig = inspect.signature(cuesheet.process_tracks)
        params = sig.parameters

        assert "sample_fmt" in params, "process_tracks missing sample_fmt parameter"
        assert params["sample_fmt"].default == "s32", "sample_fmt default should be 's32'"

    def test_process_tracks_has_sample_rate_parameter(self) -> None:
        """Verify process_tracks accepts sample_rate parameter instead of hardcoding it."""
        sig = inspect.signature(cuesheet.process_tracks)
        params = sig.parameters

        assert "sample_rate" in params, "process_tracks missing sample_rate parameter"
        assert params["sample_rate"].default == 88200, "sample_rate default should be 88200"

    def test_process_cue_file_has_sample_fmt_parameter(self) -> None:
        """Verify process_cue_file accepts sample_fmt parameter."""
        sig = inspect.signature(cuesheet.process_cue_file)
        params = sig.parameters

        assert "sample_fmt" in params, "process_cue_file missing sample_fmt parameter"
        assert params["sample_fmt"].default == "s32", "sample_fmt default should be 's32'"

    def test_process_cue_file_has_sample_rate_parameter(self) -> None:
        """Verify process_cue_file accepts sample_rate parameter."""
        sig = inspect.signature(cuesheet.process_cue_file)
        params = sig.parameters

        assert "sample_rate" in params, "process_cue_file missing sample_rate parameter"
        assert params["sample_rate"].default == 88200, "sample_rate default should be 88200"


class TestT12NoRenamFileDuplicate:
    """T1.2: Verify rename_file_red exists only in filesystem, not in audio."""

    def test_rename_file_red_not_in_audio_module(self) -> None:
        """Verify rename_file_red is NOT exported from toolkit.audio."""
        assert not hasattr(audio, "rename_file_red"), (
            "rename_file_red should not exist in audio module"
        )

    def test_rename_file_red_exists_in_filesystem_module(self) -> None:
        """Verify rename_file_red IS exported from toolkit.filesystem."""
        assert hasattr(filesystem, "rename_file_red"), (
            "rename_file_red should exist in filesystem module"
        )
        assert callable(filesystem.rename_file_red), (
            "rename_file_red should be callable"
        )


class TestT13DownsampleFlacNoTempCollision:
    """T1.3: Verify downsample_flac uses explicit source/dest paths, no temp file collisions."""

    def test_downsample_flac_signature_has_source_and_dest(self) -> None:
        """Verify downsample_flac takes source and dest Path arguments."""
        sig = inspect.signature(audio.downsample_flac)
        params = sig.parameters

        assert "source" in params, "downsample_flac missing source parameter"
        assert "dest" in params, "downsample_flac missing dest parameter"
        assert "tier" in params, "downsample_flac missing tier parameter"

        param_names = list(params.keys())
        assert param_names[:3] == ["source", "dest", "tier"], (
            f"Expected parameters [source, dest, tier] but got {param_names[:3]}"
        )

    def test_downsample_flac_does_not_use_hardcoded_temp_files(self) -> None:
        """Verify downsample_flac source code does not contain hardcoded temp file names."""
        source_code = inspect.getsource(audio.downsample_flac)

        assert '"a.flac"' not in source_code, "downsample_flac contains hardcoded 'a.flac'"
        assert "'a.flac'" not in source_code, "downsample_flac contains hardcoded 'a.flac'"
        assert '"b.flac"' not in source_code, "downsample_flac contains hardcoded 'b.flac'"
        assert "'b.flac'" not in source_code, "downsample_flac contains hardcoded 'b.flac'"


class TestT14SanitizeNameReturnsCorrectPath:
    """T1.4: Verify sanitize_name (via prepare_directory) returns correct path."""

    def test_prepare_directory_sanitizes_unicode_filenames(self, tmp_path: Path) -> None:
        """Verify prepare_directory sanitizes unicode filenames and returns the directory."""
        test_dir = tmp_path / "test_album"
        test_dir.mkdir()

        unicode_file = test_dir / "Café_♫_test.txt"
        unicode_file.write_text("test content")

        result = audio.prepare_directory(test_dir)

        assert result == test_dir, "prepare_directory should return the directory path"

        files = list(test_dir.glob("*.txt"))
        assert len(files) == 1, "Should have exactly one file"
        assert files[0].name != "Café_♫_test.txt", "File should be sanitized"
        assert "Cafe" in files[0].name or "Caf" in files[0].name, (
            "Sanitized name should contain ASCII approximation"
        )

    def test_prepare_directory_leaves_clean_filenames_unchanged(self, tmp_path: Path) -> None:
        """Verify prepare_directory leaves already-clean filenames untouched."""
        test_dir = tmp_path / "test_album"
        test_dir.mkdir()

        clean_file = test_dir / "track01.flac"
        clean_file.write_text("test content")
        original_mtime = clean_file.stat().st_mtime

        result = audio.prepare_directory(test_dir)

        assert result == test_dir, "prepare_directory should return the directory path"

        assert clean_file.exists(), "Clean filename should remain unchanged"
        files = list(test_dir.glob("*.flac"))
        assert len(files) == 1, "Should have exactly one file"
        assert files[0].name == "track01.flac", "Clean filename should not be renamed"

        assert clean_file.stat().st_mtime == original_mtime, (
            "File should not be modified if name is already clean"
        )

    def test_prepare_directory_normalizes_disc_folders(self, tmp_path: Path) -> None:
        """Verify prepare_directory normalizes disc folder names."""
        test_dir = tmp_path / "test_album"
        test_dir.mkdir()

        disc1 = test_dir / "Disc1"
        disc2 = test_dir / "CD 2"
        disc1.mkdir()
        disc2.mkdir()

        result = audio.prepare_directory(test_dir)

        assert result == test_dir, "prepare_directory should return the directory path"

        folders = sorted([f.name for f in test_dir.iterdir() if f.is_dir()])
        assert "Disc 01" in folders, "Disc1 should be normalized to 'Disc 01'"
        assert "Disc 02" in folders, "CD 2 should be normalized to 'Disc 02'"


class TestT15GetFlacTiersHandlesAllFormats:
    """T1.5: Verify get_flac_tiers handles all AudioFormat literal values."""

    @pytest.mark.parametrize(
        ("sample_rate", "bit_depth", "fmt", "expected"),
        [
            (192000, 24, "16bit", [{"sample_rate": 48000, "bit_depth": 16}]),
            (176400, 24, "16bit", [{"sample_rate": 44100, "bit_depth": 16}]),

            (192000, 24, "cd", []),
            (176400, 24, "cd", [{"sample_rate": 44100, "bit_depth": 16}]),
            (88200, 16, "cd", [{"sample_rate": 44100, "bit_depth": 16}]),

            (192000, 24, "all", [{"sample_rate": 96000, "bit_depth": 24}, {"sample_rate": 48000, "bit_depth": 16}]),
            (176400, 24, "all", [{"sample_rate": 88200, "bit_depth": 24}, {"sample_rate": 44100, "bit_depth": 16}]),

            (192000, 24, "24-bit", [{"sample_rate": 96000, "bit_depth": 24}]),
            (176400, 24, "24-bit", [{"sample_rate": 88200, "bit_depth": 24}]),

            (192000, 24, "mp3", []),
            (176400, 24, "mp3", []),
        ],
    )
    def test_get_flac_tiers_handles_all_audio_formats(
        self,
        sample_rate: int,
        bit_depth: int,
        fmt: AudioFormat,
        expected: list[AudioTier],
    ) -> None:
        """Verify get_flac_tiers returns correct tiers for all AudioFormat values."""
        result = audio.get_flac_tiers(sample_rate, bit_depth, fmt)
        assert result == expected, (
            f"get_flac_tiers({sample_rate}, {bit_depth}, '{fmt}') "
            f"returned {result}, expected {expected}"
        )

    def test_get_flac_tiers_returns_empty_when_source_is_lowest_tier(self) -> None:
        """Verify get_flac_tiers returns [] when source is already at lowest tier (T2.2 behavior)."""
        assert audio.get_flac_tiers(44100, 16, "all") == []
        assert audio.get_flac_tiers(48000, 16, "all") == []

    def test_get_flac_tiers_16bit_returns_single_tier(self) -> None:
        """Verify '16bit' format returns exactly one 16-bit tier, not multiple."""
        result = audio.get_flac_tiers(192000, 24, "16bit")

        assert len(result) == 1, f"'16bit' format should return exactly 1 tier, got {len(result)}"
        assert result[0]["bit_depth"] == 16, "Returned tier should be 16-bit"

    def test_get_flac_tiers_cd_returns_44100_16_or_empty(self) -> None:
        """Verify 'cd' format returns [(44100, 16)] or empty list."""
        result_44 = audio.get_flac_tiers(176400, 24, "cd")
        expected = [{"sample_rate": 44100, "bit_depth": 16}]
        assert result_44 == expected, "CD format should return [(44100, 16)] for 44.1k family"

        result_48 = audio.get_flac_tiers(192000, 24, "cd")
        assert result_48 == [], (
            "CD format returns empty list for 48k family (44100 not in FLAC_48)"
        )

    def test_get_flac_tiers_24bit_excludes_16bit_tiers(self) -> None:
        """Verify '24-bit' format excludes all 16-bit tiers."""
        result = audio.get_flac_tiers(192000, 24, "24-bit")

        assert all(tier["bit_depth"] == 24 for tier in result), (
            "'24-bit' format should only return 24-bit tiers"
        )
        assert len(result) >= 1, "'24-bit' format should return at least one tier"

    def test_get_flac_tiers_all_returns_multiple_tiers(self) -> None:
        """Verify 'all' format returns multiple tiers when source allows."""
        result = audio.get_flac_tiers(192000, 24, "all")

        assert len(result) > 1, "'all' format should return multiple tiers for 192/24 source"
        bit_depths = [tier["bit_depth"] for tier in result]
        assert 24 in bit_depths, "Should include 24-bit tiers"
        assert 16 in bit_depths, "Should include 16-bit tiers"

    def test_get_flac_tiers_mp3_returns_empty_for_high_quality_sources(self) -> None:
        """Verify 'mp3' format returns empty list for sources that can be downsampled."""
        test_cases = [
            (192000, 24),
            (176400, 24),
            (96000, 24),
            (88200, 24),
        ]

        for sample_rate, bit_depth in test_cases:
            result = audio.get_flac_tiers(sample_rate, bit_depth, "mp3")
            assert result == [], (
                f"'mp3' format should return empty list for {sample_rate}/{bit_depth}, got {result}"
            )

    def test_get_flac_tiers_mp3_returns_empty_for_lowest_tier_sources(self) -> None:
        """Verify 'mp3' format returns [] for lowest-tier sources (T2.2 behavior)."""
        assert audio.get_flac_tiers(48000, 16, "mp3") == []
        assert audio.get_flac_tiers(44100, 16, "mp3") == []
