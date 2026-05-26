from pathlib import Path


class ToolkitError(Exception):
	"""Base exception for all toolkit errors."""


class AudioError(ToolkitError):
	"""Base exception for audio processing."""


class ConversionError(AudioError):
	"""Audio conversion failed."""

	def __init__(
		self, message: str, file_path: Path, command: list[str] | None = None
	) -> None:
		super().__init__(message)
		self.file_path = file_path
		self.command = command


class UnsupportedFormatError(AudioError):
	"""Audio format not supported or invalid."""


class FileOperationError(ToolkitError):
	"""File operation failed."""

	def __init__(self, message: str, file_path: Path | None = None) -> None:
		super().__init__(message)
		self.file_path = file_path


class ConfigurationError(ToolkitError):
	"""Invalid configuration or missing requirements."""
