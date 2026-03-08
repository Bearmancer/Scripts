"""Type stubs for ffmpeg-python."""

from __future__ import annotations

from pathlib import Path
from typing import Any


class Stream:
    """ffmpeg stream object."""

    audio: Stream
    video: Stream

    def filter(self, filter_name: str, **kwargs: Any) -> Stream: ...
    def output(self, *args: Any, **kwargs: Any) -> Stream: ...
    def overwrite_output(self) -> Stream: ...
    def run(self, capture_stdout: bool = ..., capture_stderr: bool = ..., quiet: bool = ...) -> tuple[bytes, bytes]: ...
    def run_async(self, pipe_stdout: bool = ..., pipe_stderr: bool = ..., quiet: bool = ...) -> Any: ...


class Error(Exception):
    """ffmpeg error."""

    stdout: bytes | None
    stderr: bytes | None

    def __init__(self, cmd: str, stdout: bytes | None = ..., stderr: bytes | None = ...) -> None: ...


def input(filename: str | Path, **kwargs: Any) -> Stream: ...
def output(*streams: Stream, filename: str | Path, **kwargs: Any) -> Stream: ...
def probe(filename: str | Path, cmd: str = ..., **kwargs: Any) -> dict[str, Any]: ...
