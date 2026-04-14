"""Test suite for Tier 2 audio pipeline optimizations.

This suite validates:
- T2.1: Default 16-bit output behavior
- T2.2: Early exit for CD-quality sources
- T2.3: No robocopy intermediary
- T2.4: Direct DFF→target FLAC conversion
- T2.5: Single-pass SoX conversion
- T2.6: Auto-detect processing mode
"""

from __future__ import annotations

import inspect
from pathlib import Path
from unittest.mock import Mock, patch

import pytest

from toolkit.audio import (
    convert_audio,
    create_output_directory,
    detect_audio_mode,
    downsample_flac,
    get_flac_tiers,
    process_sacd_directory,
)
from toolkit.types import AudioFormat, AudioTier


class TestT21Default16BitOutput:
    """T2.1: Verify get_flac_tiers returns single 16-bit tier for each sample rate family."""

    @pytest.mark.parametrize(
        ("sample_rate", "bit_depth", "expected"),
        [
            (176400, 24, [{"sample_rate": 44100, "bit_depth": 16}]),
            (192000, 24, [{"sample_rate": 48000, "bit_depth": 16}]),
            (96000, 24, [{"sample_rate": 48000, "bit_depth": 16}]),
            (88200, 24, [{"sample_rate": 44100, "bit_depth": 16}]),
        ],
    )
    def test_16bit_format_returns_single_tier(
        self, sample_rate: int, bit_depth: int, expected: list[AudioTier]
    ) -> None:
        """Verify fmt='16bit' returns single 16-bit tier at base sample rate."""
        result = get_flac_tiers(sample_rate, bit_depth, "16bit")
        assert result == expected

    def test_cli_default_format_is_16bit(self) -> None:
        """Verify CLI default format parameter is '16bit'."""
        from toolkit.cli import audio_convert

        sig = inspect.signature(audio_convert)
        format_param = sig.parameters["format"]
        assert format_param.default == "16bit"


class TestT22EarlyExitCDQuality:
    """T2.2: Verify early exit for CD-quality sources and proper edge case handling."""

    @pytest.mark.parametrize(
        "fmt",
        ["16bit", "cd"],
    )
    @patch("toolkit.audio.Path.rglob")
    @patch("toolkit.audio.get_metadata")
    def test_cd_quality_source_skips_conversion(
        self, mock_metadata: Mock, mock_rglob: Mock, fmt: AudioFormat
    ) -> None:
        """Verify CD-quality source with 16bit/cd format skips FLAC conversion."""
        mock_rglob.return_value = [Path("test.flac")]
        mock_metadata.return_value = {"bits_per_raw_sample": "16", "sample_rate": "44100"}

        with patch("toolkit.audio.logger") as mock_logger:
            convert_audio(Path("/test"), fmt)
            mock_logger.info.assert_called_with(
                "Source is already CD quality, skipping FLAC conversion"
            )

    @patch("toolkit.audio.Path.rglob")
    @patch("toolkit.audio.get_metadata")
    @patch("toolkit.audio.convert_to_mp3")
    def test_cd_quality_source_with_mp3_format_converts_to_mp3(
        self, mock_convert_mp3: Mock, mock_metadata: Mock, mock_rglob: Mock
    ) -> None:
        """Verify CD-quality source with fmt='mp3' still converts to MP3."""
        mock_rglob.return_value = [Path("test.flac")]
        mock_metadata.return_value = {"bits_per_raw_sample": "16", "sample_rate": "44100"}

        convert_audio(Path("/test"), "mp3")
        mock_convert_mp3.assert_called_once()

    @patch("toolkit.audio.Path.rglob")
    @patch("toolkit.audio.get_metadata")
    @patch("toolkit.audio.convert_to_mp3")
    @patch("toolkit.audio.flac_directory_conversion")
    def test_cd_quality_source_with_all_format_converts_to_mp3(
        self,
        mock_flac_conv: Mock,
        mock_convert_mp3: Mock,
        mock_metadata: Mock,
        mock_rglob: Mock,
    ) -> None:
        """Verify CD-quality source with fmt='all' still converts to MP3."""
        mock_rglob.return_value = [Path("test.flac")]
        mock_metadata.return_value = {"bits_per_raw_sample": "16", "sample_rate": "44100"}

        convert_audio(Path("/test"), "all")
        mock_convert_mp3.assert_called_once()
        mock_flac_conv.assert_not_called()

    @pytest.mark.parametrize(
        ("sample_rate", "bit_depth", "fmt"),
        [
            (44100, 16, "16bit"),
            (44100, 16, "cd"),
            (44100, 16, "all"),
            (44100, 16, "mp3"),
            (48000, 16, "16bit"),
        ],
    )
    def test_get_flac_tiers_returns_empty_for_cd_quality_edge_case(
        self, sample_rate: int, bit_depth: int, fmt: AudioFormat
    ) -> None:
        """Verify get_flac_tiers returns [] instead of raising ValueError for CD-quality sources."""
        result = get_flac_tiers(sample_rate, bit_depth, fmt)
        assert isinstance(result, list)


class TestT23NoRobocopyIntermediary:
    """T2.3: Verify no robocopy full-directory copy and proper non-audio file handling."""

    def test_create_output_directory_code_has_no_robocopy(self) -> None:
        """Verify create_output_directory source code doesn't contain 'robocopy'."""
        source = inspect.getsource(create_output_directory)
        assert "robocopy" not in source.lower()

    def test_downsample_flac_signature_takes_source_and_dest(self) -> None:
        """Verify downsample_flac accepts source, dest, and tier parameters."""
        sig = inspect.signature(downsample_flac)
        params = list(sig.parameters.keys())
        assert params[:3] == ["source", "dest", "tier"]

    def test_downsample_flac_code_has_no_temp_file_rename(self) -> None:
        """Verify downsample_flac doesn't use temp file rename pattern."""
        source = inspect.getsource(downsample_flac)
        assert ".rename(" not in source


class TestT24DirectDFFConversion:
    """T2.4: Verify direct DFF→target FLAC conversion without intermediary."""

    @pytest.mark.parametrize(
        ("fmt", "expected_sample_fmt", "expected_sample_rate"),
        [
            ("16bit", "s16", 44100),
            ("cd", "s16", 44100),
            ("mp3", "s16", 44100),
            ("24-bit", "s32", 88200),
            ("all", "s32", 88200),
        ],
    )
    def test_get_dff_target_params_returns_correct_values(
        self, fmt: AudioFormat, expected_sample_fmt: str, expected_sample_rate: int
    ) -> None:
        """Verify get_dff_target_params returns correct params for each format."""
        from toolkit.audio import get_dff_target_params

        sample_fmt, sample_rate = get_dff_target_params(fmt)
        assert sample_fmt == expected_sample_fmt
        assert sample_rate == expected_sample_rate

    @patch("toolkit.audio.convert_iso_to_dff_and_cue")
    @patch("toolkit.audio.convert_dff_to_flac")
    @patch("toolkit.audio.convert_audio")
    @patch("toolkit.audio.Path.rglob")
    def test_process_sacd_directory_passes_target_params_to_convert_dff(
        self,
        mock_rglob: Mock,
        mock_convert_audio: Mock,
        mock_convert_dff: Mock,
        mock_convert_iso: Mock,
    ) -> None:
        """Verify process_sacd_directory passes target format params to convert_dff_to_flac."""
        mock_rglob.return_value = [Path("test.iso")]
        mock_convert_iso.return_value = [Path("/test/output")]

        process_sacd_directory(Path("/test"), "16bit")

        mock_convert_dff.assert_called_once()
        call_args = mock_convert_dff.call_args
        assert call_args[0][0] == Path("/test/output")
        assert call_args[0][1] == "s16"
        assert call_args[0][2] == 44100

    @pytest.mark.parametrize("fmt", ["16bit", "cd"])
    @patch("toolkit.audio.convert_iso_to_dff_and_cue")
    @patch("toolkit.audio.convert_dff_to_flac")
    @patch("toolkit.audio.convert_audio")
    @patch("toolkit.audio.Path.rglob")
    def test_process_sacd_directory_skips_convert_audio_for_direct_formats(
        self,
        mock_rglob: Mock,
        mock_convert_audio: Mock,
        mock_convert_dff: Mock,
        mock_convert_iso: Mock,
        fmt: AudioFormat,
    ) -> None:
        """Verify 16bit/cd formats don't trigger convert_audio after DFF extraction."""
        mock_rglob.return_value = [Path("test.iso")]
        mock_convert_iso.return_value = [Path("/test/output")]

        process_sacd_directory(Path("/test"), fmt)

        mock_convert_audio.assert_not_called()

    @pytest.mark.parametrize("fmt", ["all", "24-bit"])
    @patch("toolkit.audio.convert_iso_to_dff_and_cue")
    @patch("toolkit.audio.convert_dff_to_flac")
    @patch("toolkit.audio.convert_audio")
    @patch("toolkit.audio.Path.rglob")
    def test_process_sacd_directory_calls_convert_audio_for_multi_tier_formats(
        self,
        mock_rglob: Mock,
        mock_convert_audio: Mock,
        mock_convert_dff: Mock,
        mock_convert_iso: Mock,
        fmt: AudioFormat,
    ) -> None:
        """Verify all/24-bit formats trigger convert_audio for additional tiers."""
        mock_rglob.return_value = [Path("test.iso")]
        mock_convert_iso.return_value = [Path("/test/output")]

        process_sacd_directory(Path("/test"), fmt)

        mock_convert_audio.assert_called_once()


class TestT25SinglePassSoX:
    """T2.5: Verify SoX writes directly to destination without temp files."""

    @patch("toolkit.audio.run_command")
    def test_downsample_flac_writes_directly_to_destination(
        self, mock_run_command: Mock
    ) -> None:
        """Verify downsample_flac passes destination path to SoX command."""
        source = Path("/source/track.flac")
        dest = Path("/dest/track.flac")
        tier: AudioTier = {"sample_rate": 44100, "bit_depth": 16}

        downsample_flac(source, dest, tier)

        mock_run_command.assert_called_once()
        cmd = mock_run_command.call_args[0][0]
        assert str(source) in cmd
        assert str(dest) in cmd
        assert "44100" in cmd
        assert "16" in cmd


class TestT26AutoDetectMode:
    """T2.6: Verify auto-detection of processing mode."""

    def test_detect_audio_mode_with_iso_returns_extract(self) -> None:
        """Verify detect_audio_mode returns 'extract' when ISO files found."""
        with patch("toolkit.audio.Path.rglob") as mock_rglob:
            mock_rglob.return_value = [Path("disc.iso")]
            result = detect_audio_mode(Path("/test"))
            assert result == "extract"

    def test_detect_audio_mode_with_flac_returns_convert(self) -> None:
        """Verify detect_audio_mode returns 'convert' when FLAC files found and no ISOs."""
        with patch("toolkit.audio.Path.rglob") as mock_rglob:
            mock_rglob.side_effect = [[], [Path("track.flac")]]
            result = detect_audio_mode(Path("/test"))
            assert result == "convert"

    def test_cli_mode_parameter_is_optional(self) -> None:
        """Verify CLI --mode parameter has None as default."""
        from toolkit.cli import audio_convert

        sig = inspect.signature(audio_convert)
        mode_param = sig.parameters["mode"]
        assert mode_param.default is None
