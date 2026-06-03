"""Shared type definitions for toolkit modules."""

from typing import Literal, TypedDict


class AudioTier(TypedDict):
	"""Audio quality tier specification."""

	sample_rate: int
	bit_depth: int


AudioFormat = Literal["16bit", "cd", "all", "24-bit", "mp3"]
