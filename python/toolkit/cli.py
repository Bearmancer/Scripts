#!/usr/bin/env python3
"""Personal toolkit for audio, video, and file operations."""

from __future__ import annotations

from cyclopts import App, Parameter
from pathlib import Path
from typing import Annotated

from toolkit.logging_config import get_logger
from toolkit.types import AudioFormat

logger = get_logger()

app = App(
    name="toolkit",
    help="Personal toolkit for audio, video, and file operations.",
)
audio_app = App(name="audio", help="Audio conversion and processing tools")
video_app = App(name="video", help="Video processing and extraction tools")
fs_app = App(name="filesystem", help="Filesystem operations and torrent creation")
pristine_app = App(name="pristine", help="Pristine Classical streaming downloader")


@audio_app.command(name="convert")
def audio_convert(
    directory: Annotated[Path | None, Parameter(name=["d", "directory"])] = None,
    mode: Annotated[str | None, Parameter(name=["m", "mode"])] = None,
    format: Annotated[AudioFormat, Parameter(name=["f", "format"])] = "16bit",
) -> None:
    """Convert audio files to various formats or extract SACD ISOs.

    Auto-detects mode (convert vs extract) based on directory contents if not specified.
    Processes all audio files in the target directory.

    Args:
            directory: Target directory to process. Defaults to current directory.
            mode: Processing mode: 'convert' or 'extract' (auto-detected if omitted).
            format: Output format: '16bit', 'cd', 'all', '24-bit', 'mp3'. Defaults to '16bit'.
    """
    from toolkit.audio import (
        convert_audio,
        detect_audio_mode,
        prepare_directory,
        process_sacd_directory,
    )

    resolved = (directory or Path(".")).resolve()
    if not resolved.exists():
        logger.error(f"Directory not found: {resolved}")
        raise SystemExit(1)

    prepared = prepare_directory(resolved)
    effective_mode = mode or detect_audio_mode(prepared)

    match effective_mode:
        case "convert":
            convert_audio(prepared, format)
        case "extract":
            process_sacd_directory(prepared, format)
        case _:
            logger.error(f"Unknown mode: {effective_mode}")
            raise SystemExit(1)

    logger.info("Processing completed")


@audio_app.command(name="rename")
def audio_rename(
    directory: Annotated[Path | None, Parameter(name=["d", "directory"])] = None,
) -> None:
    """Rename files with excessively long paths to shorter versions.

    Recursively processes the directory to shorten file paths that exceed Windows path limits
    or are otherwise problematic. Defaults to current directory.

    Args:
            directory: Target directory to process. Defaults to current directory.
    """
    from toolkit.filesystem import rename_file_red

    rename_file_red((directory or Path(".")).resolve())


@audio_app.command(name="art-report")
def audio_art_report(
    directory: Annotated[Path | None, Parameter(name=["d", "directory"])] = None,
) -> None:
    """Report embedded artwork sizes in FLAC files.

    Scans all FLAC files in the directory and reports the dimensions and file size
    of embedded cover art. Useful for identifying oversized artwork. Defaults to current directory.

    Args:
            directory: Target directory containing FLAC files. Defaults to current directory.
    """
    from toolkit.audio import calculate_image_size

    calculate_image_size((directory or Path(".")).resolve())


@video_app.command(name="remux")
def video_remux(
    path: Annotated[Path | None, Parameter(name=["p", "path"])] = None,
    skip_mediainfo: bool = False,
) -> None:
    """Remux DVD/Blu-ray discs to MKV format.

    Processes disc structures and creates lossless MKV files. Generates MediaInfo reports
    for each output file unless disabled.

    Args:
            path: Source directory containing disc structure. Defaults to current directory.
            skip_mediainfo: If True, skips generating MediaInfo reports after remuxing.
    """
    from toolkit.video import remux_disc

    remux_disc((path or Path(".")).resolve(), not skip_mediainfo)


@video_app.command(name="compress")
def video_compress(
    directory: Annotated[Path | None, Parameter(name=["d", "directory"])] = None,
) -> None:
    """Batch compress MKV files using HandBrake.

    Processes all MKV files in the directory using HandBrake for size reduction while maintaining quality.

    Args:
            directory: Target directory containing MKV files. Defaults to current directory.
    """
    from toolkit.video import batch_compression

    batch_compression((directory or Path(".")).resolve())


@video_app.command(name="chapters")
def video_chapters(
    path: Annotated[Path | None, Parameter(name=["p", "path"])] = None,
) -> None:
    """Extract chapters from video files.

    Accepts either a file or directory; recursively processes all video files. Outputs chapter information for each file.

    Args:
            path: Video file path or directory to process. Defaults to current directory.
    """
    from toolkit.video import VIDEO_EXTENSIONS, extract_chapters

    resolved = (path or Path(".")).resolve()
    video_files = (
        [resolved]
        if resolved.is_file()
        else [f for f in resolved.rglob("*") if f.suffix.lower() in VIDEO_EXTENSIONS]
    )
    extract_chapters(video_files)


@video_app.command(name="resolutions")
def video_resolutions(
    path: Annotated[Path | None, Parameter(name=["p", "path"])] = None,
) -> None:
    """Print resolution information for video files.

    Accepts either a file or directory; recursively processes all video files. Displays resolution details for each file.

    Args:
            path: Video file path or directory to process. Defaults to current directory.
    """
    from toolkit.video import VIDEO_EXTENSIONS, print_video_resolution

    resolved = (path or Path(".")).resolve()
    video_files = (
        [resolved]
        if resolved.is_file()
        else [f for f in resolved.rglob("*") if f.suffix.lower() in VIDEO_EXTENSIONS]
    )
    print_video_resolution(video_files)


@video_app.command(name="gif")
def video_gif(
    input: Annotated[Path, Parameter(name=["i", "input"])],
    start: Annotated[str, Parameter(name=["s", "start"])] = "00:00",
    duration: Annotated[int, Parameter(name=["d", "duration"])] = 10,
    max_size: Annotated[int, Parameter(name=["m", "max-size"])] = 300,
    output: Annotated[Path | None, Parameter(name=["o", "output"])] = None,
) -> None:
    """Create optimized GIF from video file.

    Automatically adjusts quality parameters to meet the specified size limit using two-pass palette generation.

    Args:
            input: Source video file path.
            start: Starting timestamp in mm:ss format. Defaults to '00:00'.
            duration: Duration in seconds. Defaults to 10.
            max_size: Maximum output file size in MiB. Defaults to 300.
            output: Output directory. Defaults to ~/Desktop.
    """
    from toolkit.video import create_gif_optimized

    create_gif_optimized(
        input.resolve(),
        start,
        duration,
        max_size,
        (output or Path.home() / "Desktop").resolve(),
    )


@video_app.command(name="thumbnails")
def video_thumbnails(
    path: Annotated[Path, Parameter(name=["p", "path"])],
) -> None:
    """Extract thumbnail grid and full-size images from video.

    Generates a grid of thumbnails at regular intervals, plus individual full-resolution frames.

    Args:
            path: Source video file to process.
    """
    from toolkit.video import extract_images

    extract_images(path.resolve())


@fs_app.command(name="tree")
def filesystem_tree(
    directory: Annotated[Path | None, Parameter(name=["d", "directory"])] = None,
    sort: Annotated[str, Parameter(name=["s", "sort"])] = "size",
    include_files: Annotated[bool, Parameter(name=["f", "include-files"])] = False,
) -> None:
    """List directory tree with sizes.

    Args:
            directory: Target directory to analyze. Defaults to current directory.
            sort: Sort order, either 'size' (largest first) or 'name' (alphabetical). Defaults to 'size'.
            include_files: If True, lists both files and directories. If False, directories only.
    """
    from toolkit.filesystem import list_directories, list_files_and_directories

    resolved = (directory or Path(".")).resolve()
    sort_by_name = sort == "name"

    if include_files:
        list_files_and_directories(resolved, sort_by_name)
    else:
        list_directories(resolved, "1" if sort_by_name else "0")


@fs_app.command(name="torrents")
def filesystem_torrents(
    directory: Annotated[Path | None, Parameter(name=["d", "directory"])] = None,
    include_subdirectories: bool = False,
) -> None:
    """Create RED and OPS torrents for directory.

    Generates .torrent files for both Redacted (RED) and Orpheus (OPS) trackers.

    Args:
            directory: Target directory containing release content. Defaults to current directory.
            include_subdirectories: If True, creates separate torrents for each subdirectory; otherwise one torrent for the directory.
    """
    from toolkit.filesystem import make_torrents

    resolved = (directory or Path(".")).resolve()

    if include_subdirectories:
        for entry in (e for e in resolved.iterdir() if e.is_dir()):
            make_torrents(entry)
    else:
        make_torrents(resolved)


app.command(audio_app)
app.command(video_app)
app.command(fs_app)
app.command(pristine_app)


@pristine_app.command(name="download")
def pristine_download(
    codes: Annotated[list[str] | None, Parameter(name=["c", "code"])] = None,
    out_dir: Annotated[Path | None, Parameter(name=["o", "out-dir"])] = None,
    headless: Annotated[bool, Parameter(name=["H", "headless"])] = False,
) -> None:
    """Download albums from Pristine Classical.

    Downloads the specified release codes, or every configured release if no
    codes are given.  Codes are Pristine catalogue numbers, e.g. PASC552.

    Use --headless (or PRISTINE_HEADLESS=1) once your session is saved.

    Examples::

            toolkit pristine download PASC552 PASC553
            toolkit pristine download --headless
            toolkit pristine download --out-dir /data/Media/Pristine

    Args:
            codes: Release codes to download. Omit for all configured releases.
            out_dir: Output directory. Defaults to PRISTINE_OUT_DIR env var.
            headless: Run headlessly. Overrides PRISTINE_HEADLESS.
    """
    import os

    if headless:
        os.environ["PRISTINE_HEADLESS"] = "1"

    from toolkit.pristine import download_codes

    download_codes(codes, out_dir=out_dir)


@pristine_app.command(name="login")
def pristine_login() -> None:
    """Save a Pristine Classical login session for automated downloads.

    Opens a visible browser window. Log in with your Pristine account — the
    session is saved automatically once login is detected. Run this once; re-run
    only when the session expires.

    Examples::

            toolkit pristine login
    """
    from toolkit.pristine import login

    login()


def main() -> None:
    """Main entry point for the toolkit CLI."""
    app()


if __name__ == "__main__":
    main()
