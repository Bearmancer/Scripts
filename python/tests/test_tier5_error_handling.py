import subprocess
from pathlib import Path
from unittest.mock import MagicMock, Mock, patch

import pytest

from toolkit.audio import calculate_gain, convert_to_mp3, downsample_flac
from toolkit.exceptions import (
    AudioError,
    ConversionError,
    FileOperationError,
    ToolkitError,
    UnsupportedFormatError,
)
from toolkit.filesystem import make_torrents
from toolkit.types import AudioTier


def test_conversion_error_carries_file_path() -> None:
    """ConversionError stores file_path and command attributes."""
    file_path = Path("/test/audio.flac")
    command = ["ffmpeg", "-i", "input.wav"]
    error = ConversionError("Conversion failed", file_path, command)

    assert error.file_path == file_path
    assert error.command == command
    assert str(error) == "Conversion failed"


def test_conversion_error_inherits_audio_error() -> None:
    """ConversionError inherits from AudioError and ToolkitError."""
    file_path = Path("/test/audio.flac")
    error = ConversionError("Test error", file_path)

    assert isinstance(error, ConversionError)
    assert isinstance(error, AudioError)
    assert isinstance(error, ToolkitError)


def test_file_operation_error_carries_path() -> None:
    """FileOperationError stores file_path attribute."""
    file_path = Path("/test/torrent.file")
    error = FileOperationError("Operation failed", file_path)

    assert error.file_path == file_path
    assert str(error) == "Operation failed"


def test_unsupported_format_error_is_toolkit_error() -> None:
    """UnsupportedFormatError inherits correct exception hierarchy."""
    error = UnsupportedFormatError("Format not supported")

    assert isinstance(error, UnsupportedFormatError)
    assert isinstance(error, AudioError)
    assert isinstance(error, ToolkitError)


@patch("toolkit.audio.ffmpeg")
def test_calculate_gain_no_peaks_raises_conversion_error(mock_ffmpeg: Mock) -> None:
    """calculate_gain raises ConversionError when no peak levels detected."""
    dff_file = Path("/test/audio.dff")

    with patch.object(Path, "exists", return_value=True):
        mock_stream = MagicMock()
        mock_ffmpeg.input.return_value = mock_stream
        mock_stream.audio.filter.return_value = mock_stream
        mock_stream.output.return_value = mock_stream
        mock_stream.run.return_value = (None, b"no peak information here")

        with pytest.raises(ConversionError) as exc_info:
            calculate_gain(dff_file)

        assert exc_info.value.file_path == dff_file
        assert "Could not detect peak levels" in str(exc_info.value)


@patch("toolkit.audio.ffmpeg")
def test_calculate_gain_ffmpeg_error_raises_conversion_error(
    mock_ffmpeg: Mock,
) -> None:
    """calculate_gain raises ConversionError on ffmpeg.Error with chaining."""
    import ffmpeg

    dff_file = Path("/test/audio.dff")

    with patch.object(Path, "exists", return_value=True):
        mock_ffmpeg.Error = ffmpeg.Error

        mock_stream = MagicMock()
        mock_ffmpeg.input.return_value = mock_stream
        mock_stream.audio.filter.return_value = mock_stream
        mock_stream.output.return_value = mock_stream

        original_error = ffmpeg.Error("ffmpeg", b"", b"ffmpeg error occurred")
        mock_stream.run.side_effect = original_error

        with pytest.raises(ConversionError) as exc_info:
            calculate_gain(dff_file)

        assert exc_info.value.file_path == dff_file
        assert exc_info.value.__cause__ is original_error


@patch("toolkit.audio.run_command")
def test_downsample_sox_failure_raises_conversion_error(mock_run_command: Mock) -> None:
    """downsample_flac raises ConversionError on subprocess.CalledProcessError."""
    source = Path("/test/source.flac")
    dest = Path("/test/dest.flac")
    tier: AudioTier = {"sample_rate": 44100, "bit_depth": 16}

    original_error = subprocess.CalledProcessError(
        returncode=1,
        cmd=["sox", "-S", str(source)],
        stderr=b"sox: error processing",
    )
    mock_run_command.side_effect = original_error

    with pytest.raises(ConversionError) as exc_info:
        downsample_flac(source, dest, tier)

    assert exc_info.value.file_path == source
    assert exc_info.value.__cause__ is original_error
    assert "SoX conversion failed" in str(exc_info.value)


@patch("toolkit.audio.ffmpeg")
@patch("toolkit.audio.create_output_directory")
def test_convert_to_mp3_ffmpeg_failure_raises_conversion_error(
    mock_create_output: Mock, mock_ffmpeg: Mock
) -> None:
    """convert_to_mp3 raises ConversionError on ffmpeg.Error."""
    import ffmpeg

    test_dir = Path("/test/audio")
    flac_file = Path("/test/audio/track.flac")

    mock_create_output.return_value = Path("/test/audio/MP3")

    mock_ffmpeg.Error = ffmpeg.Error

    with patch.object(Path, "rglob", return_value=[flac_file]):
        mock_stream = MagicMock()
        mock_ffmpeg.input.return_value = mock_stream
        mock_stream.output.return_value = mock_stream

        original_error = ffmpeg.Error("ffmpeg", b"", b"encoding failed")
        mock_stream.run.side_effect = original_error

        with pytest.raises(ConversionError) as exc_info:
            convert_to_mp3(test_dir)

        assert exc_info.value.file_path == flac_file
        assert exc_info.value.__cause__ is original_error


@patch("toolkit.filesystem.create_torrent")
@patch("toolkit.filesystem.json")
@patch("toolkit.filesystem.Path")
def test_make_torrents_failure_raises_file_operation_error(
    mock_path_cls: Mock, mock_json: Mock, mock_create_torrent: Mock
) -> None:
    """make_torrents raises FileOperationError on torrent creation failure."""
    folder = Path("/test/album")

    mock_dropbox_info = MagicMock()
    mock_dropbox_info.exists.return_value = True
    mock_dropbox_info.read_text.return_value = '{"personal": {"path": "/home/dropbox"}}'

    mock_path_cls.home.return_value = Path("/home")
    mock_path_cls.return_value = folder

    def path_side_effect(arg: str) -> Path | MagicMock:
        if "Dropbox" in str(arg) and "info.json" in str(arg):
            return mock_dropbox_info
        return Path(arg)

    with patch("toolkit.filesystem.Path", side_effect=path_side_effect):
        with (
            patch("toolkit.filesystem.Path.home") as mock_home,
            patch("toolkit.filesystem.rename_file_red") as mock_rename,
        ):
            mock_home.return_value = Path("/home")

            dropbox_info_path = Path("/home/AppData/Local/Dropbox/info.json")
            with (
                patch.object(
                    Path, "exists", return_value=True
                ) as mock_exists,
                patch.object(
                    Path,
                    "read_text",
                    return_value='{"personal": {"path": "/home/dropbox"}}',
                ),
            ):
                original_error = OSError("Torrent creation failed")
                mock_create_torrent.side_effect = original_error

                with pytest.raises(FileOperationError) as exc_info:
                    make_torrents(folder)

                assert exc_info.value.file_path == folder
                assert exc_info.value.__cause__ is original_error
