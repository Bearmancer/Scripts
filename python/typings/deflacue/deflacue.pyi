"""Type stubs for deflacue."""

from __future__ import annotations

from pathlib import Path
from typing import Any


class CueParser:
    """CUE sheet parser."""

    image_path: Path

    def __init__(self, cue_file: str | Path, encoding: str | None = ...) -> None: ...

    @property
    def meta(self) -> Any: ...

    def get_data_tracks(self) -> list[Any]: ...
