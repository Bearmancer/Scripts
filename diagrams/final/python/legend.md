# Architecture Legend - Final State

## Python Types
- `Path`: `pathlib.Path` object for filesystem paths.
- `str`: Standard string.
- `int`: Standard integer.
- `float`: Standard floating-point number.
- `bool`: Boolean value.
- `list[T]`: A list containing elements of type `T`.
- `dict[K, V]`: A dictionary with keys of type `K` and values of type `V`.
- `tuple[T1, T2]`: A fixed-size sequence of types `T1, T2`.
- `Optional[T]` / `T | None`: A value that can be of type `T` or `None`.
- `Any`: Any Python type.
- `AudioFormat`: A `Literal` type restricting values to specific audio formats.
- `AudioTier`: A `TypedDict` defining sample rate and bit depth.
- `VideoInfo`: A `TypedDict` defining duration, width, and height.
- `TrackInfo`: A `dataclass` representing a track from a CUE sheet.

## Decorators & Markers
- `<<dataclass>>`: Indicates a class decorated with `@dataclass`.
- `<<TypedDict>>`: Indicates a class inheriting from `TypedDict`.
- `<<Literal>>`: Indicates a type defined using `Literal`.
- `<<Exception>>`: Indicates a custom exception class.
- `<<Class>>`: Indicates a standard Python class.

## Relationship Types
- `..>` (Dashed Arrow): `import` dependency. Module A depends on Module B.
- `+` (Plus sign): Public function or method.
- `-` (Minus sign): Internal/private function or method (starts with `_`).

## Module Organization
The toolkit is refactored into a hierarchical package structure for better scalability and separation of concerns.
- `core/`: Centralized shared logic (exceptions, logging, types, utils).
- `audio/`: Audio processing and CUE sheet handling.
- `video/`: Video processing, split into `processor`, `extraction`, and `gif` for clarity.
- `filesystem/`: Filesystem operations.
- `pristine/`: Pristine Classical downloader, split into `downloader` (public API) and `browser` (internal automation).
- `lastfm/`: Last.fm synchronization logic.
- `cli.py`: The command-line interface, acting as the orchestrator for all packages.
