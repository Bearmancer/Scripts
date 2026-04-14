"""Python Toolkit - Audio processing and file management utilities."""

from toolkit.audio import (
    convert_audio,
    detect_audio_mode,
    prepare_directory,
    process_sacd_directory,
)
from toolkit.exceptions import (
    AudioError,
    ConversionError,
    FileOperationError,
    ToolkitError,
    UnsupportedFormatError,
)
from toolkit.filesystem import make_torrents
from toolkit.types import AudioFormat, AudioTier

__all__ = [
    "convert_audio",
    "detect_audio_mode",
    "prepare_directory",
    "process_sacd_directory",
    "make_torrents",
    "AudioFormat",
    "AudioTier",
    "ToolkitError",
    "AudioError",
    "ConversionError",
    "FileOperationError",
    "UnsupportedFormatError",
]
